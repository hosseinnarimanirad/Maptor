using System.Diagnostics;
using IRI.Maptor.Sta.Common.Contracts.Google;
using IRI.Maptor.Sta.Common.Services;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Ket.WindowsBase.ManagedNativeWifi;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Ket.WindowsBase.Services.Google;

public static class GoogleMapsGeolocationService
{
    public static async Task<Response<GoogleGeolocationResult>> GetLocationAsync(string key)
    {
        try
        {
            var networks = NativeWifi.EnumerateBssNetworks();

            var
                wifiAccessPoints = networks.Select(i => new Wifiaccesspoint()
                {
                    channel = i.Channel,
                    macAddress = i.Bssid.ToString(),
                    signalStrength = i.SignalStrength,

                }).ToArray();

            //
            foreach (var item in wifiAccessPoints)
            {
                Debug.WriteLine($"mac: {item.macAddress}, signal:{item.signalStrength}, channel: {item.channel}");
            }

            return await GetLocationAsync(key, wifiAccessPoints);

            //var url = $"https://www.googleapis.com/geolocation/v1/geolocate?key={key}";

            //return await Helpers.NetHelper.HttpPostAsync<GoogleGeolocationResult>(url, parameter);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);

            //return new GoogleGeolocationResult() { accuracy = int.MaxValue };
            return ResponseFactory.CreateError<GoogleGeolocationResult>(ex.GetMessagePlus());
        }
    }

    public static async Task<Response<GoogleGeolocationResult>> GetLocationAsync(string key, Wifiaccesspoint[] networks, System.Net.WebProxy proxy = null)
    {
        try
        {
            var parameter = new GoogleGeolocationParameters() { wifiAccessPoints = networks };

            var url = $"https://www.googleapis.com/geolocation/v1/geolocate?key={key}";

            return await NetHelper.HttpPostAsync<GoogleGeolocationResult>(new HttpParameters() { Address = url, Data = parameter, Proxy = proxy });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);

            return ResponseFactory.CreateError<GoogleGeolocationResult>(ex.GetMessagePlus());
        }
    }

    public static Response<GoogleGeolocationResult> GetLocation(string key, List<Wifiaccesspoint> networks)
    {
        try
        {
            var parameter = new GoogleGeolocationParameters()
            {
                wifiAccessPoints = networks.ToArray()
            };

            var url = $"https://www.googleapis.com/geolocation/v1/geolocate?key={key}";

            //return Helpers.NetHelper.HttpPost<GoogleGeolocationResult>(url, parameter, null, null);
            return NetHelper.HttpPost<GoogleGeolocationResult>(new HttpParameters() { Address = url, Data = parameter });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);

            return ResponseFactory.CreateError<GoogleGeolocationResult>(ex.GetMessagePlus());
        }
    }

}

