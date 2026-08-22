// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj


namespace IRI.Maptor.Core.GsmGprs.AddressField;

//
//*********************GSM 03.40v5.8.1 Release 1996*********************
//
//For Type-of-number = 101 bits 3,2,1,0 are reserved and shall be 
//transmitted as 0000. The maximum length of the full address field 
//(Address-Length, Type-of-Address and Address-Value) is 12 octets.

/// <summary>
/// Bits  3   2   1   0
/// ----  -   -   -       -------------------
///       0   0   0   0   Unknown
///       0   0   0   1   ISDN/telephone numbering plan
///       0   0   1   1   Data numbering plan
///       0   1   0   0   Telex numbering plan
///       1   0   0   0   National numbering plan
///       1   0   0   1   Private numbering plan
///       1   0   1   0   ERMES numbering plan
///       1   1   1   1   Reserved for extension
/// All other values are reserved.
/// </summary>
public enum NumberingPlanIdentification
{
    Unknown = 0,
    ISDN_telephoneNumberingPlan = 1,
    DataNumberingPlan = 3,
    TelexNumberingPlan = 4,
    NationalNumberingPlan = 8,
    PrivateNumberingPlan = 9,
    ERMESNumberingPlan = 10,
    Reserved = 15
}
