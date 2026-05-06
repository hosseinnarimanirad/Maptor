namespace IRI.Maptor.Jab.Common.Data.Settings;

public interface IProxySettings
{
    bool IsProxyMode { get; set; }
    string? ProxyAddress { get; set; }
    int ProxyPort { get; set; }
    string? ProxyUserId { get; set; }
    string? ProxyUserPass { get; set; }
}