using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.MapIndexes;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;

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

    public override int Srid { get => SridHelper.WebMercator; }

    public override GeometryType? GeometryType
    {
        get => Common.Enums.GeometryType.Polygon;
        protected set => _ = value;
    }

    static GridDataSource()
    {
        _fields =
        [
            new() {IsNullable=false, Name=nameof(GeodeticSheet.Id), Length=0, TypeFullName = typeof(int).FullName/*"int"*/},
            new() {IsNullable=false, Name=nameof(GeodeticSheet.SheetName), Length=0, TypeFullName = typeof(string).FullName/*"string"*/},
            new() {IsNullable=false, Name=nameof(GeodeticSheet.SubTitle), Length=0, TypeFullName = typeof(string).FullName/*"string"*/},
            new() {IsNullable=false, Name=nameof(GeodeticSheet.Type), Length=0, TypeFullName = typeof(int).FullName/*"int"*/},
            new() {IsNullable=false, Name="Min Longitude", Length=0, TypeFullName = typeof(int).FullName/*"int"*/},
            new() {IsNullable=false, Name="Max Longitude", Length=0, TypeFullName = typeof(int).FullName/*"int"*/},
            new() {IsNullable=false, Name="Min Latitude", Length=0, TypeFullName = typeof(int).FullName/*"int"*/},
            new() {IsNullable=false, Name="Max Latitude", Length=0, TypeFullName = typeof(int).FullName/*"int"*/},
        ];
    }

    private GridDataSource() : base(_fields)
    {
        GeodeticWgs84Extent = BoundingBoxes.GeodeticWgs84_Iran;
    }

    public override string ToString() => $"{nameof(GridDataSource)} {Type.GetName()}";

    // Get as FeatureSet of Point
    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundingBox)
    {
        var geographicBoundingBox = boundingBox.Transform(MapProjects.WebMercatorToGeodeticWgs84);

        return Task.FromResult(FeatureSet<Point>.Create(string.Empty, GeodeticIndexes.FindIndexSheets(geographicBoundingBox, Type)
                                .Select(ToFeatureMappingFunc)
                                .ToList()));
    }

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry)
    {
        var geographicBoundingBox = geometry?.GetBoundingBox().Transform(MapProjects.WebMercatorToGeodeticWgs84) ?? GeodeticWgs84Extent;

        return Task.FromResult(FeatureSet<Point>.Create(string.Empty, GeodeticIndexes.FindIndexSheets(geographicBoundingBox, Type)
                                .Where(s => geometry == null || s.TheGeometry.Intersects(geometry) == true)
                                .Select(ToFeatureMappingFunc)
                                .ToList()));
    }


    public override Task<FeatureSet<Point>> SearchAsync(string searchText) => throw new NotImplementedException();

    private Feature<Point> ToFeatureMappingFunc(GeodeticSheet geodeticSheet)
    {
        return new Feature<Point>()
        {
            Id = geodeticSheet.Id,
            LabelAttribute = nameof(geodeticSheet.SheetName),
            TheGeometry = geodeticSheet.TheGeometry,
            Attributes = new Dictionary<string, object>()
            {
                {nameof(geodeticSheet.Id), geodeticSheet.Id},
                { nameof(geodeticSheet.SheetName), geodeticSheet.SheetName},
                { nameof(geodeticSheet.SubTitle), geodeticSheet.SubTitle},
                { nameof(geodeticSheet.Type), geodeticSheet.Type},
                { "Min Longitude", geodeticSheet.GeodeticExtent.XMin},
                { "Max Longitude", geodeticSheet.GeodeticExtent.XMax},
                { "Min Latitude", geodeticSheet.GeodeticExtent.YMin},
                { "Max Latitude", geodeticSheet.GeodeticExtent.YMax},
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
