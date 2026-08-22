using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Core.Ogc.SFA;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct OgcTriangle<T> : IOgcGeometry where T : IOgcPoint, new()
{
    byte byteOrder;

    UInt32 type;

    UInt32 numRings;

    OgcLinearRing<T>[] rings;

    public OgcLinearRing<T>[] Rings { get { return this.rings; } }

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

            writer.Write(numRings);

            foreach (OgcLinearRing<T> item in Rings)
            {
                writer.Write(item.ToBinary());
            }

            writer.Close();

            return stream.ToArray();
        }
    }

    public OgcTriangle(byte[] buffer)
        : this(new System.IO.BinaryReader(new System.IO.MemoryStream(buffer)))
    {
    }

    public OgcTriangle(System.IO.BinaryReader reader)
    {
        this.byteOrder = reader.ReadByte();

        this.type = reader.ReadUInt32();

        if (WkbGeometryTypeFactory.WkbTypeMap[typeof(OgcTriangle<T>)] != (WkbGeometryType)this.type)
        {
            throw new NotImplementedException();
        }

        this.numRings = reader.ReadUInt32();

        if (this.numRings != 1)
        {
            throw new NotImplementedException();
        }

        this.rings = new OgcLinearRing<T>[this.numRings];

        for (int i = 0; i < this.numRings; i++)
        {
            this.rings[i] = new OgcLinearRing<T>(reader);
        }
    }
}
