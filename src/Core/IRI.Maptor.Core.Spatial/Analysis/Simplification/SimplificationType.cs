using System.ComponentModel;

namespace IRI.Maptor.Core.Spatial.Analysis;

public enum SimplificationType
{
    /// <summary>
    /// AAP
    /// </summary>
    [Description("AAP")]
    AdditiveAreaPlus,


    /// <summary>
    /// CANG
    /// </summary>
    [Description("CANG")]
    CumulativeAngle,


    /// <summary>
    /// CED
    /// </summary>
    [Description("CED")]
    CumulativeEuclideanDistance,


    /// <summary>
    /// CTRANG
    /// </summary>
    [Description("CTRANG")]
    CumulativeAreaAngle,


    /// <summary>
    /// ANG
    /// </summary>
    [Description("ANG")]
    Angle,


    /// <summary>
    /// ED
    /// </summary>
    [Description("ED")]
    EuclideanDistance,


    /// <summary>
    /// NTH
    /// </summary>
    [Description("NTH")]
    NthPoint,

        
    // 1401.06.24
    /// <summary>
    /// RPS
    /// </summary>
    [Description("RPS")]
    RandomPointSelection,


    /// <summary>
    /// LA
    /// </summary>
    [Description("LA")]
    Lang,


    /// <summary>
    /// RDP
    /// </summary>
    [Description("RDP")]
    RamerDouglasPeucker,


    // 1400.05.11
    // http://psimpl.sourceforge.net/reumann-witkam.html
    /// <summary>
    /// RW
    /// </summary>
    [Description("RW")]
    ReumannWitkam,


    // 1400.05.11 
    /// <summary>
    /// PD
    /// </summary>
    [Description("PD")]
    PerpendicularDistance,


    // 1400.06.24 
    /// <summary>
    /// MPD
    /// </summary>
    [Description("MPD")]
    ModifiedPerpendicularDistance,


    /// <summary>
    /// VW
    /// </summary>
    [Description("VW")]
    VisvalingamWhyatt,


    /// <summary>
    /// SF
    /// </summary>
    [Description("SF")]
    SleeveFitting,


    // 1400.05.20
    /// <summary>
    /// NOPW
    /// </summary>
    [Description("NOPW")]
    NormalOpeningWindow,


    // 1400.05.20
    /// <summary>
    /// BOPW
    /// </summary>
    [Description("BOPW")]
    BeforeOpeningWindow,


    // Triangular Routine
    /// <summary>
    /// TR
    /// </summary>
    [Description("TR")]
    TriangleRoutine,


    /// <summary>
    /// MTR
    /// </summary>
    [Description("MTR")]
    ModifiedTriangleRoutine,


    /// <summary>
    /// CTR
    /// </summary>
    [Description("CTR")]
    CumulativeTriangleRoutine,

    // 1403.03.08
    /// <summary>
    /// APSC
    /// </summary>
    APSC,


    // 1405.07.10
    /// <summary>
    /// LO: Li and Openshaw (1992), vector mode (circular smallest visible object of diameter DistanceThreshold)
    /// </summary>
    [Description("LO")]
    LiOpenshaw,


    /// <summary>
    /// LOR: Li and Openshaw (1992), raster mode (square cells of side DistanceThreshold)
    /// </summary>
    [Description("LOR")]
    LiOpenshawRaster,


    // 1405.08.04
    /// <summary>
    /// CTR2: CTR as the ISPRS 2025 pseudo-code describes it (retain the middle and the last point, restart at the last
    /// point). <see cref="CumulativeTriangleRoutine"/> is the variant that produced the published numbers.
    /// </summary>
    [Description("CTR2")]
    CumulativeTriangleRoutine2,
}
