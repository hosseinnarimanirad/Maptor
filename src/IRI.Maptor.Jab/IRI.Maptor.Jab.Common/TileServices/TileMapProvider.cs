using System;
using System.Collections.Generic;
using IRI.Maptor.Jab.Common.Localization;
using IRI.Maptor.Sta.Spatial.Model;

namespace IRI.Maptor.Jab.Common.TileServices;

public class TileMapProvider : ValueObjectNotifier, IDisposable
{
    public bool RequireInternetConnection { get; set; } = true;

    //in the case of google traffic map, caching should be avoided
    public bool AllowCache { get; set; } = true;
     
     
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

     
    public string FullName
    {
        get
        {
            return $"{ProviderEn}-{MapTypeEn}";
        }
    }

    public string Title { get { return $"{Provider}-{MapType}"; } }

    public Func<TileInfo, string> MakeUrl { get; protected set; }

    //protected TileMapProvider()
    //{

    //}

    //public TileMapProvider(string provider, string mapType, Func<TileInfo, string> urlFunction, byte[]? thumbnail, byte[]? thumbnail72)
    //    : this(PersianEnglishItem.CreateUpperCasedEnglish(string.Empty, provider),
    //            PersianEnglishItem.CreateUpperCasedEnglish(string.Empty, mapType),
    //            urlFunction, thumbnail, thumbnail72)
    //{
    //    //this.MakeUrl = urlFunction;

    //    //this.Provider = new PersianEnglishItem(string.Empty, provider, Model.LanguageMode.English);

    //    //this.MapType = new PersianEnglishItem(string.Empty, mapType, Model.LanguageMode.English);
    //}

    public TileMapProvider(TileMapProvider mapProvider)
    {
        //mapProvider.Provider, mapProvider.MapType, , mapProvider.Thumbnail, mapProvider.Thumbnail72
        this.MakeUrl = mapProvider.MakeUrl;
        this._providerResourceKey = mapProvider._providerResourceKey;
        this._mapTypeResourceKey = mapProvider._mapTypeResourceKey;
        this._thumbnail = mapProvider._thumbnail;
        this._thumbnail72 = mapProvider._thumbnail72;

        LocalizationManager.Instance.LanguageChanged += Instance_LanguageChanged;
    }

    public TileMapProvider(string providerResourceKey, string mapTypeResourceKey, Func<TileInfo, string> urlFunction, byte[]? thumbnail, byte[]? thumbnail72)
    {
        this.MakeUrl = urlFunction;

        this._providerResourceKey = providerResourceKey;

        this._mapTypeResourceKey = mapTypeResourceKey;

        this._thumbnail = thumbnail;

        this._thumbnail72 = thumbnail72;

        LocalizationManager.Instance.LanguageChanged += Instance_LanguageChanged;
    }

    private void Instance_LanguageChanged()
    {
        RaisePropertyChanged(nameof(Provider));
        RaisePropertyChanged(nameof(MapType));

        RaisePropertyChanged(nameof(Title));
    }

    

    public virtual string GetUrl(TileInfo tile)
    {
        return MakeUrl?.Invoke(tile);
    }

    public override string ToString()
    {
        return Title;
    }

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
