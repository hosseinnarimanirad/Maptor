// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using IRI.Maptor.Core.Common.Helpers;
using System;
using System.Text;

namespace IRI.Maptor.Core.GsmGprs.AddressField;

public struct Address
{
    //
    //*********************GSM 03.40v5.8.1 Release 1996*********************
    //
    //The address length field is an integer representation of the number of useful
    //semi-octets within the address-value field, i.e. excludes any semi octet containing
    //only fill bits.
    //Each address field of the SM-TL and SM-RL consists of an address-length field
    //of one octet, a Type-of-Address field of one octet, and one Address-Value field
    //of variable length
    //
    //
    //*********************GSM 03.40v3.7.0 Release 1996*********************
    //
    //
    //The address field indicates the number of decimal digits within the
    //Address-Value field.
    //

    public int m_AddressLength
    {
        get { return this.m_AddressValue.Length; }
    }

    public TypeOfNumber m_TypeOfNumber;

    public NumberingPlanIdentification m_NumberingPlanIdentification;

    public string m_AddressValue;

    /// <summary>
    /// International phone number
    /// </summary>
    /// <param name="number"></param>
    public Address(long number)
    {
        string stringNumber = number.ToString();

        //this.m_AddressLength = stringNumber.Length;

        this.m_TypeOfNumber = TypeOfNumber.InternationalNumber;

        this.m_NumberingPlanIdentification = NumberingPlanIdentification.ISDN_telephoneNumberingPlan;

        this.m_AddressValue = stringNumber;
    }

    public Address(string codedAddressField)
    {
        int length = int.Parse(codedAddressField.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);

        this.m_AddressValue = BaseConversionHelper.DecimalSemiOctetToDecimal(codedAddressField.Substring(4, codedAddressField.Length - 4)).ToString();

        if (this.m_AddressValue.Length != length)
        {
            throw new NotImplementedException();
        }

        int[] typeOfAddress = BaseConversionHelper.HexToBinary(codedAddressField.Substring(2, 2));

        this.m_TypeOfNumber = (TypeOfNumber)BaseConversionHelper.BinaryToDecimal(new int[] { typeOfAddress[4], typeOfAddress[5], typeOfAddress[6] });

        this.m_NumberingPlanIdentification = (NumberingPlanIdentification)BaseConversionHelper.BinaryToDecimal(new int[] { typeOfAddress[0], typeOfAddress[1], typeOfAddress[2], typeOfAddress[3] });

    }

    private string AddressLengthInHex
    {
        get { return BaseConversionHelper.DecimalToHex(this.m_AddressLength, 2); }
    }

    private string TypeOfAddressInHex
    {
        get
        {
            string numberingPlanIdentification = BaseConversionHelper.ToBinaryString(BaseConversionHelper.DecimalToBinary((int)this.m_NumberingPlanIdentification, 4));

            string typeOfNumber = BaseConversionHelper.ToBinaryString(BaseConversionHelper.DecimalToBinary((int)this.m_TypeOfNumber, 3));

            return BaseConversionHelper.BinaryToHex(string.Format("1{0}{1}", typeOfNumber, numberingPlanIdentification));
        }
    }

    private string AddressValueInDecimalSemiOctet
    {
        get { return BaseConversionHelper.DecimalToDecimalSemiOctet(long.Parse(m_AddressValue)); }
    }

    public string AddressValue
    {
        get { return this.m_AddressValue; }
    }

    public override string ToString()
    {
        return string.Format("{0}{1}{2}", this.AddressLengthInHex, this.TypeOfAddressInHex, this.AddressValueInDecimalSemiOctet);
    }

    public static explicit operator Address(string codedAddressField)
    {
        return new Address(codedAddressField);
    }

    public static explicit operator string(Address addressField)
    {
        return addressField.ToString();
    }
}
