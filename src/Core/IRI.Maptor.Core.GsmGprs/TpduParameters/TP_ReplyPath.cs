// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

namespace IRI.Maptor.Core.GsmGprs.TpduParameters;

//
//*********************GSM 03.40v5.8.1 Release 1996*********************
//
/// <summary>
/// TP-RP: is a 1-bit field, located within bit no 7 of the first octet of both SMS-DELIVER and
/// SMS-SUBMIT, and to be given the following values:
/// Bit no 7:   0   TP-Reply-Path parameter is not set in this SMS-SUBMIT/DELIVER
///             1   TP-Reply-Path parameter is set in this SMS-SUBMIT/DELIVER
///                 Please refer to annex D for details about the Reply procedures.
/// </summary>
public enum TP_ReplyPath
{
    ReplyPathNotSet = 0,    //0 * 2^7
    ReplyPathSet = 128,     //1 * 2^7
}
