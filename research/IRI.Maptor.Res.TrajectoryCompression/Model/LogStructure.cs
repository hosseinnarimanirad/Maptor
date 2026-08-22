using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives; 
using System;
using System.Collections.Generic;
using System.Linq;

namespace IRI.Maptor.Res.TrajectoryCompression;

public class LogStructure
{
    public string FileName { get; set; }

    public int Level { get; set; }

    public double AreaThreshold { get; set; }

    public double DistanceThreshold { get; set; }

    public int OriginalNumberOfPoints { get; set; }

    public double CompressionRatio { get { return 1.0 - ((double)TotalNumberOfPoints) / ((double)OriginalNumberOfPoints); } }

    public int NumberOfFeatures { get; set; }

    public int TotalNumberOfPoints { get; set; }

    public int TotalTriangleCount { get; set; }

    public double ImageDifInPercent { get; set; }

    public double MinArea { get; set; }

    public double MaxArea { get; set; }

    public double MeanArea { get; set; }

    public double FirstQuartile { get; set; }

    public double SecondQuartile { get; set; }

    public double ThirdQuartile { get; set; }

    public string MethodName { get; set; }

    public long ElapsedMilliseconds { get; set; }

    public double Coef { get; set; }

    public LogStructure(
        string fileName,
        int originalNumberOfPoints,
        List<Geometry<Point>> geometries,
        int level,
        double imageDifferenceInPercent,
        string methodName,
        long elapsedMilliseconds,
        SimplificationParameters parameters,
        double coef)
    {
        this.FileName = fileName;

        this.Coef = coef;

        this.OriginalNumberOfPoints = originalNumberOfPoints;

        this.MethodName = methodName;

        this.ElapsedMilliseconds = elapsedMilliseconds;

        this.AreaThreshold = parameters.AreaThreshold ?? 0;

        this.DistanceThreshold = parameters.DistanceThreshold ?? 0;

        //AreaStatistics stat = new AreaStatistics(geometries);

        var totalNumberOfPoints = geometries.Sum(f => f.TotalNumberOfPoints);

        //var quanrtiles = GeneralStatistics.CalculateSummary(stat.Areas.ToArray());//.AsCsvLine("\t", "0.00");

        this.Level = level;

        this.NumberOfFeatures = geometries.Count;

        this.TotalNumberOfPoints = totalNumberOfPoints;

        //this.TotalTriangleCount = stat.Areas.Count;

        //this.ImageDifInPercent = imageDifferenceInPercent;

        //this.MinArea = quanrtiles.Min;

        //this.MaxArea = quanrtiles.Max;

        //this.MeanArea = quanrtiles.Mean;

        //this.FirstQuartile = quanrtiles.FirstQuartile;

        //this.SecondQuartile = quanrtiles.Median;

        //this.ThirdQuartile = quanrtiles.ThirdQuartile;
         
    }

    public static string GetHeader()
    {
        var headers = new List<string>()
        {
            nameof(FileName),
            nameof(Level),
            nameof(MethodName),
            nameof(OriginalNumberOfPoints),
            nameof(NumberOfFeatures),
            nameof(TotalNumberOfPoints),
            nameof(TotalTriangleCount),                

            nameof(CompressionRatio),
            nameof(ImageDifInPercent),
            nameof(ElapsedMilliseconds),

            nameof(Coef),
            nameof(AreaThreshold),
            nameof(DistanceThreshold),

            nameof(MinArea),
            nameof(FirstQuartile),
            nameof(SecondQuartile),
            nameof(MeanArea),
            nameof(ThirdQuartile),
            nameof(MaxArea),

            $"{nameof(MinArea)}(log)",
            $"{nameof(FirstQuartile)}(log)",
            $"{nameof(SecondQuartile)}(log)",
            $"{nameof(MeanArea)}(log)",
            $"{nameof(ThirdQuartile)}(log)",
            $"{nameof(MaxArea)}(log)",

        };

        return string.Join("\t", headers);
    }

    public string ToTsv()
    {
        var headers = new List<string>()
        {
            FileName,
            Level.ToString(),
            MethodName,
            OriginalNumberOfPoints.ToString(),
            NumberOfFeatures.ToString(),
            TotalNumberOfPoints.ToString(),
            TotalTriangleCount.ToString(),

            CompressionRatio.ToString("N3"),
            ImageDifInPercent.ToString("N3"),
            ElapsedMilliseconds.ToString(),

            Coef.ToString("N2"),
            AreaThreshold.ToString(),
            DistanceThreshold.ToString(),

            MinArea.ToString("N2"),
            FirstQuartile.ToString("N2"),
            SecondQuartile.ToString("N2"),
            MeanArea.ToString("N2"),
            ThirdQuartile.ToString("N2"),
            MaxArea.ToString("N2"),


            (MinArea != 0 ? Math.Log(MinArea) : 0).ToString("N2"),
            (FirstQuartile != 0 ? Math.Log(FirstQuartile) : 0).ToString("N2"),
            (SecondQuartile != 0 ? Math.Log(SecondQuartile) : 0).ToString("N2"),
            (MeanArea != 0 ? Math.Log(MeanArea) : 0).ToString("N2"),
            (ThirdQuartile != 0 ? Math.Log(ThirdQuartile) : 0).ToString("N2"),
            (MaxArea != 0 ? Math.Log(MaxArea) : 0).ToString("N2"),
        };

        return string.Join("\t", headers);
    }
}
