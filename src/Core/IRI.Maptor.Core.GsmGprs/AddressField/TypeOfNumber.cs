// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj


namespace IRI.Maptor.Core.GsmGprs.AddressField;

//
//*********************GSM 03.40v5.8.1 Release 1996*********************
//

/// <summary>
/// Bits  6   5   4
/// ----  -   -   -   -------------------
///       0   0   0   Unknown
///       0   0   1   International Number
///       0   1   0   National Number
///       0   1   1   Network Specific Number
///       1   0   0   Subscriber Number
///       1   0   1   Alphanumeric, (coded according to GSM TS 03.38 7-bit
///                   default alphabet)
///       1   1   0   Abbreviated Number
///       1   1   1   Reserved for extension
/// </summary>
public enum TypeOfNumber
{
    Unknown = 0,
    InternationalNumber = 1,
    NationalNumber = 2,
    NetworkSpecificNumber = 3,
    SubscriberNumber = 4,
    Alphanumeric = 5,
    AbbreviatedNumber = 6,
    Reserved = 7
}
