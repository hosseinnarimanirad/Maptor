using System.Collections.Generic;

using IRI.Maptor.Core.Common.Mathematics;

namespace IRI.Maptor.Presentation.Wpf;

public class SimplificationAccuracy
{
    public int FeatureIndex { get; set; }

    public int Zoomlevel { get; set; }

    public string Algorithm { get; set; }

    public double TlvdPerLength { get; set; }

    public double Compression { get; set; }

    /// <summary>Exact pixel confusion matrix of the 1-px renderings (true negatives not counted).</summary>
    public ConfusionMatrix ConfusionMatrix { get; set; }

    /// <summary>The distance-aware pixel measures (tolerant F1, displacement, Hausdorff); null in older code paths.</summary>
    public RasterComparison Raster { get; set; }

    /// <summary>Running time of the simplification alone, in milliseconds with sub-millisecond resolution; -1 when not measured.</summary>
    public double ElapsedMilliseconds { get; set; } = -1;

    public static string GetTsvHeader()
    {
        var headers = new List<string>()
        {
            {nameof(FeatureIndex)},
            {nameof(Zoomlevel)},
            {nameof(Algorithm)},
            {nameof(TlvdPerLength)},
            {nameof(Compression)},
            {nameof(ElapsedMilliseconds)},
        };

        return string.Join("\t", headers) + "\t" + ConfusionMatrix.GetTsvHeader() + "\t" + RasterComparison.GetTsvHeader();
    }

    public string ToTsv()
    {
        var values = new List<string>()
        {
            FeatureIndex.ToString(),
            Zoomlevel.ToString(),
            Algorithm.ToString(),
            TlvdPerLength.ToString("N4"),
            Compression.ToString("N6"),
            ElapsedMilliseconds.ToString("0.###"),
        };

        return string.Join("\t", values) + "\t" + ConfusionMatrix.AsTsv() + "\t" + (Raster?.AsTsv() ?? RasterComparison.EmptyTsv());
    }
}
