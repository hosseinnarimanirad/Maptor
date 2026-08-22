using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Spatial.Model;

public static class TileInfoCollection
{
    public static int GetMinColumn(this IEnumerable<TileInfo> tiles)
    {
        return tiles.Min(t => t.ColumnNumber);
    }

    public static int GetMaxColumn(this IEnumerable<TileInfo> tiles)
    {
        return tiles.Max(t => t.ColumnNumber);
    }

    public static int GetMinRow(this IEnumerable<TileInfo> tiles)
    {
        return tiles.Min(t => t.RowNumber);
    }

    public static int GetMaxRow(this IEnumerable<TileInfo> tiles)
    {
        return tiles.Max(t => t.RowNumber);
    }

    public static int GetTotalWidth(this IEnumerable<TileInfo> tiles)
    {
        return GetMaxColumn(tiles) - GetMinColumn(tiles) + 1;
    }

    public static int GetTotalHeight(this IEnumerable<TileInfo> tiles)
    {
        return GetMaxRow(tiles) - GetMinRow(tiles) + 1;
    }

    public static int GetTotalPixelWidth(this IEnumerable<TileInfo> tiles, int tilePixelWidth = 256)
    {
        return GetTotalWidth(tiles) * tilePixelWidth;
    }

    public static int GetTotalPixelHeight(this IEnumerable<TileInfo> tiles, int tilePixelHeight = 256)
    {
        return GetTotalHeight(tiles) * tilePixelHeight;
    }

    public static BoundingBox GetTotalImageBoundsInPixel(this IEnumerable<TileInfo> tiles, int tilePixelWidth, int tilePixelHeight)
    {
        return new BoundingBox(0, 0, GetTotalPixelWidth(tiles, tilePixelWidth), GetTotalPixelHeight(tiles, tilePixelHeight));
    }

    public static BoundingBox GetTotalImageBoundsInGeodetic(this IEnumerable<TileInfo> tiles)
    {
        var minX = tiles.Min(t => t.GeodeticExtent.XMin);

        var minY = tiles.Min(t => t.GeodeticExtent.YMin);

        var maxX = tiles.Max(t => t.GeodeticExtent.XMax);

        var maxY = tiles.Max(t => t.GeodeticExtent.YMax);

        return new BoundingBox(minX, minY, maxX, maxY);
    }

    //public static BoundingBox GetTotalImageBoundsInMercator(this IEnumerable<TileInfo> tiles)
    //{
    //    var minX = tiles.Min(t => t.MercatorExtent.XMin);

    //    var minY = tiles.Min(t => t.MercatorExtent.YMin);

    //    var maxX = tiles.Max(t => t.MercatorExtent.XMax);

    //    var maxY = tiles.Max(t => t.MercatorExtent.YMax);

    //    return new BoundingBox(minX, minY, maxX, maxY);
    //}

    public static BoundingBox GetTotalImageBoundsInWebMercator(this IEnumerable<TileInfo> tiles)
    {
        var minX = tiles.Min(t => t.WebMercatorExtent.XMin);

        var minY = tiles.Min(t => t.WebMercatorExtent.YMin);

        var maxX = tiles.Max(t => t.WebMercatorExtent.XMax);

        var maxY = tiles.Max(t => t.WebMercatorExtent.YMax);

        return new BoundingBox(minX, minY, maxX, maxY);
    }
}
