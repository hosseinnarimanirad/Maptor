using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.PersonalGdb;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Ket.PersonalGdbPersistence;
using IRI.Maptor.Ket.PersonalGdbPersistence.Enums;
using IRI.Maptor.Ket.PersonalGdbPersistence.Model;

namespace IRI.Maptor.Tst.Main.Esri;

public class PersonalGdbWriteTest
{
    [Fact]
    public void TestCreateFeatureClassAndInsertRoundTrip()
    {
        var mdbFile = Path.Combine(Path.GetTempPath(), $"MaptorPGdbWrite_{Guid.NewGuid():N}.mdb");

        try
        {
            var gdb = PersonalGdb.CreateEmpty(mdbFile);

            gdb.CreateFeatureClass("PointFc", GeometryType.Point, SrsBases.GeodeticWgs84,
                new List<PersonalGdbField>
                {
                    new() { Name = "Name1", FieldType = GdbEsriFieldType.esriFieldTypeString, Length = 50 },
                    new() { Name = "Code1", FieldType = GdbEsriFieldType.esriFieldTypeInteger },
                });

            var points = new List<Feature<Point>>
            {
                new(Geometry<Point>.Create(51.4, 35.7, 4326), new Dictionary<string, object> { ["Name1"] = "Tehran", ["Code1"] = 10 }),
                new(Geometry<Point>.Create(52.5, 29.6, 4326), new Dictionary<string, object> { ["Name1"] = "Shiraz", ["Code1"] = 20 }),
            };

            Assert.Equal(2, gdb.Insert("PointFc", points));

            gdb.CreateFeatureClass("PolygonFc", GeometryType.Polygon, SrsBases.GeodeticWgs84);

            var ring = new List<Point> { new(46, 25), new(63, 25), new(63, 40), new(46, 40) };

            Assert.Equal(1, gdb.Insert("PolygonFc", new List<Feature<Point>> { new(Geometry<Point>.CreatePolygon(ring, 4326)) }));

            // blobs must survive the existing parser and round-trip byte-identically
            var pointBlobs = ReadShapes(mdbFile, "PointFc");
            var polygonBlobs = ReadShapes(mdbFile, "PolygonFc");

            Assert.Equal(2, pointBlobs.Count);
            Assert.Equal(1, polygonBlobs.Count);

            foreach (var blob in pointBlobs.Concat(polygonBlobs))
                Assert.Equal(blob, EsriPGdbHelper.ParseToEsriShape(blob, 0).WriteContentsToByte());

            Assert.Equal(points[0].TheGeometry!.AsEsriShape()!.WriteContentsToByte(), pointBlobs[0]);

            // read back through the existing data source: features, attributes, extent
            var dataSource = new PersonalGdbDataSource(mdbFile, "PointFc", "PointFc");

            var featureSet = dataSource.GetAsFeatureSetAsync((Geometry<Point>?)null).Result;

            Assert.Equal(2, featureSet.Features.Count);
            Assert.Contains(featureSet.Features, f => "Tehran".Equals(f.Attributes["Name1"]?.ToString()));
            Assert.Contains(featureSet.Features, f => "20".Equals(f.Attributes["Code1"]?.ToString()));
            Assert.False(dataSource.WebMercatorExtent.IsNaN());

            // catalog rows written by CreateFeatureClass/Insert
            Assert.Equal(1, ExecuteScalar(mdbFile, "SELECT COUNT(*) FROM GDB_Items WHERE PhysicalName = 'POINTFC'"));
            Assert.Equal(2, ExecuteScalar(mdbFile, "SELECT COUNT(*) FROM GDB_ItemRelationships"));
            Assert.Equal(2, ExecuteScalar(mdbFile, "SELECT COUNT(*) FROM [PointFc_SHAPE_Index]"));
            Assert.Equal(2, ExecuteScalar(mdbFile, "SELECT COUNT(*) FROM GDB_SpatialRefs"));

            Assert.Equal(51.4, (double)ExecuteScalar(mdbFile, "SELECT ExtentLeft FROM GDB_GeomColumns WHERE TableName = 'PointFc'")!);
            Assert.Equal(35.7, (double)ExecuteScalar(mdbFile, "SELECT ExtentTop FROM GDB_GeomColumns WHERE TableName = 'PointFc'")!);
        }
        finally
        {
            if (File.Exists(mdbFile))
                File.Delete(mdbFile);
        }
    }

    private static List<byte[]> ReadShapes(string mdbFile, string tableName)
    {
        var result = new List<byte[]>();

        using var connection = new OleDbConnection(PersonalGdbInfrastructure.GetConnectionString(mdbFile));

        connection.Open();

        using var command = new OleDbCommand($"SELECT SHAPE FROM [{tableName}] ORDER BY OBJECTID", connection);

        using var reader = command.ExecuteReader();

        while (reader.Read())
            result.Add((byte[])reader[0]);

        return result;
    }

    private static object? ExecuteScalar(string mdbFile, string query)
    {
        using var connection = new OleDbConnection(PersonalGdbInfrastructure.GetConnectionString(mdbFile));

        connection.Open();

        using var command = new OleDbCommand(query, connection);

        return command.ExecuteScalar();
    }
}
