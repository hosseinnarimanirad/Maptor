using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.MachineLearning;
using IRI.Maptor.Core.Spatial.Primitives;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace IRI.Maptor.Tests.MachineLearning;

// The LRS algorithm of the LAS paper: a logistic-regression model trained on the synthetic triads of
// SyntheticDataFactory and the O(n log n) traversal of LogisticSimplification.SimplifyByLogisticRegression.
public class LogisticSimplificationTest
{
    // LRSv7 feature set of the LAS paper (LR7v23)
    private static readonly List<LRSimplificationFeatures> Lrs7Features = new()
    {
        LRSimplificationFeatures.Area,
        LRSimplificationFeatures.BaseLength,
        LRSimplificationFeatures.CosineOfAngle,
        LRSimplificationFeatures.DistanceToNext,
        LRSimplificationFeatures.DistanceToPrevious,
        LRSimplificationFeatures.SquareCosineOfAngle,
        LRSimplificationFeatures.VerticalDistance,
    };

    // LRSv4 feature set of the LAS paper (LR4v23)
    private static readonly List<LRSimplificationFeatures> Lrs4Features = new()
    {
        LRSimplificationFeatures.Area,
        LRSimplificationFeatures.BaseLength,
        LRSimplificationFeatures.DistanceToNext,
        LRSimplificationFeatures.DistanceToPrevious,
    };

    // The same conversion the research tool (SyntheticDataTrainingViewModel.GenerateTrainingData) applies:
    // every synthetic item is a 3-point line in screen space and yields exactly one training record.
    private static LRSimplificationTrainingData<Point> CreateTrainingData(List<LRSimplificationFeatures> features)
    {
        var records = new List<LRSimplificationParameters<Point>>();

        foreach (var item in SyntheticDataFactory.CreateAll())
        {
            var original = Geometry<Point>.FromWkt(item.OriginalLineString, 0);
            var simplified = Geometry<Point>.FromWkt(item.SimplifiedLineString, 0);

            records.AddRange(LRSimplificationTrainingData<Point>.Create(original.Points!, simplified.Points!, false, features).Records);
        }

        return new LRSimplificationTrainingData<Point>(records);
    }

    private static LogisticSimplification<Point> CreateModel(List<LRSimplificationFeatures> features)
        => LogisticSimplification<Point>.Create(CreateTrainingData(features), "test");

    private static Point Identity(Point p) => p;

    [Fact]
    public void SyntheticData_HasTheSizeReportedForTheV23Models()
    {
        var data = CreateTrainingData(Lrs7Features);

        Assert.Equal(4182, data.GetCountOfRecords());
        Assert.Equal(3452, data.Records.Count(r => r.IsRetained));
    }

    [Fact]
    public void Model_TrainingAccuracyIsAbove94Percent()
    {
        var data = CreateTrainingData(Lrs7Features);
        var model = LogisticSimplification<Point>.Create(data, "LRSv7");

        // a retained triad keeps all three points; a removed one keeps only the ends
        int correct = 0;

        foreach (var item in SyntheticDataFactory.CreateAll())
        {
            var points = Geometry<Point>.FromWkt(item.OriginalLineString, 0).Points!;

            var retained = model.SimplifyByLogisticRegression(points, Identity).Count == 3;

            if (retained == (item.Note == "RETAINED"))
                correct++;
        }

        Assert.True(correct / 4182.0 > 0.94, $"training accuracy {correct / 4182.0:P2}");
    }

    [Fact]
    public void Simplify_RemovesCollinearMiddlePoints_AndKeepsSharpCorners()
    {
        var model = CreateModel(Lrs4Features);

        var straight = Enumerable.Range(0, 20).Select(i => new Point(i, 0)).ToList();
        var simplified = model.SimplifyByLogisticRegression(straight, Identity);

        Assert.Equal(new List<Point> { new(0, 0), new(19, 0) }, simplified);

        var corner = new List<Point> { new(0, 0), new(10, 0), new(10, 10) };
        Assert.Equal(3, model.SimplifyByLogisticRegression(corner, Identity).Count);
    }

    [Fact]
    public void Simplify_KeepsFirstAndLastPoint_AndPreservesOrder()
    {
        var model = CreateModel(Lrs7Features);

        var points = ZigZag(200, amplitude: 0.7, step: 1.3);

        var simplified = model.SimplifyByLogisticRegression(points, Identity);

        Assert.Equal(points.First(), simplified.First());
        Assert.Equal(points.Last(), simplified.Last());
        Assert.True(simplified.Count < points.Count);

        // the output is a subsequence of the input
        int cursor = 0;
        foreach (var p in simplified)
        {
            cursor = points.IndexOf(p, cursor);
            Assert.True(cursor >= 0);
        }
    }

    [Fact]
    public void Simplify_MatchesThePseudoCodeOfThePaper()
    {
        var model = CreateModel(Lrs7Features);

        foreach (var amplitude in new[] { 0.3, 0.8, 2.0, 6.0 })
        {
            var points = ZigZag(300, amplitude, step: 1.7);

            var library = model.SimplifyByLogisticRegression(points, Identity);
            var reference = ReferenceTraversal(points, model);

            Assert.Equal(reference, library);
        }
    }

    [Fact]
    public void Simplify_TwoOrFewerPointsAreReturnedUnchanged()
    {
        var model = CreateModel(Lrs4Features);

        var two = new List<Point> { new(0, 0), new(1, 1) };
        Assert.Same(two, model.SimplifyByLogisticRegression(two, Identity));

        Assert.Null(model.SimplifyByLogisticRegression(new List<Point>(), Identity));
        Assert.Null(model.SimplifyByLogisticRegression(null!, Identity));
    }

    [Fact]
    public void Simplify_Retain3Points_AddsAMiddlePointWhenEverythingWasRemoved()
    {
        var model = CreateModel(Lrs4Features);

        var straight = Enumerable.Range(0, 11).Select(i => new Point(i, 0)).ToList();

        var simplified = model.SimplifyByLogisticRegression(straight, Identity, retain3Points: true);

        Assert.Equal(3, simplified.Count);
        Assert.Equal(new Point(5, 0), simplified[1]);
    }

    [Fact]
    public void FastCumulativeAreaVariant_DoesNotStopAtTheFirstRetainedVertex()
    {
        var model = CreateModel(Lrs4Features);

        // two sharp corners far apart: both must survive
        var points = new List<Point>();
        points.AddRange(Enumerable.Range(0, 30).Select(i => new Point(i, 0)));
        points.AddRange(Enumerable.Range(1, 30).Select(i => new Point(29, i)));
        points.AddRange(Enumerable.Range(1, 30).Select(i => new Point(29 + i, 30)));

        var simplified = model.SimplifyByLogisticRegression_Fast_O_n_CumulativeArea(points, Identity);

        Assert.Contains(new Point(29, 0), simplified);
        Assert.Contains(new Point(29, 30), simplified);
        Assert.Equal(points.Last(), simplified.Last());
    }

    // Pseudo-code "LogisticSimplification" of the LAS paper (Section 3.3), written independently of the
    // library loop so that the two can be compared.
    private static List<Point> ReferenceTraversal(List<Point> input, LogisticSimplification<Point> model)
    {
        var result = new List<Point> { input[0] };

        int first = 0;
        int last = 2;

        while (last < input.Count)
        {
            int middle = last - 1;
            int floating = 1;

            while (middle > first)
            {
                var triad = new List<Point> { input[first], input[middle], input[last] };

                // IsRetained(model, parameters): the middle point of a 3-point line is kept
                if (model.SimplifyByLogisticRegression(triad, Identity).Count == 3)
                {
                    result.Add(input[middle]);
                    result.Add(input[last]);
                    first = last;
                    break;
                }

                middle -= floating;
                floating *= 2;
            }

            last++;
        }

        if (!result.Last().Equals(input.Last()))
            result.Add(input.Last());

        return result;
    }

    private static List<Point> ZigZag(int count, double amplitude, double step)
    {
        var points = new List<Point>();

        for (int i = 0; i < count; i++)
        {
            // deterministic pseudo-random wiggle
            var noise = ((i * 7919) % 13 - 6) / 6.0;
            points.Add(new Point(i * step, (i % 2 == 0 ? amplitude : -amplitude) * (0.5 + 0.5 * noise)));
        }

        return points;
    }
}
