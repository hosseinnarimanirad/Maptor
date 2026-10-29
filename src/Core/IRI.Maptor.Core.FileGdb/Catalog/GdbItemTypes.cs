namespace IRI.Maptor.Core.FileGdb.Catalog;

/// <summary>
/// Esri item type ids used in the Type column of GDB_Items. They are fixed constants: every
/// geodatabase lists the same ids in GDB_ItemTypes.
/// </summary>
internal static class GdbItemTypes
{
    public static readonly Guid FeatureClass = new("70737809-852c-4a03-9e22-2cecea5b9bfa");

    public static readonly Guid Table = new("cd06bc3b-789d-4c51-aafa-a467912b8965");

    public static readonly Guid FeatureDataset = new("74737149-dcb5-4257-8904-b9724e32a530");

    public static readonly Guid CodedValueDomain = new("8c368b12-a12e-4c7e-9638-c9c64e69e98f");

    public static readonly Guid RangeDomain = new("c29da988-8c3e-45f7-8b5c-18e51ee7beb4");

    public static readonly Guid RelationshipClass = new("b606a7e1-fa5b-439c-849c-6e9c2481537b");

    public static readonly Guid Topology = new("767152d3-ed66-4325-8774-420d46674e07");

    public static readonly Guid RasterDataset = new("5ed667a3-9ca9-44a2-8029-d95bf23704b9");
}
