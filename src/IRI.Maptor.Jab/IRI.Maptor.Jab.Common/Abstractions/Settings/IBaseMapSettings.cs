namespace IRI.Maptor.Jab.Common.Abstractions;

public interface IBaseMapSettings
{
    string? BaseMapCacheDirectory { get; set; }
    bool IsBaseMapCacheEnabled { get; set; }

    double BaseMapOpacity { get; set; }

    string? LocalNetworkUrl { get; set; }
    string? ProxyAppUrl { get; set; }
}