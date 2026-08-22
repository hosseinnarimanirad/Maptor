using System.Text.Json.Serialization;

namespace IRI.Maptor.Core.Spatial.IO.EsriJson;

//[DataContract]
//[JsonObject]
public class EsriJsonSpatialReference
{
    //[DataMember(Name = "wkid")]
    [JsonPropertyName("wkid")]
    public int Wkid { get; set; }

    //[DataMember(Name = "latestWkid")]
    [JsonPropertyName("latestWkid")]
    public int? LatestWkid { get; set; }

    //[DataMember(Name = "vcsWkid")]
    [JsonPropertyName("vcsWkid")]
    public int? VcsWkid { get; set; }

    //[DataMember(Name = "latestVcsWkid")]
    [JsonPropertyName("latestVcsWkid")]
    public int? LatestVcsWkid { get; set; }

    public override string ToString()
    {
        return $"Wkid: {Wkid}, LatestWkid: {LatestWkid}";
    }
}
