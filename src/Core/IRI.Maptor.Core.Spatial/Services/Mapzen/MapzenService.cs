using IRI.Maptor.Core.Common.Contracts.Mapzen;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Common.Services;

namespace IRI.Maptor.Core.Spatial.Services.Mapzen;

public static class MapzenService
{
    public static async Task<MapzenIsochroneResult> GetIsochroneAsync(Point geodeticPoint, string apiKey)
    {
        //  var url = $"matrix.mapzen.com/isochrone?json={{\"locations\":[{{\"lat\":{geodeticPoint.Y},\"lon\":{geodeticPoint.X}}}],\"costing\":\"bicycle\",\"contours\":[{{\"time\":15,\"color\":\"ff0000\"}}]}}&id=Walk_From_Office&api_key={apiKey}";

        var parameter = new MapzenRouteCostingModel()
        {
            locations = new Location[] { new Location() { lon = geodeticPoint.X, lat = geodeticPoint.Y } },
            costing = "auto",
            contours = new Contour[] { new Contour() { time = 4, color = "ff0000" } },
            polygons = true,
        };

        var url = $"http://matrix.mapzen.com/isochrone?id=Walk_From_Office&api_key={apiKey}";

        var result = await IRI.Maptor.Core.Common.Helpers.HttpTransport.PostAsync<MapzenIsochroneResult>(url, parameter);

        if (result == null)
        {
            ResponseFactory.CreateError<string>("NULL VALUE");

            return null;
        }
        else
        {
            return ResponseFactory.Create(result?.Result)?.Result;
        }

    }
}
