using IRI.Maptor.Extensions;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Encodings;
using IRI.Maptor.Sta.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Sta.ShapefileFormat.Dbf;

public static class DbfFieldMappings
{
    private static Func<string, string> _defaultCorrection = (i) => { return i.ArabicToFarsi(); };

    private static string _dbfTrueValue1 = "T";
    private static string _dbfFalseValue1 = "F";

    private static string _dbfTrueValue2 = "Y";
    private static string _dbfFalseValue2 = "N";

    internal static bool? ConvertDbfLogicalValueToBoolean(byte[] buffer)
    {
        if (buffer.IsNullOrEmpty())
            return null;

        string tempValue = Encoding.ASCII.GetString(buffer)?.ToUpper();

        // 1400.02.03-refactor
        //if (tempValue.ToUpper().Equals(_dbfTrueValue1) || tempValue.ToUpper().Equals(_dbfTrueValue2))
        //{
        //    return true;
        //}
        //else if (tempValue.ToUpper().Equals("F") || tempValue.ToUpper().Equals("N"))
        //{
        //    return false;
        //}

        if (_dbfTrueValue1.Equals(tempValue) || _dbfTrueValue2.Equals(tempValue))
        {
            return true;
        }
        else if (_dbfFalseValue1.Equals(tempValue) || _dbfFalseValue2.Equals(tempValue))
        {
            return false;
        }
        else
        {
            return null;
        }
    }

    internal static string ConvertDbfGeneralToString(byte[] buffer, Encoding encoding, bool correctFarsiCharacters)
    {
        var bytes = encoding.GetType() == typeof(PersianDOS) ? buffer.Reverse().ToArray() : buffer;

        var result = encoding.GetString(bytes).Replace('\0', ' ').Trim();

        if (encoding.GetType() == typeof(PersianDOS) && !string.IsNullOrEmpty(result) && result.All(c => c == '*'))
        {
            result = EncodingHelper.ArabicEncoding.GetString(buffer).Replace('\0', ' ').Trim();
        }

        if (correctFarsiCharacters)
        {
            return _defaultCorrection(result/*encoding.GetString(buffer).Replace('\0', ' ').Trim()*/);
        }
        else
        {
            return result;// encoding.GetString(buffer.Reverse().ToArray()).Replace('\0', ' ').Trim();
        }

    }

    internal static readonly Func<byte[], object> ToDouble =
        (input) =>
        {
            // old code; this may throw exception
            //string value = Encoding.ASCII.GetString(input).Trim();
            //return string.IsNullOrEmpty(value) ? DBNull.Value : (object)double.Parse(value);

            string value = Encoding.ASCII.GetString(input);

            double doubleValue;

            return double.TryParse(value, out doubleValue) ? (object)doubleValue : DBNull.Value;
        };

    internal static readonly Func<byte[], object> ToInt =
        (input) =>
        {
            string value = Encoding.ASCII.GetString(input);

            int intValue;

            // 1400.02.03-comment
            //return string.IsNullOrEmpty(value) ? DBNull.Value : (object)int.Parse(value);

            return int.TryParse(value, out intValue) ? (object)intValue : DBNull.Value;
        };

    internal static readonly Func<byte[], object> ToDecimal =
        (input) =>
        {
            string value = Encoding.ASCII.GetString(input);

            decimal decimalValue;

            // 1400.02.03-comment
            //return string.IsNullOrEmpty(value) ? DBNull.Value : (object)decimal.Parse(value);

            return decimal.TryParse(value, out decimalValue) ? (object)decimalValue : DBNull.Value;
        };

    internal static Dictionary<char, Func<byte[], object>> GetMappingFunctions(
        Encoding currentEncoding,
        bool correctFarsiCharacters)
    {
        var _mapFunctions = new Dictionary<char, Func<byte[], object>>();

        _mapFunctions.Add('F', ToDouble);

        _mapFunctions.Add('O', ToDouble);
        _mapFunctions.Add('+', ToDouble);
        _mapFunctions.Add('I', ToInt);
        _mapFunctions.Add('Y', ToDecimal);

        _mapFunctions.Add('L', (input) => ConvertDbfLogicalValueToBoolean(input));

        //MapFunction.Add('D', (input) => new DateTime(int.Parse(new string(Encoding.ASCII.GetChars(input, 0, 4))),
        //                                                int.Parse(new string(Encoding.ASCII.GetChars(input, 4, 2))),
        //                                                int.Parse(new string(Encoding.ASCII.GetChars(input, 6, 2)))));

        _mapFunctions.Add('D', (input) =>
        {
            var first = new string(Encoding.ASCII.GetChars(input, 0, 4));
            var second = new string(Encoding.ASCII.GetChars(input, 4, 2));
            var third = new string(Encoding.ASCII.GetChars(input, 6, 2));

            if (string.IsNullOrEmpty(first + second + third) || string.IsNullOrWhiteSpace(first + second + third))
            {
                return null;
            }
            else
            {
                var year = int.Parse(first);
                var month = int.Parse(second);
                var day = int.Parse(third);

                if (year + month + day == 0) //in the case; first : 0000, second : 00, third : 00
                {
                    return null;
                }

                return new DateTime(year, month, day);
            }
        });


        _mapFunctions.Add('M', (input) => input);

        _mapFunctions.Add('B', (input) => input);

        _mapFunctions.Add('P', (input) => input);

        _mapFunctions.Add('N', ToDouble);

        _mapFunctions.Add('C', (input) => ConvertDbfGeneralToString(input, currentEncoding, correctFarsiCharacters));

        _mapFunctions.Add('G', (input) => ConvertDbfGeneralToString(input, currentEncoding, correctFarsiCharacters));

        _mapFunctions.Add('V', (input) => ConvertDbfGeneralToString(input, currentEncoding, correctFarsiCharacters));

        return _mapFunctions;
    }


    internal static byte[] Encode(object value, byte length, Encoding encoding)
    {
        byte[] result = new byte[length];

        if (value == null)
        {
            return result;
        }

        switch (value)
        {
            case string stringValue:
                result = GetBytes(stringValue, result, encoding);
                break;

            case DateTime dateTimeValue:
                result = GetBytes(dateTimeValue.ToString("yyyyMMdd"), result, Encoding.ASCII);
                break;

            case bool booleanValue:
                result = GetBytes(booleanValue ? "T" : "F", result, Encoding.ASCII);
                break;

            default:
                result = GetBytes(value.ToString(), result, encoding);
                break;
                //throw new NotImplementedException("DbfFieldDescriptor > Encode");
        }

        //if (value is DateTime dt)
        //{
        //    //value = dt.ToString("yyyyMMdd");

        //    result = GetBytes(dt.ToString("yyyyMMdd"), result, Encoding.ASCII);
        //}

        //else if (value != null)
        //{
        //    //encoding.GetBytes(value.ToString(), 0, value.ToString().Length, temp, 0);
        //    result = GetBytes(value.ToString(), result, encoding);
        //}

        return result;
    }

    private static byte[] GetBytes(string value, byte[] array, Encoding encoding)
    {
        var truncatedString = value;

        var length = array.Length;

        // Encoder en = encoding.GetEncoder().Convert(, 0, 0, null, 0, 0,, 0);
        // Consider using the Encoder.Convert method instead of GetByteCount.
        // The conversion method converts as much data as possible, and does 
        // throw an exception if the output buffer is too small.For continuous 
        // encoding of a stream, this method is often the best choice.

        if (encoding.GetByteCount(value) > length)
        {
            //Truncate Scenario
            truncatedString = new string(value.TakeWhile((c, i) => encoding.GetByteCount(value.Substring(0, i + 1)) < length).ToArray());

            System.Diagnostics.Trace.WriteLine("Truncation occured in writing the dbf file");
            System.Diagnostics.Trace.WriteLine($"Original String: {value}");
            System.Diagnostics.Trace.WriteLine($"Truncated String: {truncatedString}");
            System.Diagnostics.Trace.WriteLine($"Lost String: {value.Replace(truncatedString, string.Empty)}");
            System.Diagnostics.Trace.WriteLine(string.Empty);
        }

        encoding.GetBytes(truncatedString, 0, truncatedString.Length, array, 0);

        return array;
    }
}
