using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Sta.Common.Helpers;

public static class HexStringHelper
{
    public static byte[] ToByteArray(string hexString)
    {
        if (hexString == null || hexString.Length % 2 != 0)
        {
            throw new NotImplementedException();
        }

        if (hexString.StartsWith("0x", StringComparison.InvariantCulture))
        {
            hexString = hexString.Substring(2);
        }

        byte[] hexAsBytes = new byte[hexString.Length / 2];

        for (int i = 0; i < hexAsBytes.Length; i++)
        {
            string byteValue = hexString.Substring(i * 2, 2);

            hexAsBytes[i] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return hexAsBytes;
    }

    public static string ToHexString(byte[] array)
    {
        StringBuilder builder = new StringBuilder(array.Length * 2);

        for (int i = 0; i < array.Length; i++)
        {
            builder.AppendFormat("{0:X2}", array[i]);
        }

        return builder.ToString();
    }

    //https://stackoverflow.com/questions/311165/how-do-you-convert-byte-array-to-hexadecimal-string-and-vice-versa
    public static string ByteToHexBitFiddle(byte[]? bytes, bool append0x = false)
    {
        if (bytes is null)
            return string.Empty;

        char[] c = new char[bytes.Length * 2];
        int b;
        for (int i = 0; i < bytes.Length; i++)
        {
            b = bytes[i] >> 4;
            c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
            b = bytes[i] & 0xF;
            c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
        }

        var result = new string(c);

        return append0x ? $"0x{result}" : result;
    }
}
