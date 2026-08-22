using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Common.Contracts.Bing;
using System;
using System.Globalization;
using System.Threading.Tasks;
using IRI.Maptor.Core.Common.Services;
using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Core.Spatial.Services.Google;

public static class BingMapService
{
    public static async Task<Response<BingIsolineResult>> GetIsolineAsync(Point centerGeographic, double timeLimit, string key)
    {
        try
        {
            var pointString = $"{centerGeographic.Y.ToString(CultureInfo.InvariantCulture)},{centerGeographic.X.ToString(CultureInfo.InvariantCulture)}";

            var url = $"https://dev.virtualearth.net/REST/v1/Routes/Isochrones?waypoint={pointString}&maxTime={timeLimit}&timeUnit=second&key={key}";

            return await HttpTransport.GetAsync<BingIsolineResult>(url);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);

            return ResponseFactory.CreateError<BingIsolineResult>(ex.GetMessagePlus());
        }
    }
}