using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Core.Ogc.SFA;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct OgcMultiLineString<T> : IOgcGeometry where T : IOgcPoint
{
    byte byteOrder;

    UInt32 type;

    UInt32 numLineStrings;

    OgcLineString<T>[] lineStrings;

    public WkbByteOrder ByteOrder
    {
        get { return (WkbByteOrder)this.byteOrder; }
    }

    public WkbGeometryType Type
    {
        get { return (WkbGeometryType)this.type; }
    }

    public OgcLineString<T>[] LineStrings { get { return this.lineStrings; } }

    public byte[] ToWkb()
    {
        using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
        {
            System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream);

            writer.Write(byteOrder);

            writer.Write(type);

            writer.Write(numLineStrings);

            foreach (OgcLineString<T> item in LineStrings)
            {
                writer.Write(item.ToWkb());
            }

            writer.Close();

            return stream.ToArray();
        }
    }

    public OgcMultiLineString(byte[] buffer)
        : this(new System.IO.BinaryReader(new System.IO.MemoryStream(buffer)))
    {
    }

    public OgcMultiLineString(System.IO.BinaryReader reader)
    {
        this.byteOrder = reader.ReadByte();

        this.type = reader.ReadUInt32();

        if (WkbGeometryTypeFactory.WkbTypeMap[typeof(OgcMultiLineString<T>)] != (WkbGeometryType)this.type)
        {
            throw new NotImplementedException();
        }

        this.numLineStrings = reader.ReadUInt32();

        this.lineStrings = new OgcLineString<T>[this.numLineStrings];

        for (int i = 0; i < this.numLineStrings; i++)
        {
            this.lineStrings[i] = new OgcLineString<T>(reader);
        }
    }
}
