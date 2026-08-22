using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Core.Spatial.Model;

namespace IRI.Maptor.Extensions;

public static class TileInfoExtensions
{
    public static Tile Parse(this TileInfo tileInfo)
    {
        return new Tile(tileInfo.RowNumber, tileInfo.ColumnNumber, tileInfo.ZoomLevel);
    }
}
