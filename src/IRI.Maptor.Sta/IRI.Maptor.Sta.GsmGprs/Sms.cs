using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.GsmGprs; 

//GSM 03.40 P.41
public enum SmscNumberFormat
{
    International = 91,
    Local = 81
}

public enum DataCodingScheme
{
    Alphabet = 0,
    UCS2 = 8
}

public enum StatusReportRequest
{
    RequestReport = 32,
    NoReport = 0
}

public enum ValidPeriod
{
    OneHour = 11, //'0 to 143:(TP-VP+1)*5Min
    ThreeHours = 29,
    SixHours = 71,
    TwelveHours = 143,
    OneDay = 167,
    OneWeek = 196,
    Maximum = 255
}

//Message Type Indicator
public enum TP_MTI
{
    // located within bit no 0 of the first octet of SMS-Deliver or SMS-Submit
    SmsDeliver = 0,
    SmsSubmit = 1,
    //report=2
}

//More Message to Send
public enum TP_MMS
{
    //located within bit no 2 of the first octet of SMS-Deliver
    MoreMessagesWaiting = 0,
    NoMoreMessages = 1
}

// Validity Period Format
public enum TP_VPF
{
    //Located within bit no 3 and 4 of the first octet of SMS-Submit
    NotPresent,     //00
    Relative,       //01
    Spare,          //10
    Absolute        //11
}

//TP_MR: Message Reference; P.41, [0-255]

//TP_OA: Originating Address; is formatted according to the formatting rules of address fields.

//TP_DA: Destination Address; is formatted according to the formatting rules of address fields.

//TP_PID: Protocol Identifier; consist of one octet, and the bits in the octet are used as follows: P.42

//TP_DCS: DataCodingScheme; [0-255], value 0 = alphabet

//TP_SCTS: Service Center Time Stamp;
//is given in semi octet representation and represents the time in the following way:
//                      Year    Month   Day     Hour    Min     Sec     TimeZone
//digits(semi octets)   2       2       2       2       2       2       2
//indicates the difference expressed in quarters of an hour. In the first of the two
//semi octets, the first bit(bit 7 of the seventh octet of the TP_SCTS field) represents
//the sign of this difference (0: positive, 1: negative) 
//[-47 - +48]

//TP_VP: Validity Period
//is given in either integer or semi octet representation.
//in the first case: 1 octet; giving the length of the validity period, counted from
//when the SMS Submit is received by the SC.
//in the second case: 7 octet, giving the absolute time of the validity period termination
//in the first case MORE P.44
//in the second case is identical to TP_SCTS

//TP_UDL: User Data Length; an integer representation of the number of characters within the TP_UD

public static class PduEncoding
{
    /// <summary>
    /// SMSC Number
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static string EncodeServiceCenterNumber(string number, SmscNumberFormat format)
    {
        string result = EncodePhoneNumber(number, format);

        return (((result.Length - 2) / 2 + 1).ToString("X2") + result);
    }

    public static string EncodeDestinationNumber(string number, SmscNumberFormat format)
    {
        string result = EncodePhoneNumber(number, format);

        return (((result.Length - 2)).ToString("X2") + result);
    }

    /// <summary>
    /// Return the decimal semi-octets representation of the phone number
    /// </summary>
    /// <param name="number"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    public static string EncodePhoneNumber(string number, SmscNumberFormat format)
    {
        string tempValue = string.Empty;

        if (format == SmscNumberFormat.International)
        {
            tempValue = number.Replace("+", "");
        }
        else if (number.Contains("+"))
        {
            throw new NotImplementedException();
        }
        else
        {
            tempValue = number;
        }

        return ((int)format).ToString() + phoneNumberToSemiOctets(tempValue);

    }

    private static string phoneNumberToSemiOctets(string number)
    {
        string result = string.Empty;

        if (number.Length % 2 == 1)
        {
            number = number + "F";
        }

        for (int i = 0; i < number.Length - 1; i += 2)
        {
            result += number[i + 1];

            result += number[i];
        }

        return result;
    }

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

public class PduDecoding
{
    string pduCode;

    int length;

    int position;

    string smscNumber;

    TP_MTI messageType;

    byte TP_MMS;//More Message to Send

    byte TP_PID, TP_DCS, TP_VP, TP_UDL;

    string TP_UD;

    string message;

    public PduDecoding(string pduCode)
    {
        this.pduCode = pduCode;

        //length of smsc number
        this.length = int.Parse(pduCode.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);

        //remove international or national 91,81
        position = 4;

        for (int i = 0; i < 2 * (length - 1); i += 2)
        {
            smscNumber += pduCode[position + i + 1];

            smscNumber += pduCode[position + i];
        }

        //if (smscNumber.Contains('F'))
        //{
        //    smscNumber.Replace("F", "");
        //}

        position += 2 * (length - 1);

        int firstOctet = int.Parse(pduCode.Substring(position, 2), System.Globalization.NumberStyles.HexNumber);

        int[] firstOctetbits = ToBase2(firstOctet);

        this.messageType = (TP_MTI)firstOctetbits[0];

        position += 2;

        byte tp_MR = byte.Parse(pduCode.Substring(position, 2), System.Globalization.NumberStyles.HexNumber);

        position += 2;

        //
        //length of des number
        int destLength = int.Parse(pduCode.Substring(position, 2), System.Globalization.NumberStyles.HexNumber);

        destLength += destLength % 2;

        //ignore international or national 91,81
        position += 4;

        string desNumber = string.Empty;

        for (int i = 0; i < destLength; i += 2)
        {
            desNumber += pduCode[position + i + 1];

            desNumber += pduCode[position + i];
        }

        position += destLength;

        this.TP_PID = byte.Parse(pduCode.Substring(position, 2), System.Globalization.NumberStyles.HexNumber);

        position += 2;

        this.TP_DCS = byte.Parse(pduCode.Substring(position, 2), System.Globalization.NumberStyles.HexNumber);

        position += 2;

        this.TP_VP = byte.Parse(pduCode.Substring(position, 2), System.Globalization.NumberStyles.HexNumber);

        position += 2;

        this.TP_UDL = byte.Parse(pduCode.Substring(position, 2), System.Globalization.NumberStyles.HexNumber);

        position += 2;

        this.TP_UD = pduCode.Substring(position, 2 * TP_UDL);

        position += 2;

        this.message = GetString(this.TP_UD);
    }

    private string GetString(string unicode)
    {

        string result = string.Empty;

        for (int i = 0; i < unicode.Length / 4; i++)
        {
            result += (char)int.Parse(unicode.Substring(4 * i, 4), System.Globalization.NumberStyles.HexNumber);
        }

        return result;
    }

    private int ToBase10(int[] binaryValue)
    {
        int result = 0;

        for (int i = 0; i < binaryValue.Length; i++)
        {
            result += (int)(Math.Pow(2, i) * binaryValue[i]);
        }

        return result;
    }

    private int[] ToBase2(int value)
    {
        List<int> result = new List<int>();

        for (int i = 0; i < 8; i++)
        {
            result.Add(value % 2);

            value = (int)Math.Floor(value / 2.0);
        }

        return result.ToArray();
    }
}

public class ShortTextMessage
{
    public string ServiceCenterNumber;
    public string DestinationNumber;
    public SmscNumberFormat ServiceCenterNumberFormat = SmscNumberFormat.International;
    public SmscNumberFormat DestinationNumberFormat = SmscNumberFormat.International;
    public DataCodingScheme DataCodingScheme;
    public StatusReportRequest Report;
    public ValidPeriod Period;
    public string message;

    public byte TP_MTI = 1;
    public byte TP_RD = 0;
    public byte TP_VPF = 16;
    public byte TP_UDHI;
    public int TP_MR;
    public byte TP_PID;
    public byte TP_UDL;

    //public DataCodingScheme GetDataCodingScheme(string message)
    //{
    //    for (int i = 0; i < message.Length; i++)
    //    {
    //        if (((int)message[i]) < 0 || ((int)message[i]) > 255)
    //        {
    //            return MainProject.DataCodingScheme.UCS2;
    //        }
    //    }

    //    return MainProject.DataCodingScheme.Alphabet;
    //}

    //public string Message
    //{
    //    get { return this.message; }
    //    set { }
    //}

    public int MessageLength
    {
        get { return 0; }//this.DataCodingScheme == MainProject.DataCodingScheme.Alphabet ? message.Length : message.Length * 2; }
    }

    public string GetPDUCode()
    {
        if (this.DataCodingScheme ==0)//) MainProject.DataCodingScheme.Alphabet)
        {
            if (this.message.Length > 160)
            {
                throw new NotImplementedException();
            }
        }
        else if (this.DataCodingScheme ==0)// MainProject.DataCodingScheme.UCS2)
        {
            if (this.message.Length > 70)
            {
                // throw new NotImplementedException();
            }
        }

        string result = PduEncoding.EncodeServiceCenterNumber(ServiceCenterNumber, ServiceCenterNumberFormat);

        result += (TP_MTI + TP_VPF + (int)Report + TP_UDHI).ToString("X2");

        result += TP_MR.ToString("X2");

        result += PduEncoding.EncodeDestinationNumber(DestinationNumber, DestinationNumberFormat);

        result += TP_PID.ToString("X2");

        result += ((int)DataCodingScheme).ToString("X2");

        result += ((int)Period).ToString("X2");

        result += this.MessageLength.ToString("X2"); //TP_UDL

        result += PduEncoding.EncodeUCS2(message);

        return result;
    }

}
