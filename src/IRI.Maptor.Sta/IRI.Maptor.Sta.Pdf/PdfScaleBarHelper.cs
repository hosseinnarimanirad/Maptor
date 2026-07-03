namespace IRI.Maptor.Sta.Pdf;

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
        double minWidthPoints = 90,
        double maxWidthPoints = 200)
    {
        // ground meters represented by one page point
        var groundMetersPerPoint = cosCenterLatitude / pointsPerMercatorMeter;

        var minGround = minWidthPoints * groundMetersPerPoint;
        var maxGround = maxWidthPoints * groundMetersPerPoint;

        var groundMeters = _roundLengthsMeters.FirstOrDefault(l => l >= minGround && l <= maxGround);

        if (groundMeters == 0)
        {
            // Window missed the ladder (extreme scales): take the closest round length.
            var target = Math.Sqrt(minGround * maxGround);
            groundMeters = _roundLengthsMeters.OrderBy(l => Math.Abs(Math.Log(l / target))).First();
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