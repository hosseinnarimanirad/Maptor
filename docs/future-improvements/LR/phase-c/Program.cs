// Phase C harness (scratch, not part of the repository).
//
//   PhaseC.exe fit                 re-fit LR4v23 / LR7v23 / LR4v23-LF with the pre-fix and the fixed code, compare
//   PhaseC.exe compare <outDir>    re-run CompareSingleFeatureVaryingThreshold (research LRHelper) on the features of the
//                                  published run "1403-07-17-single feature multi threshold 128", old and new models side by side
//
// Reads E:\University.Ph.D only; writes only to <outDir>.

using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Threading;

using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Core.ShapefileFormat;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.MachineLearning;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.GeoJsonFormat;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Res.LRSimplification;
using IRI.Maptor.Res.LRSimplification.Common;

using PhaseC.Old;

namespace PhaseC;

public static class Program
{
    const string ModelFolder = @"E:\University.Ph.D\4. Paper2\Model";
    const string DataFolder = @"E:\University.Ph.D\Sample Data\OSM-1401-03-03-WebMercator";
    const string PublishedRun = @"E:\University.Ph.D\4. Paper2\AlgorithmRun\1403-07-17-single feature multi threshold 128";

    static readonly string[] ModelNames = { "LR4v23", "LR7v23", "LR4v23-LF" };

    static readonly FieldInfo RegressionField = typeof(LogisticSimplification<Point>).GetField("_regression", BindingFlags.NonPublic | BindingFlags.Instance)!;

    [STAThread]
    public static int Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var mode = args.Length > 0 ? args[0] : "fit";

        if (mode == "fit")
        {
            Fit();
            return 0;
        }

        if (mode == "compare")
        {
            var outDir = args[1];
            Directory.CreateDirectory(outDir);
            return RunOnDispatcher(() => CompareAsync(outDir));
        }

        Console.WriteLine("usage: fit | compare <outDir>");
        return 1;
    }

    // ------------------------------------------------------------------------------------------------------------
    // models
    // ------------------------------------------------------------------------------------------------------------

    record Models(string Name, LRSimplificationTrainingData<Point> Data, LogisticSimplification<Point> Old, LogisticSimplification<Point> New,
                  OldLogisticRegression OldRegression, LogisticRegression NewRegression, long OldMs, long NewMs);

    static Models BuildModels(string name)
    {
        var data = LRSimplificationTrainingData<Point>.LoadFromJson(Path.Combine(ModelFolder, name + ".json"))
                   ?? throw new InvalidOperationException("cannot load " + name);

        // new: exactly what the research tool does today
        var sw = Stopwatch.StartNew();
        var newModel = LogisticSimplification<Point>.Create(data, name);
        sw.Stop();
        newModel.Title = name + "-new";
        var newMs = sw.ElapsedMilliseconds;
        var newRegression = (LogisticRegression)RegressionField.GetValue(newModel)!;

        // old: the pre-fix LogisticRegression (commit 66d5d080a) on the matrix LogisticSimplification.Create builds
        var matrix = BuildMatrix(data, out var y);
        var oldRegression = new OldLogisticRegression(new LogisticRegressionOptions() { RegularizationMethod = RegularizationMethods.L1 });
        sw.Restart();
        oldRegression.Fit(matrix, y);
        sw.Stop();

        var oldModel = LogisticSimplification<Point>.Create(data, name);
        oldModel.Title = name + "-old";
        // the fixed Predict with the old (beta, statistics) computes exactly what the old Predict computed
        RegressionField.SetValue(oldModel, new LogisticRegression((double[])oldRegression.Beta.Clone(), oldRegression.xStatistics));

        return new Models(name, data, oldModel, newModel, oldRegression, newRegression, sw.ElapsedMilliseconds, newMs);
    }

    static IRI.Maptor.Core.Common.Mathematics.Matrix BuildMatrix(LRSimplificationTrainingData<Point> data, out double[] y)
    {
        int m = data.GetCountOfRecords(), p = data.GetCountOfFeatures();
        var x = new IRI.Maptor.Core.Common.Mathematics.Matrix(m, p);
        y = new double[m];
        for (int i = 0; i < m; i++)
        {
            var features = data.Records[i].FeatureValues;
            for (int j = 0; j < p; j++) x[i, j] = features[j];
            y[i] = data.Records[i].IsRetained ? 1 : 0;
        }
        return x;
    }

    // ------------------------------------------------------------------------------------------------------------
    // fit
    // ------------------------------------------------------------------------------------------------------------

    static void Fit()
    {
        var synthetic = SyntheticDataFactory.CreateAll();

        foreach (var name in ModelNames)
        {
            var models = BuildModels(name);
            var data = models.Data;
            int m = data.GetCountOfRecords();

            Console.WriteLine($"################ {name}: {m} records, {data.Records.Count(r => r.IsRetained)} retained");
            Console.WriteLine($"   features: {string.Join(", ", data.Features)}");
            Console.WriteLine($"   old beta ({models.OldMs} ms): {Fmt(models.OldRegression.Beta)}");
            Console.WriteLine($"   new beta ({models.NewMs} ms): {Fmt(models.NewRegression.Beta)}");

            var y = data.Records.Select(r => r.IsRetained).ToArray();
            var decOld = data.Records.Select(r => models.OldRegression.Predict(new List<double>(r.FeatureValues)) > 0.5).ToArray();
            var decNew = data.Records.Select(r => models.NewRegression.Predict(new List<double>(r.FeatureValues)) > 0.5).ToArray();
            Console.WriteLine($"   training accuracy: old {Acc(decOld, y):P2}  new {Acc(decNew, y):P2}  (always retain: {y.Count(v => v) / (double)m:P2})");
            Console.WriteLine($"   training decisions that differ old vs new: {decOld.Zip(decNew, (a, b) => a != b).Count(t => t)}");

            // optimality of the new fit on the L1 objective it solves
            Console.WriteLine($"   new fit: max L1 subgradient = {L1Optimality(models.NewRegression, data):E1}");

            // is this training set exactly the synthetic factory's output?
            int labelDiff = 0, featureDiff = 0;
            double maxAbs = 0;
            for (int i = 0; i < m; i++)
            {
                var item = synthetic[i];
                var orig = Geometry<Point>.FromWkt(item.OriginalLineString, 0);
                var simp = Geometry<Point>.FromWkt(item.SimplifiedLineString, 0);
                var td = LRSimplificationTrainingData<Point>.Create(orig.GetLastPart(), simp.GetLastPart(), orig.IsRingBase(), data.Features);
                var regenerated = td.Records.Single();
                if (regenerated.IsRetained != data.Records[i].IsRetained) labelDiff++;
                var a = regenerated.FeatureValues; var b = data.Records[i].FeatureValues;
                var d = a.Zip(b, (u, v) => Math.Abs(u - v)).Max();
                maxAbs = Math.Max(maxAbs, d);
                if (d > 1e-9) featureDiff++;
            }
            Console.WriteLine($"   vs SyntheticDataFactory.CreateAll(): label differences {labelDiff}, records with feature differences {featureDiff} (max |diff| {maxAbs:E1})");

            if (name == "LR7v23")
            {
                // IsRetainedFunc0_5 has no dy = 0 case: which labels do the base-0.5 dy = 0 records carry?
                var rows = synthetic.Select((s, i) => (s, i)).Where(t => t.s.Title.StartsWith("Base0.5-Row8 ")).ToList();
                Console.WriteLine($"   base 0.5, dy = 0: {rows.Count} records, labelled retained in the JSON: {rows.Count(t => data.Records[t.i].IsRetained)}");
                foreach (var baseLength in new[] { "1", "2", "4", "8", "16", "32" })
                {
                    var r = synthetic.Select((s, i) => (s, i)).Where(t => t.s.Title.StartsWith($"Base{baseLength}-Row8 ")).ToList();
                    Console.WriteLine($"   base {baseLength}, dy = 0: {r.Count} records, retained {r.Count(t => data.Records[t.i].IsRetained)}");
                }
                Console.WriteLine($"   old/new decision on base-0.5 dy=0 records (retained count): old {rows.Count(t => decOld[t.i])}, new {rows.Count(t => decNew[t.i])}");
            }
        }
    }

    static double L1Optimality(LogisticRegression lr, LRSimplificationTrainingData<Point> data)
    {
        int m = data.GetCountOfRecords(), n = lr.Beta.Length;
        var g = new double[n];
        foreach (var r in data.Records)
        {
            var values = r.FeatureValues;
            var z = new double[n]; z[0] = 1;
            for (int j = 1; j < n; j++) z[j] = Normalization.NormalizeUsingZScore(values[j - 1], lr.xStatistics[j].Mean, lr.xStatistics[j].StandardDeviation);
            var p = lr.Predict(new List<double>(values))!.Value;
            for (int j = 0; j < n; j++) g[j] += (p - (r.IsRetained ? 1 : 0)) * z[j];
        }
        double c = 1.0 / m, worst = 0;
        for (int k = 0; k < n; k++)
        {
            g[k] /= m;
            var v = k == 0 ? Math.Abs(g[k]) : lr.Beta[k] != 0 ? Math.Abs(g[k] + c * Math.Sign(lr.Beta[k])) : Math.Max(Math.Abs(g[k]) - c, 0);
            worst = Math.Max(worst, v);
        }
        return worst;
    }

    static string Fmt(double[] b) => string.Join(", ", b.Select(v => v.ToString("F4")));
    static double Acc(bool[] a, bool[] b) => a.Zip(b, (u, v) => u == v ? 1.0 : 0.0).Average();

    // ------------------------------------------------------------------------------------------------------------
    // compare: LRHelper.CompareSingleFeatureVaryingThreshold, but on the published features instead of a new random sample
    // ------------------------------------------------------------------------------------------------------------

    static int RunOnDispatcher(Func<Task> work)
    {
        var frame = new DispatcherFrame();
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        Exception error = null;
        work().ContinueWith(t => { error = t.Exception; frame.Continue = false; }, TaskScheduler.FromCurrentSynchronizationContext());
        Dispatcher.PushFrame(frame);
        if (error != null) { Console.WriteLine(error); return 2; }
        return 0;
    }

    static async Task CompareAsync(string outputDirectory)
    {
        var allModels = ModelNames.Select(BuildModels).ToList();
        var lrModels = allModels.SelectMany(mm => new[] { mm.Old, mm.New }).ToList();

        using var writer = new StreamWriter(Path.Combine(outputDirectory, "All-summary.txt"), false);
        writer.WriteLine(VisualSimplificationLog.GetHeader());
        using var notes = new StreamWriter(Path.Combine(outputDirectory, "harness-notes.txt"), false);

        var published = Directory.EnumerateFiles(PublishedRun, "*-C1-original.json")
            .Select(f => Regex.Match(Path.GetFileName(f), @"^(.+)-F(\d+)-Z(\d+)-C1-original\.json$"))
            .Where(mt => mt.Success)
            .GroupBy(mt => mt.Groups[1].Value)
            .ToDictionary(g => g.Key, g => g.Select(mt => (index: int.Parse(mt.Groups[2].Value), zoom: int.Parse(mt.Groups[3].Value), path: Path.Combine(PublishedRun, mt.Value))).OrderBy(t => t.index).ToList());

        foreach (var shpFile in Directory.EnumerateFiles(DataFolder, "*.shp", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileNameWithoutExtension(shpFile);
            if (!published.TryGetValue(fileName, out var sample))
                continue;

            var watch = Stopwatch.StartNew();
            var features = MatchPublishedFeatures(shpFile, sample, notes);
            Console.WriteLine($"{fileName}: {features.Count} features matched in {watch.ElapsedMilliseconds / 1000.0:N1} s");

            await CompareSingleFeatureVaryingThreshold(fileName, features, writer, notes, outputDirectory, lrModels);

            writer.Flush(); notes.Flush();
            Console.WriteLine($"{fileName}: done in {watch.ElapsedMilliseconds / 1000.0:N1} s");
        }
    }

    // the published sample, with the exact web-mercator geometries re-read from the shapefile
    static List<(int index, int zoom, Geometry<Point> feature)> MatchPublishedFeatures(string shpFile, List<(int index, int zoom, string path)> sample, StreamWriter notes)
    {
        // same reading pipeline as LRHelper.CompareSingleFeatureVaryingThreshold, without the random sample
        var candidates = Shapefile.ReadShapes(shpFile)
                                  .Select(g => g.AsGeometry())
                                  .SelectMany(g => g.Split(false))
                                  .Where(g => !g.IsNullOrEmpty())
                                  .Where(g => g.TotalNumberOfPoints > 30 && g.TotalNumberOfPoints < 2000)
                                  .ToList();

        var byX = candidates.GroupBy(c => (long)Math.Floor(c.GetBoundingBox().XMin)).ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<(int, int, Geometry<Point>)>();

        foreach (var (index, zoom, path) in sample)
        {
            var json = GeoJson.LoadFromFile(path).First().Geometry.TransformToWeMercator();
            var box = json.GetBoundingBox();

            var matches = new List<Geometry<Point>>();
            for (long k = (long)Math.Floor(box.XMin) - 1; k <= (long)Math.Floor(box.XMin) + 1; k++)
            {
                if (!byX.TryGetValue(k, out var list)) continue;
                matches.AddRange(list.Where(c =>
                {
                    var cb = c.GetBoundingBox();
                    return c.Type == json.Type && Math.Abs(cb.XMin - box.XMin) < 0.05 && Math.Abs(cb.YMin - box.YMin) < 0.05 && Math.Abs(cb.XMax - box.XMax) < 0.05 && Math.Abs(cb.YMax - box.YMax) < 0.05;
                }));
            }

            Geometry<Point> feature;
            if (matches.Count >= 1)
            {
                feature = matches.First();
                if (matches.Count > 1) notes.WriteLine($"{Path.GetFileName(path)}: {matches.Count} shapefile parts share the bounding box, first one used");
            }
            else
            {
                feature = json;
                notes.WriteLine($"{Path.GetFileName(path)}: NOT MATCHED in the shapefile, re-projected GeoJSON used");
            }

            feature.Srid = SridHelper.WebMercator;
            RemoveConsecutiveDuplicatePointsKeepingType(feature);

            result.Add((index, zoom, feature));
        }

        return result;
    }

    // Geometry.RemoveConsecutiveDuplicatePoints as it behaved when the paper runs were made. Since a4029ca83 (2026-05-22) its
    // ReevaluateGeometryType throws NotImplementedException for a one-ring Polygon and turns a multi-ring Polygon into a MultiLineString.
    static void RemoveConsecutiveDuplicatePointsKeepingType(Geometry<Point> geometry)
    {
        if (geometry.IsNullOrEmpty())
            return;

        if (geometry.IsLeafGeometry())
        {
            for (int i = geometry.Points.Count - 1; i >= 1; i--)
                if (geometry.Points[i].Equals(geometry.Points[i - 1]))
                    geometry.Points.RemoveAt(i);

            return;
        }

        for (int i = geometry.Geometries.Count - 1; i >= 0; i--)
            if (geometry.Geometries[i] == null || geometry.Geometries[i].IsNullOrEmpty())
                geometry.Geometries.RemoveAt(i);

        foreach (var part in geometry.Geometries)
            RemoveConsecutiveDuplicatePointsKeepingType(part);
    }

    // body of LRHelper.CompareSingleFeatureVaryingThreshold (research, frozen), per published feature
    static async Task CompareSingleFeatureVaryingThreshold(string fileName, List<(int index, int zoom, Geometry<Point> feature)> sample, StreamWriter writer, StreamWriter notes, string outputDirectory, List<LogisticSimplification<Point>> lrModels)
    {
        var retain3Points = true;

        var features = sample.Select(s => s.feature).ToList();

        List<SimplificationType> methods = new List<SimplificationType>()
            {
                SimplificationType.RamerDouglasPeucker,
                SimplificationType.ReumannWitkam,
                SimplificationType.NormalOpeningWindow,
                SimplificationType.BeforeOpeningWindow,
            };

        Dictionary<SimplificationType, string> methodNames = methods.ToDictionary(m => m, m => m.GetDescription());

        Stopwatch stopwatch = Stopwatch.StartNew();

        var coefs = new double[] { 2, 1, 1 / 2.0 };

        foreach (var (featureIndex, publishedZoom, feature) in sample)
        {
            var boundingBox = feature.GetBoundingBox();

            var zoomLevel = WebMercatorUtility.EstimateZoomLevel(boundingBox, 128, 128);

            if (zoomLevel != publishedZoom)
                notes.WriteLine($"{fileName} F{featureIndex}: zoom {zoomLevel}, published {publishedZoom}");

            var webMercatorResolution = WebMercatorUtility.ToWebMercatorLength(zoomLevel, 1);

            var currentScreenSize = WebMercatorUtility.ToScreenSize(zoomLevel, boundingBox);

            var scale = WebMercatorUtility.GetGoogleMapScale(zoomLevel);

            var originalVectorLayer = GeneralHelper.GetAsLayer("original", features);

            var originalBitmap = await originalVectorLayer.AsGdiBitmapAsync(boundingBox, currentScreenSize.Width, currentScreenSize.Height, scale);

            var toScreenMap3 = Utility.CreateMapToScreenMapFunc(boundingBox, currentScreenSize.Width, currentScreenSize.Height);

            var threshold = webMercatorResolution;

            var parameters = new SimplificationParameters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = retain3Points };

            foreach (var coef in coefs)
            {
                foreach (var lrModel in lrModels)
                {
                    var result = await SimplificationHelper.CompareBySingleFeature(
                                    feature,
                                    g => g.Simplify(lrModel, toScreenMap3, true),
                                    boundingBox,
                                    currentScreenSize,
                                    originalBitmap,
                                   $"{outputDirectory}\\{fileName}-F{featureIndex}-Z{zoomLevel}-C{1}",
                                    lrModel.Title,
                                    zoomLevel);

                    if (result.Item1 == null) { notes.WriteLine($"{fileName} F{featureIndex} C{coef} {lrModel.Title}: simplified type changed, skipped"); continue; }

                    writer.WriteLine(new VisualSimplificationLog(fileName, boundingBox, featureIndex, feature, result.simplified, zoomLevel, lrModel.Title,
                                            stopwatch.ElapsedMilliseconds, 1, parameters, result.Item1.TlvdPerLength, result.Item1.ConfusionMatrix).ToTsv());
                }

                foreach (var method in methods)
                {
                    threshold = webMercatorResolution * coef;

                    parameters = new SimplificationParameters() { AreaThreshold = threshold * threshold, DistanceThreshold = threshold, Retain3Points = retain3Points };

                    var result = await SimplificationHelper.CompareBySingleFeature(
                                   feature,
                                   g => g.Simplify(method, parameters),
                                   boundingBox,
                                   currentScreenSize,
                                   originalBitmap,
                                   $"{outputDirectory}\\{fileName}-F{featureIndex}-Z{zoomLevel}-C{coef}",
                                   methodNames[method],
                                   zoomLevel);

                    if (result.Item1 == null) { notes.WriteLine($"{fileName} F{featureIndex} C{coef} {methodNames[method]}: simplified type changed, skipped"); continue; }

                    writer.WriteLine(new VisualSimplificationLog(fileName, boundingBox, featureIndex, feature, result.simplified, zoomLevel, methodNames[method],
                                            stopwatch.ElapsedMilliseconds, coef, parameters, result.Item1.TlvdPerLength, result.Item1.ConfusionMatrix).ToTsv());
                }
            }
        }
    }
}
