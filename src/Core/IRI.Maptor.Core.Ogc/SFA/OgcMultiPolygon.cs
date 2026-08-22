using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices; 
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Core.Ogc.SFA;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct OgcMultiPolygon<T> : IOgcGeometry where T : IOgcPoint
{
    byte byteOrder;

    UInt32 type;

    UInt32 numPolygons;

    OgcPolygon<T>[] polygons;

    public OgcPolygon<T>[] Polygons { get { return this.polygons; } }

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

            writer.Write(numPolygons);

            foreach (OgcPolygon<T> item in Polygons)
            {
                writer.Write(item.ToWkb());
            }

            writer.Close();

            return stream.ToArray();
        }
    }

    public OgcMultiPolygon(byte[] buffer)
        : this(new System.IO.BinaryReader(new System.IO.MemoryStream(buffer)))
    {
    }

    public OgcMultiPolygon(System.IO.BinaryReader reader)
    {
        this.byteOrder = reader.ReadByte();

        this.type = reader.ReadUInt32();

        if (WkbGeometryTypeFactory.WkbTypeMap[typeof(OgcMultiPolygon<T>)] != (WkbGeometryType)this.type)
        {
            throw new NotImplementedException();
        }

        this.numPolygons = reader.ReadUInt32();

        this.polygons = new OgcPolygon<T>[this.numPolygons];

        for (int i = 0; i < this.numPolygons; i++)
        {
            this.polygons[i] = new OgcPolygon<T>(reader);
        }
    }
}
