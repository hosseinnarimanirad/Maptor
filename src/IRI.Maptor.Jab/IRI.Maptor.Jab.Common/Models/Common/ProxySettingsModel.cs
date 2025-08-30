using System; 

namespace IRI.Maptor.Jab.Common.Models;

public class ProxySettingsModel : Notifier
{
    private IProxySettings _proxySettings;

    public string Address
    {
        get { return _proxySettings.Address; }
        set
        {
            _proxySettings.Address = value;
            Update();
            RaisePropertyChanged();
            this.FireProxyChanged?.Invoke(this);
            //this.OnProxyChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public int Port
    {
        get { return _proxySettings.Port; }
        set
        {
            _proxySettings.Port = value;
            Update();
            RaisePropertyChanged();
            this.FireProxyChanged?.Invoke(this);
            //this.OnProxyChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string UserId
    {
        get { return _proxySettings.UserId; }
        set
        {
            _proxySettings.UserId = value;
            Update();
            RaisePropertyChanged();
            this.FireProxyChanged?.Invoke(this);
            //this.OnProxyChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string UserPass
    {
        get { return _proxySettings.UserPass; }
        set
        {
            _proxySettings.UserPass = value;
            Update();
            RaisePropertyChanged();
            this.FireProxyChanged?.Invoke(this);
            //this.OnProxyChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsProxyMode
    {
        get { return _proxySettings.IsProxyMode; }
        set
        {
            _proxySettings.IsProxyMode = value;
            Update();
            RaisePropertyChanged();
            this.FireProxyChanged?.Invoke(this);
            //this.OnProxyChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ProxySettingsModel(IProxySettings settings)
    {
        this._proxySettings = settings;

        Update();
    }

    private void Update()
    {
        this._proxy = null;

        if (IsProxyMode && !string.IsNullOrWhiteSpace(Address))
        {
            this._proxy = new System.Net.WebProxy(Address, Port);

            if (!string.IsNullOrWhiteSpace(UserId))
            {
                this._proxy.Credentials = new System.Net.NetworkCredential(UserId, UserPass);
            }
        }
    }

    public System.Net.WebProxy GetProxy()
    {
        return this._proxy;
    }

    private System.Net.WebProxy _proxy;

    //public event EventHandler OnProxyChanged;

    public Action<ProxySettingsModel> FireProxyChanged;
}
