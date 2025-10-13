using System;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Model;
using IRI.Maptor.Jab.Common.Localization;

namespace IRI.Maptor.Jab.Common.TileServices;

public class TileMapProvider : ValueObjectNotifier, IDisposable
{
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


    //public bool RequireInternetConnection { get; set; } = true;
    public TileMapProviderMode Mode { get; protected set; } = TileMapProviderMode.Internet;


    public string FullName => $"{ProviderEn}-{MapTypeEn}";

    public string Title { get { return $"{Provider}-{MapType}"; } }

    public Func<TileInfo, string>? MakeInternetUrl { get; protected set; }

    public Func<TileInfo, string>? MakeInteranetUrl { get; protected set; }

    public string? LocalNetworkBaseUrl { get; protected set; }

    //in the case of google traffic map, caching should be avoided
    public bool AllowCache { get; set; } = true;

    public bool ShouldBeConnectedToInternet() => Mode == TileMapProviderMode.Internet && LocalNetworkBaseUrl == null;


    public TileMapProvider(TileMapProvider mapProvider)
    {
        if (mapProvider.Mode == TileMapProviderMode.Internet)
            this.MakeInternetUrl = mapProvider.MakeInternetUrl;

        else
            this.MakeInteranetUrl = mapProvider.MakeInternetUrl;

        this.Mode = mapProvider.Mode;

        this._providerResourceKey = mapProvider._providerResourceKey;
        this._mapTypeResourceKey = mapProvider._mapTypeResourceKey;
        this._thumbnail = mapProvider._thumbnail;
        this._thumbnail72 = mapProvider._thumbnail72;

        LocalizationManager.Instance.LanguageChanged -= Instance_LanguageChanged;
        LocalizationManager.Instance.LanguageChanged += Instance_LanguageChanged;
    }

    public TileMapProvider(
        string providerResourceKey,
        string mapTypeResourceKey,
        Func<TileInfo, string> urlFunction,
        byte[]? thumbnail,
        byte[]? thumbnail72,
        TileMapProviderMode mode = TileMapProviderMode.Internet)
    {
        if (mode == TileMapProviderMode.Internet)
            this.MakeInternetUrl = urlFunction;

        else
            this.MakeInteranetUrl = urlFunction;

        this.Mode = mode;

        this._providerResourceKey = providerResourceKey;
        this._mapTypeResourceKey = mapTypeResourceKey;
        this._thumbnail = thumbnail;
        this._thumbnail72 = thumbnail72;

        LocalizationManager.Instance.LanguageChanged -= Instance_LanguageChanged;
        LocalizationManager.Instance.LanguageChanged += Instance_LanguageChanged;
    }

    private void Instance_LanguageChanged()
    {
        RaisePropertyChanged(nameof(Provider));
        RaisePropertyChanged(nameof(MapType));
        RaisePropertyChanged(nameof(Title));
    }


    public virtual string? GetUrl(TileInfo tile)
    {
        if (this.Mode == TileMapProviderMode.LocalNetwork && MakeInteranetUrl is null)
            return $"{LocalNetworkBaseUrl}/{ProviderEn}/{MapTypeEn}/{tile.ZoomLevel}/{tile.RowNumber}/{tile.RowNumber}_{tile.ColumnNumber}.png";
        
        else if (this.Mode == TileMapProviderMode.LocalNetwork)
            return MakeInteranetUrl?.Invoke(tile);

        else
            return MakeInternetUrl?.Invoke(tile);
    }

    public override string ToString() => Title;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ProviderEn;
        yield return MapTypeEn;
    }

    //public override bool Equals(object obj)
    //{
    //    if (object.Equals(obj, null))
    //    {
    //        return false;
    //    }

    //    return (obj as TileMapProvider)?.Name?.EqualsIgnoreCase(this.Name) == true;
    //}

    //public override int GetHashCode()
    //{
    //    return this.ToString().GetHashCode();
    //}

    //public static bool operator ==(TileMapProvider first, TileMapProvider second)
    //{
    //    //using object.Equals handle the case of null==null otherwise it will use Equals and return false in this case 
    //    return object.Equals(first, second);
    //}

    //public static bool operator !=(TileMapProvider first, TileMapProvider second)
    //{
    //    //using object.Equals handle the case of null==null otherwise it will use Equals and return false in this case
    //    return !object.Equals(first, second);
    //}

    public bool Is(string? fullName) => !string.IsNullOrWhiteSpace(fullName) && this.FullName.EqualsIgnoreCase(fullName);

    public void ChangeMode(TileMapProviderMode newMode, string? localNetworkBaseUrl)
    {
        if (this.Mode == newMode)
            return;

        this.Mode = newMode;

        if (newMode == TileMapProviderMode.LocalNetwork)
            this.LocalNetworkBaseUrl = localNetworkBaseUrl;
    }

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
