using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace IRI.Maptor.Core.Spatial.Analysis;

public enum CharacteristicsMeasure
{
    // Percentage change in coordinates (PCC)
    [Description("PCInCoordinates")]
    PCC = 1,

    // Point density difference (PDD)
    [Description("PointDensityDifference")]
    PDD = 2,

    // Percentage change in the line length (PCLL)
    [Description("PCInLineLength")]
    PCLL = 3,

    // Percentage change in angularity (PCANGLE)
    [Description("PCInAngularity")]
    PCANGLE = 4,

    // Percentage change in curvilinear segments (PCCS)
    [Description("PCInCurvilinearity")]
    PCCS = 5,

    // Total length of vector differences (TLVD)
    [Description("TLVD")]
    TLVD = 6,

    // Total areal polygon difference (TAPD)
    [Description("TAPD")]
    TAPD = 7,

    // Total perimeter of areal polygon (TPAP)
    [Description("TPAP")]
    TPAP = 8
}
