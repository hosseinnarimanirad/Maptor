using System;
using System.Security.Cryptography;
using System.Text;

namespace IRI.Maptor.Core.Security;

public static class CryptographyHelper
{
    /// <summary>
    /// string > byte[] > Base64String
    /// </summary>
    /// <param name="simpleString"></param>
    /// <returns></returns>
    public static string ToBase64String(string simpleString, Encoding encoding = null)
    {
        if (string.IsNullOrWhiteSpace(simpleString))
        {
            return string.Empty;
        }

        encoding = encoding ?? Encoding.UTF8;

        //return Convert.ToBase64String(Encoding.UTF8.GetBytes(simpleString.ToString()));
        return Convert.ToBase64String(encoding.GetBytes(simpleString.ToString()));
    }

    /// <summary>
    /// Base64String > byte[] > string
    /// </summary>
    /// <param name="base64BinaryString"></param>
    /// <returns></returns>
    public static string FromBase64String(string base64BinaryString, Encoding encoding = null)
    {
        if (string.IsNullOrWhiteSpace(base64BinaryString))
        {
            return string.Empty;
        }

        return Encoding.UTF8.GetString(Convert.FromBase64String(base64BinaryString));
    }


    /// <summary>
    /// Returns URI-safe data with a given input length.
    /// </summary>
    /// <param name="length">Input length (nb. output will be longer)</param>
    /// <returns></returns>
    public static string GetRandomDataBase64url(uint length)
    {
        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
        {
            byte[] bytes = new byte[length];
            rng.GetBytes(bytes);
            return GetBase64urlEncodeNoPadding(bytes);
        }
    }

    /// <summary>
    /// Base64url no-padding encodes the given input buffer.
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public static string GetBase64urlEncodeNoPadding(byte[] buffer)
    {
        string base64 = Convert.ToBase64String(buffer);

        // Converts base64 to base64url.
        base64 = base64.Replace("+", "-");
        base64 = base64.Replace("/", "_");
        // Strips padding.
        base64 = base64.Replace("=", "");

        return base64;
    }


    //public static string ToBase64String(string simpleString, Encoding encoding = null)
    //{
    //    encoding = encoding ?? Encoding.UTF8;

    //    return Convert.ToBase64String(encoding.GetBytes(simpleString));
    //}

    //public static string FromBase64String(string base64String, Encoding encoding = null)
    //{
    //    encoding = encoding ?? Encoding.UTF8;

    //    return encoding.GetString(Convert.FromBase64String(base64String));
    //}
}
