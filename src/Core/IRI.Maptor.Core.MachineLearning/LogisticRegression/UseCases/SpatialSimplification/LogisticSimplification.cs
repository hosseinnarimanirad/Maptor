using System;
using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Mathematics;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Common.Abstractions;

namespace IRI.Maptor.Core.MachineLearning;

/// <summary>
/// LRS, the logistic-regression simplification of the LAS paper: a binary classifier decides for the middle
/// point of a triad (first, middle, last), computed in screen coordinates, whether it is retained.
/// <see cref="Create(LRSimplificationTrainingData{T}, string)"/> fits the model (L1-regularized logistic
/// regression, see <see cref="LogisticRegression"/>) and <see cref="SimplifyByLogisticRegression"/> is the
/// published O(n log n) traversal. The two <c>_Fast_O_n</c> variants are unpublished alternatives kept for
/// the traversal ablation.
/// </summary>
public class LogisticSimplification<T> where T : IPoint, new()
{
    private LogisticRegression _regression;

    private List<LRSimplificationFeatures> _features;

    public string Title { get; set; }

    private LogisticSimplification()
    {

    }

    public static LogisticSimplification<T> Create(LRSimplificationTrainingData<T> trainingData, string title)
    {
        if (trainingData is null || trainingData.IsNullOrEmpty())
            return null;

        var countOfFeatures = trainingData.GetCountOfFeatures();

        var countOfRecords = trainingData.GetCountOfRecords();

        Matrix xValues = new Matrix(countOfRecords, countOfFeatures);

        double[] yValues = new double[countOfRecords];

        for (int i = 0; i < countOfRecords; i++)
        {
            var features = trainingData.Records[i].FeatureValues;

            for (int j = 0; j < countOfFeatures; j++)
            {
                xValues[i, j] = features[j];
            }

            yValues[i] = trainingData.Records[i].IsRetained ? 1 : 0;
        }

        var result = new LogisticSimplification<T>() { Title = title };

        // Lasso (L1) as in the LAS paper, Section 4.2; solver settings are the defaults of LogisticRegressionOptions
        result._regression = new LogisticRegression(new LogisticRegressionOptions() { RegularizationMethod = RegularizationMethods.L1 });

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
    /// probability exceeds 0.5. No threshold parameter is involved.
    /// </summary>
    private bool? IsRetained(LRSimplificationParameters<T> parameters)
    {
        var result = _regression.Predict(parameters.FeatureValues);

        return result.HasValue ? result.Value > 0.5 : null;
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
                var parameters = new LRSimplificationParameters<T>(screenPoints[firstIndex], screenPoints[middleIndex], screenPoints[lastIndex], _features, null);

                if (IsRetained(parameters) == true)
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

            var parameters = new LRSimplificationParameters<T>(screenPoints[firstIndex], screenPoints[middleIndex], screenPoints[lastIndex], _features, null);

            if (IsRetained(parameters) == true)
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
