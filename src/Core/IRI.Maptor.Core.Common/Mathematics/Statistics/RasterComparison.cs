using System;
using System.Collections.Generic;

namespace IRI.Maptor.Core.Common.Mathematics;

/// <summary>
/// Agreement between a reference binary raster and a test raster (e.g. an original and a simplified line,
/// both drawn 1 px wide at the target scale). Besides the exact pixel confusion matrix (true negatives are
/// not counted, as in the LAS paper) it holds distance-aware measures computed from the Euclidean distance
/// transform of each raster:
/// <list type="bullet">
/// <item>tolerant precision/recall/F1: a test pixel counts as correct when a reference pixel lies within
/// <see cref="TolerancePixels"/>·√2 of it (so tolerance 1 is the 8-neighbourhood) and a reference pixel
/// counts as found when a test pixel lies within the same distance — the buffer method of Wiedemann et al.
/// (1998, "correctness" and "completeness");</item>
/// <item>mean displacement of the test pixels to the reference (commission) and of the reference pixels to
/// the test raster (omission), in pixels;</item>
/// <item>the Hausdorff distance, the larger of the two directed maxima;</item>
/// <item>the displacement distribution of all pixels of both rasters pooled — every pixel ranked by its
/// distance to the other raster: the fraction beyond 1, 2 and 3 px, the 95th percentile
/// (<see cref="Displacement95"/>, the robust Hausdorff) and the <see cref="ExcessDisplacement"/>, the mean
/// squared distance beyond the tolerance disc. The exact F1 scores a whole line shifted by one pixel as
/// zero and a single far spike as nearly perfect, and the Hausdorff reports one pixel only; the excess
/// displacement charges nothing for the invisible shift, charges every visible pixel by the square of its
/// distance and adds them up, so several moderately displaced pixels outweigh one far pixel.</item>
/// </list>
/// </summary>
public class RasterComparison
{
    public ConfusionMatrix ConfusionMatrix { get; private set; }

    public int TolerancePixels { get; private set; }

    public int ReferencePixels { get; private set; }

    public int TestPixels { get; private set; }

    /// <summary>Fraction of test pixels within the tolerance of a reference pixel (Wiedemann's correctness).</summary>
    public double TolerantPrecision { get; private set; }

    /// <summary>Fraction of reference pixels within the tolerance of a test pixel (Wiedemann's completeness).</summary>
    public double TolerantRecall { get; private set; }

    public double TolerantF1 => TolerantPrecision + TolerantRecall == 0 ? 0 : 2 * TolerantPrecision * TolerantRecall / (TolerantPrecision + TolerantRecall);

    /// <summary>Mean distance of the test pixels to the nearest reference pixel, in pixels; NaN without test pixels.</summary>
    public double MeanDisplacement { get; private set; }

    /// <summary>Mean distance of the reference pixels to the nearest test pixel, in pixels; NaN without reference pixels.</summary>
    public double MeanReferenceDisplacement { get; private set; }

    /// <summary>Average of the two directed mean displacements.</summary>
    public double MeanSymmetricDisplacement => (MeanDisplacement + MeanReferenceDisplacement) / 2;

    /// <summary>Largest distance from any pixel of either raster to the other raster, in pixels.</summary>
    public double Hausdorff { get; private set; }

    /// <summary>
    /// Mean over all pixels of both rasters of (d − r)² for the pixels farther than the tolerance radius
    /// r = <see cref="TolerancePixels"/>·√2 from the other raster, zero for the rest. The pixels the tolerant
    /// measures accept cost nothing; the others cost the square of their excess distance. Infinite when one
    /// raster is empty.
    /// </summary>
    public double ExcessDisplacement { get; private set; }

    /// <summary>95th percentile of the pooled directed distances of all pixels of both rasters, in pixels.</summary>
    public double Displacement95 { get; private set; }

    /// <summary>Fraction of all pixels of both rasters farther than 1 px from the other raster (a diagonal neighbour counts).</summary>
    public double FractionBeyond1 { get; private set; }

    /// <summary>Fraction of all pixels of both rasters farther than 2 px from the other raster.</summary>
    public double FractionBeyond2 { get; private set; }

    /// <summary>Fraction of all pixels of both rasters farther than 3 px from the other raster.</summary>
    public double FractionBeyond3 { get; private set; }

    /// <summary>Omission: fraction of the reference's pixels farther than 2 px from the test raster (parts of the original that are missing).</summary>
    public double OmissionBeyond2 { get; private set; }

    /// <summary>Commission: fraction of the test raster's pixels farther than 2 px from the reference (lines drawn where the original has none).</summary>
    public double CommissionBeyond2 { get; private set; }

    private RasterComparison()
    {
    }

    /// <param name="reference">The raster of the original; <c>true</c> = drawn pixel.</param>
    /// <param name="test">The raster of the simplified geometry, same size.</param>
    /// <param name="tolerancePixels">Buffer half-width of the tolerant measures; 0 reproduces the exact counts.</param>
    public static RasterComparison Compare(bool[,] reference, bool[,] test, int tolerancePixels = 1)
    {
        if (reference == null || test == null)
            throw new ArgumentNullException(reference == null ? nameof(reference) : nameof(test));

        if (reference.GetLength(0) != test.GetLength(0) || reference.GetLength(1) != test.GetLength(1))
            throw new ArgumentException("the rasters must have the same size");

        if (tolerancePixels < 0)
            throw new ArgumentOutOfRangeException(nameof(tolerancePixels), tolerancePixels, "must not be negative");

        int width = reference.GetLength(0), height = reference.GetLength(1);

        var distanceToReference = DistanceTransform.SquaredEuclidean(reference);
        var distanceToTest = DistanceTransform.SquaredEuclidean(test);

        // squared radius of the tolerance disc: diagonal neighbours at distance k·√2 are still inside
        var toleranceSquared = 2.0 * tolerancePixels * tolerancePixels + 1e-9;

        var toleranceRadius = Math.Sqrt(2.0) * tolerancePixels;

        int tp = 0, fp = 0, fn = 0;

        int referencePixels = 0, testPixels = 0, tolerantTest = 0, tolerantReference = 0;

        double testSum = 0, referenceSum = 0, maxDistance = 0, excessSum = 0;

        int beyond1 = 0, beyond2 = 0, beyond3 = 0, referenceBeyond2 = 0, testBeyond2 = 0;

        // every directed distance of both rasters, for the percentile
        var distances = new List<double>();

        void Pool(double d)
        {
            distances.Add(d);

            if (d > 1 + 1e-9) beyond1++;
            if (d > 2 + 1e-9) beyond2++;
            if (d > 3 + 1e-9) beyond3++;

            if (d > toleranceRadius)
                excessSum += (d - toleranceRadius) * (d - toleranceRadius);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var inReference = reference[x, y];
                var inTest = test[x, y];

                if (inReference && inTest) tp++;
                else if (inTest) fp++;
                else if (inReference) fn++;

                if (inTest)
                {
                    testPixels++;

                    var d2 = distanceToReference[x, y];

                    if (d2 <= toleranceSquared)
                        tolerantTest++;

                    var d = Math.Sqrt(d2);

                    testSum += d;
                    maxDistance = Math.Max(maxDistance, d);

                    if (d > 2 + 1e-9) testBeyond2++;

                    Pool(d);
                }

                if (inReference)
                {
                    referencePixels++;

                    var d2 = distanceToTest[x, y];

                    if (d2 <= toleranceSquared)
                        tolerantReference++;

                    var d = Math.Sqrt(d2);

                    referenceSum += d;
                    maxDistance = Math.Max(maxDistance, d);

                    if (d > 2 + 1e-9) referenceBeyond2++;

                    Pool(d);
                }
            }
        }

        // one empty raster: every pixel of the other is infinitely far
        var oneEmpty = referencePixels == 0 || testPixels == 0;

        var pooled = distances.Count;

        distances.Sort();

        return new RasterComparison()
        {
            ConfusionMatrix = new ConfusionMatrix(tp, fp, fn, 0),
            TolerancePixels = tolerancePixels,
            ReferencePixels = referencePixels,
            TestPixels = testPixels,
            TolerantPrecision = testPixels == 0 ? 0 : (double)tolerantTest / testPixels,
            TolerantRecall = referencePixels == 0 ? 0 : (double)tolerantReference / referencePixels,
            MeanDisplacement = testPixels == 0 ? double.NaN : testSum / testPixels,
            MeanReferenceDisplacement = referencePixels == 0 ? double.NaN : referenceSum / referencePixels,
            Hausdorff = oneEmpty ? double.PositiveInfinity : maxDistance,
            ExcessDisplacement = oneEmpty ? double.PositiveInfinity : pooled == 0 ? 0 : excessSum / pooled,
            Displacement95 = oneEmpty ? double.PositiveInfinity : Quantile(distances, 0.95),
            FractionBeyond1 = oneEmpty ? 1 : pooled == 0 ? 0 : (double)beyond1 / pooled,
            FractionBeyond2 = oneEmpty ? 1 : pooled == 0 ? 0 : (double)beyond2 / pooled,
            FractionBeyond3 = oneEmpty ? 1 : pooled == 0 ? 0 : (double)beyond3 / pooled,
            OmissionBeyond2 = referencePixels == 0 ? 0 : testPixels == 0 ? 1 : (double)referenceBeyond2 / referencePixels,
            CommissionBeyond2 = testPixels == 0 ? 0 : referencePixels == 0 ? 1 : (double)testBeyond2 / testPixels,
        };
    }

    // linear interpolation between order statistics (type 7 of Hyndman and Fan, the default of R and NumPy)
    private static double Quantile(List<double> sorted, double p)
    {
        if (sorted.Count == 0)
            return double.NaN;

        var position = (sorted.Count - 1) * p;

        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);

        return sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
    }

    public static string GetTsvHeader()
    {
        var headers = new List<string>()
        {
            nameof(TolerancePixels),
            nameof(TolerantPrecision),
            nameof(TolerantRecall),
            nameof(TolerantF1),
            nameof(MeanDisplacement),
            nameof(MeanReferenceDisplacement),
            nameof(MeanSymmetricDisplacement),
            nameof(Hausdorff),
            nameof(ExcessDisplacement),
            nameof(Displacement95),
            nameof(FractionBeyond1),
            nameof(FractionBeyond2),
            nameof(FractionBeyond3),
            nameof(OmissionBeyond2),
            nameof(CommissionBeyond2),
        };

        return string.Join("\t", headers);
    }

    /// <summary>Blank fields in the layout of <see cref="GetTsvHeader"/>, for rows without a comparison.</summary>
    public static string EmptyTsv() => new string('\t', 14);

    public string AsTsv()
    {
        var values = new List<string>()
        {
            TolerancePixels.ToString(),
            TolerantPrecision.ToString("N4"),
            TolerantRecall.ToString("N4"),
            TolerantF1.ToString("N4"),
            MeanDisplacement.ToString("N4"),
            MeanReferenceDisplacement.ToString("N4"),
            MeanSymmetricDisplacement.ToString("N4"),
            Hausdorff.ToString("N4"),
            ExcessDisplacement.ToString("N4"),
            Displacement95.ToString("N4"),
            FractionBeyond1.ToString("N4"),
            FractionBeyond2.ToString("N4"),
            FractionBeyond3.ToString("N4"),
            OmissionBeyond2.ToString("N4"),
            CommissionBeyond2.ToString("N4"),
        };

        return string.Join("\t", values);
    }
}
