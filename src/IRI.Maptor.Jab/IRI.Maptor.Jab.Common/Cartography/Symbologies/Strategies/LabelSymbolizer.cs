using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using System;
using System.Windows.Media;

namespace IRI.Maptor.Jab.Common.Cartography.Symbologies;

public class LabelSymbolizer : SymbolizerBase
{
    public override SymbologyType Type => SymbologyType.Label;

    public string LabelAttribute { get; set; }

    public LabelSymbolizer(VisualParameters labels, string labelAttribute)
    {
        Param = labels;
        LabelAttribute = labelAttribute;
    }


    public static LabelSymbolizer Create(string labelAttribute, int fontSize, Brush foreground, FontFamily fontFamily, Func<Geometry<Point>, Point> positionFunc, ScaleInterval visibleRange, bool isRtl)
    { 
        var param = VisualParameters.CreateLabel(visibleRange, fontSize, foreground, fontFamily, positionFunc, isRtl);

        return new LabelSymbolizer(param, labelAttribute);
    }
}
