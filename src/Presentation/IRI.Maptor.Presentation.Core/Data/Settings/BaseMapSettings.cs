
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.TileServices; 

namespace IRI.Maptor.Presentation.Core.Data;

public class BaseMapSettings : IBaseMapSettings
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

    public BaseMapType InitialBaseMap { get; set; }


    public BaseMapSettings()
    {
        BaseMapCacheDirectory = $"{Environment.CurrentDirectory}\\Data";
        IsBaseMapCacheEnabled = true;
        BaseMapOpacity = 0.7;

        SelectedTileMapAccessMode = TileMapAccessMode.Internet;

        MapProviders = TileMapProviderFactory.GetDefault();

        InitialBaseMap = BaseMapType.Google_Terrain;
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
