using IRI.Maptor.Sta.Common.Common.JsonConverters;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Sta.Spatial.IO.EsriJson;

public class EsriJsonFeature
{
    public EsriJsonGeometry Geometry { get; set; }

    [JsonConverter(typeof(DictionaryStringObjectConverter))]
    public Dictionary<string, object> Attributes { get; set; }

    public Feature<Point> AsFeature(int srid, SrsBase? targetSrs = null)
    {
        //targetSrs = targetSrs ?? SrsBases.GeodeticWgs84;

        //var effectiveSourceSrid = sourceSrid ?? Geometry.SpatialReference.Wkid;

        var geometry = this.Geometry.Parse(srid);

        // Convert IGeometry to Geometry<Point> for projection
        Geometry<Point> pointGeometry = geometry switch
        {
            Geometry<PointZM> gzm => Geometry<Point>.Create(gzm.Points.Select(p => new Point(p.X, p.Y)).ToList(), geometry.Type, geometry.Srid),
            Geometry<PointZ> gz => Geometry<Point>.Create(gz.Points.Select(p => new Point(p.X, p.Y)).ToList(), geometry.Type, geometry.Srid),
            Geometry<Point> g => g,
            _ => throw new NotSupportedException($"Unsupported geometry type: {geometry.GetType()}")
        };

        if (targetSrs !=null)
        {
            pointGeometry = pointGeometry.Project(targetSrs);
        }

        return new Feature<Point>()
        {
            Attributes = this.Attributes ?? [],
            TheGeometry = pointGeometry,
        };
    }

}
