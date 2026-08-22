using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Core.Ogc.SFA;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct OgcGenericPoint<T> : IOgcGeometry where T : IOgcPoint, new()
{
    byte byteOrder;

    UInt32 type;

    T point;

    public OgcGenericPoint(double x, double y)
    {
        byteOrder = (byte)WkbByteOrder.WkbNdr;

        type = (UInt32)WkbGeometryType.Point;

        point = new T();
    }

    public double X { get { return this.point.X; } }

    public double Y { get { return this.point.Y; } }

    public WkbByteOrder ByteOrder
    {
        get { return (WkbByteOrder)this.byteOrder; }
    }

    public WkbGeometryType Type
    {
        get { return (WkbGeometryType)this.type; }
    }

    public OgcGenericPoint(byte[] buffer)
        : this(new System.IO.BinaryReader(new System.IO.MemoryStream(buffer)))
    {
    }

    public OgcGenericPoint(System.IO.BinaryReader reader)
    {
        this.byteOrder = reader.ReadByte();

        this.type = reader.ReadUInt32();

        if (WkbGeometryTypeFactory.WkbTypeMap[typeof(OgcGenericPoint<T>)] != (WkbGeometryType)this.type)
        {
            throw new NotImplementedException();
        }

        int length = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));

        point = IRI.Maptor.Core.Common.Helpers.StreamHelper.ByteArrayToStructure<T>(
                            reader.ReadBytes(length));
    }

    public byte[] ToWkb()
    {
        using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
        {
            System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream);

            writer.Write(byteOrder);

            writer.Write(type);

            writer.Write(IRI.Maptor.Core.Common.Helpers.StreamHelper.StructureToByteArray(point));

            writer.Close();

            return stream.ToArray();
        }
    }

}
