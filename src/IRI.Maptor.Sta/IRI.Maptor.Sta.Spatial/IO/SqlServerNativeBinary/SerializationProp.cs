namespace IRI.Maptor.Sta.Spatial.IO.SqlServerNativeBinary;

[Flags]
public enum SerializationProp : byte
{
    /// <summary>
    /// The structure has Z values (bit 0, 0x01)
    /// </summary>
    Z = 0x01,

    /// <summary>
    /// The structure has M values (bit 1, 0x02)
    /// </summary>
    M = 0x02,

    /// <summary>
    /// Geography is valid. For GEOGRAPHY structures,
    /// V in version 1 is always set (bit 2, 0x04)
    /// </summary>
    V = 0x04,

    /// <summary>
    /// Geography contains a single point. When P is set,
    /// Number of Points, Number of Figures, and Number of
    /// Shapes are implicitly assumed to be equal to 1 and
    /// are omitted from the structure (bit 3, 0x08)
    /// </summary>
    P = 0x08,

    /// <summary>
    /// Geography contains a single line segment.
    /// When L is set, Number of Points is implicitly
    /// assumed to be equal to 2 and does not explicitly
    /// appear in the serialized data (bit 4, 0x10)
    /// </summary>
    L = 0x10
}
