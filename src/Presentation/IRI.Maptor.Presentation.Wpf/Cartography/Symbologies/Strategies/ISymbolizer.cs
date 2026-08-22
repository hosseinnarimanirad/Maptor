using System;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;

public interface ISymbolizer
{
    SymbologyType Type { get; }

    double? MinScaleDenominator { get; set; }

    double? MaxScaleDenominator { get; set; }
     
    Func<Feature<Point>, bool> IsFilterPassed { get; set; }

    bool IsInScaleRange(double scale);

    VisualParameters? Param { get; set; }
}
