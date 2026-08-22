// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

namespace IRI.Maptor.Core.GsmGprs.TpduParameters;

//
//*********************GSM 03.40v5.8.1 Release 1996*********************
//
/// <summary>
/// TP-UDHI: is a 1 bit field within bit 6 of the first octet of an SMS-SUBMIT and
/// SMS-DELIVER PDU and has the following values.
/// Bit no. 6   0   The TP-UD field contains only the short message
///             1   The beginning of the TP-UD field contains a Header in addition to the short message
/// </summary>
public enum TP_UserDataHeaderIndicator
{
    UserDataWithNoHeader = 0,   //0 * 2^6
    UserDataWithHeader = 64,    //1 * 2^6
}
