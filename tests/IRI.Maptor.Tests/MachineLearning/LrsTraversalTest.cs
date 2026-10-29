using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.MachineLearning;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace IRI.Maptor.Tests.MachineLearning;

// The traversals of the ablation, and the label-boundary shift of the sensitivity experiment.
public class LrsTraversalTest
{
    private static readonly List<LRSimplificationFeatures> Seven = new()
    {
        LRSimplificationFeatures.Area, LRSimplificationFeatures.BaseLength, LRSimplificationFeatures.CosineOfAngle, LRSimplificationFeatures.DistanceToNext,
        LRSimplificationFeatures.DistanceToPrevious, LRSimplificationFeatures.SquareCosineOfAngle, LRSimplificationFeatures.VerticalDistance,
    };

    private static readonly LogisticSimplification<Point> Model = LogisticSimplification<Point>.Create(SyntheticDataFactory.CreateTrainingData(Seven), "LR7");

    private static List<Point> RandomWalk(int seed, int n, double step)
    {
        var random = new Random(seed);
        var result = new List<Point> { new(0, 0) };

        for (int i = 1; i < n; i++)
            result.Add(new Point(result[^1].X + random.NextDouble() * step, result[^1].Y + (random.NextDouble() - 0.5) * step));

        return result;
    }

    [Theory]
    [InlineData(1, 0.7)]
    [InlineData(2, 2)]
    [InlineData(3, 5)]
    public void PowerOfTwoCandidates_ReproduceThePublishedTraversal(int seed, double step)
    {
        var points = RandomWalk(seed, 400, step);

        var published = Model.SimplifyByLogisticRegression(points, p => p, true);
        var generic = Model.SimplifyByCandidates(points, p => p, true, LogisticSimplification<Point>.PowerOfTwoCandidates);

        Assert.Equal(published, generic);
        Assert.Equal(published, Model.Simplify(points, p => p, true));
    }

    [Fact]
    public void Candidates_AreTheDocumentedSequences()
    {
        Assert.Equal(new[] { 9, 8, 6, 2 }, LogisticSimplification<Point>.PowerOfTwoCandidates(1, 10));
        Assert.Equal(new[] { 9, 8, 7, 6, 5, 4, 3, 2 }, LogisticSimplification<Point>.FullScanCandidates(1, 10));
        Assert.Equal(new[] { 9 }, LogisticSimplification<Point>.PreviousOnlyCandidates(1, 10));
        Assert.Empty(LogisticSimplification<Point>.PreviousOnlyCandidates(9, 10));
    }

    [Theory]
    [InlineData(LrsTraversal.FullScan)]
    [InlineData(LrsTraversal.PreviousOnly)]
    [InlineData(LrsTraversal.Midpoint)]
    public void EveryTraversal_ReturnsASubsequenceWithBothEnds(LrsTraversal traversal)
    {
        var points = RandomWalk(7, 300, 1.5);

        var result = Model.WithTraversal(traversal, traversal.ToString()).Simplify(points, p => p, true);

        Assert.Equal(points[0], result[0]);
        Assert.Equal(points[^1], result[^1]);

        var indices = result.Select(p => points.IndexOf(p)).ToList();
        Assert.All(indices, i => Assert.True(i >= 0));
        Assert.Equal(indices.OrderBy(i => i), indices);
    }

    [Fact]
    public void ShiftBoundary_WideRemovesMoreAndNarrowRemovesLessThanV23()
    {
        int Removed(Func<double, Predicate<(double, double, double)>> rule)
            => SyntheticDataFactory.CreateTrainingData(Seven, rule).Records.Count(r => !r.IsRetained);

        var v23 = Removed(b => SyntheticDataFactory.GetRule(b, SyntheticLabelRules.V23));
        var wide = Removed(SyntheticDataFactory.ShiftBoundary(SyntheticLabelRules.V23, +1));
        var narrow = Removed(SyntheticDataFactory.ShiftBoundary(SyntheticLabelRules.V23, -1));

        Assert.Equal(730, v23);
        Assert.True(wide > v23);
        Assert.True(narrow < v23);
    }

    [Fact]
    public void ShiftBoundary_IsSymmetricInDy()
    {
        var wide = SyntheticDataFactory.ShiftBoundary(SyntheticLabelRules.V23, +1);

        foreach (var b in SyntheticDataFactory.BaseLengths)
            for (double dx1 = -4; dx1 <= b + 4; dx1 += 0.5)
                for (double dy = 0.5; dy <= 4; dy += 0.5)
                    Assert.Equal(wide(b)((dx1, dx1 - b, dy)), wide(b)((dx1, dx1 - b, -dy)));
    }
}
