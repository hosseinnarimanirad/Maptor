using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Ket.GdiPlus.Helpers;
using IRI.Maptor.Sta.MachineLearning;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;


namespace IRI.Maptor.Jab.Common.Helpers;

public static class SimplificationHelper
{
    private static VectorLayer GetAsLayer(string layerName, List<Geometry<Point>> geometries)
    {
        var vectorLayer = new VectorLayer(layerName,
                                            geometries,
                                            VisualParameters.GetStroke(Colors.Blue, 1),
                                            LayerType.VectorLayer,
                                            RenderMode.Default,
                                            RasterizationMethod.DrawingVisual);

        vectorLayer.Visibility = System.Windows.Visibility.Hidden;

        return vectorLayer;
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

        var scale = WebMercatorUtility.GetGoogleMapScale(estimatedZoomLevel);

        var originalVectorLayer = GetAsLayer("original", new() { feature });

        var layerName = $"{outputDirectory}\\{featureName}-{estimatedZoomLevel}-{featureIndex}-";

        var originalBitmap = await originalVectorLayer.AsGdiBitmapAsync(boundingBox, scale, currentScreenSize.Width, currentScreenSize.Height);

        originalBitmap.Save($"{layerName}-original.png", System.Drawing.Imaging.ImageFormat.Png);

        var toScreenMap = VectorLayer.CreateMapToScreenMapFunc(boundingBox, currentScreenSize.Width, currentScreenSize.Height);

        var threshold = webMercatorResolution   /** coef*/;

        var parameters = new SimplificationParamters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = true };

        var length_Original = feature.CalculateEuclideanLength();

        GeoJsonFeatureSet originalFeatureSet = feature.AsGeoJsonFeatureSet();

        originalFeatureSet.Save($"{layerName}-original.json", false, true);

        if (length_Original == 0)
            return new List<SimplificationAccuracy>();

        var result = new List<SimplificationAccuracy>();

        foreach (var lrModel in lrModels)
        {
            var simplified = feature.Simplify(lrModel, toScreenMap, true);

            if (simplified.Type == GeometryType.Point)
                continue;

            var vectorLayer = GetAsLayer($"{estimatedZoomLevel}-{lrModel.Title}", new List<Geometry<Point>>() { simplified });

            var simplifiedBitmap = await vectorLayer.AsGdiBitmapAsync(boundingBox, scale, currentScreenSize.Width, currentScreenSize.Height);

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

            var vectorLayer = GetAsLayer($"{estimatedZoomLevel}-{methodNames[method]}", new List<Geometry<Point>>() { simplified });

            var simplifiedBitmap = await vectorLayer.AsGdiBitmapAsync(boundingBox, scale, currentScreenSize.Width, currentScreenSize.Height);

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

    //public static async Task<List<SimplificationAccuracy>> Compare(
    //    List<Geometry<Point>> features,
    //    string outputDirectory,
    //    int estimatedZoomLevel,
    //    string featureName,
    //    List<SimplificationType> methods,
    //    List<LogisticSimplification<Point>> lrModels)
    //{
    //    Dictionary<SimplificationType, string> methodNames = methods.ToDictionary(m => m, m => m.GetDescription());

    //    var boundingBox = features.GetBoundingBox();

    //    var webMercatorResolution = WebMercatorUtility.ToWebMercatorLength(estimatedZoomLevel, 1);

    //    var currentScreenSize = WebMercatorUtility.ToScreenSize(estimatedZoomLevel, boundingBox);

    //    var scale = WebMercatorUtility.GetGoogleMapScale(estimatedZoomLevel);

    //    var originalVectorLayer = GetAsLayer("original", features);

    //    var layerName = $"{outputDirectory}\\{featureName}-{estimatedZoomLevel}-";

    //    var originalBitmap = await originalVectorLayer.ParseToBitmapImage(boundingBox, currentScreenSize.Width, currentScreenSize.Height, scale);

    //    originalBitmap.Save($"{layerName}-original.png", System.Drawing.Imaging.ImageFormat.Png);

    //    var toScreenMap = VectorLayer.CreateMapToScreenMapFunc(boundingBox, currentScreenSize.Width, currentScreenSize.Height);

    //    var threshold = webMercatorResolution   /** coef*/;

    //    var parameters = new SimplificationParamters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = true };

    //    var length_Original = features.Sum(f => f.CalculateEuclideanLength());

    //    if (length_Original == 0)
    //        return new List<SimplificationAccuracy>();

    //    var result = new List<SimplificationAccuracy>();

    //    foreach (var lrModel in lrModels)
    //    {
    //        List<double> tlvdPerLengths = new List<double>();
    //        List<double> compressions = new List<double>();
    //        List<double> lengths = new List<double>();

    //        var simplifieds = new List<Geometry<Point>>();

    //        foreach (var feature in features)
    //        {
    //            var simplified = feature.Simplify(lrModel, toScreenMap, true);

    //            if (simplified.Type == GeometryType.Point)
    //                continue;

    //            var length = feature.CalculateEuclideanLength();

    //            lengths.Add(length);
    //            tlvdPerLengths.Add(feature.CalculateTotalVectorDisplacement(simplified) / length);
    //            compressions.Add(feature.Compression(simplified));
    //            simplifieds.Add(simplified);
    //        }

    //        var vectorLayer = GetAsLayer($"{estimatedZoomLevel}-{lrModel.Title}", simplifieds);

    //        var simplifiedBitmap = await vectorLayer.ParseToBitmapImage(boundingBox, currentScreenSize.Width, currentScreenSize.Height, scale);

    //        var diff = ImageHelper.CalculateConfusionMatrixBitmaps(originalBitmap, simplifiedBitmap);

    //        simplifiedBitmap.Save($"{layerName}-{lrModel.Title}.png", System.Drawing.Imaging.ImageFormat.Tiff);

    //        diff.image.Save($"{layerName}-{lrModel.Title}-diff.png", System.Drawing.Imaging.ImageFormat.Tiff);

    //        result.Add(new SimplificationAccuracy()
    //        {
    //            FeatureIndex = 0,
    //            Zoomlevel = estimatedZoomLevel,
    //            Algorithm = lrModel.Title,
    //            Compression = compressions.Average(),
    //            ConfusionMatrix = diff.confusionMatrix,
    //            TlvdPerLength = tlvdPerLengths.Average(),
    //        });

    //    }

    //    foreach (var method in methods)
    //    {
    //        List<double> tlvdPerLengths = new List<double>();
    //        List<double> compressions = new List<double>();
    //        List<double> lengths = new List<double>();

    //        var simplifieds = new List<Geometry<Point>>();

    //        foreach (var feature in features)
    //        {
    //            var simplified = feature.Simplify(method, parameters);

    //            if (simplified.Type == GeometryType.Point)
    //                continue;

    //            var length = feature.CalculateEuclideanLength();

    //            lengths.Add(length);
    //            tlvdPerLengths.Add(feature.CalculateTotalVectorDisplacement(simplified) / length);
    //            compressions.Add(feature.Compression(simplified));
    //            simplifieds.Add(simplified);
    //        }

    //        var vectorLayer = GetAsLayer($"{estimatedZoomLevel}-{methodNames[method]}", simplifieds);

    //        var simplifiedBitmap = await vectorLayer.ParseToBitmapImage(boundingBox, currentScreenSize.Width, currentScreenSize.Height, scale);

    //        var diff = ImageHelper.CalculateConfusionMatrixBitmaps(originalBitmap, simplifiedBitmap);

    //        simplifiedBitmap.Save($"{layerName}-{methodNames[method]}.png", System.Drawing.Imaging.ImageFormat.Tiff);

    //        diff.image.Save($"{layerName}-{methodNames[method]}-diff.png", System.Drawing.Imaging.ImageFormat.Tiff);

    //        result.Add(new SimplificationAccuracy()
    //        {
    //            FeatureIndex = 0,
    //            Zoomlevel = estimatedZoomLevel,
    //            Algorithm = methodNames[method],
    //            Compression = compressions.Average(),
    //            ConfusionMatrix = diff.confusionMatrix,
    //            TlvdPerLength = tlvdPerLengths.Average(),
    //        });

    //    }

    //    StringBuilder builder = new StringBuilder();

    //    builder.AppendLine(SimplificationAccuracy.GetTsvHeader());

    //    foreach (var accuracy in result)
    //    {
    //        builder.AppendLine(accuracy.ToTsv());
    //    }

    //    File.WriteAllText($"{layerName}-log.txt", builder.ToString());

    //    return result;
    //}

    public static async Task<(SimplificationAccuracy, Geometry<Point> simplified)> CompareBySingleFeature(
       Geometry<Point> feature,
       Func<Geometry<Point>, Geometry<Point>> simplifyFunc,
       BoundingBox boundingBox,
       System.Drawing.Size currentScreenSize,
       System.Drawing.Bitmap originalBitmap,
       string layerName,
       string methodName,
       int estimatedZoomLevel)
    {
        var scale = WebMercatorUtility.GetGoogleMapScale(estimatedZoomLevel);
          
        var simplified = simplifyFunc(feature);

        if (simplified.Type != feature.Type)
            return (null, null);

        var length = feature.CalculateEuclideanLength();

        var tlvdPerLength = feature.CalculateTotalVectorDisplacement(simplified) / length;
        var compression = feature.Compression(simplified);

        GeoJsonFeatureSet originalFeatureSet = feature.AsGeoJsonFeatureSet();

        originalFeatureSet.Save($"{layerName}-original.json", false, true);

        var vectorLayer = GetAsLayer($"{estimatedZoomLevel}-{methodName}", new List<Geometry<Point>>() { simplified });

        var simplifiedBitmap = await vectorLayer.AsGdiBitmapAsync(boundingBox, scale, currentScreenSize.Width, currentScreenSize.Height);

        var diff = ImageHelper.CalculateConfusionMatrixBitmaps(originalBitmap, simplifiedBitmap);

        simplifiedBitmap.Save($"{layerName}-{methodName}.png", System.Drawing.Imaging.ImageFormat.Tiff);

        diff.image.Save($"{layerName}-{methodName}-diff.png", System.Drawing.Imaging.ImageFormat.Tiff);

        var info = new SimplificationAccuracy()
        {
            FeatureIndex = 0,
            Zoomlevel = estimatedZoomLevel,
            Algorithm = methodName,
            Compression = compression,
            ConfusionMatrix = diff.confusionMatrix,
            TlvdPerLength = tlvdPerLength,
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
        var scale = WebMercatorUtility.GetGoogleMapScale(estimatedZoomLevel);

        List<double> tlvdPerLengths = new List<double>();
        List<double> compressions = new List<double>();
        List<double> lengths = new List<double>();

        var simplifieds = new List<Geometry<Point>>();

        foreach (var feature in features)
        {
            var simplified = simplifyFunc(feature);

            if (simplified.Type == GeometryType.Point)
                continue;

            var length = feature.CalculateEuclideanLength();

            lengths.Add(length);
            tlvdPerLengths.Add(feature.CalculateTotalVectorDisplacement(simplified) / length);
            compressions.Add(feature.Compression(simplified));
            simplifieds.Add(simplified);
        }

        var vectorLayer = GetAsLayer($"{estimatedZoomLevel}-{methodName}", simplifieds);

        var simplifiedBitmap = await vectorLayer.AsGdiBitmapAsync(boundingBox, scale, currentScreenSize.Width, currentScreenSize.Height);

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
