using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Spatial.GeoJsonFormat;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Helpers;

using Point = IRI.Maptor.Core.Common.Primitives.Point;
using static IRI.Maptor.Res.LRSimplification.Common.SingleFeatureVaryingThresholdExperiment;

namespace IRI.Maptor.Res.LRSimplification.Common;

/// <summary>
/// Data of the resubmission's Figure 7 (2026-10-07): the three hand-picked features of the earlier Figure 7
/// (<c>DataWm\samples</c>, single geometries) at the LoDs of that figure, simplified by LRSv4 and by RDP at 1 px, measured as
/// in the resubmission runs (exact canvas with a 2-px margin, <see cref="SingleFeatureVaryingThresholdExperiment.MetricCanvas"/>).
/// Writes the original and both results as GeoJSON (WGS84) and <c>Figure7-measures.tsv</c>.
/// </summary>
public static class Figure7SamplesExperiment
{
    public const string SampleFolder = @"E:\University.Ph.D\4. Paper2\DataWm\samples";

    public static readonly (string Name, string File, int Zoom)[] Samples =
    {
        ("a", "Landuse_WM-11_featureIndex 2_rank 1.json", 11),
        ("b", "Waterways_WM-11_featureIndex 1_rank 1.json", 11),
        ("c", "Landuse_WM-12_featureIndex 1_rank 1.json", 12),
    };

    public static async Task RunAsync(string folder)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        Directory.CreateDirectory(folder);

        var lrs = LRHelper.GetLRAModels().Single(m => m.Title == "LR4v23");

        var report = new StringBuilder("Panel\tFile\tZoom\tMethod\tPointsOriginal\tPointsSimplified\tCompression\tExcessDisplacement\tFractionBeyond2\tOmissionBeyond2\r\n");

        foreach (var (name, file, zoom) in Samples)
        {
            var feature = GeoJson.LoadFromFile(Path.Combine(SampleFolder, file)).Single().Geometry!.TransformToWeMercator();

            feature.Srid = SridHelper.WebMercator;
            feature.RemoveConsecutiveDuplicatePoints();

            var pixel = WebMercatorUtility.ToWebMercatorLength(zoom, 1);
            var (box, size) = MetricCanvas(feature.GetBoundingBox(), pixel, ResubmissionExperiments.MetricMarginPixels);

            var original = SimplificationHelper.RenderForMetric(new() { feature }, box, size);
            var toScreen = Utility.CreateMapToScreenMapFunc(box, size.Width, size.Height);

            await feature.AsGeoJsonFeatureSet().SaveAsync(Path.Combine(folder, $"{name}-original.json"), false, true);

            var methods = new (string Method, Func<Geometry<Point>, Geometry<Point>> Simplify)[]
            {
                ("LRS", g => g.Simplify(lrs, toScreen, LRHelper.Retain3Points)),
                ("RDP", g => g.Simplify(SimplificationType.RamerDouglasPeucker, CreateParameters(pixel))),
            };

            foreach (var (method, simplify) in methods)
            {
                var (accuracy, simplified) = await SimplificationHelper.CompareBySingleFeature(feature, simplify, box, size, original, name, method, zoom, new Stopwatch(), false, measureCollapsed: true);

                await simplified.AsGeoJsonFeatureSet().SaveAsync(Path.Combine(folder, $"{name}-{method}.json"), false, true);

                var r = accuracy.Raster;

                report.Append($"{name}\t{file}\t{zoom}\t{method}\t{feature.TotalNumberOfPoints}\t{simplified.TotalNumberOfPoints}\t{accuracy.Compression:0.0000}\t{r.ExcessDisplacement:0.0000}\t{r.FractionBeyond2:0.0000}\t{r.OmissionBeyond2:0.0000}\r\n");
            }

            original.Dispose();
        }

        File.WriteAllText(Path.Combine(folder, "Figure7-measures.tsv"), report.ToString());
    }
}
