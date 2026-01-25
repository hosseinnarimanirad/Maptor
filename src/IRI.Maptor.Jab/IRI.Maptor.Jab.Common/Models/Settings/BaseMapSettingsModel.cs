using System;

using IRI.Maptor.Sta.Spatial.Model;
using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Common.TileServices;

namespace IRI.Maptor.Jab.Common.Models.Settings;

public class BaseMapSettingsModel : Notifier, IBaseMapSettings
{
    private readonly IBaseMapSettings _baseMapSettings;


    public event EventHandler<double>? OnOpacityChanged;

    public event EventHandler? OnBaseMapUrlChanged;


    private Func<TileInfo, string>? _getFileName = null;
    public Func<TileInfo, string>? GetLocalFileName
    {
        get => _getFileName;
        set
        {
            _getFileName = value;
            RaisePropertyChanged();
        }
    }


    public string? BaseMapCacheDirectory
    {
        get => _baseMapSettings.BaseMapCacheDirectory;
        set
        {
            _baseMapSettings.BaseMapCacheDirectory = value;
            RaisePropertyChanged();
        }
    }

    public bool IsBaseMapCacheEnabled
    {
        get => _baseMapSettings.IsBaseMapCacheEnabled;
        set
        {
            _baseMapSettings.IsBaseMapCacheEnabled = value;
            RaisePropertyChanged();
        }
    }
      
    public double BaseMapOpacity
    {
        get { return _baseMapSettings.BaseMapOpacity; }
        set
        {
            _baseMapSettings.BaseMapOpacity = value;
            RaisePropertyChanged();

            OnOpacityChanged?.Invoke(this, value);

            //foreach (var layer in Layers.Where(i => i.Type == LayerType.BaseMap))
            //    layer.Opacity = value;
        }
    }
     
    public string? LocalNetworkUrl
    {
        get => _baseMapSettings.LocalNetworkUrl;
        set
        {
            _baseMapSettings.LocalNetworkUrl = value;
            RaisePropertyChanged();

            OnBaseMapUrlChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string? ProxyAppUrl
    {
        get => _baseMapSettings.ProxyAppUrl;
        set
        {
            _baseMapSettings.ProxyAppUrl = value;
            RaisePropertyChanged();

            OnBaseMapUrlChanged?.Invoke(this, EventArgs.Empty);
        }
    }


    public TileMapProviderMode SelectedTileMapProviderMode
    {
        get { return _baseMapSettings.SelectedTileMapProviderMode; }
        set
        {
            _baseMapSettings.SelectedTileMapProviderMode = value;
            RaisePropertyChanged();

            OnBaseMapUrlChanged?.Invoke(this, EventArgs.Empty);
        }
    }


    public BaseMapSettingsModel(IBaseMapSettings baseMapSettings)
    { 
        _baseMapSettings = baseMapSettings;
    }

    public IBaseMapSettings GetData() => _baseMapSettings;
}
