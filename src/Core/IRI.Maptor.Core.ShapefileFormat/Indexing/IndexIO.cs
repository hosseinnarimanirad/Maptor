using IRI.Maptor.Core.Common.Primitives;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.ShapefileFormat.Indexing;

public static class IndexIO
{
    public static void Write(string indexFileName, List<ShpIndex> indexes, bool overwrite = false)
    {
        System.IO.MemoryStream writer = new System.IO.MemoryStream();

        for (int i = 0; i < indexes.Count; i++)
        {
            writer.Write(System.BitConverter.GetBytes(indexes[i].RecordNumber), 0, ShapeConstants.IntegerSize);

            writer.Write(System.BitConverter.GetBytes(indexes[i].MinimumBoundingBox.XMin), 0, ShapeConstants.DoubleSize);

            writer.Write(System.BitConverter.GetBytes(indexes[i].MinimumBoundingBox.YMin), 0, ShapeConstants.DoubleSize);

            writer.Write(System.BitConverter.GetBytes(indexes[i].MinimumBoundingBox.XMax), 0, ShapeConstants.DoubleSize);

            writer.Write(System.BitConverter.GetBytes(indexes[i].MinimumBoundingBox.YMax), 0, ShapeConstants.DoubleSize);
        }

        var mode = Shapefile.GetMode(indexFileName, overwrite);

        System.IO.FileStream stream = new System.IO.FileStream(indexFileName, mode);

        writer.WriteTo(stream);

        writer.Close();

        stream.Close();
    }

    public static Task<List<ShpIndex>> Read(string indexFileName)
    {
        return Task.Run(() =>
        {
            List<ShpIndex> result = new List<ShpIndex>();

            using (var stream = System.IO.File.Open(indexFileName, System.IO.FileMode.Open))
            {
                using (var reader = new System.IO.BinaryReader(stream))
                {
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        result.Add(new ShpIndex()
                        {
                            RecordNumber = reader.ReadInt32(),
                            MinimumBoundingBox = new BoundingBox(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble())
                        });
                    }
                };
            }

            return result;
        });
    }
}

