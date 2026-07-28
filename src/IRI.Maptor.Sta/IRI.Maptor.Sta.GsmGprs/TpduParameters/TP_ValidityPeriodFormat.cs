// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

namespace IRI.Maptor.Sta.GsmGprs.TpduParameters;


//
//*********************GSM 03.40v3.5.0 Release 1996*********************
//
//TP-VPF: is a 2 bit field, located within bit no 3 and 4 of the 
//first octet of SMS-Submit, and to be given the following values:
//bit4    bit3
//0       0:      TP-VP field not present
//1       0:      TP-VP field present and integer represented (relative)
//0       1:      Spare
//1       1:      TP-VP field present and semi-octet represented (absolute)

//*********************GSM 03.40v5.8.1 Release 1996*********************
//
/// <summary>
/// TP-VPF: is a 2 bit field, located within bit no 3 and 4 of the 
/// first octet of SMS-Submit, and to be given the following values:
/// bit4    bit3
/// 0       0:      TP-VP field not present
/// 1       0:      TP-VP field present - relative format
/// 0       1:      TP-VP field present - enhanced format
/// 1       1:      TP-VP field present - absolute format
/// </summary>
public enum TP_ValidityPeriodFormat
{
    NotPresent = 0,
    Relative = 16,      //1 * 2^4 + 0 * 2^3
    Enhanced = 8,       //0 * 2^4 + 1 * 2^3
    Absolute = 34       //1 * 2^4 + 1 * 2^3
}
