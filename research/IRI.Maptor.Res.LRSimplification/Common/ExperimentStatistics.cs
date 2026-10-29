using System.IO;
using System.Globalization;

using IRI.Maptor.Core.MachineLearning;

namespace IRI.Maptor.Res.LRSimplification.Common;

/// <summary>
/// Post-processing of a run's <c>All-summary.txt</c> (one row per feature × method × threshold) into the
/// tables the revision reports:
/// <list type="bullet">
/// <item><c>Stats-by-layer.txt</c>: per layer (and pooled), per method and threshold coefficient, the count,
/// median, quartiles and mean of compression, F1, tolerant F1, mean displacement, Hausdorff, TLVD/length and
/// running time. Medians and quartiles, because the per-feature distributions are skewed.</item>
/// <item><c>Stats-wilcoxon.txt</c>: for every model, baseline and coefficient, the paired Wilcoxon signed-rank
/// test across the features of a layer on compression, F1, tolerant F1, displacement and TLVD.</item>
/// <item><c>Stats-matched.txt</c>: the same models against each baseline at matched compression: per
/// feature the baseline's measures are interpolated along its threshold curve at the compression the model
/// reached, then compared pairwise (features outside the curve's range are excluded and counted).</item>
/// </list>
/// Works on the summary file so it can be re-run on any run folder without re-rendering.
/// </summary>
public static class ExperimentStatistics
{
    public const string PooledLayer = "All";

    /// <summary>A parsed summary row; the numeric columns the statistics use.</summary>
    public record Row(string Type, int FeatureIndex, string MethodName, double Coef, double Compression, double F1, double TolerantF1, double MeanDisplacement, double Hausdorff, double ExcessDisplacement, double Displacement95, double FractionBeyond2, double TlvdPerLength, double ElapsedMilliseconds);

    private static readonly (string Name, Func<Row, double> Value)[] Metrics =
    {
        ("Compression", r => r.Compression),
        ("F1Score", r => r.F1),
        ("TolerantF1", r => r.TolerantF1),
        ("MeanSymmetricDisplacement", r => r.MeanDisplacement),
        ("Hausdorff", r => r.Hausdorff),
        ("ExcessDisplacement", r => r.ExcessDisplacement),
        ("Displacement95", r => r.Displacement95),
        ("FractionBeyond2", r => r.FractionBeyond2),
        ("TlvdPerLength", r => r.TlvdPerLength),
        ("ElapsedMilliseconds", r => r.ElapsedMilliseconds),
    };

    public static void WriteAll(string runFolder, List<string> modelNames, List<string> baselineNames)
    {
        var rows = ReadSummary(Path.Combine(runFolder, "All-summary.txt"));

        if (rows.Count == 0)
            return;

        WriteByLayer(Path.Combine(runFolder, "Stats-by-layer.txt"), rows);

        WriteWilcoxon(Path.Combine(runFolder, "Stats-wilcoxon.txt"), rows, modelNames, baselineNames);

        WriteMatchedCompression(Path.Combine(runFolder, "Stats-matched.txt"), rows, modelNames, baselineNames);
    }

    public static List<Row> ReadSummary(string fileName)
    {
        var lines = File.ReadAllLines(fileName);

        if (lines.Length < 2)
            return new List<Row>();

        var header = lines[0].Split('\t').ToList();

        int Column(string name) => header.IndexOf(name);

        int type = Column("Type"), index = Column("FeatureIndex"), method = Column("MethodName"), coef = Column("Coef"),
            compression = Column("Compression"), f1 = Column("F1Score"), tolerantF1 = Column("TolerantF1"),
            displacement = Column("MeanSymmetricDisplacement"), hausdorff = Column("Hausdorff"), excess = Column("ExcessDisplacement"), p95 = Column("Displacement95"), beyond2 = Column("FractionBeyond2"),
            tlvd = Column("TlvdPerLength"), elapsed = Column("ElapsedMilliseconds");

        var rows = new List<Row>();

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var cells = line.Split('\t');

            rows.Add(new Row(
                cells[type],
                int.Parse(cells[index], CultureInfo.InvariantCulture),
                cells[method],
                Parse(cells, coef),
                Parse(cells, compression),
                Parse(cells, f1),
                Parse(cells, tolerantF1),
                Parse(cells, displacement),
                Parse(cells, hausdorff),
                Parse(cells, excess),
                Parse(cells, p95),
                Parse(cells, beyond2),
                Parse(cells, tlvd),
                Parse(cells, elapsed)));
        }

        return rows;
    }

    // the summary is written with the "N" format of the current culture (thousands separators); NaN for blanks
    private static double Parse(string[] cells, int column)
    {
        if (column < 0 || column >= cells.Length || string.IsNullOrWhiteSpace(cells[column]))
            return double.NaN;

        var text = cells[column];

        if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out var value))
            return value;

        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            return value;

        return double.NaN;
    }

    private static IEnumerable<string> LayersOf(List<Row> rows) => rows.Select(r => r.Type).Distinct().OrderBy(t => t).Prepend(PooledLayer);

    private static List<Row> OfLayer(List<Row> rows, string layer) => layer == PooledLayer ? rows : rows.Where(r => r.Type == layer).ToList();

    private static void WriteByLayer(string fileName, List<Row> rows)
    {
        using var writer = new StreamWriter(fileName, false);

        writer.WriteLine("Layer\tMethodName\tCoef\tN\t" + string.Join("\t", Metrics.SelectMany(m => new[] { m.Name + "_median", m.Name + "_q1", m.Name + "_q3", m.Name + "_mean" })));

        foreach (var layer in LayersOf(rows))
        {
            var groups = OfLayer(rows, layer).GroupBy(r => (r.MethodName, r.Coef)).OrderBy(g => g.Key.MethodName).ThenByDescending(g => g.Key.Coef);

            foreach (var group in groups)
            {
                var cells = new List<string> { layer, group.Key.MethodName, group.Key.Coef.ToString("0.##", CultureInfo.InvariantCulture), group.Count().ToString() };

                foreach (var metric in Metrics)
                {
                    var values = group.Select(metric.Value).Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).OrderBy(v => v).ToArray();

                    cells.Add(Format(Quantile(values, 0.5)));
                    cells.Add(Format(Quantile(values, 0.25)));
                    cells.Add(Format(Quantile(values, 0.75)));
                    cells.Add(Format(values.Length == 0 ? double.NaN : values.Average()));
                }

                writer.WriteLine(string.Join("\t", cells));
            }
        }
    }

    private static void WriteWilcoxon(string fileName, List<Row> rows, List<string> modelNames, List<string> baselineNames)
    {
        using var writer = new StreamWriter(fileName, false);

        writer.WriteLine("Layer\tModel\tBaseline\tCoef\tMetric\tN\tZeros\tModel_median\tBaseline_median\tDifference_median\tWPlus\tWMinus\tZ\tP");

        var testedMetrics = Metrics.Where(m => m.Name != "ElapsedMilliseconds").ToArray();

        foreach (var layer in LayersOf(rows))
        {
            var layerRows = OfLayer(rows, layer);

            foreach (var model in modelNames)
            {
                var modelRows = layerRows.Where(r => r.MethodName == model).ToDictionary(r => (r.Type, r.FeatureIndex));

                if (modelRows.Count == 0)
                    continue;

                foreach (var baseline in baselineNames)
                {
                    foreach (var coef in layerRows.Where(r => r.MethodName == baseline).Select(r => r.Coef).Distinct().OrderByDescending(c => c))
                    {
                        var baselineRows = layerRows.Where(r => r.MethodName == baseline && r.Coef == coef).ToDictionary(r => (r.Type, r.FeatureIndex));

                        var pairs = modelRows.Keys.Intersect(baselineRows.Keys).OrderBy(k => k).ToList();

                        foreach (var metric in testedMetrics)
                        {
                            var first = pairs.Select(k => metric.Value(modelRows[k])).ToList();
                            var second = pairs.Select(k => metric.Value(baselineRows[k])).ToList();

                            WriteTest(writer, layer, model, baseline, coef.ToString("0.##", CultureInfo.InvariantCulture), metric.Name, first, second);
                        }
                    }
                }
            }
        }
    }

    // The baselines are threshold-driven, the models are not, so their rows never share a compression. For
    // every feature the baseline's metric is interpolated linearly, along its threshold curve (compression
    // ascending), at the compression the model reached; features whose model compression lies outside the
    // baseline's curve are excluded and counted. The compression itself is then equal by construction and
    // is not tested.
    private static void WriteMatchedCompression(string fileName, List<Row> rows, List<string> modelNames, List<string> baselineNames)
    {
        using var writer = new StreamWriter(fileName, false);

        writer.WriteLine("Layer\tModel\tBaseline\tCoef\tMetric\tN\tZeros\tModel_median\tBaseline_median\tDifference_median\tWPlus\tWMinus\tZ\tP\tExcludedOutsideCurve\tCompression_median");

        var matchedMetrics = new (string Name, Func<Row, double> Value)[]
        {
            ("F1Score", r => r.F1),
            ("TolerantF1", r => r.TolerantF1),
            ("MeanSymmetricDisplacement", r => r.MeanDisplacement),
            ("Hausdorff", r => r.Hausdorff),
            ("ExcessDisplacement", r => r.ExcessDisplacement),
            ("Displacement95", r => r.Displacement95),
            ("FractionBeyond2", r => r.FractionBeyond2),
            ("TlvdPerLength", r => r.TlvdPerLength),
        };

        foreach (var layer in LayersOf(rows))
        {
            var layerRows = OfLayer(rows, layer);

            foreach (var model in modelNames)
            {
                var modelRows = layerRows.Where(r => r.MethodName == model).ToDictionary(r => (r.Type, r.FeatureIndex));

                if (modelRows.Count == 0)
                    continue;

                foreach (var baseline in baselineNames)
                {
                    var byFeature = layerRows.Where(r => r.MethodName == baseline).GroupBy(r => (r.Type, r.FeatureIndex)).ToDictionary(g => g.Key, g => g.ToList());

                    var pairs = new List<(Row Model, Row Baseline)>();

                    int excluded = 0;

                    foreach (var key in modelRows.Keys.Intersect(byFeature.Keys).OrderBy(k => k))
                    {
                        var interpolated = InterpolateAtCompression(byFeature[key], modelRows[key].Compression, matchedMetrics);

                        if (interpolated is null)
                            excluded++;
                        else
                            pairs.Add((modelRows[key], interpolated));
                    }

                    var compressionMedian = HypothesisTests.Median(pairs.Select(p => p.Model.Compression).ToList());

                    foreach (var metric in matchedMetrics)
                    {
                        WriteTest(writer, layer, model, baseline, "matched", metric.Name,
                                  pairs.Select(p => metric.Value(p.Model)).ToList(), pairs.Select(p => metric.Value(p.Baseline)).ToList(),
                                  $"\t{excluded}\t{Format(compressionMedian)}");
                    }
                }
            }
        }
    }

    // the baseline's metrics at `compression`, linearly interpolated between its two neighbouring threshold
    // rows; null when the compression is outside the range the thresholds cover
    private static Row? InterpolateAtCompression(List<Row> curve, double compression, (string Name, Func<Row, double> Value)[] metrics)
    {
        var sorted = curve.OrderBy(r => r.Compression).ToList();

        if (sorted.Count == 0 || compression < sorted.First().Compression || compression > sorted.Last().Compression)
            return null;

        var upper = sorted.FindIndex(r => r.Compression >= compression);

        var lower = Math.Max(0, upper - 1);

        var a = sorted[lower];
        var b = sorted[upper];

        var t = b.Compression == a.Compression ? 0 : (compression - a.Compression) / (b.Compression - a.Compression);

        double Mix(Func<Row, double> value) => value(a) + (value(b) - value(a)) * t;

        return new Row(a.Type, a.FeatureIndex, a.MethodName, double.NaN, compression,
                       Mix(r => r.F1), Mix(r => r.TolerantF1), Mix(r => r.MeanDisplacement), Mix(r => r.Hausdorff), Mix(r => r.ExcessDisplacement), Mix(r => r.Displacement95), Mix(r => r.FractionBeyond2), Mix(r => r.TlvdPerLength), Mix(r => r.ElapsedMilliseconds));
    }

    private static void WriteTest(StreamWriter writer, string layer, string model, string baseline, string coef, string metric, List<double> first, List<double> second, string suffix = "")
    {
        // rows where either side is undefined (empty raster, infinite Hausdorff) drop out of the pair
        var valid = Enumerable.Range(0, first.Count).Where(i => IsFinite(first[i]) && IsFinite(second[i])).ToList();

        var a = valid.Select(i => first[i]).ToList();
        var b = valid.Select(i => second[i]).ToList();

        var test = HypothesisTests.WilcoxonSignedRank(a, b);

        var cells = new List<string>
        {
            layer, model, baseline, coef, metric,
            test.N.ToString(), test.Zeros.ToString(),
            Format(HypothesisTests.Median(a)), Format(HypothesisTests.Median(b)), Format(test.MedianDifference),
            Format(test.WPlus), Format(test.WMinus), Format(test.Z), test.PValue.ToString("0.###E+00", CultureInfo.InvariantCulture),
        };

        writer.WriteLine(string.Join("\t", cells) + suffix);
    }

    private static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

    private static string Format(double value) => double.IsNaN(value) ? string.Empty : value.ToString("0.####", CultureInfo.InvariantCulture);

    // linear interpolation between order statistics (type 7 of Hyndman and Fan, the default of R and NumPy)
    private static double Quantile(double[] sorted, double p)
    {
        if (sorted.Length == 0)
            return double.NaN;

        var position = (sorted.Length - 1) * p;

        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);

        return sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
    }
}
