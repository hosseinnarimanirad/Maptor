using IRI.Maptor.Sta.Spatial.Model;

namespace IRI.Maptor.Jab.Core.TileServices;

public abstract class TileServiceUrlStrategy
{
    public abstract string? GetUrl(TileInfo tile);
}
