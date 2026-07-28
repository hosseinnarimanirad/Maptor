using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Sta.Common.Helpers;

public static class ConversionHelper
{
    public const double InchToMeterFactor = 1200.0 / (3937.0 * 12.0);

    public static readonly double MeterToPixelFactor;

    public static readonly double PixelToMeterFactor;

    static ConversionHelper()
    {
        MeterToPixelFactor = MeterToPixel(1, 96);

        PixelToMeterFactor = PixelToMeter(1, 96);
    }

    // todo: this method may need future attention, what about M11 
    public static double PixelToMeter(double pixels, int dpi = 96)
    {
        return pixels / dpi * InchToMeterFactor;
    }

    // todo: this method may need future attention, what about M11 
    static public double MeterToPixel(double meter, int dpi = 96)
    {
        return meter / InchToMeterFactor * dpi;
    }


    //SQL VARBINARY TEXT REPRESENTATION
    public static byte[] VarbinaryTextRepresentationToByteArray(string stringFromSQL)
    {
        string hexPart = stringFromSQL.Substring(2);

        byte[] result = new byte[hexPart.Length / 2];

        for (int i = 0; i < hexPart.Length / 2; i++)
        {
            string hexNumber = hexPart.Substring(i * 2, 2);
            result[i] = ((byte)Convert.ToInt32(hexNumber, 16));
        }

        return result;
    }

    public static readonly string[] FarsiNumbers =
    {
        "صفر", "یک", "دو", "سه", "چهار", "پنج", "شش", "هفت", "هشت", "نه"
    };
}
