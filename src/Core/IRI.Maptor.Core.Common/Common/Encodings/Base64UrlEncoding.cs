using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.Common.Encodings;

public static class Base64UrlEncoding
{
    public static string ToBase64UrlString(this byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static string ToBase64UrlString(this string input)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(input)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static byte[] ToByteArray(this string input)
    {
        input = input.Replace('-', '+').Replace('_', '/');
        int pad = 4 - input.Length % 4;
        pad = pad > 2 ? 0 : pad;
        input = input.PadRight(input.Length + pad, '=');
        return Convert.FromBase64String(input);
    }
}
