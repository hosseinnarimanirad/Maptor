using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Sta.GeoParquet;

/// <summary>
/// Minimal unit tests for GeoParquet functionality
/// </summary>
public static class GeoParquetTests
{
    /// <summary>
    /// Test writing and reading a simple point feature
    /// </summary>
    public static bool TestPointFeature()
    {
        try
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                // Create a point feature
                var point = Geometry<Point>.Create(10.0, 20.0, SridHelper.GeodeticWGS84);
                var feature = new Feature<Point>(point, new Dictionary<string, object> { { "name", "Test Point" } });

                // Write to GeoParquet
                GeoParquetWriter.WriteFeatures(tempFile, new[] { feature });

                // Read back
                var readFeatures = GeoParquetReader.ReadFeatures(tempFile).ToList();

                if (readFeatures.Count != 1)
                    return false;

                var readFeature = readFeatures[0];
                if (readFeature.TheGeometry.Type != GeometryType.Point)
                    return false;

                return true;
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Test writing and reading a FeatureSet
    /// </summary>
    public static bool TestFeatureSet()
    {
        try
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                // Create features
                var point1 = Geometry<Point>.Create(10.0, 20.0, SridHelper.GeodeticWGS84);
                var point2 = Geometry<Point>.Create(11.0, 21.0, SridHelper.GeodeticWGS84);
                var features = new List<Feature<Point>>
                {
                    new Feature<Point>(point1, new Dictionary<string, object> { { "id", 1 }, { "name", "Point 1" } }),
                    new Feature<Point>(point2, new Dictionary<string, object> { { "id", 2 }, { "name", "Point 2" } })
                };
                var featureSet = FeatureSet<Point>.Create("Test Set", features);

                // Write to GeoParquet
                GeoParquetWriter.WriteFeatureSet(tempFile, featureSet);

                // Read back
                var readFeatureSet = GeoParquetReader.ReadFeatureSet(tempFile);

                if (readFeatureSet.Features.Count != 2)
                    return false;

                if (readFeatureSet.Srid != SridHelper.GeodeticWGS84)
                    return false;

                return true;
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Test conversion to/from WKB
    /// </summary>
    public static bool TestWkbConversion()
    {
        try
        {
            var point = Geometry<Point>.Create(10.0, 20.0, SridHelper.GeodeticWGS84);
            
            // Convert to WKB
            var wkb = point.ToGeoParquetWkb();
            if (wkb == null || wkb.Length == 0)
                return false;

            // Convert back from WKB
            var restored = wkb.FromGeoParquetWkb(SridHelper.GeodeticWGS84);
            if (restored == null)
                return false;

            if (restored.Type != GeometryType.Point)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Test extension methods
    /// </summary>
    public static bool TestExtensions()
    {
        try
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                var point = Geometry<Point>.Create(10.0, 20.0, SridHelper.GeodeticWGS84);
                var feature = new Feature<Point>(point, new Dictionary<string, object> { { "test", "value" } });
                var featureSet = FeatureSet<Point>.Create("Test", new List<Feature<Point>> { feature });

                // Test extension methods
                featureSet.WriteToGeoParquet(tempFile);
                var readSet = GeoParquetExtensions.ReadFromGeoParquet(tempFile);

                if (readSet.Features.Count != 1)
                    return false;

                return true;
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Run all tests
    /// </summary>
    public static void RunAllTests()
    {
        Console.WriteLine("Running GeoParquet Tests...");
        
        Console.WriteLine($"TestPointFeature: {(TestPointFeature() ? "PASS" : "FAIL")}");
        Console.WriteLine($"TestFeatureSet: {(TestFeatureSet() ? "PASS" : "FAIL")}");
        Console.WriteLine($"TestWkbConversion: {(TestWkbConversion() ? "PASS" : "FAIL")}");
        Console.WriteLine($"TestExtensions: {(TestExtensions() ? "PASS" : "FAIL")}");
    }
}

