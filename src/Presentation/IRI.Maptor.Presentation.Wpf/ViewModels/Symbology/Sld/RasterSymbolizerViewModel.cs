using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Core.Ogc.SLD;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Symbology;

public class RasterSymbolizerViewModel : SymbolizerViewModelBase
{
    public override string SymbolizerType => "Raster";

    private double _opacity = 1.0;
    public double Opacity
    {
        get => _opacity;
        set
        {
            _opacity = Math.Clamp(value, 0.0, 1.0);
            RaisePropertyChanged();
        }
    }

    public ObservableCollection<ColorMapEntryViewModel> ColorMap { get; } = new ObservableCollection<ColorMapEntryViewModel>();

    private ColorMapEntryViewModel _selectedEntry;
    public ColorMapEntryViewModel SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            _selectedEntry = value;
            RaisePropertyChanged();
        }
    }

    public ICommand AddEntryCommand { get; }
    public ICommand RemoveEntryCommand { get; }

    public RasterSymbolizerViewModel()
    {
        AddEntryCommand = new RelayCommand(_ => AddEntry());
        RemoveEntryCommand = new RelayCommand(_ => RemoveEntry(), _ => SelectedEntry != null);
    }

    private void AddEntry()
    {
        var entry = new ColorMapEntryViewModel { Color = Colors.White };
        ColorMap.Add(entry);
        SelectedEntry = entry;
    }

    private void RemoveEntry()
    {
        if (SelectedEntry == null)
            return;

        var index = ColorMap.IndexOf(SelectedEntry);
        ColorMap.Remove(SelectedEntry);

        if (ColorMap.Count > 0)
            SelectedEntry = ColorMap[Math.Min(index, ColorMap.Count - 1)];
        else
            SelectedEntry = null;
    }

    public override Symbolizer ToSymbolizer()
    {
        var symbolizer = new RasterSymbolizer
        {
            Opacity = Opacity
        };

        if (ColorMap.Count > 0)
        {
            symbolizer.ColorMap = new ColorMap
            {
                ColorMapEntries = ColorMap.Select(e => e.ToColorMapEntry()).ToList()
            };
        }

        if (!string.IsNullOrWhiteSpace(GeometryPropertyName))
            symbolizer.Geometry = new IRI.Maptor.Core.Ogc.SLD.Geometry { PropertyName = GeometryPropertyName };

        return symbolizer;
    }

    public override void FromSymbolizer(Symbolizer symbolizer)
    {
        if (symbolizer is not RasterSymbolizer rasterSymbolizer)
            return;

        GeometryPropertyName = rasterSymbolizer.Geometry?.PropertyName;

        if (rasterSymbolizer.Opacity.HasValue)
            Opacity = rasterSymbolizer.Opacity.Value;

        ColorMap.Clear();
        foreach (var entry in rasterSymbolizer.ColorMap?.ColorMapEntries ?? Enumerable.Empty<ColorMapEntry>())
        {
            ColorMap.Add(ColorMapEntryViewModel.FromColorMapEntry(entry));
        }
    }
}

public class ColorMapEntryViewModel : Notifier
{
    private Color _color = Colors.White;
    public Color Color
    {
        get => _color;
        set
        {
            _color = value;
            RaisePropertyChanged();
        }
    }

    private double? _quantity;
    public double? Quantity
    {
        get => _quantity;
        set
        {
            _quantity = value;
            RaisePropertyChanged();
        }
    }

    private string _label;
    public string Label
    {
        get => _label;
        set
        {
            _label = value;
            RaisePropertyChanged();
        }
    }

    private double? _opacity;
    public double? Opacity
    {
        get => _opacity;
        set
        {
            _opacity = value;
            RaisePropertyChanged();
        }
    }

    public ColorMapEntry ToColorMapEntry() => new ColorMapEntry
    {
        Color = SldColorHelper.ToHex(Color),
        Quantity = Quantity,
        Opacity = Opacity,
        Label = Label
    };

    public static ColorMapEntryViewModel FromColorMapEntry(ColorMapEntry entry)
    {
        var vm = new ColorMapEntryViewModel
        {
            Quantity = entry.Quantity,
            Opacity = entry.Opacity,
            Label = entry.Label
        };

        if (SldColorHelper.TryParseHexColor(entry.Color, out var color))
            vm.Color = color;

        return vm;
    }
}
