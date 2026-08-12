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
            if (_baseMapSettings.LocalNetworkUrl == value)
                return;

            _baseMapSettings.LocalNetworkUrl = value;
            RaisePropertyChanged();

            // Only the url the active mode actually reads is worth rebuilding the tile services for.
            // Hosts bind this to a text box with UpdateSourceTrigger=PropertyChanged, so an unguarded
            // event here means a base map rebuild on every keystroke.
            if (SelectedTileMapAccessMode == TileMapAccessMode.LocalNetwork)
                OnBaseMapUrlChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string? ProxyAppUrl
    {
        get => _baseMapSettings.ProxyAppUrl;
        set
        {
            if (_baseMapSettings.ProxyAppUrl == value)
                return;

            _baseMapSettings.ProxyAppUrl = value;
            RaisePropertyChanged();

            if (SelectedTileMapAccessMode == TileMapAccessMode.ProxyApp)
                OnBaseMapUrlChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public TileMapAccessMode SelectedTileMapAccessMode
    {
        get => _baseMapSettings.SelectedTileMapAccessMode;
        set
        {
            // LocalNetwork and ProxyApp build every tile url from their base url. Accepting the mode
            // without one installs a strategy that yields unusable urls, and the only symptom is a
            // blank base map. Keep the previous mode and notify, so a bound toggle snaps back.
            if (!HasUrlFor(value))
            {
                RaisePropertyChanged();
                return;
            }

            _baseMapSettings.SelectedTileMapAccessMode = value;
            RaisePropertyChanged();

            OnBaseMapUrlChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Whether <paramref name="mode"/> has the base url it needs. <see cref="TileMapAccessMode.Internet"/>
    /// needs none — its urls come from the built-in per-provider web url factory.
    /// </summary>
    public bool HasUrlFor(TileMapAccessMode mode) => mode switch
    {
        TileMapAccessMode.LocalNetwork => !string.IsNullOrWhiteSpace(LocalNetworkUrl),
        TileMapAccessMode.ProxyApp => !string.IsNullOrWhiteSpace(ProxyAppUrl),
        _ => true,
    };

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
