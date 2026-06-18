using System;
using System.Linq;
using System.Windows.Input;
using System.Collections.ObjectModel;

using IRI.Maptor.Sta.Ogc;
using IRI.Maptor.Sta.Ogc.SLD;
using IRI.Maptor.Jab.Core;


namespace IRI.Maptor.Jab.Common.ViewModels.Symbology;

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

    private bool _hasFilter;
    public bool HasFilter
    {
        get => _hasFilter;
        set
        {
            _hasFilter = value;
            RaisePropertyChanged();
        }
    }

    private string _filterDescription;
    public string FilterDescription
    {
        get => _filterDescription;
        set
        {
            _filterDescription = value;
            RaisePropertyChanged();
        }
    }

    public OgcFilter Filter { get; set; }

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
    public ICommand RemoveSymbolizerCommand { get; }

    public RuleViewModel()
    {
        AddPointSymbolizerCommand = new RelayCommand(_ => Symbolizers.Add(new PointSymbolizerViewModel()));
        AddLineSymbolizerCommand = new RelayCommand(_ => Symbolizers.Add(new LineSymbolizerViewModel()));
        AddPolygonSymbolizerCommand = new RelayCommand(_ => Symbolizers.Add(new PolygonSymbolizerViewModel()));
        AddTextSymbolizerCommand = new RelayCommand(_ => Symbolizers.Add(new TextSymbolizerViewModel()));
        RemoveSymbolizerCommand = new RelayCommand(_ => RemoveSymbolizer(), _ => SelectedSymbolizer != null);
    }

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
            Filter = HasFilter ? Filter : null,
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
        Filter = rule.Filter;
        HasFilter = rule.Filter != null;
        FilterDescription = HasFilter ? GetFilterDescription(rule.Filter) : "No filter";

        Symbolizers.Clear();
        foreach (var symbolizer in rule.Symbolizers ?? Enumerable.Empty<Symbolizer>())
        {
            SymbolizerViewModelBase vm = symbolizer switch
            {
                PointSymbolizer => new PointSymbolizerViewModel(),
                LineSymbolizer => new LineSymbolizerViewModel(),
                PolygonSymbolizer => new PolygonSymbolizerViewModel(),
                TextSymbolizer => new TextSymbolizerViewModel(),
                _ => null
            };

            if (vm != null)
            {
                vm.FromSymbolizer(symbolizer);
                Symbolizers.Add(vm);
            }
        }
    }

    private string GetFilterDescription(OgcFilter filter)
    {
        if (filter?.Predicate == null)
            return "No filter";

        return filter.Predicate switch
        {
            OgcPropertyIsEqualTo eq => $"{eq.GetPropertyName()} = {eq.GetLiteral()}",
            OgcComparisonOperator comp => $"{comp.GetPropertyName()} [comparison]",
            _ => "Filter defined"
        };
    }
}

