﻿using IRI.Sta.Spatial.Primitives;
using IRI.Sta.Common.Primitives;
using System.Text.Json.Serialization;
using IRI.Sta.SpatialReferenceSystem;

namespace IRI.Sta.Spatial.Model.GeoJsonFormat;

//[JsonConverter(typeof(GeoJsonGeometryConverter))]
public class GeoJsonMultiPolygon : IGeoJsonGeometry
{
    [JsonIgnore]
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("coordinates")]
    public double[][][][] Coordinates { get; set; }

    [JsonIgnore]
    public GeometryType GeometryType { get => GeometryType.MultiPolygon; }

    public bool IsNullOrEmpty()
    {
        return Coordinates == null || Coordinates.Length < 1;
    }

    public Geometry<Point> Parse(bool isLongitudeFirst = true, int srid = 0)
    {
        return new Geometry<Point>(Coordinates?.Select(c => Geometry<Point>.ParsePolygonToGeometry(c, GeometryType.Polygon, isLongitudeFirst, srid)).ToList(), this.GeometryType, srid);
    }

    public string Serialize(bool indented, bool removeSpaces = false)
    {
        return GeoJson.Serialize(this, indented, removeSpaces);
    }

    public int NumberOfGeometries()
    {
        return Coordinates == null ? 0 : Coordinates.Length;
    }

    public int NumberOfPoints()
    {
        return Coordinates == null ? 0 : Coordinates.Sum(part => part == null ? 0 : part.Sum(ring => ring == null ? 0 : ring.Length));
    }

    public Geometry<Point> TransformToWeMercator(bool isLongitudeFirst = true)
    {
        return this.Parse(isLongitudeFirst, SridHelper.GeodeticWGS84)
                    .Transform(MapProjects.GeodeticWgs84ToWebMercator, SridHelper.WebMercator);
    }
}
