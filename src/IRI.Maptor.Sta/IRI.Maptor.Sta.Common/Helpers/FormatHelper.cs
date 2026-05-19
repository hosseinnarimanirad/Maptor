using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace IRI.Maptor.Sta.Common.Helpers;

public static class FormatHelper
{
    public static string FormatWithPrecision(double value, int precision, bool useThousandSeparator)
    {
        // Ensure precision is non-negative to avoid FormatException
        precision = Math.Clamp(precision, 0, 20);

        // Use standard numeric format strings:
        // "N" includes thousand separators, "F" is fixed-point without separators.
        string format = useThousandSeparator ? $"N{precision}" : $"F{precision}";

        return value.ToString(format, CultureInfo.InvariantCulture);

        //var defaultFormat = thousandSeparator ? "#,#" : "#";

        //if (precision == 0)
        //    return value.ToString(defaultFormat, System.Globalization.CultureInfo.InvariantCulture);
        ////return value.ToString("#,#");

        //string format = $"{defaultFormat}." + new string('0', precision);
        ////string format = "#,#." + new string('0', precision);

        //return value.ToString(format);
    }
}
