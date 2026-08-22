 
namespace IRI.Maptor.Presentation.Core.Data;

public class ProxySettings : IProxySettings
{
    public bool IsProxyMode { get; set; }

    public string? ProxyAddress { get; set; }

    public int ProxyPort { get; set; }

    public string? ProxyUserId { get; set; }

    public string? ProxyUserPass { get; set; }

    public static ProxySettings Default => new ProxySettings();
}
