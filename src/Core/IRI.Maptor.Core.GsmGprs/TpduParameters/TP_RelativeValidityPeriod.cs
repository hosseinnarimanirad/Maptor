// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

namespace IRI.Maptor.Core.GsmGprs.TpduParameters;

/// <summary>
///TP-VP: is given in either integer or semi octet representation.
///in the first case: 1 octet; giving the length of the validity period, 
///counted from when the SMS Submit is received by the SC.
///in the second case: 7 octet, giving the absolute time of the validity 
///period termination. 
///In the first case, the representation of time is as follows:
///TP-VP value               Validity period value
///0   to 143                (TP-VP + 1) * 5 minutes 
///144 to 167                12 hours + ((TP-VP - 143) * 30 minutes)
///168 to 196                (TP-VP - 166) * 1 day
///197 to 255                (TP-VP - 192) * 1 week
///in the second case is identical to TP_SCTS
/// </summary>
public struct TP_RelativeValidityPeriod : IValidityPeriod
{
    private byte byteValue;

    private string octetValue;

    public TP_RelativeValidityPeriod(byte value)
    {
        this.byteValue = value;
        //if (value < 144)
        //{
        //    this.integerValue = (value + 1) * 5;            //min
        //}
        //else if (value < 168)
        //{
        //    this.integerValue = 12 + ((value - 143) * 30);  //hours
        //}
        //else if (value < 197)
        //{
        //    this.integerValue = (value - 166);              //day
        //}
        //else
        //{
        //    this.integerValue = (value - 192);              //week
        //}

        this.octetValue = this.byteValue.ToString("X2");
    }

    public string OctetRepresentation
    {
        get { return this.octetValue; }
    }

    public static TP_RelativeValidityPeriod Maximum
    {
        get { return new TP_RelativeValidityPeriod(byte.MaxValue); }
    }

}
