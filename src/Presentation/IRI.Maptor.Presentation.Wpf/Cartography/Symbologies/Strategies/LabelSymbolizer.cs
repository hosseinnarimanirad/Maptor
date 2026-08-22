using IRI.Maptor.Presentation.Core.Models;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using System;
using System.Windows.Media;

namespace IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;

public class LabelSymbolizer : SymbolizerBase
{
    public override SymbologyType Type => SymbologyType.Label;

    public string? LabelAttribute { get; set; }

    public LabelSymbolizer(VisualParameters labels, string? labelAttribute = null)
    {
        Param = labels;
        LabelAttribute = labelAttribute;
    }


    public static LabelSymbolizer Create(int fontSize, Brush foreground, FontFamily fontFamily, Func<Geometry<Point>, Point> positionFunc, ScaleInterval visibleRange, bool isRtl)
    {
        var param = VisualParameters.CreateLabel(visibleRange, fontSize, foreground, fontFamily, positionFunc, isRtl);

        return new LabelSymbolizer(param);
    }
}
