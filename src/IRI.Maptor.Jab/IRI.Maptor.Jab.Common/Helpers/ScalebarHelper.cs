using IRI.Maptor.Sta.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.Helpers;

public static class ScalebarHelper
{
    // consecutive ratios must stay <= 2.5 (max/min scalebar width ratio),
    // otherwise ChooseRoundScale finds no fitting length for some scales
    private static readonly List<double> _roundLengths =
        new List<double>()
        {
            0.1,        0.2,        0.5,        // meters
            1,          2,          5,          // meters
            10,         20,         50,         // meters
            100,        200,        500,        // meters
            1_000,      2_000,      5_000,      // 1k, 2k, 5k
            10_000,     20_000,     50_000,     // 10k, 20k, 50k
            100_000,    200_000,    500_000,    // 100k, 200k, 500k
            1_000_000,  2_000_000,  5_000_000,  // 1000k, 2000k, 5000k
            10_000_000, 20_000_000              // 10000k, 20000k
        };

    public static double GetUnitDistance(double dpiX) => ConversionHelper.InchToMeterFactor / dpiX;

    public static double ChooseRoundScale(double mapScale, double unitDistance)
    {
        if (mapScale <= 0 || double.IsInfinity(mapScale) || double.IsNaN(mapScale))
            return 0;

        var minScalebarWidth = 100; // in pixels
        var maxScalebarWidth = 250; // in pixels

        var minScreenLengthInMeter = minScalebarWidth * unitDistance;
        var maxScreenLengthInMeter = maxScalebarWidth * unitDistance;

        var minGroundLengthInMeter = minScreenLengthInMeter / mapScale;
        var maxGroundLengthInMeter = maxScreenLengthInMeter / mapScale;

        return _roundLengths.FirstOrDefault(l => l >= minGroundLengthInMeter && l <= maxGroundLengthInMeter);
    }

    public static double GetScalebarLength(double mapLength, double mapScale, double unitDistance)
    {
        return (mapLength * mapScale) / unitDistance;
    }

    public static string GetGroundLengthLabel(double groundLengthInMeter)
    {
        return (groundLengthInMeter / 1000.0 >= 1) ?
                string.Format("{0:f0} km", groundLengthInMeter / 1000) :
                string.Format("{0} m", groundLengthInMeter);          
    }
}
