using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Core.Ogc.SFA;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct OgcLineString<T> : IOgcGeometry, IOgcLineString where T : IOgcPoint
{
    byte byteOrder;

    UInt32 type;

    UInt32 numPoints;

    OgcPointCollection<T> points;

    public IOgcPointCollection Points
    {
        get { return this.points; }
    }

    public WkbByteOrder ByteOrder
    {
        get { return (WkbByteOrder)this.byteOrder; }
    }

    public WkbGeometryType Type { get { return (WkbGeometryType)this.type; } }

    public byte[] ToWkb()
    {
        using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
        {
            System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream);

            writer.Write(byteOrder);

            writer.Write(type);

            writer.Write(numPoints);

            foreach (T item in points)
            {
                writer.Write(IRI.Maptor.Core.Common.Helpers.StreamHelper.StructureToByteArray(item));
            }

            writer.Close();

            return stream.ToArray();
        }
    }

    public OgcLineString(byte[] buffer)
        : this(new System.IO.BinaryReader(new System.IO.MemoryStream(buffer)))
    {
    }

    public OgcLineString(System.IO.BinaryReader reader)
    {
        this.byteOrder = reader.ReadByte();

        this.type = reader.ReadUInt32();

        if (WkbGeometryTypeFactory.WkbTypeMap[typeof(OgcLineString<T>)] != (WkbGeometryType)this.type)
        {
            throw new NotImplementedException();
        }

        this.numPoints = reader.ReadUInt32();

        this.points = new OgcPointCollection<T>((int)this.numPoints);

        int length = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));

        for (int i = 0; i < this.numPoints; i++)
        {
            this.points.Add(IRI.Maptor.Core.Common.Helpers.StreamHelper.ByteArrayToStructure<T>(
                                reader.ReadBytes(length)));
        }
    }
}
