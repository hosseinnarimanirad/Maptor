using System.Collections.Generic;

using IRI.Maptor.Sta.Mathematics;

namespace IRI.Maptor.Jab.Common;

public class SimplificationAccuracy
{
    public int FeatureIndex { get; set; }

    public int Zoomlevel { get; set; }

    public string Algorithm { get; set; }

    public double TlvdPerLength { get; set; }

    public double Compression { get; set; }

    public ConfusionMatrix ConfusionMatrix { get; set; }

    public static string GetTsvHeader()
    {
        var headers = new List<string>()
        {
            {nameof(FeatureIndex)},
            {nameof(Zoomlevel)},
            {nameof(Algorithm)},
            {nameof(TlvdPerLength)},
            {nameof(Compression)}
        };

        return string.Join("\t", headers) + "\t" + ConfusionMatrix.GetTsvHeader();
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
        };

        return string.Join("\t", values) + "\t" + ConfusionMatrix.AsTsv();
    }
}