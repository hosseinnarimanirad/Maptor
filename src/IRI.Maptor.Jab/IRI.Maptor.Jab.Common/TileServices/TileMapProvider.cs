using System;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Model;
using IRI.Maptor.Jab.Common.Localization;

namespace IRI.Maptor.Jab.Common.TileServices;

public class TileMapProvider : ValueObjectNotifier, IDisposable
{
    TileServiceUrlStrategy _urlStrategy;


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

    public TileMapProviderMode Mode { get; protected set; } = TileMapProviderMode.Internet;


    //in the case of google traffic map, caching should be avoided
    public bool AllowCache { get; set; } = true;


    //public Func<TileInfo, string>? MakeInternetUrl { get; protected set; }

    //public Func<TileInfo, string>? MakeInteranetUrl { get; protected set; }

    //public string? LocalNetworkBaseUrl { get; protected set; }


    protected TileMapProvider(TileMapProvider mapProvider)
        : this(mapProvider._providerResourceKey, mapProvider._mapTypeResourceKey, mapProvider.Thumbnail, mapProvider.Thumbnail72, mapProvider.Mode)
    {

    }

    protected TileMapProvider(
        string providerResourceKey,
        string mapTypeResourceKey,
        //Func<TileInfo, string> urlFunction,
        byte[]? thumbnail,
        byte[]? thumbnail72,
        TileMapProviderMode mode = TileMapProviderMode.Internet)
    {
        //if (mode == TileMapProviderMode.Internet)
        //    this.MakeInternetUrl = urlFunction;

        //else
        //    this.MakeInteranetUrl = urlFunction;
        //this.MakeInternetUrl = TileMapWebUrlFactory.GetMakeUrlFunc(providerResourceKey, mapTypeResourceKey);
        _urlStrategy = new TileServiceUrlStrategy_Web(providerResourceKey, mapTypeResourceKey);

        this._providerResourceKey = providerResourceKey;
        this._mapTypeResourceKey = mapTypeResourceKey;
        this._thumbnail = thumbnail;
        this._thumbnail72 = thumbnail72;

        this.Mode = mode;

        LocalizationManager.Instance.LanguageChanged -= Instance_LanguageChanged;
        LocalizationManager.Instance.LanguageChanged += Instance_LanguageChanged;
    }

    private void Instance_LanguageChanged()
    {
        RaisePropertyChanged(nameof(Provider));
        RaisePropertyChanged(nameof(MapType));
        RaisePropertyChanged(nameof(Title));
    }

    public bool ShouldBeConnectedToInternet() => Mode == TileMapProviderMode.Internet /*&& LocalNetworkBaseUrl == null*/;

    public override string ToString() => Title;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ProviderEn;
        yield return MapTypeEn;
    }


    public virtual string? GetUrl(TileInfo tile)
    {
        //if (this.Mode == TileMapProviderMode.LocalNetwork && MakeInteranetUrl is null)
        //    return $"{LocalNetworkBaseUrl}/{ProviderEn}/{MapTypeEn}/{tile.ZoomLevel}/{tile.RowNumber}/{tile.RowNumber}_{tile.ColumnNumber}.png";

        //else if (this.Mode == TileMapProviderMode.LocalNetwork)
        //    return MakeInteranetUrl?.Invoke(tile);

        //else
        //    return MakeInternetUrl?.Invoke(tile);

        return _urlStrategy.GetUrl(tile);
    }

    public bool Is(string? fullName) => !string.IsNullOrWhiteSpace(fullName) && this.FullName.EqualsIgnoreCase(fullName);

    // this is the set strategy method for TileMapProvider as the Context of the TileServiceUrlStrategy
    public void ChangeMode(TileMapProviderMode newMode, string? localNetworkBaseUrl, string? proxyAppBaseUrl)
    {
        //if (this.Mode == newMode)
        //    return;

        this.Mode = newMode;

        //if (newMode == TileMapProviderMode.LocalNetwork)
        //    this.LocalNetworkBaseUrl = localNetworkBaseUrl;
        switch (newMode)
        {
            case TileMapProviderMode.Internet:
                _urlStrategy = new TileServiceUrlStrategy_Web(_providerResourceKey, _mapTypeResourceKey);
                break;

            case TileMapProviderMode.LocalNetwork:
                _urlStrategy = new TileServiceUrlStrategy_LocalNetwork(localNetworkBaseUrl!, ProviderEn, MapTypeEn);
                break;

            case TileMapProviderMode.ProxyApp:
                _urlStrategy = new TileServiceUrlStrategy_ProxyApp(proxyAppBaseUrl!, _providerResourceKey, _mapTypeResourceKey);
                break;

            default:
                throw new NotImplementedException("TileMapProvider > ChangeMode!");
        }
    }

    #region Static Factory Methods

    public static TileMapProvider Create(string providerResourceKey, string mapTypeResourceKey, byte[]? thumbnail, byte[]? thumbnail72)
    {
        return new TileMapProvider(providerResourceKey, mapTypeResourceKey, thumbnail, thumbnail72);
    }

    public static TileMapProvider CreateLocalNetwork(string providerResourceKey, string mapTypeResourceKey, byte[]? thumbnail, byte[]? thumbnail72, Func<TileInfo, string> interanetUrlFunc)
    {
        return new TileMapProvider(providerResourceKey, mapTypeResourceKey, thumbnail, thumbnail72, TileMapProviderMode.LocalNetwork)
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
