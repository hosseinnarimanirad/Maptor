namespace IRI.Maptor.Core.Spatial.IO.SqlServerNativeBinary;

/// <summary>
/// Helper class for parsing SQL Server Native Binary serialization properties
/// </summary>
public static class SerializationPropHelper
{
    /// <summary>
    /// Parses a byte value and returns the set serialization property flags
    /// </summary>
    /// <param name="serializationProperties">The 1-byte serialization properties value</param>
    /// <returns>A SerializationProp enum with all set flags</returns>
    public static SerializationProp ParseFlags(byte serializationProperties)
    {
        SerializationProp flags = 0;

        if ((serializationProperties & 0x01) != 0)
            flags |= SerializationProp.Z;

        if ((serializationProperties & 0x02) != 0)
            flags |= SerializationProp.M;

        if ((serializationProperties & 0x04) != 0)
            flags |= SerializationProp.V;

        if ((serializationProperties & 0x08) != 0)
            flags |= SerializationProp.P;

        if ((serializationProperties & 0x10) != 0)
            flags |= SerializationProp.L;

        return flags;
    }
}


