using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Common.Primitives;

public class PointM : Point, IHasM
{
    public double M { get; set; }

    public override byte[] AsWkb()
    {
        byte[] result = new byte[29];

        result[0] = (byte)WkbByteOrder.WkbNdr;

        Array.Copy(BitConverter.GetBytes((int)WkbGeometryType.PointM), 0, result, 1, BaseConversionHelper.IntegerSize);

        Array.Copy(BitConverter.GetBytes(X), 0, result, 5, BaseConversionHelper.DoubleSize);

        Array.Copy(BitConverter.GetBytes(Y), 0, result, 13, BaseConversionHelper.DoubleSize);

        Array.Copy(BitConverter.GetBytes(M), 0, result, 21, BaseConversionHelper.DoubleSize);

        return result;
    }
     
}
