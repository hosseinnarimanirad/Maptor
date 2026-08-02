// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using IRI.Maptor.Sta.Mathematics;
using IRI.Maptor.Sta.DataStructures;
using System.Xml.Serialization;

namespace IRI.Maptor.Ket.DigitalImageProcessing.ImageMatching;

[Serializable()]
public struct Descriptor
{

    public Extremum extremum;

    public double Orientation;

    public double Magnitude;

    [XmlArray]
    public double[] vector;

    private double m_Norm;

    public double Norm
    {
        get { return this.m_Norm; }
    }

    public Descriptor(Extremum extremum, KeyPoint keyPoint, double[] vector)
    {
        if (vector.Length != 128)
        {
            throw new NotImplementedException();
        }

        this.extremum = extremum;

        this.Orientation = keyPoint.Orientation;

        this.Magnitude = keyPoint.Magnitude;

        this.vector = vector;

        this.m_Norm = 0;

        this.m_Norm = Math.Sqrt(Descriptor.InnerMultiplication(this, this));
    }

    public static double InnerMultiplication(Descriptor first, Descriptor second)
    {
        double result =
            first.vector[0] * second.vector[0] + first.vector[1] * second.vector[1] + first.vector[2] * second.vector[2] + first.vector[3] * second.vector[3] + first.vector[4] * second.vector[4] +
            first.vector[5] * second.vector[5] + first.vector[6] * second.vector[6] + first.vector[7] * second.vector[7] + first.vector[8] * second.vector[8] + first.vector[9] * second.vector[9] +
            first.vector[10] * second.vector[10] + first.vector[11] * second.vector[11] + first.vector[12] * second.vector[12] + first.vector[13] * second.vector[13] + first.vector[14] * second.vector[14] +
            first.vector[15] * second.vector[15] + first.vector[16] * second.vector[16] + first.vector[17] * second.vector[17] + first.vector[18] * second.vector[18] + first.vector[19] * second.vector[19] +
            first.vector[20] * second.vector[20] + first.vector[21] * second.vector[21] + first.vector[22] * second.vector[22] + first.vector[23] * second.vector[23] + first.vector[24] * second.vector[24] +
            first.vector[25] * second.vector[25] + first.vector[26] * second.vector[26] + first.vector[27] * second.vector[27] + first.vector[28] * second.vector[28] + first.vector[29] * second.vector[29] +
            first.vector[30] * second.vector[30] + first.vector[31] * second.vector[31] + first.vector[32] * second.vector[32] + first.vector[33] * second.vector[33] + first.vector[34] * second.vector[34] +
            first.vector[35] * second.vector[35] + first.vector[36] * second.vector[36] + first.vector[37] * second.vector[37] + first.vector[38] * second.vector[38] + first.vector[39] * second.vector[39] +
            first.vector[40] * second.vector[40] + first.vector[41] * second.vector[41] + first.vector[42] * second.vector[42] + first.vector[43] * second.vector[43] + first.vector[44] * second.vector[44] +
            first.vector[45] * second.vector[45] + first.vector[46] * second.vector[46] + first.vector[47] * second.vector[47] + first.vector[48] * second.vector[48] + first.vector[49] * second.vector[49] +
            first.vector[50] * second.vector[50] + first.vector[51] * second.vector[51] + first.vector[52] * second.vector[52] + first.vector[53] * second.vector[53] + first.vector[54] * second.vector[54] +
            first.vector[55] * second.vector[55] + first.vector[56] * second.vector[56] + first.vector[57] * second.vector[57] + first.vector[58] * second.vector[58] + first.vector[59] * second.vector[59] +
            first.vector[60] * second.vector[60] + first.vector[61] * second.vector[61] + first.vector[62] * second.vector[62] + first.vector[63] * second.vector[63] + first.vector[64] * second.vector[64] +
            first.vector[65] * second.vector[65] + first.vector[66] * second.vector[66] + first.vector[67] * second.vector[67] + first.vector[68] * second.vector[68] + first.vector[69] * second.vector[69] +
            first.vector[70] * second.vector[70] + first.vector[71] * second.vector[71] + first.vector[72] * second.vector[72] + first.vector[73] * second.vector[73] + first.vector[74] * second.vector[74] +
            first.vector[75] * second.vector[75] + first.vector[76] * second.vector[76] + first.vector[77] * second.vector[77] + first.vector[78] * second.vector[78] + first.vector[79] * second.vector[79] +
            first.vector[80] * second.vector[80] + first.vector[81] * second.vector[81] + first.vector[82] * second.vector[82] + first.vector[83] * second.vector[83] + first.vector[84] * second.vector[84] +
            first.vector[85] * second.vector[85] + first.vector[86] * second.vector[86] + first.vector[87] * second.vector[87] + first.vector[88] * second.vector[88] + first.vector[89] * second.vector[89] +
            first.vector[90] * second.vector[90] + first.vector[91] * second.vector[91] + first.vector[92] * second.vector[92] + first.vector[93] * second.vector[93] + first.vector[94] * second.vector[94] +
            first.vector[95] * second.vector[95] + first.vector[96] * second.vector[96] + first.vector[97] * second.vector[97] + first.vector[98] * second.vector[98] + first.vector[99] * second.vector[99] +
            first.vector[100] * second.vector[100] + first.vector[101] * second.vector[101] + first.vector[102] * second.vector[102] + first.vector[103] * second.vector[103] + first.vector[104] * second.vector[104] +
            first.vector[105] * second.vector[105] + first.vector[106] * second.vector[106] + first.vector[107] * second.vector[107] + first.vector[108] * second.vector[108] + first.vector[109] * second.vector[109] +
            first.vector[110] * second.vector[110] + first.vector[111] * second.vector[111] + first.vector[112] * second.vector[112] + first.vector[113] * second.vector[113] + first.vector[114] * second.vector[114] +
            first.vector[115] * second.vector[115] + first.vector[116] * second.vector[116] + first.vector[117] * second.vector[117] + first.vector[118] * second.vector[118] + first.vector[119] * second.vector[119] +
            first.vector[120] * second.vector[120] + first.vector[121] * second.vector[121] + first.vector[122] * second.vector[122] + first.vector[123] * second.vector[123] + first.vector[124] * second.vector[124];

        return result;
    }

    public static double InnerMultiplicationSlow(Descriptor first, Descriptor second)
    {
        double result = 0;

        for (int i = 0; i < first.vector.Length; i++)
        {
            result += first.vector[i] * second.vector[i];
        }

        return result;
    }

    public static double CalculateAngle(Descriptor first, Descriptor second)
    {
        return Math.Acos(Math.Round(Descriptor.InnerMultiplication(first, second) / (first.Norm * second.Norm), 15));
    }

    public static double CalculateDistance(Descriptor first, Descriptor second)
    {
        double dy = first.extremum.Row - second.extremum.Row;

        double dx = first.extremum.Column - second.extremum.Column;

        return Math.Sqrt(dx * dx + dy * dy);
    }

}
