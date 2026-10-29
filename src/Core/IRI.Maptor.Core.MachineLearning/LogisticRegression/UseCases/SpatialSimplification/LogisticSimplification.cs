using System;
using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Mathematics;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Common.Abstractions;

namespace IRI.Maptor.Core.MachineLearning;

/// <summary>Which candidate middle points the LRS traversal tests for every new last point (traversal ablation).</summary>
public enum LrsTraversal
{
    /// <summary>The published traversal: last−1, last−2, last−4, … back to the last retained point; O(n log n).</summary>
    PowerOfTwo,

    /// <summary>Every vertex between the last retained point and the last point, nearest first; O(n²) worst case.</summary>
    FullScan,

    /// <summary>Only last−1; O(n).</summary>
    PreviousOnly,

    /// <summary>Only the vertex midway between the last retained point and the last point (<c>_Fast_O_n</c>); O(n).</summary>
    Midpoint,

    /// <summary>
    /// Unpublished (2026-10-07): the candidates of <see cref="PowerOfTwo"/>, but when a candidate is retained only that
    /// vertex is kept and the window restarts at it, so the vertices after it are tested again against the new chord
    /// (the restart rule of the opening-window algorithms). The published rule keeps the last point as well, untested.
    /// </summary>
    RestartAtRetained,

    /// <summary>
    /// Unpublished (2026-10-07): as <see cref="RestartAtRetained"/>, but the last point is not rewound: the window restarts
    /// at the retained vertex and the scan continues from the current last point, so the last point only moves forward and
    /// the published O(n log n) bound holds. The vertices between the retained vertex and the last point are judged in the
    /// following windows, not against the chord to the current last point.
    /// </summary>
    RestartAtRetainedNoRewind,
}

/// <summary>
/// LRS, the logistic-regression simplification of the LAS paper: a binary classifier decides for the middle
/// point of a triad (first, middle, last), computed in screen coordinates, whether it is retained.
/// <see cref="Create(LRSimplificationTrainingData{T}, string)"/> fits the model (L1-regularized logistic
/// regression, see <see cref="LogisticRegression"/>) and <see cref="SimplifyByLogisticRegression"/> is the
/// published O(n log n) traversal. The two <c>_Fast_O_n</c> variants are unpublished alternatives kept for
/// the traversal ablation.
/// <see cref="Create(LRSimplificationTrainingData{T}, string, IBinaryClassifier)"/> accepts any other
/// classifier (e.g. a <see cref="DecisionTree"/>) for the classifier ablation; the traversal is the same.
/// </summary>
public class LogisticSimplification<T> where T : IPoint, new()
{
    private IBinaryClassifier _regression;

    private List<LRSimplificationFeatures> _features;

    public string Title { get; set; }

    /// <summary>The fitted retain/remove classifier.</summary>
    public IBinaryClassifier Classifier => _regression;

    /// <summary>
    /// The middle point is retained when the classifier's probability of "retain" exceeds this. 0.5 is the
    /// published rule (Equation 3); raising it removes more (a point is kept only when the model is sure),
    /// which trades quality for compression without any map-unit threshold.
    /// </summary>
    public double DecisionThreshold { get; set; } = 0.5;

    /// <summary>The traversal <see cref="Simplify"/> uses; <see cref="LrsTraversal.PowerOfTwo"/> as published.</summary>
    public LrsTraversal Traversal { get; set; } = LrsTraversal.PowerOfTwo;

    /// <summary>A model with the same fitted classifier, features and threshold but another traversal.</summary>
    public LogisticSimplification<T> WithTraversal(LrsTraversal traversal, string title)
    {
        return new LogisticSimplification<T>() { Title = title, _regression = _regression, _features = _features, DecisionThreshold = DecisionThreshold, Traversal = traversal };
    }

    /// <summary>Simplifies with the traversal of <see cref="Traversal"/>.</summary>
    public List<T> Simplify(List<T> points, Func<T, T> toScreenMap, bool retain3Points = false)
    {
        return Traversal switch
        {
            LrsTraversal.PowerOfTwo => SimplifyByLogisticRegression(points, toScreenMap, retain3Points),
            LrsTraversal.FullScan => SimplifyByCandidates(points, toScreenMap, retain3Points, FullScanCandidates),
            LrsTraversal.PreviousOnly => SimplifyByCandidates(points, toScreenMap, retain3Points, PreviousOnlyCandidates),
            LrsTraversal.Midpoint => SimplifyByLogisticRegression_Fast_O_n(points, toScreenMap, retain3Points),
            LrsTraversal.RestartAtRetained => SimplifyByRestartAtRetained(points, toScreenMap, retain3Points, rewind: true),
            LrsTraversal.RestartAtRetainedNoRewind => SimplifyByRestartAtRetained(points, toScreenMap, retain3Points, rewind: false),
            _ => throw new NotImplementedException(Traversal.ToString()),
        };
    }

    /// <summary>last−1, last−2, last−4, … while greater than first: the candidates of the published traversal.</summary>
    public static IEnumerable<int> PowerOfTwoCandidates(int first, int last)
    {
        for (int step = 1; last - step > first; step *= 2)
            yield return last - step;
    }

    /// <summary>last−1, last−2, …, first+1.</summary>
    public static IEnumerable<int> FullScanCandidates(int first, int last)
    {
        for (int middle = last - 1; middle > first; middle--)
            yield return middle;
    }

    /// <summary>last−1 only.</summary>
    public static IEnumerable<int> PreviousOnlyCandidates(int first, int last)
    {
        if (last - 1 > first)
            yield return last - 1;
    }

    /// <summary>
    /// <see cref="LrsTraversal.RestartAtRetained"/>: the power-of-two candidates of the published traversal, the same
    /// classifier; when a candidate middle point is retained, only it is added and the window restarts there, with the
    /// vertex after it as the next last point, so every vertex is eventually judged against the chord that ends up
    /// drawn. The published traversal adds the untested last point as well and never re-examines the vertices between
    /// the retained middle point and it.
    /// </summary>
    /// <param name="rewind">Rescan from the retained vertex (<see cref="LrsTraversal.RestartAtRetained"/>) or continue from
    /// the current last point (<see cref="LrsTraversal.RestartAtRetainedNoRewind"/>, O(n log n)).</param>
    public List<T> SimplifyByRestartAtRetained(List<T> points, Func<T, T> toScreenMap, bool retain3Points, bool rewind)
    {
        if (points == null || points.Count == 0)
            return null;
        else if (points.Count == 2)
            return points;

        var result = new List<T>();

        var screenPoints = points.Select(p => toScreenMap(p)).ToList();

        result.Add(points.First());

        int firstIndex = 0, lastIndex = 2;

        while (lastIndex < points.Count)
        {
            var retained = -1;

            foreach (var middleIndex in PowerOfTwoCandidates(firstIndex, lastIndex))
            {
                if (IsRetained(screenPoints[firstIndex], screenPoints[middleIndex], screenPoints[lastIndex]) == true)
                {
                    retained = middleIndex;
                    break;
                }
            }

            if (retained < 0)
            {
                lastIndex++;
            }
            else
            {
                result.Add(points[retained]);

                firstIndex = retained;
                lastIndex = rewind ? retained + 2 : lastIndex + 1;
            }
        }

        if (retain3Points && result.Count == 1)
        {
            result.Add(points[points.Count / 2]);
        }

        if (SpatialUtility.GetEuclideanLength(result.Last(), points.Last()) != 0)
        {
            result.Add(points.Last());
        }

        return result;
    }

    /// <summary>
    /// The traversal of <see cref="SimplifyByLogisticRegression"/> with any candidate order: for every last point
    /// the candidates are tested in turn; the first one retained is kept together with the last point, and the
    /// next window starts at the last point. With <see cref="PowerOfTwoCandidates"/> it equals the published traversal.
    /// </summary>
    public List<T> SimplifyByCandidates(List<T> points, Func<T, T> toScreenMap, bool retain3Points, Func<int, int, IEnumerable<int>> candidates)
    {
        if (points == null || points.Count == 0)
            return null;
        else if (points.Count == 2)
            return points;

        var result = new List<T>();

        var screenPoints = points.Select(p => toScreenMap(p)).ToList();

        result.Add(points.First());

        int firstIndex = 0, lastIndex = 2;

        while (lastIndex < points.Count)
        {
            foreach (var middleIndex in candidates(firstIndex, lastIndex))
            {
                if (IsRetained(screenPoints[firstIndex], screenPoints[middleIndex], screenPoints[lastIndex]) == true)
                {
                    result.Add(points[middleIndex]);
                    result.Add(points[lastIndex]);

                    firstIndex = lastIndex;
                    lastIndex++;

                    break;
                }
            }

            lastIndex++;
        }

        if (retain3Points && result.Count == 1)
            result.Add(points[points.Count() / 2]);

        if (SpatialUtility.GetEuclideanLength(result.Last(), points.Last()) != 0)
            result.Add(points.Last());

        return result;
    }

    private LogisticSimplification()
    {

    }

    /// <summary>
    /// The regression settings of the LRS models: L1 (Lasso) with λ = 1, features standardized (z-score),
    /// proximal Newton solver run to convergence (<see cref="LogisticRegressionOptions"/> defaults otherwise).
    /// A new instance on every call, so callers may change it.
    /// </summary>
    public static LogisticRegressionOptions CreateDefaultRegressionOptions() => new() { RegularizationMethod = RegularizationMethods.L1, RegularizationStrength = 1.0 };

    /// <summary>Fits an L1-regularized logistic regression with <see cref="CreateDefaultRegressionOptions"/>.</summary>
    public static LogisticSimplification<T> Create(LRSimplificationTrainingData<T> trainingData, string title)
    {
        return Create(trainingData, title, CreateDefaultRegressionOptions());
    }

    /// <summary>Fits a logistic regression with the given settings (e.g. a λ chosen by cross-validation).</summary>
    public static LogisticSimplification<T> Create(LRSimplificationTrainingData<T> trainingData, string title, LogisticRegressionOptions options)
    {
        return Create(trainingData, title, new LogisticRegression(options));
    }

    /// <summary>
    /// Wraps an already fitted (or hand-built) classifier, e.g. a rule expressed as a <see cref="DecisionTree"/>
    /// from <see cref="DecisionTreeNode"/>s, for the rule-based baseline.
    /// </summary>
    /// <param name="features">The features, in the order the classifier expects them.</param>
    public static LogisticSimplification<T> FromFitted(IBinaryClassifier classifier, List<LRSimplificationFeatures> features, string title)
    {
        if (classifier is null || features is null || features.Count == 0)
            return null;

        return new LogisticSimplification<T>() { Title = title, _regression = classifier, _features = features };
    }

    /// <param name="classifier">An unfitted classifier; it is fitted on <paramref name="trainingData"/>.</param>
    public static LogisticSimplification<T> Create(LRSimplificationTrainingData<T> trainingData, string title, IBinaryClassifier classifier)
    {
        if (trainingData is null || trainingData.IsNullOrEmpty() || classifier is null)
            return null;

        var xValues = trainingData.ToMatrix(out var labels);

        var yValues = labels.Select(l => l ? 1.0 : 0.0).ToArray();

        var result = new LogisticSimplification<T>() { Title = title };

        result._regression = classifier;

        result._regression.Fit(xValues, yValues);

        result._features = trainingData.Features;

        return result;
    }

    public static LogisticSimplification<T> Create(
        string title,
        List<T> originalPoints,
        List<T> simplifiedPoints,
        Func<T, T> toScreenMap,
        bool isRingMode,
        List<LRSimplificationFeatures> features)
    {
        var parameters = LRSimplificationTrainingData<T>.Create(originalPoints, simplifiedPoints, isRingMode, features, toScreenMap);

        return Create(parameters, title);
    }

    /// <summary>
    /// The IsRetained function of the paper (Equation 3): the middle point is retained when the predicted
    /// probability exceeds <see cref="DecisionThreshold"/> (0.5 as published). No map-unit threshold is involved.
    /// </summary>
    private bool? IsRetained(LRSimplificationParameters<T> parameters)
    {
        var result = _regression.Predict(parameters.FeatureValues);

        return result.HasValue ? result.Value > DecisionThreshold : null;
    }

    // feature values of the current triad, reused across candidates (one buffer per model instance; not thread-safe)
    private double[] _buffer;

    private List<double> _values;

    /// <summary>
    /// The same decision as <see cref="IsRetained(LRSimplificationParameters{T})"/> without building a parameter
    /// object: the coordinates are read once, the features of the model are computed into a reused buffer with
    /// the formulas of <see cref="LRSimplificationParameters{T}"/> (same operations in the same order, so the values
    /// are bit-identical), and for a <see cref="LogisticRegression"/> the logit is formed in place (z-score, dot
    /// product, sigmoid, in the order <see cref="LogisticRegression.Predict"/> uses). This fused evaluation removes
    /// the allocations and interface calls that dominated the running time (2026-10-04); it changes no decision
    /// (test <c>FusedEvaluationTest</c>).
    /// </summary>
    private bool? IsRetained(T first, T middle, T last)
    {
        _buffer ??= new double[_features.Count];

        double fx = first.X, fy = first.Y, mx = middle.X, my = middle.Y, lx = last.X, ly = last.Y;

        // the vectors of SpatialUtility: a = last - middle, b = middle - first, segment = last - first
        double ax = lx - mx, ay = ly - my, bx = mx - fx, by = my - fy, sx = lx - fx, sy = ly - fy;

        // squared lengths as GetSquareEuclideanLength forms them (dx*dx + dy*dy; the sign of dx, dy is immaterial)
        double sqNext = ax * ax + ay * ay, sqPrev = bx * bx + by * by, sqBase = sx * sx + sy * sy;

        for (int i = 0; i < _features.Count; i++)
        {
            _buffer[i] = _features[i] switch
            {
                // GetUnsignedEuclideanArea(first, middle, last)
                LRSimplificationFeatures.Area => Math.Abs((fx * (my - ly) + mx * (ly - fy) + lx * (fy - my)) / 2.0),
                LRSimplificationFeatures.DistanceToNext => Math.Sqrt(sqNext),
                LRSimplificationFeatures.DistanceToPrevious => Math.Sqrt(sqPrev),
                LRSimplificationFeatures.BaseLength => Math.Sqrt(sqBase),
                // GetCosineOfOuterAngle(first, middle, last)
                LRSimplificationFeatures.CosineOfAngle => CosineOfOuterAngle(first, middle, last, ax, ay, bx, by, sqNext, sqPrev),
                // GetSquareCosineOfAngle(first, middle, last)
                LRSimplificationFeatures.SquareCosineOfAngle => first.Equals(middle) || middle.Equals(last) ? 1 : (ax * bx + ay * by) * (ax * bx + ay * by) / (sqNext * sqPrev),
                // SpatialUtility.GetPointToLineDistance(first, last, middle)
                LRSimplificationFeatures.VerticalDistance => sx == 0 && sy == 0 ? Math.Sqrt(sqPrev) : Math.Abs(sy * mx - sx * my + lx * fy - ly * fx) / Math.Sqrt(sqBase),
                // GetProjectionOverhang(first, last, middle)
                LRSimplificationFeatures.Overhang => Overhang(sx, sy, bx, by, sqPrev, Math.Sqrt(sqBase)),
                LRSimplificationFeatures.dX12 => Math.Abs(mx - fx),
                LRSimplificationFeatures.dX13 => Math.Abs(lx - fx),
                LRSimplificationFeatures.dX23 => Math.Abs(lx - mx),
                LRSimplificationFeatures.dY12 => Math.Abs(my - fy),
                LRSimplificationFeatures.dY13 => Math.Abs(ly - fy),
                LRSimplificationFeatures.dY23 => Math.Abs(ly - my),
                _ => throw new NotImplementedException(_features[i].ToString()),
            };
        }

        double? probability;

        if (_regression is LogisticRegression regression && regression.Beta is not null)
        {
            var beta = regression.Beta;
            var statistics = regression.xStatistics;

            var z = 1.0 * beta[0];

            for (int i = 0; i < _buffer.Length; i++)
            {
                var value = statistics is null ? _buffer[i] : Normalization.NormalizeUsingZScore(_buffer[i], statistics[i + 1].Mean, statistics[i + 1].StandardDeviation);

                z += value * beta[i + 1];
            }

            probability = Sigmoid.CalculateSigmoid(z);
        }
        else
        {
            _values ??= new List<double>(_buffer.Length);

            _values.Clear();

            _values.AddRange(_buffer);

            probability = _regression.Predict(_values);
        }

        return probability.HasValue ? probability.Value > DecisionThreshold : null;
    }

    // SpatialUtility.GetCosineOfOuterAngle, operation for operation
    private static double CosineOfOuterAngle(T first, T middle, T last, double ax, double ay, double bx, double by, double sqNext, double sqPrev)
    {
        if (first.Equals(middle) || middle.Equals(last))
            return 1;

        var result = (ax * bx + ay * by) / Math.Sqrt(sqNext * sqPrev);

        if (result > 1 || result < 0)
            result = Math.Round(result, 13);

        return result;
    }

    // SpatialUtility.GetProjectionOverhang(first, last, middle): sx, sy = the segment, bx, by = middle - first
    private static double Overhang(double sx, double sy, double bx, double by, double sqPrev, double length)
    {
        if (length == 0)
            return Math.Sqrt(sqPrev);

        var t = (bx * sx + by * sy) / length;

        return Math.Max(0, Math.Max(-t, t - length));
    }

    /// <summary>
    /// The published traversal (LAS paper, Section 3.3 and its pseudo-code). The first point is kept; for
    /// every new last point the candidate middle points are last-1, last-2, last-4, ... back to the last
    /// retained point (powers of two), which bounds the work by O(n log n). Once a middle point is retained,
    /// both it and the last point are kept and the window restarts at the last point.
    /// </summary>
    /// <param name="points">Points in map coordinates; the result is a subsequence of them.</param>
    /// <param name="toScreenMap">Map-to-screen transform of the target scale; the features are computed in pixels.</param>
    /// <param name="retain3Points">Keep a middle point when everything between the two ends was removed.</param>
    public List<T> SimplifyByLogisticRegression(List<T> points, Func<T, T> toScreenMap, bool retain3Points = false)
    {
        if (points == null || points.Count == 0)
        {
            return null;
        }
        else if (points.Count == 2)
        {
            return points;
        }

        List<T> result = new List<T>();

        var screenPoints = points.Select(p => toScreenMap(p)).ToList();

        // add first point automatically
        result.Add(points.First());

        int firstIndex = 0, middleIndex = 1, lastIndex = 2;

        while (lastIndex < points.Count)
        {
            middleIndex = lastIndex - 1;

            int floatingIndex = 1;

            while (middleIndex > firstIndex)
            {
                if (IsRetained(screenPoints[firstIndex], screenPoints[middleIndex], screenPoints[lastIndex]) == true)
                {
                    result.Add(points[middleIndex]);
                    result.Add(points[lastIndex]);

                    firstIndex = lastIndex;
                    lastIndex++;

                    break;
                }
                else
                {
                    floatingIndex = floatingIndex * 2;
                    middleIndex = lastIndex - floatingIndex;
                }
            }

            lastIndex++;
        }

        if (retain3Points && result.Count == 1)
        {
            result.Add(points[points.Count() / 2]);
        }

        // do not add last point if it is already added
        if (SpatialUtility.GetEuclideanLength(result.Last(), points.Last()) != 0)
        {
            result.Add(points.Last());
        }

        return result;
    }

    /// <summary>
    /// Unpublished O(n) variant: only the triad (last retained, last-1, last) is tested and the triangle
    /// areas since the last retained point are accumulated into the Area feature (the CTR idea applied to LRS).
    /// </summary>
    public List<T> SimplifyByLogisticRegression_Fast_O_n_CumulativeArea(List<T> points, Func<T, T> toScreenMap, bool retain3Points = false)
    {
        if (points == null || points.Count == 0)
        {
            return null;
        }
        else if (points.Count == 2)
        {
            return points;
        }

        List<T> result = new List<T>();

        var screenPoints = points.Select(p => toScreenMap(p)).ToList();

        // add first point automatically
        result.Add(points.First());

        int firstIndex = 0, middleIndex = 1, lastIndex = 2;

        var tempArea = 0.0;

        while (lastIndex < points.Count)
        {
            middleIndex = lastIndex - 1;

            // right after a retained vertex there is no middle point yet: move on to the next triad
            // (this used to `break`, which ended the traversal at the first retained vertex)
            if (firstIndex == middleIndex)
            {
                lastIndex++;
                continue;
            }

            var parameters = new LRSimplificationParameters<T>(screenPoints[firstIndex], screenPoints[middleIndex], screenPoints[lastIndex], _features, null);

            tempArea += parameters.Area ?? 0;
            parameters.UpdateArea(tempArea);

            if (IsRetained(parameters) == true)
            {
                result.Add(points[middleIndex]);
                result.Add(points[lastIndex]);

                firstIndex = lastIndex;
                tempArea = 0;
            }

            lastIndex++;
        }

        if (retain3Points && result.Count == 1)
        {
            result.Add(points[points.Count() / 2]);
        }

        // do not add last point if it is already added
        if (SpatialUtility.GetEuclideanLength(result.Last(), points.Last()) != 0)
        {
            result.Add(points.Last());
        }

        return result;
    }

    /// <summary>
    /// Unpublished O(n) variant: for every last point only the midpoint between the last retained point and
    /// the last point is tested.
    /// </summary>
    public List<T> SimplifyByLogisticRegression_Fast_O_n(List<T> points, Func<T, T> toScreenMap, bool retain3Points = false)
    {
        if (points == null || points.Count == 0)
        {
            return null;
        }
        else if (points.Count == 2)
        {
            return points;
        }

        List<T> result = new List<T>();

        var screenPoints = points.Select(p => toScreenMap(p)).ToList();

        // add first point automatically
        result.Add(points.First());

        int firstIndex = 0, middleIndex = 1, lastIndex = 2;

        while (lastIndex < points.Count)
        {
            middleIndex = (lastIndex + firstIndex) / 2;

            if (firstIndex == middleIndex || middleIndex == lastIndex)
            {
                lastIndex++;
                break;
            }

            if (IsRetained(screenPoints[firstIndex], screenPoints[middleIndex], screenPoints[lastIndex]) == true)
            {
                result.Add(points[middleIndex]);
                result.Add(points[lastIndex]);

                firstIndex = lastIndex;
                lastIndex++;
            }

            lastIndex++;
        }

        if (retain3Points && result.Count == 1)
        {
            result.Add(points[points.Count() / 2]);
        }

        // do not add last point if it is already added
        if (SpatialUtility.GetEuclideanLength(result.Last(), points.Last()) != 0)
        {
            result.Add(points.Last());
        }

        return result;
    }
}
