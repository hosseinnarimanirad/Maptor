using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace IRI.Maptor.Core.Ogc.SFA;

//[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct OgcLinearRing<T> : IOgcLinearRing where T : IOgcPoint
{
    UInt32 numPoints;

    OgcPointCollection<T> points;

    public IOgcPointCollection Points
    {
        get { return this.points; }
    }

    public byte[] ToBinary()
    {
        using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
        {
            System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream);

            writer.Write(numPoints);

            foreach (T item in points)
            {
                writer.Write(IRI.Maptor.Core.Common.Helpers.StreamHelper.StructureToByteArray(item));
            }

            writer.Close();

            return stream.ToArray();
        }
    }

    public OgcLinearRing(byte[] buffer)
        : this(new System.IO.BinaryReader(new System.IO.MemoryStream(buffer)))
    {
    }

    public OgcLinearRing(System.IO.BinaryReader reader)
    {
        this.numPoints = reader.ReadUInt32();

        this.points = new OgcPointCollection<T>((int)this.numPoints);

        int length = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));

        for (int i = 0; i < this.numPoints; i++)
        {
            this.points.Add(IRI.Maptor.Core.Common.Helpers.StreamHelper.ByteArrayToStructure<T>(
                                reader.ReadBytes(length)));
        }
    }

    public IEnumerator<IOgcPoint> GetEnumerator()
    {
        return points.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
