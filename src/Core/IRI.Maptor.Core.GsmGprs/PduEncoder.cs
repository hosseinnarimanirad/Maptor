// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.GsmGprs;

public static class PduEncoder
{

    /// <summary>
    /// SMSC Number
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static string EncodeServiceCenterNumber(AddressField.Address number)
    {
        string temp = number.ToString();

        int length = (int.Parse(temp.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) + 2) / 2;

        temp = temp.Remove(0, 2);

        return (length.ToString("X2") + temp);
    }

    //public static string EncodeDestinationNumber(string number, SmscNumberFormat format)
    //{
    //    string result = EncodePhoneNumber(number, format);

    //    return (((result.Length - 2)).ToString("X2") + result);
    //}

    ///// <summary>
    ///// Return the decimal semi-octets representation of the phone number
    ///// </summary>
    ///// <param name="number"></param>
    ///// <param name="format"></param>
    ///// <returns></returns>
    //public static string EncodePhoneNumber(string number, SmscNumberFormat format)
    //{
    //    string tempValue = string.Empty;

    //    if (format == SmscNumberFormat.International)
    //    {
    //        tempValue = number.Replace("+", "");
    //    }
    //    else if (number.Contains("+"))
    //    {
    //        throw new NotImplementedException();
    //    }
    //    else
    //    {
    //        tempValue = number;
    //    }

    //    return ((int)format).ToString() + phoneNumberToSemiOctets(tempValue);

    //}

    //private static string phoneNumberToSemiOctets(string number)
    //{
    //    string result = string.Empty;

    //    if (number.Length % 2 == 1)
    //    {
    //        number = number + "F";
    //    }

    //    for (int i = 0; i < number.Length - 1; i += 2)
    //    {
    //        result += number[i + 1];

    //        result += number[i];
    //    }

    //    return result;
    //}

    public static string EncodeUCS2(string value)
    {
        string result = string.Empty;

        for (int i = 0; i < value.Length; i++)
        {

            int temp = (int)value[i];

            result += temp.ToString("X4");
        }

        return result;

    }
}
