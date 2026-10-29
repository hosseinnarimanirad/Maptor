using IRI.Maptor.Core.MachineLearning;
using System;
using System.Collections.Generic;

namespace IRI.Maptor.Tests.MachineLearning;

// reference values from scipy.stats.wilcoxon(x, y, zero_method="wilcox", correction=True, method="approx"), scipy 1.18
public class HypothesisTestsTest
{
    [Fact]
    public void Wilcoxon_MatchesScipy_WithoutTies()
    {
        var x = new List<double> { 1.83, 0.50, 1.62, 2.48, 1.68, 1.88, 1.55, 3.06, 1.30 };
        var y = new List<double> { 0.878, 0.647, 0.598, 2.05, 1.06, 1.29, 1.06, 3.14, 1.29 };

        var result = HypothesisTests.WilcoxonSignedRank(x, y);

        Assert.Equal(9, result.N);
        Assert.Equal(5.0, result.Statistic);
        Assert.Equal(-2.0139861843809137, result.Z, 9);
        Assert.Equal(0.04401098401295143, result.PValue, 6);
    }

    [Fact]
    public void Wilcoxon_MatchesScipy_WithTiesAndZeros()
    {
        var x = new List<double> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var y = new List<double> { 1, 3, 2, 6, 3, 9, 7, 5, 12, 8 };

        var result = HypothesisTests.WilcoxonSignedRank(x, y);

        Assert.Equal(8, result.N);
        Assert.Equal(2, result.Zeros);
        Assert.Equal(16.5, result.Statistic);
        Assert.Equal(-0.14159846508095775, result.Z, 9);
        Assert.Equal(0.8873971794046103, result.PValue, 6);
    }

    [Fact]
    public void Wilcoxon_AllDifferencesZero_HasNoPValue()
    {
        var result = HypothesisTests.WilcoxonSignedRank(new List<double> { 1, 2, 3 }, new List<double> { 1, 2, 3 });

        Assert.Equal(0, result.N);
        Assert.Equal(3, result.Zeros);
        Assert.True(double.IsNaN(result.PValue));
    }

    [Fact]
    public void Wilcoxon_RejectsUnpairedSamples()
    {
        Assert.Throws<ArgumentException>(() => HypothesisTests.WilcoxonSignedRank(new List<double> { 1, 2 }, new List<double> { 1 }));
    }

    [Fact]
    public void Erfc_AndNormalSurvival_AreAccurate()
    {
        Assert.Equal(1, HypothesisTests.Erfc(0), 7);
        Assert.Equal(0.1572992070502851, HypothesisTests.Erfc(1), 7);
        Assert.Equal(1.8427007929497148, HypothesisTests.Erfc(-1), 7);

        Assert.Equal(0.5, HypothesisTests.StandardNormalSurvival(0), 7);
        Assert.Equal(0.02275013194817921, HypothesisTests.StandardNormalSurvival(2), 7);
        Assert.Equal(0.0013498980316301035, HypothesisTests.StandardNormalSurvival(3), 7);
    }

    [Fact]
    public void Median_OfEvenAndOddCounts()
    {
        Assert.Equal(2.5, HypothesisTests.Median(new List<double> { 4, 1, 3, 2 }));
        Assert.Equal(3, HypothesisTests.Median(new List<double> { 5, 1, 3 }));
        Assert.True(double.IsNaN(HypothesisTests.Median(new List<double>())));
    }
}
