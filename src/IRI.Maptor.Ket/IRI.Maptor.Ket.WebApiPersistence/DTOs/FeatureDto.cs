using IRI.Maptor.Sta.Common.Common.JsonConverters;
using IRI.Maptor.Sta.Common.Primitives;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Ket.WebApiPersistence.DTOs;

public class FeatureDto
{
    public int Id { get; set; }

    public byte[] Shape { get; set; }

    [JsonConverter(typeof(DictionaryStringObjectConverter))]
    public Dictionary<string, object> Attributes { get; set; }
}
