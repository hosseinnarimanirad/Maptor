using IRI.Maptor.Core.MachineLearning;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace IRI.Maptor.Tests.MachineLearning.Published;

/// <summary>
/// Provenance of the LRSv4/LRSv7 models of the published LAS run: they were fitted by the 2024 gradient-descent
/// trainer on the V23 synthetic set (= <see cref="SyntheticDataFactory.CreateTrainingData"/>), and the library's
/// converged trainer gives different coefficients. The coefficients printed in Tables 4 and 5 of the manuscript are
/// neither: they come from scikit-learn (L1, liblinear, C = 1) fitted on an earlier 1,328-triad export
/// (<c>4. Paper2\Model\Model-All-7Feature.csv</c>, bases 0.5–4 px), see docs/future-improvements/LR.
/// </summary>
public class PublishedCoefficientsTest
{
    public static readonly List<LRSimplificationFeatures> FourFeatures = new()
    {
        LRSimplificationFeatures.Area, LRSimplificationFeatures.BaseLength, LRSimplificationFeatures.DistanceToNext, LRSimplificationFeatures.DistanceToPrevious,
    };

    public static readonly List<LRSimplificationFeatures> SevenFeatures = new()
    {
        LRSimplificationFeatures.Area, LRSimplificationFeatures.BaseLength, LRSimplificationFeatures.CosineOfAngle, LRSimplificationFeatures.DistanceToNext,
        LRSimplificationFeatures.DistanceToPrevious, LRSimplificationFeatures.SquareCosineOfAngle, LRSimplificationFeatures.VerticalDistance,
    };

    public static IEnumerable<object[]> Models => new[]
    {
        // intercept first, z-scored features (values of the Phase C harness, 2026-09-17)
        new object[] { "LR4v23", FourFeatures, new[] { 55.0818, 67.8608, -74.4174, 43.1712, 43.3399 } },
        new object[] { "LR7v23", SevenFeatures, new[] { 28.2447, 33.1531, -62.1421, 4.7963, 44.5168, 44.6876, -1.9863, 2.3296 } },
    };

    [Theory]
    [MemberData(nameof(Models))]
    public void PublishedModels_AreTheGradientDescentFitOfTheV23Set(string name, List<LRSimplificationFeatures> features, double[] expected)
    {
        var x = SyntheticDataFactory.CreateTrainingData(features).ToMatrix(out var labels);
        var y = labels.Select(l => l ? 1.0 : 0.0).ToArray();

        var published = new PublishedGradientDescentLogisticRegression(new LogisticRegressionOptions() { RegularizationMethod = RegularizationMethods.L1 });
        published.Fit(x, y);

        Assert.Equal(expected.Length, published.Beta.Length);

        for (int k = 0; k < expected.Length; k++)
            Assert.Equal(expected[k], published.Beta[k], 3);

        // the library's converged fit of the same data differs
        var converged = new LogisticRegression(LogisticSimplification<Core.Common.Primitives.Point>.CreateDefaultRegressionOptions());
        converged.Fit(x, y);

        Assert.True(Enumerable.Range(0, expected.Length).Max(k => System.Math.Abs(converged.Beta[k] - published.Beta[k])) > 1, name);
    }
}
