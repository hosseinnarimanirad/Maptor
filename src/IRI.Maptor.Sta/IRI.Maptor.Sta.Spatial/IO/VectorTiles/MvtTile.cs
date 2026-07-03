using System.Collections.Generic;

namespace IRI.Maptor.Sta.Spatial.IO.VectorTiles;

/// <summary>
/// Geometry kind of an MVT feature, matching the <c>GeomType</c> enum of the
/// Mapbox Vector Tile specification.
/// </summary>
public enum MvtGeometryKind
{
    Unknown = 0,
    Point = 1,
    LineString = 2,
    Polygon = 3,
}

/// <summary>Decoded Mapbox Vector Tile: a flat list of named layers.</summary>
public sealed class MvtTile
{
    public List<MvtLayer> Layers { get; } = new List<MvtLayer>();
}

/// <summary>A single layer within an MVT tile.</summary>
public sealed class MvtLayer
{
    public string Name { get; set; } = string.Empty;

    public uint Version { get; set; } = 1;

    /// <summary>Tile-local coordinate extent (default 4096).</summary>
    public uint Extent { get; set; } = 4096;

    public List<MvtFeature> Features { get; } = new List<MvtFeature>();
}

/// <summary>
/// A single feature: its geometry kind, resolved attributes and the raw command/parameter
/// integers of its geometry (decoded later by <see cref="MvtGeometryDecoder"/>).
/// </summary>
public sealed class MvtFeature
{
    public ulong Id { get; set; }

    public MvtGeometryKind GeometryKind { get; set; }

    public Dictionary<string, object?> Attributes { get; set; } = new Dictionary<string, object?>();

    /// <summary>Raw MVT geometry command/parameter integers.</summary>
    public List<uint> Geometry { get; set; } = new List<uint>();
}
