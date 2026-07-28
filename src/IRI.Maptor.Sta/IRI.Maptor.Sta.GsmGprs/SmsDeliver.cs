// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;
using IRI.Maptor.Sta.GsmGprs.TpduParameters;

namespace IRI.Maptor.Sta.GsmGprs;

public class SmsDeliver
{
    //GSM 03.40, Version 5.8.1
    //
    //
    //Basic Elements of the SMS-Deliver Type:
    //
    //ABBR.     REFERENCE                   PROVISION   REPRESENTATION
    //-----     ---------                   ---------   --------------
    //TP_MTI    Message Type Indicator      Mandatory   2 bits
    //TP_MMS    More Message to Send        Mandatory   bit
    //TP_RP     Reply Path                  Mandatory   bit
    //TP_UDHI   User Data Header Indicator  Optional    bit
    //TP_SRI    Status Report Indication    Optional    bit
    //TP_OA     Originating Address         Mandatory   2-12 Octets
    //TP_PID    Protocol Identifier         Mandatory   Octet
    //TP_DCS    Data Coding Scheme          Mandatory   Octet
    //TP_SCTS   Service Center Time Stamp   Optional    7 Octet
    //TP_UDL    User Data Length            Mandatory   Integer
    //TP_UD     User Data                   Optional    Dependent on the TP-DCS

    //----------------TP-OA-------------------//
    //Is formatted according to the formatting rules of address fields.

    //----------------TP-PID------------------//
    //consist of one octet, and the bits in the octet are used as follows:
    //MORE ON P.42, GSM 03.40


    //----------------TP-SCTS-----------------//
    //Is given in semi-octet representation, and represents the time in the 
    //following way:
    //                      Year    Month   Day     Hour    Min     Sec     TimeZone
    //digits(semi octets)   2       2       2       2       2       2       2
    //indicates the difference expressed in quarters of an hour. In the first of the two
    //semi octets, the first bit(bit 7 of the seventh octet of the TP_SCTS field) represents
    //the sign of this difference (0: positive, 1: negative) 
    //[-47 - +48]

    private static readonly TP_MessageTypeIndicator m_MessageTypeIndicator = TP_MessageTypeIndicator.SmsSubmit;

    private Tp_MoreMessageToSend m_MoreMessageToSend = Tp_MoreMessageToSend.NoMoreMessagesAreWaiting;

    private TP_StatusReportIndication m_StatusReportIndication = TP_StatusReportIndication.StatusReportWillBeReturnedToTheSME;

    private TP_UserDataHeaderIndicator m_UserDataHeaderIndicator = TP_UserDataHeaderIndicator.UserDataWithNoHeader;

    private TP_ReplyPath m_ReplyPath = TP_ReplyPath.ReplyPathSet;

    private AddressField.Address OriginatingAddress;

    private TP_ProtocolIdentifier m_ProtocolIdentifier = TP_ProtocolIdentifier.NoInterworking;

    private TP_DataCodingScheme m_DataCodingScheme = TP_DataCodingScheme.UCS2Coding;
    
    //private TP_SCTS
    //
    private int m_UserDataLength;

    private string m_UserData;

    //
    //end of first octet

    private int m_MessageReference = 0;


    private string pduCode;

    public SmsDeliver(long smscNumber, AddressField.Address destinationNumber, string message)
    {
        string result = PduEncoder.EncodeServiceCenterNumber(new AddressField.Address(smscNumber));

        //result += ((int)m_MessageTypeIndicator +
        //            (int)m_RejectDuplicates +
        //            (int)m_ValidityPeriodFormat +
        //            (int)m_StatusReportRequest +
        //            (int)m_UserDataHeaderIndicator +
        //            (int)m_ReplyPath).ToString("X2");

        //result += ((int)m_MessageReference).ToString("X2");

        //result += destinationNumber.ToString();

        //result += ((int)m_ProtocolIdentifier).ToString("X2");

        //result += m_DataCodingScheme.OctetRepresentation;

        //result += m_ValidityPeriod.OctetRepresentation;

        ////in the case of UCS2
        //result += (message.Length * 2).ToString("X2");

        //result += PduEncoder.EncodeUCS2(message);

        //this.pduCode = result;

    }

    public string PduCode
    {
        get { return this.pduCode; }
    }

}
