using IRI.Maptor.Core.Common.Primitives;
using System.Text.Json.Serialization;


namespace IRI.Maptor.Core.Common.Contracts.Google;

public class GoogleLocation
{

    [JsonPropertyName("lat")]
    public double Latitude { get; set; }

    [JsonPropertyName("lng")]
    public double Longitude { get; set; }
}

//public class Northeast
//{

//    [JsonPropertyName("lat")]
//    public double Lat { get; set; }

//    [JsonPropertyName("lng")]
//    public double Lng { get; set; }
//}

//public class Southwest
//{

//    [JsonPropertyName("lat")]
//    public double Lat { get; set; }

//    [JsonPropertyName("lng")]
//    public double Lng { get; set; }
//}

public class Viewport
{

    [JsonPropertyName("northeast")]
    public GoogleLocation Northeast { get; set; }

    [JsonPropertyName("southwest")]
    public GoogleLocation Southwest { get; set; }
}


public class GoogleGeometry
{

    [JsonPropertyName("location")]
    public GoogleLocation Location { get; set; }

    [JsonPropertyName("viewport")]
    public Viewport Viewport { get; set; }
}

public class Photo
{

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("html_attributions")]
    public string[] HtmlAttributions { get; set; }

    [JsonPropertyName("photo_reference")]
    public string PhotoReference { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }
}

public class OpeningHours
{

    [JsonPropertyName("open_now")]
    public bool OpenNow { get; set; }

    [JsonPropertyName("weekday_text")]
    public object[] WeekdayText { get; set; }
}

public class GooglePlaces
{

    [JsonPropertyName("formatted_address")]
    public string FormattedAddress { get; set; }

    [JsonPropertyName("geometry")]
    public GoogleGeometry Geometry { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("place_id")]
    public string PlaceId { get; set; }

    [JsonPropertyName("reference")]
    public string Reference { get; set; }

    [JsonPropertyName("types")]
    public string[] Types { get; set; }

    [JsonPropertyName("photos")]
    public Photo[] Photos { get; set; }

    [JsonPropertyName("opening_hours")]
    public OpeningHours OpeningHours { get; set; }

    public Point AsGeodeticPoint()
    {
        if (this.Geometry?.Location == null)
        {
            return Point.NaN;
        }

        return new Point(this.Geometry.Location.Longitude, this.Geometry.Location.Latitude);
    }

    //public Point AsWebMercatorPoint()
    //{
    //    return IRI.Maptor.Core.SpatialReferenceSystem.MapProjections.MapProjects.GeodeticWgs84ToWebMercator(AsGeodeticPoint());
    //}

    //public BoundingBox AsGeodeticBoundingBox()
    //{
    //    if (this.Geometry?.Viewport == null)
    //    {
    //        return BoundingBox.NaN;
    //    }

    //    return new BoundingBox(xMin: this.Geometry.Viewport.Southwest.Longitude,
    //        yMin: this.Geometry.Viewport.Southwest.Latitude,
    //        xMax: this.Geometry.Viewport.Northeast.Longitude,
    //        yMax: this.Geometry.Viewport.Northeast.Latitude);
    //}

    //public BoundingBox AsWebMercatorBoundingBox()
    //{
    //    return (AsGeodeticBoundingBox().Transform(IRI.Maptor.Core.SpatialReferenceSystem.MapProjections.MapProjects.GeodeticWgs84ToWebMercator));
    //}
}

public class GooglePlacesResult
{

    [JsonPropertyName("html_attributions")]
    public object[] HtmlAttributions { get; set; }

    [JsonPropertyName("next_page_token")]
    public string NextPageToken { get; set; }

    [JsonPropertyName("results")]
    public GooglePlaces[] Results { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }
}
