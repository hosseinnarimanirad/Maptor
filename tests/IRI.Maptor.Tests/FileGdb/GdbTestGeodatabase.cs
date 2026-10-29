using System.IO;
using System.Text;

using IRI.Maptor.Core.FileGdb.Enums;

namespace IRI.Maptor.Tests.FileGdb;

/// <summary>
/// Writes the two system tables a synthetic geodatabase needs: GDB_SystemCatalog and GDB_Items.
/// </summary>
internal static class GdbTestGeodatabase
{
    public const string Ns = "xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.8'";

    public static void WriteVarString(BinaryWriter w, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        GdbTestTableBuilder.WriteVarUInt(w, (ulong)bytes.Length);
        w.Write(bytes);
    }

    public static void WriteSystemCatalog(string folder, params (long Number, string Name)[] tables)
    {
        var builder = new GdbTestTableBuilder()
            .ObjectIdField("ID")
            .StringField("Name", 160, nullable: false)
            .FixedField("FileFormat", GdbFieldType.Int32, 4, nullable: false);

        foreach (var (number, name) in tables)
            builder.Row(number, GdbTestTableBuilder.Record(w => { WriteVarString(w, name); w.Write(0); }));

        builder.Write(folder, "a00000001");
    }

    public static void WriteItems(string folder, params (Guid Type, string Name, string Path, string Definition)[] items)
    {
        var builder = new GdbTestTableBuilder()
            .ObjectIdField("ObjectID")
            .ShortField("UUID", GdbFieldType.GlobalId, 38, nullable: false)
            .ShortField("Type", GdbFieldType.Guid, 38, nullable: false)
            .StringField("Name", 160, nullable: false)
            .StringField("PhysicalName", 160, nullable: false)
            .StringField("Path", 260, nullable: false)
            .ShortField("Definition", GdbFieldType.Xml, 0, nullable: false);

        long objectId = 1;
        foreach (var (type, name, path, definition) in items)
        {
            builder.Row(objectId++, GdbTestTableBuilder.Record(w =>
            {
                w.Write(Guid.NewGuid().ToByteArray());
                w.Write(type.ToByteArray());
                WriteVarString(w, name);
                WriteVarString(w, name.ToUpperInvariant());
                WriteVarString(w, path);
                WriteVarString(w, definition);
            }));
        }

        builder.Write(folder, "a00000004");
    }
}
