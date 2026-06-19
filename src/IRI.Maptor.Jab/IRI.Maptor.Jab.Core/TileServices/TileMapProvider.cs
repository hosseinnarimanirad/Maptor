using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Core;
using IRI.Maptor.Sta.Spatial.Model;
using IRI.Maptor.Jab.Core.Localization;

namespace IRI.Maptor.Jab.Core.TileServices;

public class TileMapProvider : ValueObjectNotifier, IDisposable
{
    TileServiceUrlStrategy _urlStrategy;

    public BaseMapType Type { get; set; }

    public string FullName => $"{ProviderEn}-{MapTypeEn}";

    public string Title { get { return $"{Provider}-{MapType}"; } }

    private string _providerResourceKey { get; set; }
    public string Provider => LocalizationManager.Instance[_providerResourceKey];
    public string ProviderEn => LocalizationManager.Instance.GetDefaultValue(_providerResourceKey);


    private string _mapTypeResourceKey { get; set; }
    public string MapType => LocalizationManager.Instance[_mapTypeResourceKey];
    public string MapTypeEn => LocalizationManager.Instance.GetDefaultValue(_mapTypeResourceKey);


    private byte[]? _thumbnail;
    public byte[]? Thumbnail
    {
        get { return _thumbnail; }
        protected set
        {
            _thumbnail = value;
            RaisePropertyChanged();
        }
    }

    private byte[]? _thumbnail72;
    public byte[]? Thumbnail72
    {
        get { return _thumbnail72; }
        set
        {
            _thumbnail72 = value;
            RaisePropertyChanged();
        }
    }

    public TileMapAccessMode Mode { get; protected set; } = TileMapAccessMode.Internet;

    //in the case of google traffic map, caching should be avoided
    public bool AllowCache { get; set; } = true;

    protected TileMapProvider(TileMapProvider mapProvider)
        : this(mapProvider.Type, mapProvider._providerResourceKey, mapProvider._mapTypeResourceKey, mapProvider.Thumbnail, mapProvider.Thumbnail72, mapProvider.Mode)
    {
    }

    protected TileMapProvider(
        BaseMapType baseMapType,
        string providerResourceKey,
        string mapTypeResourceKey,
        byte[]? thumbnail,
        byte[]? thumbnail72,
        TileMapAccessMode mode = TileMapAccessMode.Internet)
    {
        _urlStrategy = new TileServiceUrlStrategy_Web(providerResourceKey, mapTypeResourceKey);

        _providerResourceKey = providerResourceKey;
        _mapTypeResourceKey = mapTypeResourceKey;
        _thumbnail = thumbnail;
        _thumbnail72 = thumbnail72;

        Mode = mode;
        Type = baseMapType;

        LocalizationManager.Instance.LanguageChanged -= Instance_LanguageChanged;
        LocalizationManager.Instance.LanguageChanged += Instance_LanguageChanged;
    }

    private void Instance_LanguageChanged()
    {
        RaisePropertyChanged(nameof(Provider));
        RaisePropertyChanged(nameof(MapType));
        RaisePropertyChanged(nameof(Title));
    }

    public bool ShouldBeConnectedToInternet() => Mode == TileMapAccessMode.Internet;

    public override string ToString() => Title;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Type;
        yield return ProviderEn;
        yield return MapTypeEn;
    }


    public virtual string? GetUrl(TileInfo tile) => _urlStrategy.GetUrl(tile);

    public bool Is(string? fullName) => !string.IsNullOrWhiteSpace(fullName) && FullName.EqualsIgnoreCase(fullName);

    // this is the set strategy method for TileMapProvider as the Context of the TileServiceUrlStrategy
    public void ChangeMode(TileMapAccessMode newMode, string? localNetworkBaseUrl, string? proxyAppBaseUrl)
    {
        //if (this.Mode == newMode)
        //    return;

        Mode = newMode;

        switch (newMode)
        {
            case TileMapAccessMode.Internet:
                _urlStrategy = new TileServiceUrlStrategy_Web(_providerResourceKey, _mapTypeResourceKey);
                break;

            case TileMapAccessMode.LocalNetwork:
                _urlStrategy = new TileServiceUrlStrategy_LocalNetwork(localNetworkBaseUrl!, ProviderEn, MapTypeEn);
                break;

            case TileMapAccessMode.ProxyApp:
                _urlStrategy = new TileServiceUrlStrategy_ProxyApp(proxyAppBaseUrl!, _providerResourceKey, _mapTypeResourceKey);
                break;

            default:
                throw new NotImplementedException("TileMapProvider > ChangeMode!");
        }
    }


    #region Static Factory Methods

    public static TileMapProvider Create(BaseMapType type, string providerResourceKey, string mapTypeResourceKey, byte[]? thumbnail, byte[]? thumbnail72)
    {
        return new TileMapProvider(type, providerResourceKey, mapTypeResourceKey, thumbnail, thumbnail72);
    }

    public static TileMapProvider CreateLocalNetwork(BaseMapType type, string providerResourceKey, string mapTypeResourceKey, byte[]? thumbnail, byte[]? thumbnail72, Func<TileInfo, string> interanetUrlFunc)
    {
        return new TileMapProvider(type, providerResourceKey, mapTypeResourceKey, thumbnail, thumbnail72, TileMapAccessMode.LocalNetwork)
        {
            _urlStrategy = new TileServiceUrlStrategy_LocalNetwork(interanetUrlFunc),
        };
    }

    #endregion

    #region IDispose

    private bool _disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                LocalizationManager.Instance.LanguageChanged -= Instance_LanguageChanged;
            }

            // Dispose unmanaged resources here if any
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}