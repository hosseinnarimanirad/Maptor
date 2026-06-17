using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Sta.Common.Helpers;

public static class UnitHelper
{
    public static string GetLengthLabel(double length)
    {
        if (length < 1)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:N1} cm", length * 100);
        }
        else if (length < 1000)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:N2} m", length); ;
        }
        else
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:N1} Km", length / 1E3);
        }
        //else
        //{
        //    return $"{length / 1E6:N3} Mm";
        //}
    }

    public static string GetAreaLabel(double area)
    {
        if (area < 1E6)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:N2} m\xB2", area);
        }
        else
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:N1} Km\xB2", area / 1E6);
        }
    }


    const double kiloByte = 1024.0;
    const double megaByte = kiloByte * kiloByte;
    const double gigaByte = megaByte * kiloByte;
    const double teraByte = gigaByte * kiloByte;

    public static string GetFileSizeHelper(long fileSizeInBytes)
    {
        if (fileSizeInBytes < megaByte)
        {
            return $"{fileSizeInBytes / kiloByte:N2} KB";
        }
        else if (fileSizeInBytes < gigaByte)
        {
            return $"{fileSizeInBytes / megaByte:N2} MB";
        }
        else if (fileSizeInBytes < teraByte)
        {
            return $"{fileSizeInBytes / gigaByte:N2} GB";
        }
        else
        {
            return $"{fileSizeInBytes / gigaByte:N2} TB";
        }
    }
}
