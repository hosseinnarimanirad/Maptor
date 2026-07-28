// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Globalization;
using System.Text;
using System.Collections.Generic;

namespace IRI.Maptor.Sta.Common.Helpers;


#region 1

/// <summary>
/// A class to allow the conversion of doubles to string representations of
/// their exact decimal values. The implementation aims for readability over
/// efficiency.
/// </summary>
class DoubleConverter
{
    /// <summary>
    /// Converts the given double to a string representation of its
    /// exact decimal value.
    /// </summary>
    /// <param name="d">The double to convert.</param>
    /// <returns>A string representation of the double's exact decimal value.</return>
    public static string ToExactString(double d)
    {
        if (double.IsPositiveInfinity(d))
            return "+Infinity";
        if (double.IsNegativeInfinity(d))
            return "-Infinity";
        if (double.IsNaN(d))
            return "NaN";

        // Translate the double into sign, exponent and mantissa.
        long bits = BitConverter.DoubleToInt64Bits(d);
        // Note that the shift is sign-extended, hence the test against -1 not 1
        bool negative = (bits < 0);
        int exponent = (int)((bits >> 52) & 0x7ffL);
        long mantissa = bits & 0xfffffffffffffL;

        // Subnormal numbers; exponent is effectively one higher,
        // but there's no extra normalisation bit in the mantissa
        if (exponent == 0)
        {
            exponent++;
        }
        // Normal numbers; leave exponent as it is but add extra
        // bit to the front of the mantissa
        else
        {
            mantissa = mantissa | (1L << 52);
        }

        // Bias the exponent. It's actually biased by 1023, but we're
        // treating the mantissa as m.0 rather than 0.m, so we need
        // to subtract another 52 from it.
        exponent -= 1075;

        if (mantissa == 0)
        {
            return "0";
        }

        /* Normalize */
        while ((mantissa & 1) == 0)
        {    /*  i.e., Mantissa is even */
            mantissa >>= 1;
            exponent++;
        }

        /// Construct a new decimal expansion with the mantissa
        ArbitraryDecimal ad = new ArbitraryDecimal(mantissa);

        // If the exponent is less than 0, we need to repeatedly
        // divide by 2 - which is the equivalent of multiplying
        // by 5 and dividing by 10.
        if (exponent < 0)
        {
            for (int i = 0; i < -exponent; i++)
                ad.MultiplyBy(5);
            ad.Shift(-exponent);
        }
        // Otherwise, we need to repeatedly multiply by 2
        else
        {
            for (int i = 0; i < exponent; i++)
                ad.MultiplyBy(2);
        }

        // Finally, return the string with an appropriate sign
        if (negative)
            return "-" + ad.ToString();
        else
            return ad.ToString();
    }

    /// <summary>Private class used for manipulating
    class ArbitraryDecimal
    {
        /// <summary>Digits in the decimal expansion, one byte per digit
        byte[] digits;
        /// <summary> 
        /// How many digits are *after* the decimal point
        /// </summary>
        int decimalPoint = 0;

        /// <summary> 
        /// Constructs an arbitrary decimal expansion from the given long.
        /// The long must not be negative.
        /// </summary>
        internal ArbitraryDecimal(long x)
        {
            string tmp = x.ToString(CultureInfo.InvariantCulture);
            digits = new byte[tmp.Length];
            for (int i = 0; i < tmp.Length; i++)
                digits[i] = (byte)(tmp[i] - '0');
            Normalize();
        }

        /// <summary>
        /// Multiplies the current expansion by the given amount, which should
        /// only be 2 or 5.
        /// </summary>
        internal void MultiplyBy(int amount)
        {
            byte[] result = new byte[digits.Length + 1];
            for (int i = digits.Length - 1; i >= 0; i--)
            {
                int resultDigit = digits[i] * amount + result[i + 1];
                result[i] = (byte)(resultDigit / 10);
                result[i + 1] = (byte)(resultDigit % 10);
            }
            if (result[0] != 0)
            {
                digits = result;
            }
            else
            {
                Array.Copy(result, 1, digits, 0, digits.Length);
            }
            Normalize();
        }

        /// <summary>
        /// Shifts the decimal point; a negative value makes
        /// the decimal expansion bigger (as fewer digits come after the
        /// decimal place) and a positive value makes the decimal
        /// expansion smaller.
        /// </summary>
        internal void Shift(int amount)
        {
            decimalPoint += amount;
        }

        /// <summary>
        /// Removes leading/trailing zeroes from the expansion.
        /// </summary>
        internal void Normalize()
        {
            int first;
            for (first = 0; first < digits.Length; first++)
                if (digits[first] != 0)
                    break;
            int last;
            for (last = digits.Length - 1; last >= 0; last--)
                if (digits[last] != 0)
                    break;

            if (first == 0 && last == digits.Length - 1)
                return;

            byte[] tmp = new byte[last - first + 1];
            for (int i = 0; i < tmp.Length; i++)
                tmp[i] = digits[i + first];

            decimalPoint -= digits.Length - (last + 1);
            digits = tmp;
        }

        /// <summary>
        /// Converts the value to a proper decimal string representation.
        /// </summary>
        public override String ToString()
        {
            char[] digitString = new char[digits.Length];
            for (int i = 0; i < digits.Length; i++)
                digitString[i] = (char)(digits[i] + '0');

            // Simplest case - nothing after the decimal point,
            // and last real digit is non-zero, eg value=35
            if (decimalPoint == 0)
            {
                return new string(digitString);
            }

            // Fairly simple case - nothing after the decimal
            // point, but some 0s to add, eg value=350
            if (decimalPoint < 0)
            {
                return new string(digitString) +
                       new string('0', -decimalPoint);
            }

            // Nothing before the decimal point, eg 0.035
            if (decimalPoint >= digitString.Length)
            {
                return "0." +
                    new string('0', (decimalPoint - digitString.Length)) +
                    new string(digitString);
            }

            // Most complicated case - part of the string comes
            // before the decimal point, part comes after it,
            // eg 3.5
            return new string(digitString, 0,
                               digitString.Length - decimalPoint) +
                "." +
                new string(digitString,
                            digitString.Length - decimalPoint,
                            decimalPoint);
        }
    }
}

#endregion

#region 2

static class BaseTransformations
{
    private const int DecimalRadix = 10;

    private const int MaxBit = 32;

    private static char[] _hexChars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

    private static int[] _hexValues = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

    public static int BaseToDecimal(string completeHex, int radix)
    {

        int value = 0;

        int product = 1;

        for (int i = completeHex.Length - 1; i >= 0; i--, product = product * radix)
        {

            char hex = completeHex[i];

            int hexValue = -1;



            for (int j = 0; j < _hexChars.Length - 1; j++)
            {

                if (_hexChars[j] == hex)
                {

                    hexValue = _hexValues[j];

                    break;

                }

            }



            value += (hexValue * product);

        }

        return value;

    }

    public static string DecimalToBase(int decimalValue, int radix)
    {

        string completeHex = "";



        int[] remainders = new int[MaxBit];

        int maxBit = MaxBit;



        for (; decimalValue > 0; decimalValue = decimalValue / radix)
        {

            maxBit = maxBit - 1;



            remainders[maxBit] = decimalValue % radix;

        }



        for (int i = 0; i < remainders.Length; i++)
        {

            int value = remainders[i];



            if (value >= DecimalRadix)
            {

                completeHex += _hexChars[value % DecimalRadix];

            }

            else
            {

                completeHex += value;

            }

        }



        completeHex = completeHex.TrimStart(new char[] { '0' });



        return completeHex;

    }
}

#endregion
 
public static class BaseConversionHelper
{
    //Lengths are in bytes

    private const int Int32Length = 4;

    private const int DoubleLength = 8;

    private const int HexadecimalUnit = 16;

    private const int HexadecimalLength = 4;

    private const int DecimalUnit = 10;

    private const int OctetUnit = 8;

    //each octet value [0 to 7] can be represented with 3 bits
    private const int OctetLength = 3;

    private const int BinaryUnit = 2;

    private const int minimumBinaryOutputLength = 1;

    public static int IntegerSize { get { return Int32Length; } }

    public static int DoubleSize { get { return DoubleLength; } }
    

    public static string ToBinaryString(int[] binaryRepresentation)
    {
        Array.Reverse(binaryRepresentation);

        StringBuilder result = new StringBuilder();

        for (int i = 0; i < binaryRepresentation.Length; i++)
        {
            result.Append(binaryRepresentation[i]);
        }

        return result.ToString();
    }

    public static int BinaryToDecimal(int[] binaryRepresentation)
    {
        int result = 0;

        for (int i = 0; i < binaryRepresentation.Length; i++)
        {
            if (binaryRepresentation[i] != 0 && binaryRepresentation[i] != 1)
            {
                throw new NotImplementedException();
            }

            result += (int)(Math.Pow(BinaryUnit, i) * binaryRepresentation[i]);
        }

        return result;
    }

    public static int BinaryToDecimal(string binaryRepresentation)
    {
        int result = 0;

        for (int i = 0; i < binaryRepresentation.Length; i++)
        {
            if (binaryRepresentation[i] != '0' && binaryRepresentation[i] != '1')
            {
                throw new NotImplementedException();
            }
            string temp = binaryRepresentation[i].ToString();
            result += (int)(Math.Pow(BinaryUnit, i) * int.Parse(binaryRepresentation[binaryRepresentation.Length - 1 - i].ToString()));
        }

        return result;
    }

    public static string BinaryToHex(string binaryRepresentation)
    {
        return DecimalToHex(BinaryToDecimal(binaryRepresentation));
    }

    public static int BinaryToOctet(string binaryRepresentation)
    {
        return DecimalToOctet(BinaryToDecimal(binaryRepresentation));
    }


    public static int[] DecimalToBinary(int decimalRepresentation)
    {
        return DecimalToBinary(decimalRepresentation, minimumBinaryOutputLength);
    }

    public static int[] DecimalToBinary(int decimalRepresentation, int minimumOutputLength)
    {
        List<int> result = new List<int>();

        do
        {
            result.Add(decimalRepresentation % 2);

            decimalRepresentation = (int)Math.Floor((double)decimalRepresentation / BinaryUnit);

        } while (decimalRepresentation != 0 || result.Count < minimumOutputLength);

        return result.ToArray();
    }

    public static string DecimalToHex(int decimalRepresentation)
    {
        return decimalRepresentation.ToString("X");
    }

    public static string DecimalToHex(int decimalRepresentation, int minimumOutputLength)
    {
        return decimalRepresentation.ToString(string.Format("X{0}", minimumOutputLength));
    }

    public static int DecimalToOctet(int decimalRepresentation)
    {
        List<int> binaryRepresentation = new List<int>(DecimalToBinary(decimalRepresentation));

        int tempNumber = OctetLength - binaryRepresentation.Count % OctetLength;

        for (int i = 0; i < tempNumber; i++)
        {
            binaryRepresentation.Add(0);
        }

        StringBuilder result = new StringBuilder();

        for (int i = binaryRepresentation.Count - 1; i > 1; i -= 3)
        {
            result.Append(BinaryToDecimal(new int[] { binaryRepresentation[i - 2], binaryRepresentation[i - 1], binaryRepresentation[i] }).ToString());
        }

        if (result[0] == '0')
        {
            result.Remove(0, 1);
        }

        return int.Parse(result.ToString());
    }

    public static string DecimalToDecimalSemiOctet(long decimalRepresentation)
    {
        string temp = decimalRepresentation.ToString();

        string result = string.Empty;

        if (temp.Length % 2 == 1)
        {
            temp = temp + "F";
        }

        for (int i = 0; i < temp.Length - 1; i += 2)
        {
            result += temp[i + 1];

            result += temp[i];
        }

        return result;
    }

    public static long DecimalSemiOctetToDecimal(string decimalSemiOctetRepresentation)
    {
        string result = string.Empty;

        for (int i = 0; i < decimalSemiOctetRepresentation.Length - 1; i += 2)
        {
            result += decimalSemiOctetRepresentation[i + 1];

            result += decimalSemiOctetRepresentation[i];
        }

        if (result.Contains("F"))
        {
            result.Replace("F", "");
        }

        return long.Parse(result);
    }


    public static int[] OctetToBinary(int octetRepresentation)
    {
        string temp = octetRepresentation.ToString();

        if (temp.Contains("8") || temp.Contains("9"))
        {
            throw new NotImplementedException();
        }

        List<int> result = new List<int>();

        for (int i = temp.Length - 1; i >= 0; i--)
        {
            result.AddRange(DecimalToBinary(int.Parse(temp[i].ToString()), OctetLength));
        }

        return result.ToArray();
    }

    public static int OctetToDecimal(int octetRepresentation)
    {
        return BinaryToDecimal(OctetToBinary(octetRepresentation));
    }

    public static string OctetToHex(int octetRepresentation)
    {
        return DecimalToHex(OctetToDecimal(octetRepresentation));
    }


    public static int[] HexToBinary(string hexRepresentation)
    {
        return DecimalToBinary(HexToDecimal(hexRepresentation), HexadecimalLength);
    }

    public static int HexToDecimal(string hexRepresentation)
    {
        return int.Parse(hexRepresentation, NumberStyles.HexNumber);
    }

    public static int HexToOctet(string hexRepresentation)
    {
        return DecimalToOctet(HexToDecimal(hexRepresentation));
    }


    
}
