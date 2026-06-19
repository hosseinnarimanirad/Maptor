namespace IRI.Maptor.Jab.Core.Data;

public interface IProxySettings
{
    bool IsProxyMode { get; set; }
    string? ProxyAddress { get; set; }
    int ProxyPort { get; set; }
    string? ProxyUserId { get; set; }
    string? ProxyUserPass { get; set; }
}