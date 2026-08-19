using System;
using System.Linq;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

using IRI.Maptor.Sta.Ogc;
using IRI.Maptor.Sta.Ogc.SLD;
using IRI.Maptor.Jab.Core;
using IRI.Maptor.Jab.Core.Localization;
using IRI.Maptor.Jab.Wpf.Cartography.Legend;


namespace IRI.Maptor.Jab.Wpf.ViewModels.Symbology;

public class RuleViewModel : Notifier
{
    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            RaisePropertyChanged();
        }
    }

    private string _title;
    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            RaisePropertyChanged();
        }
    }

    private string _abstract;
    public string Abstract
    {
        get => _abstract;
        set
        {
            _abstract = value;
            RaisePropertyChanged();
        }
    }

    private double? _minScale;
    public double? MinScale
    {
        get => _minScale;
        set
        {
            _minScale = value;
            RaisePropertyChanged();
        }
    }

    private double? _maxScale;
    public double? MaxScale
    {
        get => _maxScale;
        set
        {
            _maxScale = value;
            RaisePropertyChanged();
        }
    }

    // A filter loaded from an existing rule that the simple editor can't represent
    // (spatial/logical/like/…) is retained here so it round-trips unchanged.
    private OgcFilter _loadedFilter;

    // Rule parts the editor doesn't surface, retained for lossless round-trip.
    private LegendGraphic? _loadedLegendGraphic;
    private ElseFilter? _loadedElseFilter;

    /// <summary>
    /// Index of the FeatureTypeStyle this rule was loaded from within its UserStyle
    /// (GeoServer commonly splits one logical style across several FeatureTypeStyles,
    /// one rule each, for compositing order). Used to map the rule back to the same
    /// slot on save; -1 marks a rule created in the editor.
    /// </summary>
    public int SourceFeatureTypeStyleIndex { get; set; } = -1;

    private bool _hasFilter;
    public bool HasFilter
    {
        get => _hasFilter;
        set
        {
            _hasFilter = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(FilterDescription));
        }
    }

    private string _filterPropertyName;
    public string FilterPropertyName
    {
        get => _filterPropertyName;
        set
        {
            _filterPropertyName = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(FilterDescription));
        }
    }

    private FilterComparisonOperator _filterOperator = FilterComparisonOperator.Equal;
    public FilterComparisonOperator FilterOperator
    {
        get => _filterOperator;
        set
        {
            _filterOperator = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(FilterDescription));
        }
    }

    private string _filterValue;
    public string FilterValue
    {
        get => _filterValue;
        set
        {
            _filterValue = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(FilterDescription));
        }
    }

    /// <summary>Human-readable summary of the current filter for the editor.</summary>
    public string FilterDescription
    {
        get
        {
            if (!HasFilter)
                return LocalizationManager.Instance["sldEditor_filter_noFilter"];

            if (!string.IsNullOrWhiteSpace(FilterPropertyName))
                return $"{FilterPropertyName} {OperatorSymbol(FilterOperator)} {FilterValue}";

            return _loadedFilter?.Predicate != null
                ? LocalizationManager.Instance["sldEditor_filter_advancedFilter"]
                : LocalizationManager.Instance["sldEditor_filter_noFilter"];
        }
    }

    public ObservableCollection<SymbolizerViewModelBase> Symbolizers { get; } = new ObservableCollection<SymbolizerViewModelBase>();

    private SymbolizerViewModelBase _selectedSymbolizer;
    public SymbolizerViewModelBase SelectedSymbolizer
    {
        get => _selectedSymbolizer;
        set
        {
            _selectedSymbolizer = value;
            RaisePropertyChanged();
        }
    }

    public ICommand AddPointSymbolizerCommand { get; }
    public ICommand AddLineSymbolizerCommand { get; }
    public ICommand AddPolygonSymbolizerCommand { get; }
    public ICommand AddTextSymbolizerCommand { get; }
    public ICommand AddRasterSymbolizerCommand { get; }
    public ICommand RemoveSymbolizerCommand { get; }

    public RuleViewModel()
    {
        AddPointSymbolizerCommand = new RelayCommand(_ => Symbolizers.Add(new PointSymbolizerViewModel()));
        AddLineSymbolizerCommand = new RelayCommand(_ => Symbolizers.Add(new LineSymbolizerViewModel()));
        AddPolygonSymbolizerCommand = new RelayCommand(_ => Symbolizers.Add(new PolygonSymbolizerViewModel()));
        AddTextSymbolizerCommand = new RelayCommand(_ => Symbolizers.Add(new TextSymbolizerViewModel()));
        AddRasterSymbolizerCommand = new RelayCommand(_ => Symbolizers.Add(new RasterSymbolizerViewModel()));
        RemoveSymbolizerCommand = new RelayCommand(_ => RemoveSymbolizer(), _ => SelectedSymbolizer != null);

        Symbolizers.CollectionChanged += OnSymbolizersCollectionChanged;
    }

    #region Live swatch preview

    private const int SwatchDebounceMilliseconds = 300;

    private static readonly SldLegendOptions _swatchOptions = new SldLegendOptions();

    private ImageSource? _swatchImage;
    private bool _swatchDirty = true;
    private DispatcherTimer? _swatchTimer;

    /// <summary>
    /// Live preview of this rule's symbolizers (same swatch renderer as the legend;
    /// filter/scale neutralized). Regenerated lazily on read after edits, so headless
    /// usage (tests, serialization) never triggers rendering.
    /// </summary>
    public ImageSource? SwatchImage
    {
        get
        {
            if (_swatchDirty)
            {
                _swatchDirty = false;
                _swatchImage = CreateSwatchImage();
            }

            return _swatchImage;
        }
    }

    private ImageSource? CreateSwatchImage()
    {
        try
        {
            using var bitmap = LegendSwatchFactory.CreateSwatchBitmap(ToRule(), _swatchOptions);

            return SldLegendBuilder.ToFrozenBitmapImage(bitmap);
        }
        catch
        {
            return null;
        }
    }

    private void OnSymbolizersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (INotifyPropertyChanged item in e.OldItems)
                item.PropertyChanged -= OnSymbolizerPropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (INotifyPropertyChanged item in e.NewItems)
                item.PropertyChanged += OnSymbolizerPropertyChanged;
        }

        InvalidateSwatch();
    }

    private void OnSymbolizerPropertyChanged(object? sender, PropertyChangedEventArgs e) => InvalidateSwatch();

    private void InvalidateSwatch()
    {
        _swatchDirty = true;

        if (_swatchTimer is null)
        {
            _swatchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(SwatchDebounceMilliseconds) };
            _swatchTimer.Tick += (_, _) =>
            {
                _swatchTimer.Stop();
                RaisePropertyChanged(nameof(SwatchImage));
            };
        }

        // restart on every change: bursts (per-keystroke edits) collapse into one refresh
        _swatchTimer.Stop();
        _swatchTimer.Start();
    }

    #endregion

    private void RemoveSymbolizer()
    {
        if (SelectedSymbolizer != null)
        {
            Symbolizers.Remove(SelectedSymbolizer);
            SelectedSymbolizer = null;
        }
    }

    public Rule ToRule()
    {
        var rule = new Rule
        {
            Name = Name,
            Title = Title,
            Abstract = Abstract,
            MinScaleDenominator = MinScale,
            MaxScaleDenominator = MaxScale,
            Filter = HasFilter ? BuildFilter() : null,
            LegendGraphic = _loadedLegendGraphic,
            ElseFilter = _loadedElseFilter,
            Symbolizers = Symbolizers.Select(s => s.ToSymbolizer()).ToList()
        };

        return rule;
    }

    public void FromRule(Rule rule)
    {
        Name = rule.Name;
        Title = rule.Title;
        Abstract = rule.Abstract;
        MinScale = rule.MinScaleDenominator;
        MaxScale = rule.MaxScaleDenominator;
        _loadedLegendGraphic = rule.LegendGraphic;
        _loadedElseFilter = rule.ElseFilter;
        LoadFilter(rule.Filter);

        Symbolizers.Clear();
        foreach (var symbolizer in rule.Symbolizers ?? Enumerable.Empty<Symbolizer>())
        {
            SymbolizerViewModelBase vm = symbolizer switch
            {
                PointSymbolizer => new PointSymbolizerViewModel(),
                LineSymbolizer => new LineSymbolizerViewModel(),
                PolygonSymbolizer => new PolygonSymbolizerViewModel(),
                TextSymbolizer => new TextSymbolizerViewModel(),
                RasterSymbolizer => new RasterSymbolizerViewModel(),
                _ => null
            };

            if (vm != null)
            {
                vm.FromSymbolizer(symbolizer);
                Symbolizers.Add(vm);
            }
        }
    }

    /// <summary>
    /// Builds an <see cref="OgcFilter"/> from the simple property/operator/value fields.
    /// If the property name is empty, falls back to a filter loaded from an existing rule
    /// (so filters the simple editor can't represent are preserved on round-trip).
    /// </summary>
    private OgcFilter BuildFilter()
    {
        if (string.IsNullOrWhiteSpace(FilterPropertyName))
            return _loadedFilter;

        var property = new OgcPropertyName { Value = FilterPropertyName };
        var literal = new OgcLiteral { Value = FilterValue };

        OgcFilterBase predicate = FilterOperator switch
        {
            FilterComparisonOperator.Equal => new OgcPropertyIsEqualTo { Expressions = { property, literal } },
            FilterComparisonOperator.NotEqual => new OgcPropertyIsNotEqualTo { Expressions = { property, literal } },
            FilterComparisonOperator.LessThan => new OgcPropertyIsLessThan { Expressions = { property, literal } },
            FilterComparisonOperator.GreaterThan => new OgcPropertyIsGreaterThan { Expressions = { property, literal } },
            FilterComparisonOperator.LessThanOrEqual => new OgcPropertyIsLessThanOrEqualTo { Expressions = { property, literal } },
            FilterComparisonOperator.GreaterThanOrEqual => new OgcPropertyIsGreaterThanOrEqualTo { Expressions = { property, literal } },
            _ => new OgcPropertyIsEqualTo { Expressions = { property, literal } }
        };

        return new OgcFilter { Predicate = predicate };
    }

    /// <summary>
    /// Populates the simple filter fields from an existing rule's filter, when it is a
    /// single property/value comparison; otherwise retains it for lossless round-tripping.
    /// </summary>
    private void LoadFilter(OgcFilter filter)
    {
        _loadedFilter = filter;
        FilterPropertyName = null;
        FilterValue = null;
        FilterOperator = FilterComparisonOperator.Equal;

        if (filter?.Predicate == null)
        {
            HasFilter = false;
            return;
        }

        HasFilter = true;

        switch (filter.Predicate)
        {
            case OgcPropertyIsEqualTo eq:
                SetSimpleFilter(eq.GetPropertyName(), FilterComparisonOperator.Equal, eq.GetLiteral());
                break;
            case OgcPropertyIsNotEqualTo ne:
                SetSimpleFilter(ne.GetPropertyName(), FilterComparisonOperator.NotEqual, ne.GetLiteral());
                break;
            case OgcPropertyIsLessThan lt:
                SetSimpleFilter(lt.GetPropertyName(), FilterComparisonOperator.LessThan, FormatLiteral(lt.GetLiteral()));
                break;
            case OgcPropertyIsGreaterThan gt:
                SetSimpleFilter(gt.GetPropertyName(), FilterComparisonOperator.GreaterThan, FormatLiteral(gt.GetLiteral()));
                break;
            case OgcPropertyIsLessThanOrEqualTo le:
                SetSimpleFilter(le.GetPropertyName(), FilterComparisonOperator.LessThanOrEqual, FormatLiteral(le.GetLiteral()));
                break;
            case OgcPropertyIsGreaterThanOrEqualTo ge:
                SetSimpleFilter(ge.GetPropertyName(), FilterComparisonOperator.GreaterThanOrEqual, FormatLiteral(ge.GetLiteral()));
                break;
            default:
                // Keep _loadedFilter for round-trip; fields stay empty (shown as "advanced").
                break;
        }
    }

    private void SetSimpleFilter(string propertyName, FilterComparisonOperator op, string value)
    {
        FilterPropertyName = propertyName;
        FilterOperator = op;
        FilterValue = value;
    }

    private static string FormatLiteral(double? value) =>
        value?.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static string OperatorSymbol(FilterComparisonOperator op) => op switch
    {
        FilterComparisonOperator.Equal => "=",
        FilterComparisonOperator.NotEqual => "≠",
        FilterComparisonOperator.LessThan => "<",
        FilterComparisonOperator.GreaterThan => ">",
        FilterComparisonOperator.LessThanOrEqual => "≤",
        FilterComparisonOperator.GreaterThanOrEqual => "≥",
        _ => "="
    };
}

/// <summary>
/// Comparison operators supported by the simple SLD filter editor.
/// </summary>
public enum FilterComparisonOperator
{
    Equal,
    NotEqual,
    LessThan,
    GreaterThan,
    LessThanOrEqual,
    GreaterThanOrEqual
}

