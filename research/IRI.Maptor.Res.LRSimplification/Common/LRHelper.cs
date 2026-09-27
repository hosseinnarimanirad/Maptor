using System.IO;
using System.Diagnostics;

using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.MachineLearning;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.GeoJsonFormat;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Res.LRSimplification.Common;

/// <summary>The experiments of the LAS paper; see the README of this project for what each one produced.</summary>
public enum LrsExperiment
{
    /// <summary>The published comparison (Figure 8, Appendix A), 128² screen.</summary>
    SingleFeatureVaryingThreshold128,
    /// <summary>The same with a 256² screen.</summary>
    SingleFeatureVaryingThreshold256,
    SingleFeatureMultiScale,
    LayersVaryingThreshold512,
    LayersVaryingThreshold1024,
    LayersByZoomLevel,
    SpecialCases,
}

/// <summary>
/// Entry points of the LRS experiments. Data and output folders are the author's; the models are
/// re-fitted from their training data at load time (<see cref="GetLRAModels"/>).
/// </summary>
public static class LRHelper
{
    /// <summary>All LRS experiments keep a middle point when everything between the ends was removed.</summary>
    public const bool Retain3Points = true;

    /// <summary>OSM layers in Web Mercator: AdminArea, Building, Landuse, Railway, Road, Water, Waterways.</summary>
    public const string DataFolder = @"E:\University.Ph.D\Sample Data\OSM-1401-03-03-WebMercator";

    public const string ModelFolder = @"E:\University.Ph.D\4. Paper2\Model";

    /// <summary>Every run writes into a new dated sub-folder so that published run folders are never touched.</summary>
    public const string OutputRoot = @"E:\University.Ph.D\4. Paper2\AlgorithmRun";

    /// <summary>
    /// The two models of the LAS paper (LRSv4 = LR4v23, LRSv7 = LR7v23), re-fitted from their training data.
    /// The training data equals <see cref="SyntheticDataFactory.CreateAll"/>; the JSON files also store the
    /// feature list of each model. Other versions (v3–v22, the LF and LR5 variants) are in the same folder.
    /// </summary>
    public static List<LogisticSimplification<Point>> GetLRAModels()
    {
        var names = new Dictionary<string, string>
        {
            { Path.Combine(ModelFolder, "LR4v23.json"), "LR4v23" },
            { Path.Combine(ModelFolder, "LR7v23.json"), "LR7v23" },
        };

        var result = new List<LogisticSimplification<Point>>();

        foreach (var item in names)
        {
            var trainingData = LRSimplificationTrainingData<Point>.LoadFromJson(item.Key)
                                ?? throw new FileNotFoundException("training data of the LRS model not found", item.Key);

            var model = LogisticSimplification<Point>.Create(trainingData, item.Value);

            result.Add(model);
        }

        return result;
    }

    public static async Task GeneralTest(LrsExperiment experiment = LrsExperiment.SingleFeatureVaryingThreshold128)
    {
        var files = Directory.EnumerateFiles(DataFolder, "*.shp", SearchOption.AllDirectories);

        var writeFolder = Path.Combine(OutputRoot, $"{DateTime.Now:yyyy-MM-dd}-{experiment}");

        Directory.CreateDirectory(writeFolder);

        using var writer = new StreamWriter(Path.Combine(writeFolder, "All-summary.txt"), false);

        writer.WriteLine(experiment == LrsExperiment.SpecialCases ? SimplificationAccuracy.GetTsvHeader() : VisualSimplificationLog.GetHeader());

        // a layer whose summary already exists in the output folder is skipped, so an interrupted run can be resumed
        var finishedLayers = Directory.EnumerateFiles(writeFolder, "*.txt", SearchOption.AllDirectories)
                                      .Select(s => Path.GetFileNameWithoutExtension(s).Replace("-summary", string.Empty))
                                      .ToHashSet();

        var logFile = Path.Combine(writeFolder, $"Log-{DateTime.Now:yyyy-MM-dd HH-mm-ss}.txt");

        var watch = new Stopwatch();

        var lrModels = GetLRAModels();

        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);

            if (finishedLayers.Contains(fileName))
                continue;

            finishedLayers.Add(fileName);

            watch.Restart();

            switch (experiment)
            {
                case LrsExperiment.SingleFeatureVaryingThreshold128:
                    await SingleFeatureVaryingThresholdExperiment.RunAsync(file, writer, writeFolder, lrModels, screenSize: 128);
                    break;

                case LrsExperiment.SingleFeatureVaryingThreshold256:
                    await SingleFeatureVaryingThresholdExperiment.RunAsync(file, writer, writeFolder, lrModels, screenSize: 256);
                    break;

                case LrsExperiment.SingleFeatureMultiScale:
                    await SingleFeatureMultiScaleExperiment.RunAsync(file, writer, writeFolder, lrModels);
                    break;

                case LrsExperiment.LayersVaryingThreshold512:
                    await LayerExperiments.RunVaryingThresholdAsync(file, writer, writeFolder, lrModels, screenSize: 512);
                    break;

                case LrsExperiment.LayersVaryingThreshold1024:
                    await LayerExperiments.RunVaryingThresholdAsync(file, writer, writeFolder, lrModels, screenSize: 1024);
                    break;

                case LrsExperiment.LayersByZoomLevel:
                    await LayerExperiments.RunByZoomLevelAsync(file, writer, writeFolder, lrModels, new[] { 10, 11, 12, 13, 14, 15 });
                    break;

                case LrsExperiment.SpecialCases:
                    await SpecialCaseExperiment.RunAsync(file, writer, writeFolder, lrModels);
                    break;

                default:
                    throw new NotImplementedException(experiment.ToString());
            }

            File.AppendAllLines(logFile, new[] { $"Finished At: {DateTime.Now.ToLongTimeString()}; Elapsed: {watch.ElapsedMilliseconds / 1000.0:N0} (s) - ({fileName})" });
        }
    }

    /// <summary>
    /// Renders hand-picked sample features (GeoJSON, WGS84) with every method at the zoom level where the
    /// feature fits a 256² screen: the material of the LAS figures. Outputs go next to the input files.
    /// </summary>
    public static async Task ProcessGeoJson(string dataFolder = @"E:\University.Ph.D\4. Paper2\DataWm\samples")
    {
        var files = Directory.EnumerateFiles(dataFolder, "*.json", SearchOption.AllDirectories);

        var lrModels = GetLRAModels();

        foreach (var jsonFile in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(jsonFile);

            var features = GeoJson.LoadFromFile(jsonFile).Select(f => f.Geometry.TransformToWeMercator()).ToList();

            foreach (var feature in features)
            {
                feature.Srid = SridHelper.WebMercator;
                feature.RemoveConsecutiveDuplicatePoints();
            }

            features = features.Where(f => !f.HasDuplicatePoints()).ToList();

            int featureIndex = 0;

            foreach (var feature in features)
            {
                featureIndex++;

                var zoomLevel = WebMercatorUtility.EstimateZoomLevel(feature.GetBoundingBox(), 256, 256);

                await SimplificationHelper.Compare(feature, featureIndex, dataFolder, zoomLevel, fileName, SpecialCaseExperiment.Methods, lrModels);
            }
        }
    }
}
