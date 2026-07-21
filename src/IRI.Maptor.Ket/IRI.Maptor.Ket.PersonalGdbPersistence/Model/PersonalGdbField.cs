using IRI.Maptor.Ket.PersonalGdbPersistence.Enums;

namespace IRI.Maptor.Ket.PersonalGdbPersistence.Model;

// A user attribute field for PersonalGdb.CreateFeatureClass. OBJECTID, SHAPE,
// SHAPE_Length and SHAPE_Area are created automatically and must not be listed here.
public class PersonalGdbField
{
    public required string Name { get; init; }

    public string? Alias { get; init; }

    public GdbEsriFieldType FieldType { get; init; } = GdbEsriFieldType.esriFieldTypeString;

    // character length for esriFieldTypeString; values above 255 map to an Access memo column
    public int Length { get; init; } = 255;

    public bool IsNullable { get; init; } = true;
}
