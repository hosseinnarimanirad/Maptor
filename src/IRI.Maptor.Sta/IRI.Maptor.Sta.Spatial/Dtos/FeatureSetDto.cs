using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;

namespace IRI.Maptor.Sta.Spatial.Dtos;

public class FeatureSetDto
{
    public List<FeatureDto> Features { get; set; }

    public int Srid { get; set; }

    public int Count { get; set; }

    public List<Field>? Fields { get; set; }

    public int? LayerId { get; set; }

    // Parameterless constructor for JSON deserialization
    public FeatureSetDto()
    {
        Features = new List<FeatureDto>();
        Count = 0;
        Srid = 0;
    }

    public FeatureSetDto(List<FeatureDto> features, List<Field>? fields)
    {
        Features = features;
        Count = features?.Count ?? 0;
        Fields = fields;
        Srid = features?.FirstOrDefault()?.Srid ?? 0;
    }

    public FeatureSetDto(List<FeatureDto> features, List<Field>? fields, int? layerId)
    {
        Features = features;
        Count = features?.Count ?? 0;
        Fields = fields;
        LayerId = layerId;
        Srid = features?.FirstOrDefault()?.Srid ?? 0;
    }

    public void SetSrid(int srid)
    {
        if (srid == 0)
            return;

        if (this.Srid != 0)
            throw new NotImplementedException("FeatureSetDto > SetSrid");

        this.Srid = srid;

        if (this.Features.IsNullOrEmpty())
            return;

        foreach (var feature in Features)
        {
            feature.Srid = srid;
        }
    }

    public FeatureSet<Point> AsFeatureSet(string? idColumnName)
    {
        var features = new List<Feature<Point>>();

        foreach (var featureDto in this.Features)
        {
            try
            {
                //var feature = ConvertFeatureDtoToFeature(featureDto);
                var feature = featureDto.AsFeature(idColumnName, SridHelper.WebMercator);

                if (feature != null)
                    features.Add(feature);
            }
            catch
            {
                continue;
            }
        }

        if (features.Count == 0)
            return FeatureSet<Point>.Empty;

        //if (featureSetDto.Fields != null && featureSetDto.Fields.Count > 0)
        //    Fields = featureSetDto.Fields;

        //else if (features.Count > 0 && features[0].Attributes != null)
        //    Fields = Field.FromDictionary(features[0].Attributes);

        //if (features.Count > 0 && features[0].TheGeometry != null)
        //    GeometryType = features[0].GeometryType;

        //if (features.Count > 0)
        //{
        //    var extent = BoundingBox.GetMergedBoundingBox(features.Select(f => f.TheGeometry.GetBoundingBox()));

        //    if (!double.IsNaN(extent.Width) && !double.IsNaN(extent.Height))
        //        WebMercatorExtent = extent;
        //}

        var result = FeatureSet<Point>.Create(string.Empty, features);

        if (this.Fields != null && this.Fields.Count > 0)
            result.Fields = this.Fields;

        else if (features.Count > 0 && features[0].Attributes != null)
            result.Fields = Field.FromDictionary(features[0].Attributes);

        if (features.Count > 0 && features[0].TheGeometry != null)
            result.GeometryType = features[0].GeometryType;



        //result.Fields = this.Fields;

        return result;
    }


    public static FeatureSetDto Parse<T>(FeatureSet<T> featureSet, int? targetSrid = SridHelper.GeodeticWGS84) where T : IPoint, new()
    {
        return new FeatureSetDto()
        {
            Count = featureSet.Features?.Count ?? 0,
            Features = featureSet.Features.Select(f => FeatureDto.Parse(f, targetSrid)).ToList(),
            Fields = featureSet.Fields,
            //LayerId = featureSet.LayerId,
            Srid = featureSet.Srid
        };
    }
}
