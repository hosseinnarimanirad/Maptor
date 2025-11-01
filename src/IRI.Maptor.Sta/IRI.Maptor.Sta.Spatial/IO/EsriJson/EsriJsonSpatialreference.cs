using System.Text.Json.Serialization;

namespace IRI.Maptor.Sta.Spatial.Primitives.Esri;

//[DataContract]
//[JsonObject]
public class EsriJsonSpatialreference
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
}
