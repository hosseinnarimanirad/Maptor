using IRI.Maptor.Jab.Common.TileServices;
using System.Collections.Generic;

namespace IRI.Maptor.Jab.Common.Data.Settings;

public interface IBaseMapSettings
{
    string BaseMapCacheDirectory { get; set; }
    bool IsBaseMapCacheEnabled { get; set; }

    double BaseMapOpacity { get; set; }

    string? LocalNetworkUrl { get; set; }
    string? ProxyAppUrl { get; set; }

    TileMapAccessMode SelectedTileMapAccessMode { get; set; }

    List<TileMapProvider> MapProviders { get; set; }

    BaseMapType InitialBaseMap { get; set; }
}