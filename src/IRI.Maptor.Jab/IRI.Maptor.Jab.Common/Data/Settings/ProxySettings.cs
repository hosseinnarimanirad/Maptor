using IRI.Maptor.Jab.Common.Data.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.Data;

public class ProxySettings : IProxySettings
{
    public bool IsProxyMode { get; set; }

    public string? ProxyAddress { get; set; }

    public int ProxyPort { get; set; }

    public string? ProxyUserId { get; set; }

    public string? ProxyUserPass { get; set; }

    public static ProxySettings Default => new ProxySettings();
}
