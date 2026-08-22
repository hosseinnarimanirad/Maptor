// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

//To be implemented later
using IRI.Maptor.Core.Common.Helpers;

namespace IRI.Maptor.Core.GsmGprs.TpduParameters;


public enum CodingGroup
{
    GeneralDataCoding = 0,
    //Bits 3..0 are coded exactly the same as Group 1101, however with bits 7..4 set to
    //1100 the mobile may discard the contents of the message, and only present the
    //indication to the user.
    MessageWaitingIndicationGroup_DiscardMessage = 12,
    MessageWaitingIndicationGroup_StoreMessage1101 = 13,
    MessageWaitingIndicationGroup_StoreMessage1110 = 14,
    DataCoding_MessageClass = 15
}
//
//*********************GSM 03.38v5.6.1 Release 1998*********************
//
//MORE ON Page 8
//
//  7   6   5   4   3   2   1   0
//  
//  0   0   0   0   0   0   0   0       General Data Coding, uncompressed text, No Class, default alphabet
//  0   0   0   0   0   1   0   0       General Data Coding, uncompressed text, No Class, 8 bit data
//  0   0   0   0   1   0   0   0       General Data Coding, uncompressed text, No Class, UCS2(16bit)
//  0   0   0   0   1   1   0   0       General Data Coding, uncompressed text, No Class, Reserved
//  
/// <summary>
/// TP-DCS: indicates the data coding scheme of the TP-UD field, and may 
/// indicate a message class.
/// </summary>
public struct TP_DataCodingScheme
{
    private static readonly int[] DefaultAlphabet = new int[] { 0, 0, 0, 0, 0, 0, 0, 0 };

    //bit mo  0  1  2  3  4  5  6  7
    private static readonly int[] UCS2 = new int[] { 0, 0, 0, 1, 0, 0, 0, 0 };

    private int[] value;

    private int integerRepresentation;

    private string octetRepresentation;

    public TP_DataCodingScheme(int[] binaryRepresentation)
    {
        this.value = binaryRepresentation;

        this.integerRepresentation = BaseConversionHelper.BinaryToDecimal(this.value);

        this.octetRepresentation = this.integerRepresentation.ToString("X2");
    }

    public static TP_DataCodingScheme DefaultAlphabetCoding
    {
        get { return new TP_DataCodingScheme(TP_DataCodingScheme.DefaultAlphabet); }
    }

    public static TP_DataCodingScheme UCS2Coding
    {
        get { return new TP_DataCodingScheme(TP_DataCodingScheme.UCS2); }
    }

    public int IntegerRepresentation
    {
        get { return integerRepresentation; }
    }

    public string OctetRepresentation
    {
        get { return octetRepresentation; }
    }
}
