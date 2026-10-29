using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Infrastructure.GdiPlus.Helpers;
using IRI.Maptor.Core.MachineLearning;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Spatial.GeoJsonFormat;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.ViewModels.Map;
using IRI.Maptor.Presentation.Core;


namespace IRI.Maptor.Presentation.Wpf.Helpers;

public static class SimplificationHelper
{
    /// <summary>
    /// How the original and the simplified geometries are drawn for the visual metric. The default is the
    /// rendering of the published LAS run (aliased 1-pixel pen); the map renderer is not used, so the metric does
    /// not change when map styling changes.
    /// </summary>
    public static MetricRenderOptions RenderOptions { get; set; } = MetricRenderOptions.Published;

    /// <summary>Draws <paramref name="geometries"/> for the visual metric with <see cref="RenderOptions"/>.</summary>
    public static System.Drawing.Bitmap RenderForMetric(List<Geometry<Point>> geometries, BoundingBox boundingBox, System.Drawing.Size screenSize)
    {
        return MetricRasterizer.Render(geometries, boundingBox, screenSize.Width, screenSize.Height, RenderOptions);
    }

    public static async Task<List<SimplificationAccuracy>> Compare(
        Geometry<Point> feature,
        int featureIndex,
        string outputDirectory,
        int estimatedZoomLevel,
        string featureName,
        List<SimplificationType> methods,
        List<LogisticSimplification<Point>> lrModels)
    {
        Dictionary<SimplificationType, string> methodNames = methods.ToDictionary(m => m, m => m.GetDescription());

        var boundingBox = feature.GetBoundingBox();

        var webMercatorResolution = WebMercatorUtility.ToWebMercatorLength(estimatedZoomLevel, 1);

        var currentScreenSize = WebMercatorUtility.ToScreenSize(estimatedZoomLevel, boundingBox);

        var layerName = $"{outputDirectory}\\{featureName}-{estimatedZoomLevel}-{featureIndex}-";

        var originalBitmap = RenderForMetric(new() { feature }, boundingBox, currentScreenSize);

        originalBitmap.Save($"{layerName}-original.png", System.Drawing.Imaging.ImageFormat.Png);

        var toScreenMap = Utility.CreateMapToScreenMapFunc(boundingBox, currentScreenSize.Width, currentScreenSize.Height);

        var threshold = webMercatorResolution   /** coef*/;

        var parameters = new SimplificationParameters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = true };

        var length_Original = feature.GetEuclideanLength();

        GeoJsonFeatureSet originalFeatureSet = feature.AsGeoJsonFeatureSet();

        await originalFeatureSet.SaveAsync($"{layerName}-original.json", false, true);

        if (length_Original == 0)
            return new List<SimplificationAccuracy>();

        var result = new List<SimplificationAccuracy>();

        foreach (var lrModel in lrModels)
        {
            var simplified = feature.Simplify(lrModel, toScreenMap, true);

            if (simplified.Type == GeometryType.Point)
                continue;

            var simplifiedBitmap = RenderForMetric(new List<Geometry<Point>>() { simplified }, boundingBox, currentScreenSize);

            var diff = ImageHelper.CalculateConfusionMatrixBitmaps(originalBitmap, simplifiedBitmap);

            simplifiedBitmap.Save($"{layerName}-{lrModel.Title}.png", System.Drawing.Imaging.ImageFormat.Tiff);

            diff.image.Save($"{layerName}-{lrModel.Title}-diff.png", System.Drawing.Imaging.ImageFormat.Tiff);

            var tlvdPerLength = feature.CalculateTotalVectorDisplacement(simplified) / length_Original;

            var compression = feature.Compression(simplified);

            result.Add(new SimplificationAccuracy()
            {
                FeatureIndex = featureIndex,
                Zoomlevel = estimatedZoomLevel,
                Algorithm = lrModel.Title,
                Compression = compression,
                ConfusionMatrix = diff.confusionMatrix,
                TlvdPerLength = tlvdPerLength
            });
        }

        foreach (var method in methods)
        {
            var simplified = feature.Simplify(method, parameters);

            if (simplified.Type == GeometryType.Point)
                continue;

            var simplifiedBitmap = RenderForMetric(new List<Geometry<Point>>() { simplified }, boundingBox, currentScreenSize);

            var diff = ImageHelper.CalculateConfusionMatrixBitmaps(originalBitmap, simplifiedBitmap);

            simplifiedBitmap.Save($"{layerName}-{methodNames[method]}.png", System.Drawing.Imaging.ImageFormat.Tiff);

            diff.image.Save($"{layerName}-{methodNames[method]}-diff.png", System.Drawing.Imaging.ImageFormat.Tiff);

            var tlvdPerLength = feature.CalculateTotalVectorDisplacement(simplified) / length_Original;

            var compression = feature.Compression(simplified);

            result.Add(new SimplificationAccuracy()
            {
                FeatureIndex = featureIndex,
                Zoomlevel = estimatedZoomLevel,
                Algorithm = methodNames[method],
                Compression = compression,
                ConfusionMatrix = diff.confusionMatrix,
                TlvdPerLength = tlvdPerLength
            });
        }


        StringBuilder builder = new StringBuilder();

        builder.AppendLine(SimplificationAccuracy.GetTsvHeader());

        foreach (var accuracy in result)
        {
            builder.AppendLine(accuracy.ToTsv());
        }

        File.WriteAllText($"{layerName}-log.txt", builder.ToString());

        return result;
    }
     
    /// <summary>Buffer half-width of the tolerant pixel measures (<see cref="RasterComparison"/>): one pixel.</summary>
    public const int RasterTolerancePixels = 1;

    /// <summary>
    /// Simplifies one feature, renders it on the screen of the original and measures it against the
    /// original raster: exact confusion matrix and F1, tolerant F1, pixel displacement, Hausdorff distance,
    /// compression and TLVD per length. The original is expected to have been rendered alone (no neighbours).
    /// </summary>
    /// <param name="stopwatch">When given, the simplification alone is timed and the milliseconds are recorded.</param>
    /// <param name="saveFiles">Write the original as GeoJSON and the simplified and difference rasters as images.</param>
    public static async Task<(SimplificationAccuracy, Geometry<Point> simplified)> CompareBySingleFeature(
       Geometry<Point> feature,
       Func<Geometry<Point>, Geometry<Point>> simplifyFunc,
       BoundingBox boundingBox,
       System.Drawing.Size currentScreenSize,
       System.Drawing.Bitmap originalBitmap,
       string layerName,
       string methodName,
       int estimatedZoomLevel,
       System.Diagnostics.Stopwatch stopwatch = null,
       bool saveFiles = true,
       bool measureCollapsed = false)
    {
        stopwatch?.Restart();

        var simplified = simplifyFunc(feature);

        stopwatch?.Stop();

        // a result of another geometry type (e.g. a polygon whose ring collapsed to a line) used to be skipped, which
        // hides a visible failure; measureCollapsed (2026-10-06, resubmission sweeps) measures it like any other result;
        // the published drivers keep the skip so that their numbers reproduce
        if (simplified.Type != feature.Type && !measureCollapsed)
            return (null, null);

        var length = feature.GetEuclideanLength();

        // 2026-10-06: a part or ring that collapses (e.g. RDP on a ring at a threshold larger than the feature) changes
        // the structure; TLVD is undefined then (it used to throw and stop the run); the raster measures still apply
        double tlvdPerLength;

        try
        {
            tlvdPerLength = feature.CalculateTotalVectorDisplacement(simplified) / length;
        }
        catch (NotImplementedException)
        {
            // the structures differ (at any level: parts or rings)
            tlvdPerLength = double.NaN;
        }
        var compression = feature.Compression(simplified);

        GeoJsonFeatureSet originalFeatureSet = feature.AsGeoJsonFeatureSet();

        if (saveFiles)
            await originalFeatureSet.SaveAsync($"{layerName}-original.json", false, true);

        var simplifiedBitmap = RenderForMetric(new List<Geometry<Point>>() { simplified }, boundingBox, currentScreenSize);

        var diff = ImageHelper.CalculateConfusionMatrixBitmaps(originalBitmap, simplifiedBitmap);

        if (saveFiles)
        {
            simplifiedBitmap.Save($"{layerName}-{methodName}.png", System.Drawing.Imaging.ImageFormat.Tiff);

            diff.image.Save($"{layerName}-{methodName}-diff.png", System.Drawing.Imaging.ImageFormat.Tiff);
        }

        var raster = ImageHelper.CompareRasters(originalBitmap, simplifiedBitmap, RasterTolerancePixels);

        var info = new SimplificationAccuracy()
        {
            FeatureIndex = 0,
            Zoomlevel = estimatedZoomLevel,
            Algorithm = methodName,
            Compression = compression,
            ConfusionMatrix = diff.confusionMatrix,
            Raster = raster,
            TlvdPerLength = tlvdPerLength,
            ElapsedMilliseconds = stopwatch?.Elapsed.TotalMilliseconds ?? -1,
        };

        return (info, simplified);
    }


    public static async Task<(SimplificationAccuracy, List<Geometry<Point>> simplifieds)> CompareByLayer(
       List<Geometry<Point>> features,
       Func<Geometry<Point>, Geometry<Point>> simplifyFunc,
       BoundingBox boundingBox,
       System.Drawing.Size currentScreenSize,
       System.Drawing.Bitmap originalBitmap,
       string layerName,
       string methodName,
       int estimatedZoomLevel)
    {
        List<double> tlvdPerLengths = new List<double>();
        List<double> compressions = new List<double>();
        List<double> lengths = new List<double>();

        var simplifieds = new List<Geometry<Point>>();

        foreach (var feature in features)
        {
            var simplified = simplifyFunc(feature);

            if (simplified.Type == GeometryType.Point)
                continue;

            var length = feature.GetEuclideanLength();

            lengths.Add(length);
            tlvdPerLengths.Add(feature.CalculateTotalVectorDisplacement(simplified) / length);
            compressions.Add(feature.Compression(simplified));
            simplifieds.Add(simplified);
        }

        var simplifiedBitmap = RenderForMetric(simplifieds, boundingBox, currentScreenSize);

        var diff = ImageHelper.CalculateConfusionMatrixBitmaps(originalBitmap, simplifiedBitmap);

        simplifiedBitmap.Save($"{layerName}-{methodName}.png", System.Drawing.Imaging.ImageFormat.Tiff);

        diff.image.Save($"{layerName}-{methodName}-diff.png", System.Drawing.Imaging.ImageFormat.Tiff);

        var info = new SimplificationAccuracy()
        {
            FeatureIndex = 0,
            Zoomlevel = estimatedZoomLevel,
            Algorithm = methodName,
            Compression = compressions.Average(),
            ConfusionMatrix = diff.confusionMatrix,
            TlvdPerLength = tlvdPerLengths.Average(),
        };

        return (info, simplifieds);
    }

}
