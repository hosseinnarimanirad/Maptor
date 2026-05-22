namespace IRI.Maptor.Sta.Spatial.Analysis;

public class SimplificationParameters
{
    // 1401.02.26
    // used in n-th point simplification methods
    public uint N { get; set; }

    public double? DistanceThreshold { get; set; }

    public double? AreaThreshold { get; set; }

    /// <summary>
    /// Square Cosine of angle should be provided, must be between 0 and 1
    /// </summary>
    public double? AngleThreshold { get; set; }

    public bool Retain3Points { get; set; } = false;


    // used in Lang algorithm
    public int? LookAhead { get; set; }

    public double? AverageLatitude { get; set; }
}
