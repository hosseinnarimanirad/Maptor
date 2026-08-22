using System.Collections.Generic;

namespace IRI.Maptor.Infrastructure.Sqlite.MbTiles;

/// <summary>
/// Represents metadata from an MBTiles database
/// Specification: https://github.com/mapbox/mbtiles-spec
/// </summary>
public class MbTilesMetadata
{
    /// <summary>
    /// The plain-english name of the tileset
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The file format of the tile data: png, jpg, pbf, webp
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// The maximum extent of the rendered map area.
    /// Bounds must define an area covered by all zoom levels.
    /// Format: west,south,east,north (WGS84)
    /// </summary>
    public string? Bounds { get; set; }

    /// <summary>
    /// The longitude, latitude, and zoom level of the default view of the map
    /// Format: longitude,latitude,zoom
    /// </summary>
    public string? Center { get; set; }

    /// <summary>
    /// The lowest zoom level for which the tileset provides data
    /// </summary>
    public int? MinZoom { get; set; }

    /// <summary>
    /// The highest zoom level for which the tileset provides data
    /// </summary>
    public int? MaxZoom { get; set; }

    /// <summary>
    /// A description of the tileset's content
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// An attribution string, which explains the sources of data and/or style
    /// </summary>
    public string? Attribution { get; set; }

    /// <summary>
    /// The type of tileset: overlay or baselayer
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// The version of the tileset
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Any additional metadata stored in the database
    /// </summary>
    public Dictionary<string, string> AdditionalMetadata { get; set; } = new Dictionary<string, string>();

    public override string ToString()
    {
        return $"MBTiles: {Name ?? "Unnamed"} (Format: {Format}, Zoom: {MinZoom}-{MaxZoom})";
    }
}

