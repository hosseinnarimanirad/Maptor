using System;
using System.Collections.Generic;
using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Common.TileServices;

namespace IRI.Maptor.Jab.Common.Data;

public class BaseMapSettings : /*ValueObject, */IBaseMapSettings
{
    public string BaseMapCacheDirectory { get; set; }

    public bool IsBaseMapCacheEnabled { get; set; }

    private double _baseMapOpacity;
    public double BaseMapOpacity
    {
        get => _baseMapOpacity;
        set => _baseMapOpacity = Math.Min(1, Math.Max(0, value));
    }


    public string? LocalNetworkUrl { get; set; }

    public string? ProxyAppUrl { get; set; }

    public TileMapAccessMode SelectedTileMapAccessMode { get; set; }

    public List<TileMapProvider> MapProviders { get; set; }


    public BaseMapSettings()
    {
        this.BaseMapCacheDirectory = $"{Environment.CurrentDirectory}\\Data";
        this.IsBaseMapCacheEnabled = true;
        this.BaseMapOpacity = 0.7;

        this.SelectedTileMapAccessMode = TileMapAccessMode.Internet;

        this.MapProviders = TileMapProviderFactory.GetDefault();
    }

    //protected override IEnumerable<object> GetEqualityComponents()
    //{
    //    yield return BaseMapCacheDirectory ?? string.Empty;
    //    yield return IsBaseMapCacheEnabled;
    //    yield return LocalNetworkUrl ?? string.Empty;
    //    yield return ProxyAppUrl ?? string.Empty;
    //}

    public static BaseMapSettings Default => new BaseMapSettings();

}
