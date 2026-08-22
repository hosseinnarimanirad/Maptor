// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

namespace IRI.Maptor.Core.GsmGprs.TpduParameters;

//
//*********************GSM 03.40v5.8.1 Release 1996*********************
//
/// <summary>
/// TP-SRR: is a 1-bit field, located within bit no. 5 of the first octet of SMS-SUBMIT
/// and SMS-COMMAND, and to be given the following values:
/// Bit no. 5:  0       A status report is not requested
///             1       A status report is requested
/// </summary>
public enum TP_StatusReportRequest
{
    StatusReportIsNotRequested = 0,     //0 * 2^5
    StatusReportIsRequested = 32,       //1 * 2^5
}



