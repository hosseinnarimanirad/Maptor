using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace IRI.Maptor.Presentation.Maui.Services;

/// <summary>
/// Default <see cref="ILocationService"/> built on MAUI Essentials' <see cref="Geolocation"/>.
/// Handles the runtime location permission request and falls back to the last known
/// position when a fresh fix is not available.
/// </summary>
public sealed class LocationService : ILocationService
{
    private readonly GeolocationAccuracy _accuracy;
    private readonly TimeSpan _timeout;

    public LocationService(GeolocationAccuracy accuracy = GeolocationAccuracy.Medium, TimeSpan? timeout = null)
    {
        _accuracy = accuracy;
        _timeout = timeout ?? TimeSpan.FromSeconds(10);
    }

    public Task<LocationResult?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        // Permission requests and the geolocation API must run on the UI thread.
        return MainThread.InvokeOnMainThreadAsync(() => GetLocationCoreAsync(cancellationToken));
    }

    private async Task<LocationResult?> GetLocationCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status != PermissionStatus.Granted)
            {
                return null;
            }

            var request = new GeolocationRequest(_accuracy, _timeout);

            var location = await Geolocation.Default.GetLocationAsync(request, cancellationToken)
                           ?? await Geolocation.Default.GetLastKnownLocationAsync();

            if (location is null)
            {
                return null;
            }

            return new LocationResult
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Accuracy = location.Accuracy,
            };
        }
        catch (Exception)
        {
            // Feature not supported, permission denied at OS level, or no provider.
            return null;
        }
    }
}
