using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Common.Helpers;

public static class EncodingHelper
{
    private const int _arabicWindowsEncoding = 1256;

    private const int _defaultEncoding = 1252;

    public static Encoding ArabicEncoding => Encoding.GetEncoding(_arabicWindowsEncoding);

    public static Encoding DefaultEncoding => Encoding.GetEncoding(_defaultEncoding);
}
