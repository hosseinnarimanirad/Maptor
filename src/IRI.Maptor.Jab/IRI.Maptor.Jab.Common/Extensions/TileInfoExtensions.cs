using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Sta.Spatial.Model;

namespace IRI.Maptor.Extensions;

public static class TileInfoExtensions
{
    public static Tile Parse(this TileInfo tileInfo)
    {
        return new Tile(tileInfo.RowNumber, tileInfo.ColumnNumber, tileInfo.ZoomLevel);
    }
}
