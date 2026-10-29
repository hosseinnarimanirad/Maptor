using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.MachineLearning;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace IRI.Maptor.Tests.MachineLearning;

// The fused evaluation inside LogisticSimplification (features into a reused buffer, logit formed in place) must
// give exactly the decisions of the reference path: an LRSimplificationParameters object per triad and
// IBinaryClassifier.Predict on its FeatureValues.
public class FusedEvaluationTest
{
    private static readonly List<LRSimplificationFeatures> Seven = new()
    {
        LRSimplificationFeatures.Area, LRSimplificationFeatures.BaseLength, LRSimplificationFeatures.CosineOfAngle, LRSimplificationFeatures.DistanceToNext,
        LRSimplificationFeatures.DistanceToPrevious, LRSimplificationFeatures.SquareCosineOfAngle, LRSimplificationFeatures.VerticalDistance,
    };

    private static readonly List<LRSimplificationFeatures> Pixel = new()
    {
        LRSimplificationFeatures.VerticalDistance, LRSimplificationFeatures.Overhang, LRSimplificationFeatures.BaseLength,
    };

    // the published traversal written against the reference evaluation
    private static List<Point> Reference(LogisticSimplification<Point> model, List<LRSimplificationFeatures> features, List<Point> points)
    {
        var result = new List<Point> { points[0] };
        int first = 0, last = 2;

        while (last < points.Count)
        {
            for (int step = 1; last - step > first; step *= 2)
            {
                var parameters = new LRSimplificationParameters<Point>(points[first], points[last - step], points[last], features, null);

                if (model.Classifier.Predict(parameters.FeatureValues) > model.DecisionThreshold)
                {
                    result.Add(points[last - step]);
                    result.Add(points[last]);
                    first = last;
                    last++;
                    break;
                }
            }

            last++;
        }

        if (!result[^1].Equals(points[^1]))
            result.Add(points[^1]);

        return result;
    }

    private static List<Point> Line(int seed, int n, double step, bool sine)
    {
        var random = new Random(seed);
        var result = new List<Point> { new(0, 0) };

        for (int i = 1; i < n; i++)
        {
            var p = result[^1];
            result.Add(sine ? new Point(i * step, 40 * Math.Sin(i / 30.0) + (random.NextDouble() - 0.5) * 0.3)
                            : new Point(p.X + random.NextDouble() * step, p.Y + (random.NextDouble() - 0.5) * step));
        }

        return result;
    }

    public static IEnumerable<object[]> Cases()
    {
        for (int seed = 1; seed <= 4; seed++)
        {
            yield return new object[] { seed, 0.4, false };
            yield return new object[] { seed, 1.5, false };
            yield return new object[] { seed, 6.0, false };
            yield return new object[] { seed, 2.0, true };
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void LogisticRegression_FusedDecisionsEqualTheReference(int seed, double step, bool sine)
    {
        var model = LogisticSimplification<Point>.Create(SyntheticDataFactory.CreateTrainingData(Seven), "LR7");
        var points = Line(seed, 3000, step, sine);

        Assert.Equal(Reference(model, Seven, points), model.Simplify(points, p => p, false));
    }

    [Fact]
    public void DecisionTree_FusedDecisionsEqualTheReference()
    {
        var model = LogisticSimplification<Point>.Create(SyntheticDataFactory.CreateTrainingData(Pixel), "Tree", new DecisionTree(new DecisionTreeOptions() { MaxDepth = 4 }));

        foreach (var seed in new[] { 1, 2, 3 })
        {
            var points = Line(seed, 3000, 1.5, false);

            Assert.Equal(Reference(model, Pixel, points), model.Simplify(points, p => p, false));
        }
    }

    [Fact]
    public void FourFeatureModel_FusedDecisionsEqualTheReference()
    {
        var four = new List<LRSimplificationFeatures> { LRSimplificationFeatures.Area, LRSimplificationFeatures.BaseLength, LRSimplificationFeatures.DistanceToNext, LRSimplificationFeatures.DistanceToPrevious };
        var model = LogisticSimplification<Point>.Create(SyntheticDataFactory.CreateTrainingData(four), "LR4");
        var points = Line(9, 5000, 1.0, false);

        Assert.Equal(Reference(model, four, points), model.Simplify(points, p => p, false));
    }
}
