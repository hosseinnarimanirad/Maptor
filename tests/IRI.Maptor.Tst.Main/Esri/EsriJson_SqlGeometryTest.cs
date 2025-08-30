using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Spatial.Primitives.Esri;
using static IRI.Maptor.Tst.NetFrameworkTest.Assets.EsriJsonSamples;

namespace IRI.Maptor.Tst.Esri;


public class EsriJson_SqlGeometryTest
{
    public EsriJson_SqlGeometryTest()
    {
        //SqlServerTypes.Utilities.LoadNativeAssembliesv14();
    }


    [Fact]
    public void TestEsriJson_SqlGeometryConversion()
    {
        
        try
        {
            var point2D = JsonHelper.Deserialize<EsriJsonGeometry>(point2DJson);
            var point3D = JsonHelper.Deserialize<EsriJsonGeometry>(point3DJson);
            var pointNull = JsonHelper.Deserialize<EsriJsonGeometry>(pointNullJson);
            var pointNaN = JsonHelper.Deserialize<EsriJsonGeometry>(pointNaNJson);

            var multipoint2D = JsonHelper.Deserialize<EsriJsonGeometry>(multipoint2DJson);
            var multipoint3D = JsonHelper.Deserialize<EsriJsonGeometry>(multipoint3DJson);
            var multipointEmpty = JsonHelper.Deserialize<EsriJsonGeometry>(multipointEmptyJson);

            var polyline2D = JsonHelper.Deserialize<EsriJsonGeometry>(polyline2DJson);
            var polyline3D = JsonHelper.Deserialize<EsriJsonGeometry>(polylineMJson);
            var polylineEmpty = JsonHelper.Deserialize<EsriJsonGeometry>(polylineEmptyJson);

            var polygon2D = JsonHelper.Deserialize<EsriJsonGeometry>(polygon2DJson);
            var polygon3D = JsonHelper.Deserialize<EsriJsonGeometry>(polygon3DJson);
            var polygonEmpty = JsonHelper.Deserialize<EsriJsonGeometry>(polygonEmptyJson);

            IRI.Maptor.Ket.SqlServerSpatialExtension.Helpers.SqlSpatialHelper.Parse(point2D.AsWkt()).AsEsriJsonGeometry();
            IRI.Maptor.Ket.SqlServerSpatialExtension.Helpers.SqlSpatialHelper.Parse(point3D.AsWkt()).AsEsriJsonGeometry();
            IRI.Maptor.Ket.SqlServerSpatialExtension.Helpers.SqlSpatialHelper.Parse(pointNull.AsWkt()).AsEsriJsonGeometry();
            IRI.Maptor.Ket.SqlServerSpatialExtension.Helpers.SqlSpatialHelper.Parse(pointNaN.AsWkt()).AsEsriJsonGeometry();

            IRI.Maptor.Ket.SqlServerSpatialExtension.Helpers.SqlSpatialHelper.Parse(multipoint2D.AsWkt()).AsEsriJsonGeometry();
            IRI.Maptor.Ket.SqlServerSpatialExtension.Helpers.SqlSpatialHelper.Parse(multipoint3D.AsWkt()).AsEsriJsonGeometry();
            IRI.Maptor.Ket.SqlServerSpatialExtension.Helpers.SqlSpatialHelper.Parse(multipointEmpty.AsWkt()).AsEsriJsonGeometry();

            IRI.Maptor.Ket.SqlServerSpatialExtension.Helpers.SqlSpatialHelper.Parse(polyline2D.AsWkt()).AsEsriJsonGeometry();
            IRI.Maptor.Ket.SqlServerSpatialExtension.Helpers.SqlSpatialHelper.Parse(polyline3D.AsWkt()).AsEsriJsonGeometry();
            IRI.Maptor.Ket.SqlServerSpatialExtension.Helpers.SqlSpatialHelper.Parse(polylineEmpty.AsWkt()).AsEsriJsonGeometry();

            IRI.Maptor.Ket.SqlServerSpatialExtension.Helpers.SqlSpatialHelper.Parse(polygon2D.AsWkt()).AsEsriJsonGeometry();
            IRI.Maptor.Ket.SqlServerSpatialExtension.Helpers.SqlSpatialHelper.Parse(polygon3D.AsWkt()).AsEsriJsonGeometry();
            IRI.Maptor.Ket.SqlServerSpatialExtension.Helpers.SqlSpatialHelper.Parse(polygonEmpty.AsWkt()).AsEsriJsonGeometry();
        }
        catch (Exception ex)
        {
            throw new NotImplementedException();
            
        }
    }

}
