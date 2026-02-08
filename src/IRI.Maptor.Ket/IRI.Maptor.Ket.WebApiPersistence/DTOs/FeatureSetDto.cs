using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Ket.WebApiPersistence.DTOs;

public class FeatureSetDto
{
    public List<FeatureDto> Features { get; set; }

    public int Count { get; set; }

    public List<Field>? Fields { get; set; }

    public int? LayerId { get; set; }

    // Parameterless constructor for JSON deserialization
    public FeatureSetDto()
    {
        Features = new List<FeatureDto>();
        Count = 0;
    }

    public FeatureSetDto(List<FeatureDto> features, List<Field>? fields)
    {
        Features = features;
        Count = features?.Count ?? 0;
        Fields = fields;
    }

    public FeatureSetDto(List<FeatureDto> features, List<Field>? fields, int? layerId)
    {
        Features = features;
        Count = features?.Count ?? 0;
        Fields = fields;
        LayerId = layerId;
    }
}
