// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

namespace IRI.Maptor.Core.GsmGprs.TpduParameters;

//
//*********************GSM 03.40v3.5.0 Release 1996*********************
//
//TP-MMS: is a 1 bit field, located within bit no 2 of the first octet
//of SMS-Deliver, and to be given the following values:
//0: More messages are waiting for the MS in this SC
//1: No more messages are waiting for the MS in this SC


//*********************GSM 03.40v5.8.1 Release 1996*********************
//
/// <summary>
///The TP-More-Messages-to-Send is a 1-bit field, located within bit no 2 of the first octet of SMS-DELIVER
///and SMS-STATUS-REPORT, and to be given the following values:
///0: More messages are waiting for the MS in this SC
///1: No more messages are waiting for the MS in this SC
/// </summary>
public enum Tp_MoreMessageToSend
{
    MoreMessagesAreWaiting = 0,
    NoMoreMessagesAreWaiting = 1
}
