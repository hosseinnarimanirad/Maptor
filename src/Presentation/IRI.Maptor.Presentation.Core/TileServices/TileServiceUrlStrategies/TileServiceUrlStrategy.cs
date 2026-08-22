using IRI.Maptor.Core.Spatial.Model;

namespace IRI.Maptor.Presentation.Core.TileServices;

public abstract class TileServiceUrlStrategy
{
    public abstract string? GetUrl(TileInfo tile);
}
