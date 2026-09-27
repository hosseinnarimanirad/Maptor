using System.IO;

using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.MachineLearning;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Res.LRSimplification.Common;

/// <summary>
/// Finds the features of a layer on which LRS has the lowest TLVD per length of all methods, ranks them
/// by the margin to the worst baseline and renders the best 15 with every method
/// (<see cref="SimplificationHelper.Compare"/>). This is how the visually failing RW/BOPW cases of the
/// LAS paper (Figures 9 and 10) were located.
/// </summary>
public static class SpecialCaseExperiment
{
    public static readonly List<SimplificationType> Methods = new()
    {
        SimplificationType.RamerDouglasPeucker,
        SimplificationType.ReumannWitkam,
        SimplificationType.SleeveFitting,
        SimplificationType.NormalOpeningWindow,
        SimplificationType.BeforeOpeningWindow,
    };

    public static async Task RunAsync(string shpFile, StreamWriter writer, string outputDirectory, List<LogisticSimplification<Point>> lrModels, int take = 15, double minDifference = 1)
    {
        var fileName = Path.GetFileNameWithoutExtension(shpFile);

        var features = ExperimentData.LoadFeatures(shpFile, minPoints: 15, maxPoints: 3000);

        // the "LF" (least features) model is not part of the comparison
        var comparedModels = lrModels.Where(m => !m.Title.Contains("LF")).ToList();

        var candidates = new List<SpecialFeatureInfo>();

        int featureIndex = 0;

        foreach (var feature in features)
        {
            featureIndex++;

            var boundingBox = feature.GetBoundingBox();

            var zoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, 256, 256);

            var currentScreenSize = WebMercatorUtility.ToScreenSize(zoomLevel, boundingBox);

            var toScreenMap = Utility.CreateMapToScreenMapFunc(boundingBox, currentScreenSize.Width, currentScreenSize.Height);

            var parameters = SingleFeatureVaryingThresholdExperiment.CreateParameters(WebMercatorUtility.ToWebMercatorLength(zoomLevel, 1));

            if (feature.GetEuclideanLength() == 0)
                continue;

            var ranks = new List<(string type, double value)>();

            foreach (var lrModel in comparedModels)
            {
                var simplified = feature.Simplify(lrModel, toScreenMap, LRHelper.Retain3Points);

                if (simplified.Type != GeometryType.Point)
                    ranks.Add((lrModel.Title, feature.CalculateTotalVectorDisplacement(simplified)));
            }

            foreach (var method in Methods)
            {
                var simplified = feature.Simplify(method, parameters);

                if (simplified.Type != GeometryType.Point)
                    ranks.Add((method.ToString(), feature.CalculateTotalVectorDisplacement(simplified)));
            }

            if (ranks.Count == 0 || !ranks.OrderBy(r => r.value).First().type.StartsWith("LR"))
                continue;

            var lrMinValue = ranks.Where(r => r.type.StartsWith("LR")).Min(r => r.value);
            var otherMaxValue = ranks.Where(r => !r.type.StartsWith("LR")).Max(r => r.value);

            candidates.Add(new SpecialFeatureInfo
            {
                Rank = 1,
                diff = otherMaxValue - lrMinValue,
                FeatureIndex = featureIndex - 1,
                OriginalGeometry = feature,
                Zoomlevel = zoomLevel
            });
        }

        int outputIndex = 0;

        foreach (var item in candidates.Where(d => d.diff > minDifference).OrderByDescending(d => d.diff).Take(take))
        {
            outputIndex++;

            var zoomLevel = WebMercatorUtility.EstimateZoomLevel(item.OriginalGeometry.GetBoundingBox(), 256, 256);

            var comparison = await SimplificationHelper.Compare(item.OriginalGeometry, outputIndex, outputDirectory, zoomLevel, fileName, Methods, lrModels);

            foreach (var accuracy in comparison)
                writer.WriteLine(accuracy.ToTsv());
        }
    }
}
