using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Common.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Res.LRSimplification;

/// <summary>
/// One row of an experiment summary: a feature (or a layer) simplified by one method at one threshold, with
/// compression, TLVD per length, the exact pixel confusion matrix and, since 2026-09-28, the distance-aware
/// raster measures of <see cref="RasterComparison"/> (blank for the layer-level experiments).
/// </summary>
public class VisualSimplificationLog
{
    // e.g. AdminArea_WM
    public string FileName { get; set; }

    public int FeatureIndex { get; set; }

    // e.g. AdminArea
    public string Type { get; set; }

    public int Level { get; set; }

    public double AreaThreshold { get; set; }

    public double DistanceThreshold { get; set; }

    public double Coef { get; set; }

    public string MethodName { get; set; }

    /// <summary>Simplification time in milliseconds; fractional since 2026-09-28 (the features take microseconds).</summary>
    public double ElapsedMilliseconds { get; set; }

    // Max(With, Height)
    public double MaxExtent { get; set; }

    public int NumberOfPoints_Original { get; set; }
    public int NumberOfPoints_Simplified { get; set; }
    // درصد تغییرات در تعداد نقاط
    public double PCInCoordinates { get; set; }

    public double Compression { get; set; }

    public double TlvdPerLength { get; set; }

    public ConfusionMatrix? ConfusionMatrix { get; set; }

    public RasterComparison? Raster { get; set; }

    /// <summary>Rings of the simplified polygon with fewer than three distinct vertices (<see cref="Common.TopologyCounts"/>).</summary>
    public int CollapsedRings { get; set; }

    /// <summary>Proper crossings between non-adjacent edges of the simplified geometry (<see cref="Common.TopologyCounts"/>).</summary>
    public int SelfIntersections { get; set; }


    public VisualSimplificationLog(
        string fileName,
        BoundingBox originalBoundingBox,
        List<Geometry<Point>> original,
        List<Geometry<Point>> simplified,
        int level,
        string methodName,
        double elapsedMilliseconds,
        double coef,
        SimplificationParameters parameters,
        double tlvdPerLength = 0,
        ConfusionMatrix? confusionMatrix = null)
    {
        this.FileName = fileName;

        this.Type = fileName.Split('_').First();

        this.Level = level;

        this.AreaThreshold = parameters.AreaThreshold ?? 0;

        this.DistanceThreshold = parameters.DistanceThreshold ?? 0;

        this.Coef = coef;

        this.MethodName = methodName;

        this.ElapsedMilliseconds = elapsedMilliseconds;

        this.MaxExtent = Math.Max(originalBoundingBox.Width, originalBoundingBox.Height);

        this.NumberOfPoints_Original = original.Sum(o => o.TotalNumberOfPoints);
        this.NumberOfPoints_Simplified = simplified.Sum(o => o.TotalNumberOfPoints);
        this.PCInCoordinates = NumberOfPoints_Simplified == 0 ? 1 : (double)NumberOfPoints_Simplified / NumberOfPoints_Original;
        this.Compression = 1 - PCInCoordinates;

        this.ConfusionMatrix = confusionMatrix;

        this.TlvdPerLength = tlvdPerLength;
    }

    public VisualSimplificationLog(
        string fileName,
        BoundingBox originalBoundingBox,
        int featureIndex,
        Geometry<Point> original,
        Geometry<Point> simplified,
        int level,
        string methodName,
        double elapsedMilliseconds,
        double coef,
        SimplificationParameters parameters,
        double tlvdPerLength = 0,
        ConfusionMatrix? confusionMatrix = null,
        RasterComparison? raster = null)
    {
        this.FileName = fileName;

        this.Type = fileName.Split('_').First();

        this.FeatureIndex = featureIndex;

        this.Level = level;

        this.AreaThreshold = parameters.AreaThreshold ?? 0;

        this.DistanceThreshold = parameters.DistanceThreshold ?? 0;

        this.Coef = coef;

        this.MethodName = methodName;

        this.ElapsedMilliseconds = elapsedMilliseconds;

        this.MaxExtent = Math.Max(originalBoundingBox.Width, originalBoundingBox.Height);

        this.NumberOfPoints_Original = original.TotalNumberOfPoints;
        this.NumberOfPoints_Simplified = simplified.TotalNumberOfPoints;
        this.PCInCoordinates = original.PercentageChangeInCoordinates(simplified);
        this.Compression = 1 - PCInCoordinates;

        this.ConfusionMatrix = confusionMatrix;

        this.Raster = raster;

        this.TlvdPerLength = tlvdPerLength;

        this.CollapsedRings = Common.TopologyCounts.CollapsedRings(simplified);

        this.SelfIntersections = Common.TopologyCounts.SelfIntersections(simplified);
    }

    public static string GetHeader()
    {
        var headers = new List<string>()
        {
            nameof(FileName),
            nameof(Type),
            nameof(FeatureIndex),
            nameof(Level),
            nameof(Coef),
            nameof(AreaThreshold),
            nameof(DistanceThreshold),
            nameof(MethodName),
            nameof(ElapsedMilliseconds),

            nameof(MaxExtent),

            nameof(NumberOfPoints_Original),
            nameof(NumberOfPoints_Simplified),
            nameof(Compression),
            nameof(TlvdPerLength),
        };

        return string.Join("\t", headers) + "\t" + ConfusionMatrix.GetTsvHeader() + "\t" + RasterComparison.GetTsvHeader() + "\tCollapsedRings\tSelfIntersections";
    }

    public string ToTsv()
    {
        var values = new List<string>()
        {
            FileName,
            Type,
            FeatureIndex.ToString(),
            Level.ToString(),
            Coef.ToString("N2"),
            AreaThreshold.ToString(),
            DistanceThreshold.ToString(),

            MethodName,
            ElapsedMilliseconds.ToString("0.###"),

            MaxExtent.ToString("N1"),

            NumberOfPoints_Original.ToString(),
            NumberOfPoints_Simplified.ToString(),
            Compression.ToString("N4"),
            TlvdPerLength.ToString("N4")
        };

        return string.Join("\t", values) + "\t" + ConfusionMatrix.AsTsv() + "\t" + (Raster?.AsTsv() ?? RasterComparison.EmptyTsv()) + $"\t{CollapsedRings}\t{SelfIntersections}";
    }
}
