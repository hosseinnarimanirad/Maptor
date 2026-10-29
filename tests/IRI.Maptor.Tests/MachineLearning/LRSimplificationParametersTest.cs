using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.MachineLearning;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace IRI.Maptor.Tests.MachineLearning;

// The triad features of the LRS model must be invariant to rigid motions of the triad
public class LRSimplificationParametersTest
{
    private static readonly List<LRSimplificationFeatures> InvariantFeatures = new()
    {
        LRSimplificationFeatures.Area,
        LRSimplificationFeatures.BaseLength,
        LRSimplificationFeatures.CosineOfAngle,
        LRSimplificationFeatures.DistanceToNext,
        LRSimplificationFeatures.DistanceToPrevious,
        LRSimplificationFeatures.SquareCosineOfAngle,
        LRSimplificationFeatures.VerticalDistance,
        LRSimplificationFeatures.Overhang,
    };

    private static Point Transform(Point p, double angle, double dx, double dy, bool mirror)
    {
        var x = mirror ? -p.X : p.X;

        var c = Math.Cos(angle);
        var s = Math.Sin(angle);

        return new Point(c * x - s * p.Y + dx, s * x + c * p.Y + dy);
    }

    [Theory]
    [InlineData(5, 1)]      // projects 1 px beyond the end
    [InlineData(-2.5, 0.5)] // projects 2.5 px before the start
    [InlineData(2, 3)]      // projects inside the base
    [InlineData(2, 0)]      // collinear
    public void Features_AreInvariantToRotationTranslationAndReflection(double middleX, double middleY)
    {
        var first = new Point(0, 0);
        var middle = new Point(middleX, middleY);
        var last = new Point(4, 0);

        var reference = new LRSimplificationParameters<Point>(first, middle, last, InvariantFeatures).FeatureValues;

        foreach (var angle in new[] { 0.3, Math.PI / 2, 2.1, -1.0 })
            foreach (var mirror in new[] { false, true })
            {
                Point Map(Point p) => Transform(p, angle, 137.5, -42.25, mirror);

                var values = new LRSimplificationParameters<Point>(Map(first), Map(middle), Map(last), InvariantFeatures).FeatureValues;

                for (int i = 0; i < reference.Count; i++)
                    Assert.Equal(reference[i], values[i], 9);
            }
    }

    [Theory]
    [InlineData(5, 1, 1)]
    [InlineData(-2.5, 0.5, 2.5)]
    [InlineData(2, 3, 0)]
    [InlineData(4, 2, 0)]
    [InlineData(0, -1, 0)]
    public void Overhang_IsTheDistanceBeyondTheNearerEnd(double middleX, double middleY, double expected)
    {
        var features = new List<LRSimplificationFeatures> { LRSimplificationFeatures.Overhang, LRSimplificationFeatures.VerticalDistance };

        var parameters = new LRSimplificationParameters<Point>(new Point(0, 0), new Point(middleX, middleY), new Point(4, 0), features);

        Assert.Equal(expected, parameters.Overhang!.Value, 12);

        // vertical distance is the distance to the base line, unaffected by the overhang
        Assert.Equal(Math.Abs(middleY), parameters.VerticalDistance!.Value, 12);
    }

    [Fact]
    public void Overhang_OfAZeroLengthBase_IsTheDistanceToTheEnd()
    {
        var parameters = new LRSimplificationParameters<Point>(new Point(1, 1), new Point(4, 5), new Point(1, 1), new List<LRSimplificationFeatures> { LRSimplificationFeatures.Overhang });

        Assert.Equal(5, parameters.Overhang!.Value, 12);
    }

    [Fact]
    public void dXdYFeatures_AreNotRotationInvariant()
    {
        // documents why dX/dY are not candidates for a screen-space model with arbitrary base orientation
        var features = new List<LRSimplificationFeatures> { LRSimplificationFeatures.dX12 };

        var flat = new LRSimplificationParameters<Point>(new Point(0, 0), new Point(5, 1), new Point(4, 0), features).dX12;
        var upright = new LRSimplificationParameters<Point>(new Point(0, 0), new Point(-1, 5), new Point(0, 4), features).dX12;

        Assert.NotEqual(flat, upright);
    }
}
