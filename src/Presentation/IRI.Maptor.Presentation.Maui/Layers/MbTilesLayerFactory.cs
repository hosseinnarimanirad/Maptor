using IRI.Maptor.Infrastructure.Sqlite.MbTiles;

using Microsoft.Maui.Graphics;

namespace IRI.Maptor.Presentation.Maui.Layers;

/// <summary>
/// Turns an <c>.mbtiles</c> file into one or more <see cref="MapLayer"/>s: a single
/// <see cref="MbTilesRasterLayer"/> for raster (PNG/JPG) files, or one
/// <see cref="MbTilesVectorLayer"/> per sub-layer for vector (MVT/pbf) files, all sharing one
/// <see cref="MbTilesVectorTileProvider"/>.
/// </summary>
public static class MbTilesLayerFactory
{
    /// <summary>Raster placeholder color (a raster layer draws tiles, not styled geometry).</summary>
    private static readonly Color RasterColor = Colors.Gray;

    /// <summary>
    /// Builds the map layer(s) for the given file. Returns an empty list if the file has no
    /// readable layers. Throws if the file cannot be opened as MBTiles.
    /// </summary>
    public static IReadOnlyList<MapLayer> CreateLayers(string filePath, string name)
    {
        if (IsVectorMbTiles(filePath))
        {
            return CreateVectorLayers(filePath);
        }

        return new List<MapLayer> { new MbTilesRasterLayer(filePath, name, RasterColor) };
    }

    /// <summary>True if the file's <c>format</c> metadata indicates vector tiles (pbf/mvt).</summary>
    public static bool IsVectorMbTiles(string filePath)
    {
        using var reader = new MbTilesReader(filePath);
        reader.Open();

        var format = reader.Metadata?.Format?.Trim().ToLowerInvariant();

        return format is "pbf" or "mvt";
    }

    private static IReadOnlyList<MapLayer> CreateVectorLayers(string filePath)
    {
        // The provider is shared by every sub-layer so each physical tile is read/decoded once.
        var provider = new MbTilesVectorTileProvider(filePath);

        var layers = new List<MapLayer>();

        foreach (var info in provider.VectorLayers)
        {
            layers.Add(new MbTilesVectorLayer(provider, info, MbTilesColor.FromId(info.Id)));
        }

        return layers;
    }
}
