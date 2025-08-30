using System.Globalization;
using IRI.Maptor.Sta.Common.Model;
using System.Net;
using IRI.Maptor.Sta.Common.Contracts.Google;
using IRI.Maptor.Sta.Common.Services;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Sta.Spatial.Services.Google;

public static class GoogleMapService
{
    static DateTime _baseDateTime = new DateTime(1970, 1, 1, 0, 0, 0);

    public static string ParseToString(Point geodeticPoint)
    {
        return $"{geodeticPoint.Y.ToString(CultureInfo.InvariantCulture)},{geodeticPoint.X.ToString(CultureInfo.InvariantCulture)}";
    }

    public static int GetTime(DateTime dateTime)
    {
        return (int)(dateTime - _baseDateTime).TotalSeconds;
    }

    public static int GetNow()
    {
        return GetTime(DateTime.Now);
    }

    public static async Task<Response<GoogleDirectionsResult>> GetDirectionAsync(Point start, Point end, string key)
    {
        try
        {
            var startString = ParseToString(start);

            var endString = ParseToString(end);

            var time = GetNow();

            var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={startString}&destination={endString}&alternatives=true&departure_time={time}&key={key}";

            return await IRI.Maptor.Sta.Common.Helpers.NetHelper.HttpGetAsync<GoogleDirectionsResult>(url);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);

            return ResponseFactory.CreateError<GoogleDirectionsResult>(ex.Message);
        }
    }

    public static async Task<Response<GoogleDistanceMatrixResult>> GetDistanceMatrixAsync(List<Point> origins, List<Point> destinations, string key)
    {
        try
        {
            var originString = string.Join("|", origins.Select(p => $"{p.Y.ToString(CultureInfo.InvariantCulture)},{p.X.ToString(CultureInfo.InvariantCulture)}"));

            var destinationString = string.Join("|", destinations.Select(p => $"{p.Y.ToString(CultureInfo.InvariantCulture)},{p.X.ToString(CultureInfo.InvariantCulture)}"));

            var time = (int)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

            var url = $"https://maps.googleapis.com/maps/api/distancematrix/json?units=metric&origins={originString}&destinations={destinationString}&departure_time={time}&key={key}";

            return await IRI.Maptor.Sta.Common.Helpers.NetHelper.HttpGetAsync<GoogleDistanceMatrixResult>(url);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);

            return ResponseFactory.CreateError<GoogleDistanceMatrixResult>(ex.GetMessagePlus());
            //return new Response<GoogleDistanceMatrixResult>() { Result = new GoogleDistanceMatrixResult() { rows = new Row[0] }, IsFailed = true, ErrorMessage = ex.Message };
        }
    }

    const string googlePlacesApiUrl = "https://maps.googleapis.com/maps/api/place/textsearch/json?";

    //static WebClient client = new WebClient();

    //static GooglePlaces()
    //{
    //    client.Headers.Add("user-agent", "App!");

    //    client.Encoding = Encoding.UTF8;
    //}

    public async static Task<Response<GooglePlacesResult>> SearchAsync(string query, string key)
    {
        //var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={startString}&destination={endString}&alternatives=true&departure_time={time}&key={key}";

        return await IRI.Maptor.Sta.Common.Helpers.NetHelper.HttpGetAsync<GooglePlacesResult>($"{googlePlacesApiUrl}query={query}&key={key}");
    }

    public static Response<GooglePlacesResult> Search(string query, string key)
    {
        return IRI.Maptor.Sta.Common.Helpers.NetHelper.HttpGet<GooglePlacesResult>($"{googlePlacesApiUrl}query={query}&key={key}");
    }



    public static async Task<Response<GeoReferencedImage>> GetStaticMapAsync(Point centerGeoPoint, int width, int height, BoundingBox mbb, int zoom)
    {
        try
        {
            WebClient client = new WebClient();

            var center = mbb.Center;

            var url = $@"https://maps.googleapis.com/maps/api/staticmap?center={center.Y},{center.X}&zoom={zoom}&size={width}x{height}&maptype=roadmap";

            var byteImage = await client.DownloadDataTaskAsync(url);

            return ResponseFactory.Create(new GeoReferencedImage(byteImage, mbb));

        }
        catch (Exception ex)
        {
            return ResponseFactory.CreateError<GeoReferencedImage>(ex.GetMessagePlus());
        }
    }
}
