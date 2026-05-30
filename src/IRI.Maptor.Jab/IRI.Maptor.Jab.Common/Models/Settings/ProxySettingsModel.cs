using System;
using IRI.Maptor.Jab.Common.Data.Settings;
using IRI.Maptor.Jab.Common.Events;

namespace IRI.Maptor.Jab.Common.Models.Settings;

public class ProxySettingsModel : Notifier, IProxySettings
{
    private IProxySettings _settings;

    private System.Net.WebProxy? _proxy;

    public event EventHandler? OnProxyChanged;


    public string? ProxyAddress
    {
        get => _settings.ProxyAddress;
        set
        {
            _settings.ProxyAddress = value;
            Update();
            RaisePropertyChanged();
        }
    }

    public int ProxyPort
    {
        get => _settings.ProxyPort;
        set
        {
            _settings.ProxyPort = value;
            RaisePropertyChanged();
            Update();
        }
    }

    public string? ProxyUserId
    {
        get => _settings.ProxyUserId;
        set
        {
            _settings.ProxyUserId = value;
            RaisePropertyChanged();
            Update();
        }
    }

    public string? ProxyUserPass
    {
        get => _settings.ProxyUserPass;
        set
        {
            _settings.ProxyUserPass = value;
            RaisePropertyChanged();
            Update();
        }
    }

    public bool IsProxyMode
    {
        get => _settings.IsProxyMode;
        set
        {
            _settings.IsProxyMode = value;
            RaisePropertyChanged();
            Update();
        }
    }

    private int _timeOutInSeconds = 30;
    public int TimeOutInSeconds
    {
        get { return _timeOutInSeconds; }
        set
        {
            _timeOutInSeconds = value;
            RaisePropertyChanged();
        }
    }


    public ProxySettingsModel(IProxySettings settings/*, Action<ProxySettingsModel> fireProxyChanged*/)
    {
        this._settings = settings;

        //FireProxyChanged = fireProxyChanged;

        Update();
    }

    private void Update()
    {
        _proxy = null;

        if (IsProxyMode && !string.IsNullOrWhiteSpace(ProxyAddress))
        {
            _proxy = new System.Net.WebProxy(ProxyAddress, ProxyPort);

            if (!string.IsNullOrWhiteSpace(ProxyUserId) && !string.IsNullOrWhiteSpace(ProxyUserPass))
            {
                _proxy.Credentials = new System.Net.NetworkCredential(ProxyUserId, ProxyUserPass);
            }
        }

        OnProxyChanged?.Invoke(this, EventArgs.Empty);
    }

    public System.Net.WebProxy? GetProxy() => _proxy;
}
