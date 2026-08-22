namespace IRI.Maptor.Core.Pdf;

/// <summary>
/// Picks a round ground length and its on-page width for the printed scale bar.
/// Ground distances are true meters: web-mercator lengths shrink by cos(latitude).
/// </summary>
internal static class PdfScaleBarHelper
{
    private static readonly double[] _roundLengthsMeters =
    {
        5,          10,         20,
        50,         100,        200,
        500,        1_000,      2_000,
        5_000,      10_000,     20_000,
        50_000,     100_000,    200_000,
        500_000,    1_000_000,  2_000_000
    };

    /// <summary>
    /// Chooses the scale-bar length so it lands within the given page-width window.
    /// </summary>
    /// <param name="pointsPerMercatorMeter">The map frame transform scale (page points per web-mercator meter)</param>
    /// <param name="cosCenterLatitude">cos(latitude) at the extent center — mercator-to-ground correction</param>
    public static (double BarWidthPoints, double GroundMeters, string Label) Choose(
        double pointsPerMercatorMeter,
        double cosCenterLatitude,
        double minWidthPoints = 80,
        double maxWidthPoints = 220)
    {
        // ground meters represented by one page point
        var groundMetersPerPoint = cosCenterLatitude / pointsPerMercatorMeter;

        var minGround = minWidthPoints * groundMetersPerPoint;
        var maxGround = maxWidthPoints * groundMetersPerPoint;

        // Largest round length whose bar lands within the width window.
        var groundMeters = _roundLengthsMeters.LastOrDefault(l => l >= minGround && l <= maxGround);

        if (groundMeters == 0)
        {
            // No ladder value fits the window: take the largest that still fits maxWidth (so the
            // bar never overflows the footer), else the smallest available. The bar width always
            // equals GroundMeters / groundMetersPerPoint, so the printed length stays truthful.
            groundMeters = _roundLengthsMeters.LastOrDefault(l => l <= maxGround);

            if (groundMeters == 0)
                groundMeters = _roundLengthsMeters[0];
        }

        var barWidthPoints = groundMeters / groundMetersPerPoint;

        return (barWidthPoints, groundMeters, GetGroundLengthLabel(groundMeters));
    }

    public static string GetGroundLengthLabel(double groundLengthInMeter)
    {
        return groundLengthInMeter / 1000.0 >= 1
            ? string.Format("{0:f0} km", groundLengthInMeter / 1000)
            : string.Format("{0} m", groundLengthInMeter);
    }

    /// <summary>
    /// The representative-fraction denominator ("1 : N") of the printed map frame
    /// </summary>
    public static double GetPaperScaleDenominator(double pointsPerMercatorMeter, double cosCenterLatitude)
    {
        const double metersPerPoint = 0.0254 / 72.0;

        var groundMetersPerPoint = cosCenterLatitude / pointsPerMercatorMeter;

        return groundMetersPerPoint / metersPerPoint;
    }
}