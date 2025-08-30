using System;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Jab.Common.Cartography.Symbologies;

public interface ISymbolizer
{
    SymbologyType Type { get; }

    double? MinScaleDenominator { get; set; }

    double? MaxScaleDenominator { get; set; }
     
    Func<Feature<Point>, bool> IsFilterPassed { get; set; }

    bool IsInScaleRange(double scale);

    VisualParameters? Param { get; set; }
}
