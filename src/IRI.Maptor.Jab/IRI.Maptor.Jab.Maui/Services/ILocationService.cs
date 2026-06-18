namespace IRI.Maptor.Jab.Maui.Services;

/// <summary>The result of a device-location query (WGS84 degrees).</summary>
public sealed class LocationResult
{
    public double Latitude { get; init; }

    public double Longitude { get; init; }

    /// <summary>Horizontal accuracy in meters, if reported by the platform.</summary>
    public double? Accuracy { get; init; }
}

/// <summary>
/// Abstraction over the device geolocation provider so view models can request the
/// current position without depending on the platform APIs directly.
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Requests permission (if needed) and returns the current device location, or
    /// <c>null</c> if it is unavailable or permission was denied.
    /// </summary>
    Task<LocationResult?> GetCurrentLocationAsync(CancellationToken cancellationToken = default);
}
