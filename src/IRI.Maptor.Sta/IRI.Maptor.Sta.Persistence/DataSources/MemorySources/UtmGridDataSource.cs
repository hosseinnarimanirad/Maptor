using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.MapIndexes;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Sta.Persistence.DataSources;

public class UtmGridDataSource : VectorDataSource
{
    private readonly static List<Field> _fields = new List<Field>();

    public int UtmZone { get; set; }

    public UtmIndexType Type { get; protected set; }

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

    static UtmGridDataSource()
    {
        _fields =
        [
            new() {IsNullable=false, Name=nameof(UtmSheet.Id), Length=0, Type="int"},
            new() {IsNullable=false, Name=nameof(UtmSheet.SheetName), Length=0, Type="string"},
            new() {IsNullable=false, Name=nameof(UtmSheet.UtmZone), Length=0, Type="int"},
            new() {IsNullable=false, Name=nameof(UtmSheet.Type), Length=0, Type="int"},
            new() {IsNullable=false, Name=nameof(UtmSheet.Row), Length=0, Type="int"},
            new() {IsNullable=false, Name=nameof(UtmSheet.Column), Length=0, Type="int"},
        ];
    }
    //    {nameof(geometryAware.Id), geometryAware.Id},
    //    {nameof(geometryAware.SheetName), geometryAware.SheetName},
    //    { nameof(geometryAware.UtmZone), geometryAware.UtmZone},
    //    { nameof(geometryAware.Type), geometryAware.Type},
    //    { nameof(geometryAware.Row), geometryAware.Row},
    //    { nameof(geometryAware.Column), geometryAware.Column},

    private UtmGridDataSource() : base(_fields)
    {
        GeodeticWgs84Extent = BoundingBoxes.GeodeticWgs84_Iran;
    }

    public override string ToString()
    {
        return $"UtmGridDataSource {Type.GetName()}";
    }


    // Get as FeatureSet of Point
    public override FeatureSet<Point> GetAsFeatureSet(BoundingBox boundingBox)
    {
        var geographicBoundingBox = boundingBox.Transform(MapProjects.WebMercatorToGeodeticWgs84);

        var features = UtmIndexes.GetIndexSheets(geographicBoundingBox, Type, UtmZone)
                            //.Where(s => s.TheGeometry?.Intersects(boundingBox) == true)
                            .Select(ToFeatureMappingFunc)
                            .ToList();

        return FeatureSet<Point>.Create(string.Empty, features);
    }

    public override FeatureSet<Point> GetAsFeatureSet(Geometry<Point>? geometry)
    {
        var geographicBoundingBox = geometry?.GetBoundingBox().Transform(MapProjects.WebMercatorToGeodeticWgs84) ?? GeodeticWgs84Extent;

        var features = UtmIndexes.GetIndexSheets(geographicBoundingBox, Type, UtmZone)
                            .Where(s => s.TheGeometry?.Intersects(geometry) == true)
                            .Select(ToFeatureMappingFunc)
                            .ToList();

        return FeatureSet<Point>.Create(string.Empty, features);
    }


    public override FeatureSet<Point> Search(string searchText)
    {
        throw new NotImplementedException();
    }

    private Feature<Point> ToFeatureMappingFunc(UtmSheet geometryAware)
    {
        return new Feature<Point>()
        {
            Id = geometryAware.Id,
            LabelAttribute = nameof(geometryAware.SheetName),
            TheGeometry = geometryAware.TheGeometry,
            Attributes = new Dictionary<string, object>()
                                {
                                    {nameof(geometryAware.Id), geometryAware.Id},
                                    {nameof(geometryAware.SheetName), geometryAware.SheetName},
                                    {nameof(geometryAware.UtmZone), geometryAware.UtmZone},
                                    {nameof(geometryAware.Type), geometryAware.Type},
                                    {nameof(geometryAware.Row), geometryAware.Row},
                                    {nameof(geometryAware.Column), geometryAware.Column},
                                }
        };
    }



    public static UtmGridDataSource Create(UtmIndexType indexType, int utmZone)
    {
        UtmGridDataSource result = new UtmGridDataSource();

        result.Type = indexType;

        result.UtmZone = utmZone;

        return result;
    }
}
