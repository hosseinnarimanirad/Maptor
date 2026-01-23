namespace IRI.Maptor.Jab.Common.Abstractions;

public interface IProxySettings
{
    bool IsProxyMode { get; set; }
    string? ProxyAddress { get; set; }
    int ProxyPort { get; set; }
    string? ProxyUserId { get; set; }
    string? ProxyUserPass { get; set; }
}