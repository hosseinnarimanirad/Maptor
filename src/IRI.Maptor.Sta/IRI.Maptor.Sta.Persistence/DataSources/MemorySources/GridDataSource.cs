using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.MapIndexes;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Sta.Persistence.DataSources;

public class GridDataSource : VectorDataSource
{
    private readonly static List<Field> _fields = new List<Field>();

    public GeodeticIndexType Type { get; protected set; }

    public BoundingBox GeodeticWgs84Extent { get; set; }

    public override BoundingBox WebMercatorExtent
    {
        get => GeodeticWgs84Extent.Transform(MapProjects.GeodeticWgs84ToWebMercator);
        protected set => _ = value;
    }

    public override int Srid { get => SridHelper.WebMercator; protected set => _ = value; }

    public override GeometryType? GeometryType
    {
        get => Common.Primitives.GeometryType.Polygon;
        protected set => _ = value;
    }

    private GridDataSource() : base(_fields)
    {
        GeodeticWgs84Extent = BoundingBoxes.GeodeticWgs84_Iran;
    }

    static GridDataSource()
    {
        _fields =
        [
            new() {IsNullable=false, Name=nameof(GeodeticSheet.Id), Length=0, Type="int"},
            new() {IsNullable=false, Name=nameof(GeodeticSheet.SheetName), Length=0, Type="string"},
            new() {IsNullable=false, Name=nameof(GeodeticSheet.SubTitle), Length=0, Type="string"},
            new() {IsNullable=false, Name=nameof(GeodeticSheet.Type), Length=0, Type="int"},
            new() {IsNullable=false, Name="Min Longitude", Length=0, Type="int"},
            new() {IsNullable=false, Name="Max Longitude", Length=0, Type="int"},
            new() {IsNullable=false, Name="Min Latitude", Length=0, Type="int"},
            new() {IsNullable=false, Name="Max Latitude", Length=0, Type="int"},
        ];
    }
 
    public override string ToString()
    {
        return $"GridDataSource {Type.GetName()}";
    }


    // Get GeometryAwares [GENERIC]
    //public List<GeodeticSheet> GetGeometryAwares(BoundingBox boundingBox)
    //{
    //    var geographicBoundingBox = boundingBox.Transform(MapProjects.WebMercatorToGeodeticWgs84);

    //    return GeodeticIndexes.FindIndexSheets(geographicBoundingBox, Type);
    //}

    //public List<GeodeticSheet> GetGeometryAwares(Geometry<Point>? geometry)
    //{
    //    var geographicBoundingBox = geometry?.GetBoundingBox().Transform(MapProjects.WebMercatorToGeodeticWgs84) ?? GeodeticWgs84Extent;

    //    return GeodeticIndexes.FindIndexSheets(geographicBoundingBox, Type)
    //                            .Where(s => s.TheGeometry?.Intersects(geometry) == true)
    //                            .ToList();
    //}

    // Get as FeatureSet of Point
    public override FeatureSet<Point> GetAsFeatureSet(BoundingBox boundingBox)
    {
        var geographicBoundingBox = boundingBox.Transform(MapProjects.WebMercatorToGeodeticWgs84);

        return FeatureSet<Point>.Create(string.Empty, GeodeticIndexes.FindIndexSheets(geographicBoundingBox, Type)
                                .Select(ToFeatureMappingFunc)
                                .ToList());
    }

    public override FeatureSet<Point> GetAsFeatureSet(Geometry<Point>? geometry)
    {
        var geographicBoundingBox = geometry?.GetBoundingBox().Transform(MapProjects.WebMercatorToGeodeticWgs84) ?? GeodeticWgs84Extent;

        return FeatureSet<Point>.Create(string.Empty, GeodeticIndexes.FindIndexSheets(geographicBoundingBox, Type)
                                .Where(s => s.TheGeometry?.Intersects(geometry) == true)
                                .Select(ToFeatureMappingFunc)
                                .ToList());
    }


    public override FeatureSet<Point> Search(string searchText)
    {
        throw new NotImplementedException();
    }

    private Feature<Point> ToFeatureMappingFunc(GeodeticSheet geometryAware)
    {
        return new Feature<Point>()
        {
            Id = geometryAware.Id,
            LabelAttribute = nameof(geometryAware.SheetName),
            TheGeometry = geometryAware.TheGeometry,
            Attributes = new Dictionary<string, object>()
            {
                {nameof(geometryAware.Id), geometryAware.Id},
                { nameof(geometryAware.SheetName), geometryAware.SheetName},
                { nameof(geometryAware.SubTitle), geometryAware.SubTitle},
                { nameof(geometryAware.Type), geometryAware.Type},
                { "Min Longitude", geometryAware.GeodeticExtent.XMin},
                { "Max Longitude", geometryAware.GeodeticExtent.XMax},
                { "Min Latitude", geometryAware.GeodeticExtent.YMin},
                { "Max Latitude", geometryAware.GeodeticExtent.YMax},
            }
        };
    }

    public static GridDataSource Create(GeodeticIndexType indexType)
    {
        GridDataSource result = new GridDataSource();

        result.Type = indexType;

        return result;
    }

}
