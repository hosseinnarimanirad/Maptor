using System;

namespace IRI.Maptor.Sta.Spatial.IO.PmTiles;

public sealed class PmTilesWriterOptions
{
    public PmTilesTileType TileType { get; set; } = PmTilesTileType.Unknown;

    public PmTilesCompression InternalCompression { get; set; } = PmTilesCompression.Brotli;

    public PmTilesCompression TileCompression { get; set; } = PmTilesCompression.Gzip;

    public bool ClusterTiles { get; set; } = true;

    public string? MetadataJson { get; set; }

    public byte? MinZoomOverride { get; set; }

    public byte? MaxZoomOverride { get; set; }

    public byte? CenterZoomOverride { get; set; }

    public PmTilesBounds? BoundsOverride { get; set; }

    public PmTilesPosition? CenterOverride { get; set; }
}

