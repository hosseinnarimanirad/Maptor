// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

namespace IRI.Maptor.Core.GsmGprs.TpduParameters;

//
//*********************GSM 03.40v5.8.1 Release 1996*********************
//
/// <summary>
/// TP-SRI:is a 1-bit field, located within bit no. 5 of the first octet of SMS-DELIVER,
/// and to be given the following values:
/// Bit no. 5:  0 A status report will not be returned to the SME
///             1 A status report will be returned to the SME
/// </summary>
public enum TP_StatusReportIndication
{
    StatusReportWillNotBeReturnedToTheSME = 0,
    StatusReportWillBeReturnedToTheSME = 1,
}


