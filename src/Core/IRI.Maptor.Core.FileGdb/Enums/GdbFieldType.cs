namespace IRI.Maptor.Core.FileGdb.Enums;

/// <summary>
/// Field type byte stored in a .gdbtable field description.
/// </summary>
public enum GdbFieldType : byte
{
    Int16 = 0,
    Int32 = 1,
    Float32 = 2,
    Float64 = 3,
    String = 4,
    DateTime = 5,
    ObjectId = 6,
    Geometry = 7,
    Binary = 8,
    Raster = 9,
    Guid = 10,
    GlobalId = 11,
    Xml = 12,

    // added in ArcGIS Pro 3.2
    Int64 = 13,
    DateOnly = 14,
    TimeOnly = 15,
    DateTimeWithOffset = 16,
}
