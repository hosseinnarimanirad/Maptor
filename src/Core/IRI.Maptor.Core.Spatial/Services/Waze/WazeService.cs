using IRI.Maptor.Core.Spatial.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using IRI.Maptor.Core.Common.Services;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Core.Spatial.Services.Waze;

public static class WazeService
{
    public static Response<WazeGeoRssResult> GetRss(BoundingBox geographicBound, long _epoch = -1)
    {

        //https://www.waze.com/row-rtserver/web/TGeoRSS?ma=600&mj=100&mu=100&left=51.263646841049194&right=51.328981161117554&bottom=35.72281242757668&top=35.74842331753895&_=1520571802973

        var time = DateTime.UtcNow - DateTimeExtensions.JulianDate;

        long epoch = ((long)time.TotalSeconds) * (long)1000;
        epoch = _epoch > 0 ? _epoch : epoch;
        //epoch = 1520571803066;
        var url = $"https://www.waze.com/row-rtserver/web/TGeoRSS?ma=600&mj=1&mu=400&left={geographicBound.XMin}&right={geographicBound.XMax}&bottom={geographicBound.YMin}&top={geographicBound.YMax}&_={epoch}";

        //var result = await IRI.Maptor.Core.Common.Helpers.NetHelper.HttpGetAsync<WazeGeoRssResult>(url);
        try
        {
            WebClient client = new WebClient();

            client.Headers.Add(HttpRequestHeader.Accept, "application/json, text/javascript, */*; q=0.01");
            client.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9");
            client.Headers.Add(HttpRequestHeader.Cookie, "_csrf_token=MGNYlmxqIUAR87QomEqOgXyEIvXOQ7ni1WK_EMqyYk8;_web_session=YlBPcnpkY0pNeHUzYU9LOXQ2aGtlWDYzeTBrQ2c2UHZuNk1VNmhnUkpibz0tLVlDUFF6bkZIeGFmUjhHUlNET2kwRUE9PQ%3D%3D--87c91cc13cedf517dc0bf3f1770e9d092f83e116;_ga=GA1.2.1611471394.1520548309;_gid=GA1.2.441536613.1520548309");
            client.Headers.Add(HttpRequestHeader.Host, "www.waze.com");
            client.Headers.Add("X-Requested-With", "XMLHttpRequest");
            client.Headers.Add(HttpRequestHeader.Referer, "https://www.waze.com/livemap");
            client.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");

            client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.186 Safari/537.36");

            client.Encoding = Encoding.UTF8;

            var stringResult = new StreamReader(new GZipStream(client.OpenRead(url), CompressionMode.Decompress)).ReadToEnd();

            var result = JsonHelper.Deserialize<WazeGeoRssResult>(stringResult);

            return ResponseFactory.Create(result);
        }
        catch (Exception ex)
        {
            return ResponseFactory.CreateError<WazeGeoRssResult>(ex.GetMessagePlus());
        }


    }
}
