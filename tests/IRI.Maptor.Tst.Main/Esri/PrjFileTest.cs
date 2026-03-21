using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Sta.ShapefileFormat;
using IRI.Maptor.Sta.ShapefileFormat.EsriType;
using IRI.Maptor.Sta.Spatial.IO.Prj;

namespace IRI.Maptor.Tst.Main.Esri;

public class PrjFileTest
{
    // ---------------------------------------------------------------------------------
    // Data for tests that assert a specific SRID
    // ---------------------------------------------------------------------------------
    public static IEnumerable<object[]> PrjSridTestData()
    {
        yield return new object[] {
            // Clarke 1880 (RGS)
            @"GEOGCS[""GCS_Clarke_1880_RGS"",DATUM[""D_Clarke_1880_RGS"",SPHEROID[""Clarke_1880_RGS"",6378249.145,293.465]],PRIMEM[""Greenwich"",0.0],UNIT[""Degree"",0.0174532925199433],AUTHORITY[""EPSG"",4012]]",
            4012,
            "Clarke 1880 (RGS)"
        };
        yield return new object[] {
            // WGS 1984 UTM Zone 39N
            @"PROJCS[""WGS_1984_UTM_Zone_39N"",GEOGCS[""GCS_WGS_1984"",DATUM[""D_WGS_1984"",SPHEROID[""WGS_1984"",6378137.0,298.257223563]],PRIMEM[""Greenwich"",0.0],UNIT[""Degree"",0.0174532925199433]],PROJECTION[""Transverse_Mercator""],PARAMETER[""False_Easting"",500000.0],PARAMETER[""False_Northing"",0.0],PARAMETER[""Central_Meridian"",51.0],PARAMETER[""Scale_Factor"",0.9996],PARAMETER[""Latitude_Of_Origin"",0.0],UNIT[""Meter"",1.0],AUTHORITY[""EPSG"",32639]]",
            32639,
            "WGS 1984 UTM Zone 39N"
        };
        yield return new object[] {
            // WGS 1984 Web Mercator (auxiliary sphere)
            @"PROJCS[""WGS_1984_Web_Mercator_Auxiliary_Sphere"",GEOGCS[""GCS_WGS_1984"",DATUM[""D_WGS_1984"",SPHEROID[""WGS_1984"",6378137.0,298.257223563]],PRIMEM[""Greenwich"",0.0],UNIT[""Degree"",0.0174532925199433]],PROJECTION[""Mercator_Auxiliary_Sphere""],PARAMETER[""False_Easting"",0.0],PARAMETER[""False_Northing"",0.0],PARAMETER[""Central_Meridian"",0.0],PARAMETER[""Standard_Parallel_1"",0.0],PARAMETER[""Auxiliary_Sphere_Type"",0.0],UNIT[""Meter"",1.0],AUTHORITY[""EPSG"",3857]]",
            3857,
            "WGS 1984 Web Mercator (auxiliary sphere)"
        };
        yield return new object[] {
            // Same as above but no AUTHORITY node (common ArcGIS / older exports)
            @"PROJCS[""WGS_1984_Web_Mercator_Auxiliary_Sphere"",GEOGCS[""GCS_WGS_1984"",DATUM[""D_WGS_1984"",SPHEROID[""WGS_1984"",6378137.0,298.257223563]],PRIMEM[""Greenwich"",0.0],UNIT[""Degree"",0.0174532925199433]],PROJECTION[""Mercator_Auxiliary_Sphere""],PARAMETER[""False_Easting"",0.0],PARAMETER[""False_Northing"",0.0],PARAMETER[""Central_Meridian"",0.0],PARAMETER[""Standard_Parallel_1"",0.0],PARAMETER[""Auxiliary_Sphere_Type"",0.0],UNIT[""Meter"",1.0]]",
            3857,
            "WGS 1984 Web Mercator (auxiliary sphere, no AUTHORITY)"
        };
        yield return new object[] {
            // WGS 1984 World Mercator
            @"PROJCS[""WGS_1984_World_Mercator"",GEOGCS[""GCS_WGS_1984"",DATUM[""D_WGS_1984"",SPHEROID[""WGS_1984"",6378137.0,298.257223563]],PRIMEM[""Greenwich"",0.0],UNIT[""Degree"",0.0174532925199433]],PROJECTION[""Mercator""],PARAMETER[""False_Easting"",0.0],PARAMETER[""False_Northing"",0.0],PARAMETER[""Central_Meridian"",0.0],PARAMETER[""Standard_Parallel_1"",0.0],UNIT[""Meter"",1.0],AUTHORITY[""EPSG"",3395]]",
            3395,
            "WGS 1984 World Mercator"
        };
        yield return new object[] {
            // WGS 1984
            @"GEOGCS[""GCS_WGS_1984"",DATUM[""D_WGS_1984"",SPHEROID[""WGS_1984"",6378137.0,298.257223563]],PRIMEM[""Greenwich"",0.0],UNIT[""Degree"",0.0174532925199433],AUTHORITY[""EPSG"",4326]]",
            4326,
            "WGS 1984"
        };
    }

    [Theory]
    [MemberData(nameof(PrjSridTestData))]
    public void TestPrjFile_ParsingAndSrid_Parameterized(string prjContent, int expectedSrid, string displayName)
    {
        // Arrange (Input prjContent is provided by MemberData, displayName is for test reporting)

        // Act
        var prj = EsriPrjFile.Parse(prjContent);

        // Assert
        Assert.NotNull(prj); // Good to ensure parsing didn't return null
        Assert.Equal(expectedSrid, prj.Srid);
    }

    // ---------------------------------------------------------------------------------
    // Data for tests that only assert successful parsing (for files prj2, prj3, prj4, prj5, prj6, prj11 from original)
    // ---------------------------------------------------------------------------------
    public static IEnumerable<object[]> PrjSuccessfulParseTestData()
    {
        yield return new object[] {
            // Cylindrical Equal Area (world)
            @"PROJCS[""World_Cylindrical_Equal_Area"",GEOGCS[""GCS_WGS_1984"",DATUM[""D_WGS_1984"",SPHEROID[""WGS_1984"",6378137.0,298.257223563]],PRIMEM[""Greenwich"",0.0],UNIT[""Degree"",0.0174532925199433]],PROJECTION[""Cylindrical_Equal_Area""],PARAMETER[""False_Easting"",0.0],PARAMETER[""False_Northing"",0.0],PARAMETER[""Central_Meridian"",0.0],PARAMETER[""Standard_Parallel_1"",0.0],UNIT[""Meter"",1.0],AUTHORITY[""ESRI"",54034]]",
            "Cylindrical Equal Area (world)"
        };
        yield return new object[] {
            // d900
            @"PROJCS[""LCC_D900"",GEOGCS["""",DATUM[""D_unknown"",SPHEROID[""unretrievable_using_WGS84"",6378137.0,298.257223563]],PRIMEM[""Greenwich"",0.0],UNIT[""Degree"",0.0174532925199433]],PROJECTION[""Lambert_Conformal_Conic""],PARAMETER[""False_Easting"",1500000.0],PARAMETER[""False_Northing"",1166200.0],PARAMETER[""Central_Meridian"",45.0],PARAMETER[""Standard_Parallel_1"",29.65508275],PARAMETER[""Standard_Parallel_2"",35.31468808333333],PARAMETER[""Scale_Factor"",0.9987864078],PARAMETER[""Latitude_Of_Origin"",32.5],UNIT[""Meter"",1.0]]",
            "d900"
        };
        yield return new object[] {
            // lccnioc
            @"PROJCS[""lccnioc"", GEOGCS[""GCS_Clarke_1880_RGS"",DATUM[""D_Clarke_1880_RGS"",SPHEROID[""Clarke_1880_RGS"",6378249.145,293.465]],PRIMEM[""Greenwich"",0.0],UNIT[""Degree"",0.0174532925199433]],PROJECTION[""Lambert_Conformal_Conic""],PARAMETER[""False_Easting"",1500000.0],PARAMETER[""False_Northing"",1166200.0],PARAMETER[""Central_Meridian"",45.0],PARAMETER[""Standard_Parallel_1"",29.65508274166],PARAMETER[""Standard_Parallel_2"",35.31468809166],PARAMETER[""Scale_Factor"",0.9987864078],PARAMETER[""Latitude_Of_Origin"",32.5],UNIT[""Meter"",1.0]]",
            "lccnioc"
        };
        yield return new object[] {
            // Mercator (sphere)
            @"PROJCS[""Sphere_Mercator"",GEOGCS[""GCS_Sphere"",DATUM[""D_Sphere"",SPHEROID[""Sphere"",6371000.0,0.0]],PRIMEM[""Greenwich"",0.0],UNIT[""Degree"",0.0174532925199433]],PROJECTION[""Mercator""],PARAMETER[""False_Easting"",0.0],PARAMETER[""False_Northing"",0.0],PARAMETER[""Central_Meridian"",0.0],PARAMETER[""Standard_Parallel_1"",0.0],UNIT[""Meter"",1.0],AUTHORITY[""ESRI"",53004]]",
            "Mercator (sphere)"
        };
        yield return new object[] {
            // Mercator (world)
            @"PROJCS[""World_Mercator"",GEOGCS[""GCS_WGS_1984"",DATUM[""D_WGS_1984"",SPHEROID[""WGS_1984"",6378137.0,298.257223563]],PRIMEM[""Greenwich"",0.0],UNIT[""Degree"",0.0174532925199433]],PROJECTION[""Mercator""],PARAMETER[""False_Easting"",0.0],PARAMETER[""False_Northing"",0.0],PARAMETER[""Central_Meridian"",0.0],PARAMETER[""Standard_Parallel_1"",0.0],UNIT[""Meter"",1.0],AUTHORITY[""ESRI"",54004]]",
            "Mercator (world)"
        };
        yield return new object[] {
            // World_Mercator (Note: content is same as "Mercator (world)")
            @"PROJCS[""World_Mercator"",GEOGCS[""GCS_WGS_1984"",DATUM[""D_WGS_1984"",SPHEROID[""WGS_1984"",6378137.0,298.257223563]],PRIMEM[""Greenwich"",0.0],UNIT[""Degree"",0.0174532925199433]],PROJECTION[""Mercator""],PARAMETER[""False_Easting"",0.0],PARAMETER[""False_Northing"",0.0],PARAMETER[""Central_Meridian"",0.0],PARAMETER[""Standard_Parallel_1"",0.0],UNIT[""Meter"",1.0],AUTHORITY[""ESRI"",54004]]",
            "World_Mercator"
        };
    }

    [Theory]
    [MemberData(nameof(PrjSuccessfulParseTestData))]
    public void TestPrjFile_ParsesSuccessfully_Parameterized(string prjContent, string displayName)
    {
        // Arrange (Input prjContent is provided by MemberData, displayName is for test reporting)

        // Act
        var prj = EsriPrjFile.Parse(prjContent);

        // Assert
        Assert.NotNull(prj); // Basic assertion that parsing produced an object
                             // If EsriPrjFile.Parse might throw on invalid string, this test covers that too.
                             // Optionally, if these files are expected to have a specific Srid (e.g. 0 or an ESRI code),
                             // you could assert it here instead of in the other test.
                             // For instance, "Cylindrical Equal Area (world)" has ESRI:54034. If that should be checked,
                             // move it to PrjSridTestData.
    }


    // ---------------------------------------------------------------------------------
    // TestD900ToWebMercatorProjection with embedded PRJ strings
    // ---------------------------------------------------------------------------------
    [Fact]
    public void TestD900ToWebMercatorProjection()
    {
        // Arrange
        string d900PrjContent = @"PROJCS[""LCC_D900"",GEOGCS["""",DATUM[""D_unknown"",SPHEROID[""unretrievable_using_WGS84"",6378137.0,298.257223563]],PRIMEM[""Greenwich"",0.0],UNIT[""Degree"",0.0174532925199433]],PROJECTION[""Lambert_Conformal_Conic""],PARAMETER[""False_Easting"",1500000.0],PARAMETER[""False_Northing"",1166200.0],PARAMETER[""Central_Meridian"",45.0],PARAMETER[""Standard_Parallel_1"",29.65508275],PARAMETER[""Standard_Parallel_2"",35.31468808333333],PARAMETER[""Scale_Factor"",0.9987864078],PARAMETER[""Latitude_Of_Origin"",32.5],UNIT[""Meter"",1.0]]";
        string webMercatorPrjContent = @"PROJCS[""WGS_1984_Web_Mercator_Auxiliary_Sphere"",GEOGCS[""GCS_WGS_1984"",DATUM[""D_WGS_1984"",SPHEROID[""WGS_1984"",6378137.0,298.257223563]],PRIMEM[""Greenwich"",0.0],UNIT[""Degree"",0.0174532925199433]],PROJECTION[""Mercator_Auxiliary_Sphere""],PARAMETER[""False_Easting"",0.0],PARAMETER[""False_Northing"",0.0],PARAMETER[""Central_Meridian"",0.0],PARAMETER[""Standard_Parallel_1"",0.0],PARAMETER[""Auxiliary_Sphere_Type"",0.0],UNIT[""Meter"",1.0],AUTHORITY[""EPSG"",3857]]";

        var d900Prj = EsriPrjFile.Parse(d900PrjContent)
                                 .AsMapProjection();

        var webMercator = EsriPrjFile.Parse(webMercatorPrjContent)
                                     .AsMapProjection();

        // These still rely on external files. Consider if these shapefiles' data
        // can also be embedded or mocked for fully self-contained tests if needed.
        var sourceShapes = Shapefile.ReadShapes($"Assets\\ShapefileSamples\\sourceD900.shp").ToList();
        var targetShapes = Shapefile.ReadShapes($"Assets\\ShapefileSamples\\targetWebMercator.shp").ToList();

        // Act
        var projected = Shapefile.Project(sourceShapes, d900Prj, webMercator);

        // Assert
        Assert.Equal(targetShapes.Count, projected.Count); // Good to check counts match before looping
        for (int i = 0; i < targetShapes.Count; i++) // Loop to targetShapes.Count, not Count-1
        {
            // Assuming EsriPoint, adjust if the actual type is different or a base class
            var expectedPoint = (EsriPoint)targetShapes[i];
            var actualPoint = (EsriPoint)projected[i];

            Assert.Equal(expectedPoint.X, actualPoint.X, 4 /*precision*/);
            Assert.Equal(expectedPoint.Y, actualPoint.Y, 4 /*precision*/);
        }
    }
}
