using System.Data.OleDb;
using System.Collections.Generic;

using IRI.Maptor.Sta.PersonalGdb;

namespace IRI.Maptor.Tst.Main.Esri;

public class PersonalGdbTest
{
    readonly string _gdbFile;/*= @"E:\Programming\100. IRI.Japey\IRI.Tst\IRI.Tst.MainTestProject\Assets\PersonalGdb\Test.mdb";*/

    public PersonalGdbTest()
    {
        _gdbFile = $"{System.Environment.CurrentDirectory}\\Assets\\PersonalGdb\\Test.mdb";
    }

    private List<byte[]> ReadGeometries(string mdbFile, string tableName)
    {
        string connString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={mdbFile};Persist Security Info=False;";

        List<byte[]> result = new List<byte[]>();

        using (var conn = new OleDbConnection(connString))
        {
            conn.Open();
            var query = $"SELECT * FROM {tableName}";
            var cmd = new OleDbCommand(query, conn);
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    result.Add((byte[])reader[1]);
                }
            }
        }

        return result;
    }

    private void TestGeometries(List<byte[]> geometries)
    {
        foreach (var geometry in geometries)
        {
            var geo = EsriPGdbHelper.ParseToEsriShape(geometry, 0);
            var newBytes = geo.WriteContentsToByte();

            Assert.Equal(geometry, newBytes);
        }
    }

    private void VerifyTable(string mdbFile, string tableName)
    {
        if (mdbFile is null)
            throw new ArgumentNullException(nameof(mdbFile));

        if (tableName is null)
            throw new ArgumentNullException(nameof(tableName));

        var geometries = ReadGeometries(_gdbFile, "Point");

        TestGeometries(geometries);
    }

    [Fact]
    public void TestPoint()
    {
        VerifyTable(_gdbFile, "Point");
        VerifyTable(_gdbFile, "PointM");
        VerifyTable(_gdbFile, "PointZ");
        VerifyTable(_gdbFile, "PointZM");
    }

    [Fact]
    public void TestMultiPoint()
    {
        VerifyTable(_gdbFile, "MultiPoint");
        VerifyTable(_gdbFile, "MultiPointM");
        VerifyTable(_gdbFile, "MultiPointZ");
        VerifyTable(_gdbFile, "MultiPointZM");
    }

    [Fact]
    public void TestPolyline()
    {
        VerifyTable(_gdbFile, "Polyline");
        VerifyTable(_gdbFile, "PolylineM");
        VerifyTable(_gdbFile, "PolylineZ");
        VerifyTable(_gdbFile, "PolylineZM");
    }

    [Fact]
    public void TestPolygon()
    {
        VerifyTable(_gdbFile, "Polygon");
        VerifyTable(_gdbFile, "PolygonM");
        VerifyTable(_gdbFile, "PolygonZ");
        VerifyTable(_gdbFile, "PolygonZM");
    }
}
