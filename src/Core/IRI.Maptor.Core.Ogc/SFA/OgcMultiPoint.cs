using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices; 
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Core.Ogc.SFA;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct OgcMultiPoint<T> : IOgcGeometry where T : IOgcPoint, new()
{
    byte byteOrder;

    UInt32 type;

    UInt32 numPoints;

    OgcGenericPoint<T>[] points;

    public OgcGenericPoint<T>[] Points { get { return this.points; } }

    public WkbByteOrder ByteOrder
    {
        get { return (WkbByteOrder)this.byteOrder; }
    }

    public WkbGeometryType Type
    {
        get { return (WkbGeometryType)this.type; }
    }

    public byte[] ToWkb()
    {
        using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
        {
            System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream);

            writer.Write(byteOrder);

            writer.Write(type);

            writer.Write(numPoints);

            foreach (OgcGenericPoint<T> item in Points)
            {
                writer.Write(item.ToWkb());
            }

            writer.Close();

            return stream.ToArray();
        }
    }

    public OgcMultiPoint(byte[] buffer)
        : this(new System.IO.BinaryReader(new System.IO.MemoryStream(buffer)))
    {
    }

    public OgcMultiPoint(System.IO.BinaryReader reader)
    {
        this.byteOrder = reader.ReadByte();

        this.type = reader.ReadUInt32();

        if (WkbGeometryTypeFactory.WkbTypeMap[typeof(OgcMultiPoint<T>)] != (WkbGeometryType)this.type)
        {
            throw new NotImplementedException();
        }

        this.numPoints = reader.ReadUInt32();

        this.points = new OgcGenericPoint<T>[this.numPoints];

        for (int i = 0; i < this.numPoints; i++)
        {
            this.points[i] = new OgcGenericPoint<T>(reader);
        }
    }
}
