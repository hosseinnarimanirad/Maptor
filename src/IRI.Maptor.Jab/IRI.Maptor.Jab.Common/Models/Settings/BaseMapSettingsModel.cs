using System;

using IRI.Maptor.Sta.Spatial.Model;
using IRI.Maptor.Jab.Common.Abstractions;

namespace IRI.Maptor.Jab.Common.Models.Settings;

public class BaseMapSettingsModel : Notifier, IBaseMapSettings
{
    private readonly IBaseMapSettings _baseMapSettings;

    public event EventHandler<double>? OnOpacityChanged;



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


    //private Action<double>? FireOpacityChanged;

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
        }
    }

    public string? ProxyAppUrl
    {
        get => _baseMapSettings.ProxyAppUrl;
        set
        {
            _baseMapSettings.ProxyAppUrl = value;
            RaisePropertyChanged();
        }
    }

    public BaseMapSettingsModel(IBaseMapSettings baseMapSettings/*, Action<double> fireOpacityChanged*/)
    {
        //FireOpacityChanged = fireOpacityChanged;

        _baseMapSettings = baseMapSettings;
    }

}
