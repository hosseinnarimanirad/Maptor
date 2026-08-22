// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

namespace IRI.Maptor.Core.GsmGprs.TpduParameters;

//
//*********************GSM 03.40v3.5.0 Release 1996*********************
//
//TP-MTI: is a 1 bit field, located within bit no 0 of the first octet
//of SMS-Deliver or SMS-Submit, and to be given the following values:
//0: Message Type: SMS-Deliver
//1: Message Type: SMS-Submit

//*********************GSM 03.40v5.8.1 Release 1996*********************
/// <summary>
/// The TP-Message-Type-Indicator is a 2-bit field, located within bits 
/// no 0 and 1 of the first octet of all PDUs which can be given the 
/// following values:
/// 
/// bit1    bit0    Message Type
/// 0       0       SMS Deliver (in the direction SC to MS)
/// 0       0       SMS Deliver Report (in the direction MS to SC)
/// 1       0       SMS Status Report (in the direction SC to MS)
/// 1       0       SMS Command (in the direction MS to SC)
/// 0       1       SMS Submit (in the direction MS to SC)
/// 0       1       SMS Submit Report (in the direction SC to MS)
/// 1       1       Reserved
/// If an MS receives a TPDU with a "Reserved" value in the TP-MTI it 
/// shall process the message as if it were an "SMS-DELIVER" but store 
/// the message exactly as received.
/// </summary>
public enum TP_MessageTypeIndicator
{
    SmsDeliver = 0,
    SmsSubmit = 1,
    SmsStatusReport = 2,
    Reserved = 3
}
