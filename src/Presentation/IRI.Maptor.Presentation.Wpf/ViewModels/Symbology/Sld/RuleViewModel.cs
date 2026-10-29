using System;
using System.Linq;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

using IRI.Maptor.Core.Ogc;
using IRI.Maptor.Core.Ogc.SLD;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Wpf.Cartography.Legend;


namespace IRI.Maptor.Presentation.Wpf.ViewModels.Symbology;

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
            RaisePropertyChanged(nameof(DisplayTitle));
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
            RaisePropertyChanged(nameof(DisplayTitle));
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
            RaiseScaleRangeChanged();
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
            RaiseScaleRangeChanged();
        }
    }

    /// <summary>What the legend shows for the rule: its title, or its name when it has no title.</summary>
    public string DisplayTitle => !string.IsNullOrWhiteSpace(Title) ? Title : Name ?? string.Empty;

    public bool HasScaleRange => MinScale.HasValue || MaxScale.HasValue;

    /// <summary>Both limits are set but the zoomed-in one is not the smaller: the rule would never draw.</summary>
    public bool HasScaleRangeError => MinScale.HasValue && MaxScale.HasValue && MinScale.Value >= MaxScale.Value;

    /// <summary>The scale range in words, for the rule list and the preview.</summary>
    public string ScaleRangeText
    {
        get
        {
            static string Format(double value) => value.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture);

            if (MinScale.HasValue && MaxScale.HasValue)
                return string.Format(LocalizationManager.Instance["sldEditor_scale_between"], Format(MinScale.Value), Format(MaxScale.Value));

            if (MaxScale.HasValue)
                return string.Format(LocalizationManager.Instance["sldEditor_scale_closerThan"], Format(MaxScale.Value));

            if (MinScale.HasValue)
                return string.Format(LocalizationManager.Instance["sldEditor_scale_fartherThan"], Format(MinScale.Value));

            return LocalizationManager.Instance["sldEditor_scale_all"];
        }
    }

    private void RaiseScaleRangeChanged()
    {
        RaisePropertyChanged(nameof(HasScaleRange));
        RaisePropertyChanged(nameof(HasScaleRangeError));
        RaisePropertyChanged(nameof(ScaleRangeText));
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
            RaisePropertyChanged(nameof(IsAdvancedFilter));
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
            RaisePropertyChanged(nameof(IsAdvancedFilter));
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
            RaisePropertyChanged(nameof(IsAdvancedFilter));
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
            RaisePropertyChanged(nameof(IsAdvancedFilter));
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

    /// <summary>
    /// The rule keeps a filter the simple editor cannot show (logical, spatial, like …); it is saved unchanged
    /// until the user picks a field here.
    /// </summary>
    public bool IsAdvancedFilter => HasFilter && string.IsNullOrWhiteSpace(FilterPropertyName) && _loadedFilter?.Predicate != null;

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
    /// <summary>Removes the symbolizer passed as the parameter, or the selected one.</summary>
    public ICommand RemoveSymbolizerCommand { get; }
    public ICommand MoveSymbolizerUpCommand { get; }
    public ICommand MoveSymbolizerDownCommand { get; }

    /// <summary>
    /// Raised when anything that ends up in the SLD changes: the rule's own fields, its symbolizer list or a
    /// field of one of its symbolizers. Editor-only state (selection, expanded cards, previews) does not count.
    /// </summary>
    public event EventHandler? Changed;

    private static readonly System.Collections.Generic.HashSet<string> _uiOnlyProperties = new()
    {
        nameof(SwatchImage), nameof(PreviewImage), nameof(PreviewLabel), nameof(HasPreviewLabel), nameof(HasSymbolizers),
        nameof(SelectedSymbolizer), nameof(DisplayTitle), nameof(HasScaleRange), nameof(HasScaleRangeError),
        nameof(ScaleRangeText), nameof(FilterDescription), nameof(IsAdvancedFilter),
    };

    public RuleViewModel()
    {
        AddPointSymbolizerCommand = new RelayCommand(_ => AddSymbolizer(new PointSymbolizerViewModel()));
        AddLineSymbolizerCommand = new RelayCommand(_ => AddSymbolizer(new LineSymbolizerViewModel()));
        AddPolygonSymbolizerCommand = new RelayCommand(_ => AddSymbolizer(new PolygonSymbolizerViewModel()));
        AddTextSymbolizerCommand = new RelayCommand(_ => AddSymbolizer(new TextSymbolizerViewModel()));
        AddRasterSymbolizerCommand = new RelayCommand(_ => AddSymbolizer(new RasterSymbolizerViewModel()));
        RemoveSymbolizerCommand = new RelayCommand(p => RemoveSymbolizer(p as SymbolizerViewModelBase ?? SelectedSymbolizer),
                                                   p => (p as SymbolizerViewModelBase ?? SelectedSymbolizer) != null);
        MoveSymbolizerUpCommand = new RelayCommand(p => MoveSymbolizer(p as SymbolizerViewModelBase, -1),
                                                   p => p is SymbolizerViewModelBase s && Symbolizers.IndexOf(s) > 0);
        MoveSymbolizerDownCommand = new RelayCommand(p => MoveSymbolizer(p as SymbolizerViewModelBase, 1),
                                                     p => p is SymbolizerViewModelBase s && Symbolizers.IndexOf(s) >= 0 && Symbolizers.IndexOf(s) < Symbolizers.Count - 1);

        Symbolizers.CollectionChanged += OnSymbolizersCollectionChanged;

        PropertyChanged += (_, e) =>
        {
            if (!_uiOnlyProperties.Contains(e.PropertyName ?? string.Empty))
                Changed?.Invoke(this, EventArgs.Empty);
        };
    }

    public bool HasSymbolizers => Symbolizers.Count > 0;

    /// <summary>The first label of the rule; the editor's preview draws its sample text with it.</summary>
    public TextSymbolizerViewModel? PreviewLabel => Symbolizers.OfType<TextSymbolizerViewModel>().FirstOrDefault();

    public bool HasPreviewLabel => PreviewLabel is not null;

    /// <summary>
    /// A copy of the rule for "Duplicate": same fields, symbolizers and kept SLD parts, and the same
    /// FeatureTypeStyle on save.
    /// </summary>
    public RuleViewModel Clone()
    {
        var copy = new RuleViewModel { SourceFeatureTypeStyleIndex = SourceFeatureTypeStyleIndex };

        copy.FromRule(ToRule());

        return copy;
    }

    private void AddSymbolizer(SymbolizerViewModelBase symbolizer)
    {
        Symbolizers.Add(symbolizer);
        SelectedSymbolizer = symbolizer;
    }

    private void MoveSymbolizer(SymbolizerViewModelBase? symbolizer, int offset)
    {
        var index = symbolizer is null ? -1 : Symbolizers.IndexOf(symbolizer);
        var target = index + offset;

        if (index < 0 || target < 0 || target >= Symbolizers.Count)
            return;

        Symbolizers.Move(index, target);
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
        RaisePropertyChanged(nameof(HasSymbolizers));
        RaisePropertyChanged(nameof(PreviewLabel));
        RaisePropertyChanged(nameof(HasPreviewLabel));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnSymbolizerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // opening or closing a card changes nothing that is drawn or saved
        if (e.PropertyName == nameof(SymbolizerViewModelBase.IsExpanded))
            return;

        InvalidateSwatch();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private const int PreviewWidth = 240;
    private const int PreviewHeight = 150;

    private ImageSource? _previewImage;
    private bool _previewDirty = true;

    /// <summary>
    /// The editor's large preview of the rule's geometry symbolizers (labels are drawn over it by the view).
    /// Lazy and debounced like <see cref="SwatchImage"/>.
    /// </summary>
    public ImageSource? PreviewImage
    {
        get
        {
            if (_previewDirty)
            {
                _previewDirty = false;
                _previewImage = CreatePreviewImage();
            }

            return _previewImage;
        }
    }

    private ImageSource? CreatePreviewImage()
    {
        try
        {
            using var bitmap = LegendSwatchFactory.CreatePreviewBitmap(ToRule(), PreviewWidth, PreviewHeight);

            return SldLegendBuilder.ToFrozenBitmapImage(bitmap);
        }
        catch
        {
            return null;
        }
    }

    private void InvalidateSwatch()
    {
        _swatchDirty = true;
        _previewDirty = true;

        if (_swatchTimer is null)
        {
            _swatchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(SwatchDebounceMilliseconds) };
            _swatchTimer.Tick += (_, _) =>
            {
                _swatchTimer.Stop();
                RaisePropertyChanged(nameof(SwatchImage));
                RaisePropertyChanged(nameof(PreviewImage));
            };
        }

        // restart on every change: bursts (per-keystroke edits) collapse into one refresh
        _swatchTimer.Stop();
        _swatchTimer.Start();
    }

    #endregion

    private void RemoveSymbolizer(SymbolizerViewModelBase? symbolizer)
    {
        if (symbolizer is null)
            return;

        Symbolizers.Remove(symbolizer);

        if (ReferenceEquals(SelectedSymbolizer, symbolizer))
            SelectedSymbolizer = null;
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

