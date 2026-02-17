using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.MapIndexes;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;

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

    public override int Srid { get => SridHelper.WebMercator; /*protected set => _ = value; */}

    public override GeometryType? GeometryType
    {
        get => Common.Enums.GeometryType.Polygon;
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

    private UtmGridDataSource() : base(_fields)
    {
        GeodeticWgs84Extent = BoundingBoxes.GeodeticWgs84_Iran;
    }

    public override string ToString() => $"{nameof(UtmGridDataSource)} {Type.GetName()}";


    // Get as FeatureSet of Point
    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundingBox)
    {
        var geographicBoundingBox = boundingBox.Transform(MapProjects.WebMercatorToGeodeticWgs84);

        var features = UtmIndexes.GetIndexSheets(geographicBoundingBox, Type, UtmZone)
                            .Select(ToFeatureMappingFunc)
                            .ToList();

        return Task.FromResult(FeatureSet<Point>.Create(string.Empty, features));
    }

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry)
    {
        var geographicBoundingBox = geometry?.GetBoundingBox().Transform(MapProjects.WebMercatorToGeodeticWgs84) ?? GeodeticWgs84Extent;

        var features = UtmIndexes.GetIndexSheets(geographicBoundingBox, Type, UtmZone)
                            .Where(s => geometry is null || s.TheGeometry?.Intersects(geometry) == true)
                            .Select(ToFeatureMappingFunc)
                            .ToList();

        return Task.FromResult(FeatureSet<Point>.Create(string.Empty, features));
    }


    public override Task<FeatureSet<Point>> SearchAsync(string searchText) => throw new NotImplementedException();

    private Feature<Point> ToFeatureMappingFunc(UtmSheet utmSheet)
    {
        return new Feature<Point>()
        {
            Id = utmSheet.Id,
            LabelAttribute = nameof(utmSheet.SheetName),
            TheGeometry = utmSheet.TheGeometry,
            Attributes = new Dictionary<string, object>()
                                {
                                    {nameof(utmSheet.Id), utmSheet.Id},
                                    {nameof(utmSheet.SheetName), utmSheet.SheetName},
                                    {nameof(utmSheet.UtmZone), utmSheet.UtmZone},
                                    {nameof(utmSheet.Type), utmSheet.Type},
                                    {nameof(utmSheet.Row), utmSheet.Row},
                                    {nameof(utmSheet.Column), utmSheet.Column},
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
