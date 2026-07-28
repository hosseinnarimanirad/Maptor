using System;
using IRI.Maptor.Sta.Common.Common.JsonConverters;
using IRI.Maptor.Sta.Common.Primitives;
using System.Text.Json.Serialization;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Abstractions;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using System.Security.Cryptography;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Sta.Spatial.Dtos;

public class FeatureDto
{
    public int Id { get; set; }

    public Guid Key { get; set; }

    public byte[] Shape { get; set; }

    private int _srid;
    public int Srid
    {
        get { return _srid; }
        set
        {
            if (value != SridHelper.GeodeticWGS84)
                throw new NotImplementedException();

            _srid = value;
        }
    }


    [JsonConverter(typeof(DictionaryStringObjectConverter))]
    public Dictionary<string, object> Attributes { get; set; }


    public Feature<Point>? AsFeature(string? idColumnName, int? targetSrid = SridHelper.WebMercator)
    {
        if (this.Shape == null || this.Shape.Length == 0)
            return null;

        var geometry = Geometry<Point>.FromWkb(this.Shape, Srid);

        if (geometry == null || geometry.IsNullOrEmpty())
            return null;

        if (targetSrid.HasValue && this.Srid != targetSrid)
        {
            var targetSrs = SrsBase.Create(targetSrid.Value)!;

            geometry = geometry.Project(targetSrs);
        }

        var feature = new Feature<Point>(geometry, this.Attributes ?? new Dictionary<string, object>())
        {
            Id = this.Id,
            Key = this.Key != Guid.Empty ? this.Key : Guid.NewGuid(),
        };

        if (!string.IsNullOrWhiteSpace(idColumnName) && this.Attributes != null && this.Attributes.ContainsKey(idColumnName))
        {
            if (this.Attributes[idColumnName] is int id)
                feature.Id = id;
        }

        return feature;
    }

    public static FeatureDto Parse<T>(Feature<T> feature, int? targetSrid = SridHelper.GeodeticWGS84) where T : IPoint, new()
    {
        var geometry = feature.TheGeometry;

        if (targetSrid != null && targetSrid.Value != feature.Srid)
        {
            var targetSrs = SrsBase.Create(targetSrid.Value)!;

            geometry = geometry.Project(targetSrs);
        }
        //if (geometry.Srid != SridHelper.GeodeticWGS84)
        //{
        //    geometry = geometry.Project(SrsBases.GeodeticWgs84);
        //}

        var wkbBytes = geometry.AsWkb();

        var isNew = feature.Status == Sta.Common.Enums.FeatureStatus.New;

        var srid = geometry.Srid;

        return new FeatureDto
        {
            Id = isNew ? 0 : feature.Id,
            Key = feature.Key,
            Srid = srid,
            Shape = wkbBytes ?? Array.Empty<byte>(),
            Attributes = feature.Attributes ?? new Dictionary<string, object>(),
        };
    }
}
