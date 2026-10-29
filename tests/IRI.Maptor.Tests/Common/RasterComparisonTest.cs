using IRI.Maptor.Core.Common.Mathematics;
using System;

namespace IRI.Maptor.Tests.Common;

public class RasterComparisonTest
{
    private static bool[,] Blank(int width = 20, int height = 10) => new bool[width, height];

    private static bool[,] HorizontalLine(int y, int x0 = 2, int x1 = 17, int width = 20, int height = 10)
    {
        var mask = Blank(width, height);

        for (int x = x0; x <= x1; x++)
            mask[x, y] = true;

        return mask;
    }

    [Fact]
    public void DistanceTransform_IsExactEuclidean()
    {
        var mask = Blank(5, 5);
        mask[1, 1] = true;
        mask[4, 4] = true;

        var d2 = DistanceTransform.SquaredEuclidean(mask);

        Assert.Equal(0, d2[1, 1]);
        Assert.Equal(0, d2[4, 4]);
        Assert.Equal(1, d2[2, 1]);
        Assert.Equal(2, d2[0, 0]);      // (1,1) is at (1,1) away
        Assert.Equal(5, d2[3, 0]);      // nearest is (1,1): 2² + 1²
        Assert.Equal(2, d2[3, 3]);      // nearest is (4,4): 1² + 1²
        Assert.Equal(1, d2[4, 3]);
    }

    [Fact]
    public void DistanceTransform_OfAnEmptyMask_IsInfinite()
    {
        var d2 = DistanceTransform.SquaredEuclidean(Blank(3, 2));

        Assert.All(new[] { d2[0, 0], d2[2, 1] }, v => Assert.Equal(double.PositiveInfinity, v));
    }

    [Fact]
    public void IdenticalRasters_ScorePerfectly()
    {
        var line = HorizontalLine(4);

        var result = RasterComparison.Compare(line, line, 1);

        Assert.Equal(16, result.ConfusionMatrix.TruePositive);
        Assert.Equal(0, result.ConfusionMatrix.FalsePositive);
        Assert.Equal(0, result.ConfusionMatrix.FalseNegative);
        Assert.Equal(0, result.ConfusionMatrix.TrueNegative);
        Assert.Equal(1, result.ConfusionMatrix.F1Score);
        Assert.Equal(1, result.TolerantF1);
        Assert.Equal(0, result.MeanDisplacement);
        Assert.Equal(0, result.MeanReferenceDisplacement);
        Assert.Equal(0, result.Hausdorff);
    }

    [Fact]
    public void LineShiftedByOnePixel_HasZeroExactF1_ButPerfectTolerantF1()
    {
        // the weakness of the exact metric: visually indistinguishable, yet no pixel coincides
        var reference = HorizontalLine(4);
        var shifted = HorizontalLine(5);

        var result = RasterComparison.Compare(reference, shifted, 1);

        Assert.Equal(0, result.ConfusionMatrix.F1Score);
        Assert.Equal(1, result.TolerantPrecision);
        Assert.Equal(1, result.TolerantRecall);
        Assert.Equal(1, result.TolerantF1);
        Assert.Equal(1, result.MeanDisplacement, 12);
        Assert.Equal(1, result.MeanReferenceDisplacement, 12);
        Assert.Equal(1, result.Hausdorff, 12);

        // with tolerance 0 the tolerant measures fall back to the exact ones
        Assert.Equal(0, RasterComparison.Compare(reference, shifted, 0).TolerantF1);
    }

    [Fact]
    public void DiagonalNeighbour_IsInsideToleranceOne()
    {
        var reference = Blank(5, 5);
        reference[2, 2] = true;

        var test = Blank(5, 5);
        test[3, 3] = true;

        var result = RasterComparison.Compare(reference, test, 1);

        Assert.Equal(1, result.TolerantF1);
        Assert.Equal(Math.Sqrt(2), result.Hausdorff, 12);
    }

    [Fact]
    public void DroppedDetail_ShowsUpInRecallAndReferenceDisplacement_NotInPrecision()
    {
        // the reference has a spike the simplification removed: the chord is correct, the spike is missing
        var reference = HorizontalLine(4);
        reference[10, 3] = reference[10, 2] = reference[10, 1] = true;

        var chord = HorizontalLine(4);

        var result = RasterComparison.Compare(reference, chord, 1);

        Assert.Equal(1, result.ConfusionMatrix.Precision);
        Assert.Equal(16.0 / 19, result.ConfusionMatrix.Recall, 12);
        Assert.Equal(1, result.TolerantPrecision);
        Assert.Equal(17.0 / 19, result.TolerantRecall, 12);   // the spike pixel next to the line is within tolerance
        Assert.Equal(0, result.MeanDisplacement);
        Assert.Equal((1 + 2 + 3) / 19.0, result.MeanReferenceDisplacement, 12);
        Assert.Equal(3, result.Hausdorff, 12);
    }

    [Fact]
    public void EmptyTestRaster_IsHandled()
    {
        var result = RasterComparison.Compare(HorizontalLine(4), Blank(), 1);

        Assert.Equal(0, result.TestPixels);
        Assert.Equal(0, result.ConfusionMatrix.F1Score);
        Assert.Equal(0, result.TolerantF1);
        Assert.True(double.IsNaN(result.MeanDisplacement));
        Assert.Equal(double.PositiveInfinity, result.Hausdorff);
        Assert.Equal(double.PositiveInfinity, result.ExcessDisplacement);
        Assert.Equal(double.PositiveInfinity, result.Displacement95);
        Assert.Equal(1, result.FractionBeyond1);
    }

    [Fact]
    public void ShiftedLine_CostsNothing_ButFarSpikeIsCharged()
    {
        // the case the exact F1 gets backwards: a whole line one pixel off is invisible (F1 = 0), a spike
        // of 10 px is visible (F1 ≈ 0.9); the displacement measures order them the other way round
        var reference = HorizontalLine(4, 2, 101, 110, 20);
        var shifted = HorizontalLine(5, 2, 101, 110, 20);

        var spiked = HorizontalLine(4, 2, 101, 110, 20);
        for (int y = 5; y <= 14; y++)
            spiked[50, y] = true;

        var shift = RasterComparison.Compare(reference, shifted, 1);
        var spike = RasterComparison.Compare(reference, spiked, 1);

        Assert.Equal(0, shift.ConfusionMatrix.F1Score);
        Assert.True(spike.ConfusionMatrix.F1Score > 0.8);

        Assert.Equal(0, shift.ExcessDisplacement);
        Assert.Equal(0, shift.FractionBeyond1);
        Assert.Equal(1, shift.Displacement95, 12);

        // the spike: 10 test pixels at distances 1..10 among 100 + 110 pooled pixels
        double expected = 0;
        for (int d = 1; d <= 10; d++)
            if (d > Math.Sqrt(2))
                expected += (d - Math.Sqrt(2)) * (d - Math.Sqrt(2));

        Assert.Equal(expected / 210, spike.ExcessDisplacement, 12);
        Assert.Equal(9.0 / 210, spike.FractionBeyond1, 12);
        Assert.Equal(8.0 / 210, spike.FractionBeyond2, 12);
        Assert.Equal(7.0 / 210, spike.FractionBeyond3, 12);
        Assert.Equal(10, spike.Hausdorff, 12);
        Assert.True(spike.ExcessDisplacement > shift.ExcessDisplacement);
    }

    [Fact]
    public void SeveralModeratePixels_OutweighOneFarPixel()
    {
        // the Hausdorff says the single 4-px pixel is worse; the excess displacement says five 3-px pixels are
        var reference = HorizontalLine(4, 2, 41, 50, 20);

        var one = HorizontalLine(4, 2, 41, 50, 20);
        one[20, 8] = true;                                   // 4 px away

        var five = HorizontalLine(4, 2, 41, 50, 20);
        foreach (var x in new[] { 5, 12, 19, 26, 33 })
            five[x, 7] = true;                               // 3 px away each

        var oneResult = RasterComparison.Compare(reference, one, 1);
        var fiveResult = RasterComparison.Compare(reference, five, 1);

        Assert.True(oneResult.Hausdorff > fiveResult.Hausdorff);
        Assert.True(fiveResult.ExcessDisplacement > oneResult.ExcessDisplacement);
        Assert.True(fiveResult.FractionBeyond2 > oneResult.FractionBeyond2);
    }

    [Fact]
    public void Displacement95_IsTheRobustHausdorff()
    {
        // one far pixel out of 85 pooled: the Hausdorff jumps to 4, the 95th percentile stays at 0
        var reference = HorizontalLine(4, 2, 43, 50, 20);
        var test = HorizontalLine(4, 2, 43, 50, 20);
        test[20, 8] = true;

        var result = RasterComparison.Compare(reference, test, 1);

        Assert.Equal(4, result.Hausdorff, 12);
        Assert.Equal(0, result.Displacement95, 12);
    }

    [Fact]
    public void MismatchedSizes_AreRejected()
    {
        Assert.Throws<ArgumentException>(() => RasterComparison.Compare(Blank(4, 4), Blank(5, 4)));
    }

    [Fact]
    public void Tsv_HasOneValuePerHeaderColumn()
    {
        var header = RasterComparison.GetTsvHeader().Split('\t');

        Assert.Equal(header.Length, RasterComparison.Compare(HorizontalLine(4), HorizontalLine(5)).AsTsv().Split('\t').Length);
        Assert.Equal(header.Length, RasterComparison.EmptyTsv().Split('\t').Length);
    }

    [Fact]
    public void OmittedArm_IsOmission_NotCommission()
    {
        // the simplified line lacks a 10-px spike of the original: original pixels far from the result = omission
        var reference = HorizontalLine(4, 2, 101, 110, 20);
        for (int y = 5; y <= 14; y++)
            reference[50, y] = true;

        var chord = HorizontalLine(4, 2, 101, 110, 20);

        var result = RasterComparison.Compare(reference, chord, 1);

        Assert.Equal(8.0 / 110, result.OmissionBeyond2, 12);
        Assert.Equal(0, result.CommissionBeyond2);
    }
}
