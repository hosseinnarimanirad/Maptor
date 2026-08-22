// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

namespace IRI.Maptor.Core.GsmGprs.TpduParameters;

//
//*********************GSM 03.40v5.8.1 Release 1996*********************
//
/// <summary>
/// TP-RD:is a 1 bit field located within bit 2 of the first octet of SMS-SUBMIT and has the following values.
/// Bit no. 2:  0   Instruct the SC to accept an SMS-SUBMIT for an SM still held in the SC which has the same 
///                 TP-MR and the same TP-DA as a previously submitted SM from the same OA.
/// 
///             1   Instruct the SC to reject an SMS-SUBMIT for an SM still held in the SC which has the same 
///                 TP-MR and the same TP-DA as the previously submitted SM from the same OA. 
///                 In this case an appropriate TP-FCS value will be returned in the SMS-SUBMITREPORT.
/// </summary>

public enum TP_RejectDuplicates
{
    AcceptDuplicates = 0,
    RejectDuplicated = 4 //2^(Bit no)
}
