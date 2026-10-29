using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Drawing.Imaging;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Helpers;

using Point = IRI.Maptor.Core.Common.Primitives.Point;
using static IRI.Maptor.Res.LRSimplification.Common.SingleFeatureVaryingThresholdExperiment;

namespace IRI.Maptor.Res.LRSimplification.Common;

/// <summary>
/// Data of the resubmission's Appendix A (2026-10-07): the seven features of the earlier appendix (from the 348 published
/// features) at the zoom level where each fits a 128² screen, simplified by LRS (resubmission traversal) and by RDP, BOPW,
/// NOPW, RW (and CTR since 2026-10-09) at 1 px, on the exact canvas of the resubmission runs. Writes the metric rasters (original and every result) as
/// PNG and <c>Appendix-measures.tsv</c>; `Assets\figures\figA_appendix.py` colours the pixels and checks E against this file.
/// </summary>
public static class AppendixSamplesExperiment
{
    public static readonly (string Layer, int Index)[] Samples =
    {
        ("Building_WM", 30), ("Building_WM", 39), ("Building_WM", 47), ("Landuse_WM", 8), ("Landuse_WM", 14),
        ("Waterways_WM", 31), ("Waterways_WM", 39),
    };

    /// <summary>
    /// The two thin-part polygons of the resubmission's Figures 9 and 10 (author's choice, 2026-10-07, from the candidates where
    /// both BOPW and RW drop a part at 32 or 16 px while LRS does not, polygons with a body): Landuse F6977701 (a round body
    /// with two thin arms) and Water F957100 (a lake with a long thin channel), at the 16² screen.
    /// </summary>
    public static readonly (string Layer, int Index)[] ThinPartExamples = { ("Landuse_WM", 6977701), ("Water_WM", 957100) };

    /// <summary>
    /// The two polygons of the 2024 manuscript's Figures 9 and 10 (author's request, 2026-10-08): AdminArea F1507 (LoD 11) and
    /// Water F2 (LoD 12, featureIndex 2 of the 2024 FindGoodFeatures sample), copied to <see cref="OriginalFigures910Folder"/>.
    /// Both LoDs are the natural scale of a 256² screen.
    /// </summary>
    public static readonly (string Layer, int Index)[] OriginalFigures910 = { ("AdminArea_WM", 1507), ("Water_WM", 2) };

    public const string OriginalFigures910Folder = @"E:\University.Ph.D\4. Paper2\DataWm\OriginalFigures9-10-2026-10-08";

    public static Task RunAsync(string folder) => RunAsync(folder, LRHelper.PublishedRunFolder, Samples, 128, "Appendix");

    /// <summary>Data for the redraw of the original Figures 9 and 10: <see cref="OriginalFigures910"/> at the 256² screen.</summary>
    public static Task RunOriginalFigures910Async(string folder) => RunAsync(folder, OriginalFigures910Folder, OriginalFigures910, 256, "Figure9-10-original");

    /// <summary>Data of Figures 9 and 10: <see cref="ThinPartExamples"/> at the 16² screen, with the simplified geometries.</summary>
    public static Task RunThinPartExamplesAsync(string folder) => RunAsync(folder, ResubmissionExperiments.ThinPartsFolder, ThinPartExamples, 16, "Figure9-10");

    private static async Task RunAsync(string folder, string featureFolder, (string Layer, int Index)[] samples, int screen, string name)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        Directory.CreateDirectory(folder);

        var lrs = ResubmissionExperiments.ResubmissionLrs();
        var layers = PublishedFeaturesRevisionExperiment.LoadPublishedFeatures(featureFolder);

        var report = new StringBuilder("Layer\tIndex\tZoom\tMethod\tPointsOriginal\tPointsSimplified\tCompression\tExcessDisplacement\tOmissionBeyond2\r\n");

        foreach (var (layer, index) in samples)
        {
            var feature = layers[layer].Single(f => f.Index == index).Feature;

            var box = feature.GetBoundingBox();
            var zoom = WebMercatorUtility.EstimateZoomLevel(box, screen, screen);
            var pixel = WebMercatorUtility.ToWebMercatorLength(zoom, 1);
            var (extent, size) = MetricCanvas(box, pixel, ResubmissionExperiments.MetricMarginPixels);

            using var original = SimplificationHelper.RenderForMetric(new() { feature }, extent, size);
            original.Save(Path.Combine(folder, $"{layer}-F{index}-original.png"), ImageFormat.Png);

            await feature.AsGeoJsonFeatureSet().SaveAsync(Path.Combine(folder, $"{layer}-F{index}-original.json"), false, true);

            var toScreen = Utility.CreateMapToScreenMapFunc(extent, size.Width, size.Height);

            var methods = new (string Name, Func<Geometry<Point>, Geometry<Point>> Simplify)[]
            {
                ("LRS", g => g.Simplify(lrs, toScreen, LRHelper.Retain3Points)),
                ("RDP", g => g.Simplify(SimplificationType.RamerDouglasPeucker, CreateParameters(pixel))),
                ("BOPW", g => g.Simplify(SimplificationType.BeforeOpeningWindow, CreateParameters(pixel))),
                ("NOPW", g => g.Simplify(SimplificationType.NormalOpeningWindow, CreateParameters(pixel))),
                ("RW", g => g.Simplify(SimplificationType.ReumannWitkam, CreateParameters(pixel))),
                // CTR added 2026-10-09 (author's decision to include it as a fifth baseline); area threshold = (1 px)^2 as in E16
                ("CTR", g => g.Simplify(ResubmissionExperiments.CtrType, CreateParameters(pixel))),
            };

            foreach (var (method, simplify) in methods)
            {
                var (accuracy, simplified) = await SimplificationHelper.CompareBySingleFeature(feature, simplify, extent, size, original, layer, method, zoom, new Stopwatch(), false, measureCollapsed: true);

                using var bitmap = SimplificationHelper.RenderForMetric(new() { simplified }, extent, size);
                bitmap.Save(Path.Combine(folder, $"{layer}-F{index}-{method}.png"), ImageFormat.Png);

                await simplified.AsGeoJsonFeatureSet().SaveAsync(Path.Combine(folder, $"{layer}-F{index}-{method}.json"), false, true);

                report.Append($"{layer}\t{index}\t{zoom}\t{method}\t{feature.TotalNumberOfPoints}\t{simplified.TotalNumberOfPoints}\t{accuracy.Compression:0.0000}\t{accuracy.Raster.ExcessDisplacement:0.000000}\t{accuracy.Raster.OmissionBeyond2:0.0000}\r\n");
            }
        }

        File.WriteAllText(Path.Combine(folder, $"{name}-measures.tsv"), report.ToString());
    }
}
