using System;
using System.Windows.Media;
using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Jab.Common.Cartography.Symbologies;

public class SimpleSymbolizer : SymbolizerBase
{
    public override SymbologyType Type { get => SymbologyType.Single; }

    //private VisualParameters _param;

    //public VisualParameters Param
    //{
    //    get { return _param; }
    //    set
    //    {
    //        _param = value;
    //        RaisePropertyChanged();
    //    }
    //}

    public SimpleSymbolizer(VisualParameters visualParameters)
    {
        Param = visualParameters;
    }

    public SimpleSymbolizer(Func<Feature<Point>, bool> filter, VisualParameters visualParameters) : this(visualParameters)
    {
        this.IsFilterPassed = filter;
    }



    public static SimpleSymbolizer Create(Brush? fill, Brush? stroke, double strokeThickness, double opacity, bool isOn = true)
    {
        var visualParamters = new VisualParameters(fill, stroke, strokeThickness, opacity, isOn);

        return new SimpleSymbolizer(visualParamters);
    }

    public static SimpleSymbolizer Create(Color? fill, Color? stroke = null, double strokeThickness = 1, double opacity = 1, bool isOn = true)
    {
        return Create(fill.HasValue ? new SolidColorBrush(fill.Value) : null, stroke.HasValue ? new SolidColorBrush(stroke.Value) : null, strokeThickness, opacity, isOn: isOn);
    }

    public static SimpleSymbolizer Create(string? hexFill, string? hexStroke, double strokeThickness = 1, double opacity = 1, bool isOn = true)
    {
        return Create(BrushHelper.CreateBrush(hexFill), BrushHelper.CreateBrush(hexStroke), strokeThickness, opacity, isOn: isOn);
    }

}
