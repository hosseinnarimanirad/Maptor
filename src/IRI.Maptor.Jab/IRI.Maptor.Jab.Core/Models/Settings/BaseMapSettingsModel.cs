using System;

using IRI.Maptor.Sta.Spatial.Model;
using System.Collections.Generic;
using IRI.Maptor.Jab.Core;
using IRI.Maptor.Jab.Core.TileServices;
using IRI.Maptor.Jab.Core.Data;

namespace IRI.Maptor.Jab.Core.Models;

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

    public TileMapAccessMode SelectedTileMapAccessMode
    {
        get => _baseMapSettings.SelectedTileMapAccessMode;
        set
        {
            _baseMapSettings.SelectedTileMapAccessMode = value;
            RaisePropertyChanged();

            OnBaseMapUrlChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public List<TileMapProvider> MapProviders
    {
        get => _baseMapSettings.MapProviders;
        set
        {
            _baseMapSettings.MapProviders = value;
            RaisePropertyChanged();
        }
    }

    public BaseMapType InitialBaseMap
    {
        get => _baseMapSettings.InitialBaseMap;
        set
        {
            if (_baseMapSettings.InitialBaseMap == value)
                return;

            _baseMapSettings.InitialBaseMap = value;
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
