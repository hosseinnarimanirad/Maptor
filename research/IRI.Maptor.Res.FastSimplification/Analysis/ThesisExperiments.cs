using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Media;

using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Spatial.GeoJsonFormat;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.SpatialReferenceSystem;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Res.FastSimplification;

/// <summary>
/// The re-run of the ISPRS 2025 experiments for the thesis (author's decision 2026-10-26): CTR is run as the paper's
/// pseudo-code describes it (<see cref="SimplificationType.CumulativeTriangleRoutine2"/>), the other methods with the
/// current code. The published run (<see cref="SimplificationHelper"/>) is left as it is.
/// <list type="bullet">
/// <item><see cref="VisualAsync"/>: the protocol of <see cref="SimplificationHelper.ProcessVisualQuality"/> (same features
///       and indices, 64/128/256 px, 4-px threshold), one log row per feature, size and method instead of one PNG; the
///       features of the paper's Figures 5–8 are also written as GeoJSON and PNG.</item>
/// <item><see cref="Timing"/>: execution time against the number of points, which the published run did not keep.</item>
/// </list>
/// </summary>
public static class ThesisExperiments
{
    public const string OutputRoot = @"E:\University.Ph.D\5. ISPRS Conf Paper\AlgorithmRun";

    private const string DataFolder = @"E:\University.Ph.D\Sample Data\OSM-1401-03-03-WebMercator";

    private const double ThresholdInPixels = 4;

    private static readonly List<SimplificationType> VisualMethods = new()
    {
        SimplificationType.RamerDouglasPeucker,
        SimplificationType.VisvalingamWhyatt,
        SimplificationType.NormalOpeningWindow,
        SimplificationType.CumulativeTriangleRoutine2,
    };

    private static readonly List<int> RenderSizes = new() { 64, 128, 256 };

    /// <summary>Features of the ISPRS figures (layer, published index): Figure 5, 6, 7 and 8a–d.</summary>
    public static readonly (string Layer, int Index)[] FigureFeatures =
    {
        ("Building_WM", 11), ("Road_WM", 9938), ("AdminArea_WM", 662),
        ("AdminArea_WM", 455), ("AdminArea_WM", 438), ("AdminArea_WM", 53), ("AdminArea_WM", 443),
    };

    public static async Task VisualAsync(string writeFolder)
    {
        Directory.CreateDirectory(writeFolder);

        var figureFolder = Path.Combine(writeFolder, "FigureFeatures");

        Directory.CreateDirectory(figureFolder);

        File.WriteAllText(Path.Combine(writeFolder, "protocol.txt"),
            $"data: {DataFolder}\r\nfeatures: single parts with 40-500 points (indices as in 2025-05-25-fix colors)\r\n" +
            $"render sizes: {string.Join(", ", RenderSizes)}; bounding box x 1.1; threshold {ThresholdInPixels} px at the zoom level " +
            $"(squared for VW and CTR)\r\nmethods: {string.Join(", ", VisualMethods)}\r\n");

        using var writer = new StreamWriter(Path.Combine(writeFolder, "All-summary.txt"), false);

        writer.WriteLine("Layer\tFeatureIndex\tNumberOfPoints\tRenderSize\tZoomLevel\tScale\tMethod\tNumberOfPoints_Simplified\tCompression\tTlvdPerLength\tCollapsed");

        foreach (var file in Directory.EnumerateFiles(DataFolder, "*.shp"))
        {
            var layer = Path.GetFileNameWithoutExtension(file);

            var features = SimplificationHelper.LoadFeatures(file);

            for (int i = 0; i < features.Count; i++)
            {
                var index = i + 1;

                var feature = features[i];

                var isFigureFeature = FigureFeatures.Contains((layer, index));

                if (isFigureFeature)
                    await feature.AsGeoJsonFeatureSet().SaveAsync(Path.Combine(figureFolder, $"{layer}-F{index}-original.json"), false, true);

                var boundingBox = feature.GetBoundingBox().Expand(1.1);

                foreach (var renderSize in RenderSizes)
                {
                    var zoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, renderSize, renderSize);

                    var threshold = WebMercatorUtility.ToWebMercatorLength(zoomLevel, ThresholdInPixels);

                    var screenSize = WebMercatorUtility.ToScreenSize(zoomLevel, boundingBox);

                    if (screenSize.Width * screenSize.Height <= 0)
                        continue;

                    var scale = Math.Floor((1.0 / WebMercatorUtility.GetGoogleMapScale(zoomLevel)) / 1000.0) * 1000;

                    var parameters = new SimplificationParameters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = false };

                    foreach (var method in VisualMethods)
                    {
                        var simplified = feature.Simplify(method, parameters);

                        // the published run skipped these (no PNG); here they are logged
                        var collapsed = simplified.IsNullOrEmpty() || simplified.Type == GeometryType.Point;

                        var simplifiedPoints = collapsed ? 0 : simplified.TotalNumberOfPoints;

                        var compression = collapsed ? double.NaN : feature.Compression(simplified);

                        // TLVD needs the same parts and rings in both geometries; a ring that collapsed inside a
                        // polygon changes that, so the row is kept with TLVD = NaN and Collapsed = 2
                        var sameStructure = collapsed || SameStructure(feature, simplified);

                        var tlvd = collapsed || !sameStructure ? double.NaN : feature.CalculateTotalVectorDisplacementPerLength(simplified);

                        writer.WriteLine(string.Join("\t", layer, index, feature.TotalNumberOfPoints, renderSize, zoomLevel,
                            scale.ToString(CultureInfo.InvariantCulture), method.GetDescription(), simplifiedPoints,
                            compression.ToString("0.#####", CultureInfo.InvariantCulture), tlvd.ToString("0.#####", CultureInfo.InvariantCulture), collapsed ? 1 : sameStructure ? 0 : 2));

                        if (isFigureFeature && !collapsed)
                        {
                            var name = $"{layer}-F{index}-{zoomLevel}-{renderSize}-{method.GetDescription()}";

                            await simplified.AsGeoJsonFeatureSet().SaveAsync(Path.Combine(figureFolder, $"{name}.json"), false, true);

                            var originalFrame = feature.AsDrawingVisual(VisualParameters.Get(Colors.Transparent, ColorHelper.ToWpfColor("#DE36A1"), 2, 1), screenSize.Width, screenSize.Height, boundingBox);
                            var simplifiedFrame = simplified.AsDrawingVisual(VisualParameters.Get(Colors.Transparent, ColorHelper.ToWpfColor("#08686E"), 2, 1), screenSize.Width, screenSize.Height, boundingBox);

                            ImageUtility.MergeAndSave(Path.Combine(figureFolder, $"{name}-{scale:N0}-{Math.Round(compression * 100)}%.png"), [originalFrame!, simplifiedFrame!], screenSize.Width, screenSize.Height);
                        }
                    }
                }
            }

            writer.Flush();
        }
    }

    // HasTheSameSignature compares one level only; a ring that collapsed to a point inside a polygon passes it
    private static bool SameStructure(Geometry<Point> a, Geometry<Point> b)
    {
        if (!a.HasTheSameSignature(b))
            return false;

        if (a.Geometries.IsNullOrEmpty())
            return true;

        return a.Geometries.Zip(b.Geometries!).All(pair => SameStructure(pair.First, pair.Second));
    }

    // ---------------------------------------------------------------------------------------------------------
    // timing
    // ---------------------------------------------------------------------------------------------------------

    private static readonly List<SimplificationType> TimingMethods = new()
    {
        SimplificationType.RamerDouglasPeucker,
        SimplificationType.VisvalingamWhyatt,
        SimplificationType.NormalOpeningWindow,
        SimplificationType.BeforeOpeningWindow,
        SimplificationType.CumulativeTriangleRoutine2,
    };

    /// <summary>Scale denominators of the paper's Figures 9 and 10.</summary>
    private static readonly double[] TimingScales = { 2_000_000, 1_000_000 };

    /// <summary>Nested samples: the first features of the shuffled list up to these totals of points.</summary>
    private static readonly int[] TimingSizes = { 10_000, 100_000, 1_000_000, 3_500_000, 7_000_000 };

    private const int TimingRepetitions = 5;

    public const int Seed = 2026;

    /// <summary>
    /// Single parts of all seven layers (consecutive duplicates removed, at least three points) in a fixed random order
    /// (<see cref="Seed"/>); nested samples are the first features up to <see cref="TimingSizes"/> points. For each scale the
    /// threshold is 4 px at that scale on the equator, in Web Mercator units (squared for the area-based VW and CTR). Each
    /// method simplifies the whole sample once as a warm-up and then <see cref="TimingRepetitions"/> times; every repetition
    /// is logged, the median is the reported value. Single-threaded; run from a Release build.
    /// </summary>
    public static void Timing(string writeFolder)
    {
        Directory.CreateDirectory(writeFolder);

        var all = new List<Geometry<Point>>();

        foreach (var file in Directory.EnumerateFiles(DataFolder, "*.shp"))
        {
            foreach (var g in IRI.Maptor.Core.ShapefileFormat.Shapefile.ReadShapes(file).Select(s => s.AsGeometry()).SelectMany(g => g.NumberOfGeometries > 1 ? g.Split(false) : [g]))
            {
                if (g.IsNullOrEmpty())
                    continue;

                g.Srid = SridHelper.WebMercator;
                g.RemoveConsecutiveDuplicatePoints();
                SimplificationHelper.RestorePublishedRingOrientation(g);

                if (g.TotalNumberOfPoints >= 3)
                    all.Add(g);
            }
        }

        var random = new Random(Seed);

        var shuffled = all.OrderBy(_ => random.Next()).ToList();

        File.WriteAllText(Path.Combine(writeFolder, "protocol.txt"),
            $"data: {DataFolder} (all seven layers, single parts, >= 3 points: {all.Count} features, {all.Sum(f => (long)f.TotalNumberOfPoints)} points)\r\n" +
            $"order: shuffled with seed {Seed}; nested samples up to {string.Join(", ", TimingSizes)} points\r\n" +
            $"scales: 1:{string.Join(", 1:", TimingScales)}; threshold {ThresholdInPixels} px at the scale (equator), squared for VW and CTR\r\n" +
            $"methods: {string.Join(", ", TimingMethods)}; warm-up + {TimingRepetitions} repetitions; {(Debugger.IsAttached ? "DEBUGGER ATTACHED" : "no debugger")}\r\n" +
            $"machine: {Environment.MachineName}, {Environment.ProcessorCount} logical processors, {Environment.OSVersion}\r\n");

        using var writer = new StreamWriter(Path.Combine(writeFolder, "Timing.txt"), false);

        writer.WriteLine("Scale\tMethod\tTargetPoints\tNumberOfFeatures\tNumberOfPoints\tRepetition\tElapsedMilliseconds\tNumberOfPoints_Simplified");

        foreach (var target in TimingSizes)
        {
            var sample = new List<Geometry<Point>>();

            long points = 0;

            foreach (var f in shuffled)
            {
                if (points >= target)
                    break;

                sample.Add(f);
                points += f.TotalNumberOfPoints;
            }

            foreach (var scale in TimingScales)
            {
                // ground resolution of one pixel at this scale = Web Mercator units per pixel on the equator
                var threshold = ThresholdInPixels * WebMercatorUtility.CalculateGroundResolution(1.0 / scale);

                var parameters = new SimplificationParameters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = false };

                foreach (var method in TimingMethods)
                {
                    long simplifiedPoints = 0;

                    for (int repetition = 0; repetition <= TimingRepetitions; repetition++)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();

                        long count = 0;

                        var watch = Stopwatch.StartNew();

                        foreach (var feature in sample)
                        {
                            var simplified = feature.Simplify(method, parameters);

                            count += simplified.IsNullOrEmpty() ? 0 : simplified.TotalNumberOfPoints;
                        }

                        watch.Stop();

                        simplifiedPoints = count;

                        // repetition 0 is the warm-up and is not logged
                        if (repetition > 0)
                        {
                            writer.WriteLine(string.Join("\t", scale.ToString(CultureInfo.InvariantCulture), method.GetDescription(), target, sample.Count, points, repetition,
                                watch.Elapsed.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture), simplifiedPoints));
                        }
                    }

                    writer.Flush();
                }
            }
        }
    }
}
