using System;

using IRI.Maptor.Jab.Common.Abstractions;

namespace IRI.Maptor.Jab.Common.Data;

public class BaseMapSettings : /*ValueObject, */IBaseMapSettings
{
    public string? BaseMapCacheDirectory { get; set; } = $"{Environment.CurrentDirectory}\\Data";
    public bool IsBaseMapCacheEnabled { get; set; } = true;

    private double _baseMapOpacity = 0.7;

    public double BaseMapOpacity
    {
        get => _baseMapOpacity;
        set => _baseMapOpacity = Math.Min(1, Math.Max(0, value));
    }



    public string? LocalNetworkUrl { get; set; }
    public string? ProxyAppUrl { get; set; }

    //protected override IEnumerable<object> GetEqualityComponents()
    //{
    //    yield return BaseMapCacheDirectory ?? string.Empty;
    //    yield return IsBaseMapCacheEnabled;
    //    yield return LocalNetworkUrl ?? string.Empty;
    //    yield return ProxyAppUrl ?? string.Empty;
    //}

    public static BaseMapSettings Default => new BaseMapSettings();
}
