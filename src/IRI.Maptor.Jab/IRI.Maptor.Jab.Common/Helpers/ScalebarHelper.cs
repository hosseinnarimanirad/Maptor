using DocumentFormat.OpenXml.Bibliography;
using IRI.Maptor.Sta.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.Helpers;

public static class ScalebarHelper
{
    private static readonly List<double> _roundLengths =
        new List<double>()
        {
            5,          10,         20,         // meters
            50,         100,        200,        // meters
            500,        1_000,      2_000,      // meters (2 km)
            5_000,      10_000,     20_000,     // 5k, 10k, 20k
            50_000,     100_000,    200_000,    // 50k, 100k, 200k
            500_000,    1_000_000,  2_000_000   // 500k, 1000k, 2000k
        };

    public static double GetUnitDistance(double dpiX) => ConversionHelper.InchToMeterFactor / dpiX;

    public static double ChooseRoundScale(double mapScale, double unitDistance)
    {
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
        return (mapLength * mapScale) / unitDistance; ;
    }

    public static string GetGroundLengthLabel(double groundLengthInMeter)
    {
        return (groundLengthInMeter / 1000.0 >= 1) ?
                string.Format("{0:f0} km", groundLengthInMeter / 1000) :
                string.Format("{0} m", groundLengthInMeter);          
    }
}
