using IRI.Maptor.Extensions;
using IRI.Maptor.Samples.Core.Runner;
using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.ShapefileFormat;
using IRI.Maptor.Core.ShapefileFormat.ShapeTypes.Abstractions;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Samples.Core.SpatialFormats;

/// <summary>
/// Writes the same polygons as GeoJSON and as a Shapefile and compares the file sizes.
/// Two data sets are used because the answer depends on the coordinates: a Shapefile stores every
/// vertex as two 8-byte doubles, GeoJSON stores them as text, so short coordinates favour GeoJSON
/// and full-precision coordinates favour the Shapefile.
/// The polygons are generated, so the sample needs no input file; output goes to the temp folder.
/// </summary>
public static class GeoJsonVsShapefile
{
    [Sample("formats/geojson-vs-shapefile", "Same geometries as GeoJSON and Shapefile — size comparison")]
    public static void Run()
    {
        var folder = Path.Combine(Path.GetTempPath(), "maptor-samples", "geojson-vs-shapefile");
        Directory.CreateDirectory(folder);

        Compare("grid", CreateGrid(columns: 100, rows: 100, cellSizeDegrees: 0.01), folder);
        Compare("circles", CreateCircles(count: 2000, vertices: 24, radiusDegrees: 0.004), folder);
    }

    static void Compare(string name, List<Geometry<Point>> polygons, string folder)
    {
        int vertices = polygons.Sum(p => p.TotalNumberOfPoints);

        // GeoJSON: convert each geometry and serialize the list (geometries only, no attributes)
        var geoJsonPath = Path.Combine(folder, $"{name}.json");
        File.WriteAllText(geoJsonPath, JsonHelper.Serialize(polygons.Select(p => p.AsGeoJson()).ToList()));

        // Shapefile: convert to Esri shapes and save (.shp + .shx; no .dbf since there are no attributes)
        var shapefilePath = Path.Combine(folder, $"{name}.shp");
        var esriShapes = polygons.Select(p => p.AsEsriShape()).OfType<EsriShapeBase>().ToList();
        Shapefile.Save(shapefilePath, esriShapes, createDbf: false, overwrite: true);

        long geoJsonSize = new FileInfo(geoJsonPath).Length;
        long shapefileSize = new FileInfo(shapefilePath).Length
                           + new FileInfo(Path.ChangeExtension(shapefilePath, ".shx")).Length;

        Console.WriteLine($"{name}: {polygons.Count:N0} polygons, {vertices:N0} vertices, written to {folder}");
        Console.WriteLine();
        Console.WriteLine($"| format                | bytes       | bytes / vertex |");
        Console.WriteLine($"| --------------------- | ----------- | -------------- |");
        Console.WriteLine($"| GeoJSON               | {geoJsonSize,11:N0} | {(double)geoJsonSize / vertices,14:0.0} |");
        Console.WriteLine($"| Shapefile (.shp+.shx) | {shapefileSize,11:N0} | {(double)shapefileSize / vertices,14:0.0} |");
        Console.WriteLine();
        Console.WriteLine($"Shapefile / GeoJSON size ratio: {(double)shapefileSize / geoJsonSize:0.00}");
        Console.WriteLine();
    }

    /// <summary>A regular grid of squares in WGS 84 (north of Paris) — coordinates have at most two decimals.</summary>
    static List<Geometry<Point>> CreateGrid(int columns, int rows, double cellSizeDegrees)
    {
        const double originX = 2.0, originY = 48.0;

        var result = new List<Geometry<Point>>(columns * rows);

        for (int c = 0; c < columns; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                double x0 = originX + c * cellSizeDegrees, y0 = originY + r * cellSizeDegrees;
                double x1 = x0 + cellSizeDegrees, y1 = y0 + cellSizeDegrees;

                var ring = new List<Point> { new(x0, y0), new(x1, y0), new(x1, y1), new(x0, y1), new(x0, y0) };

                result.Add(Geometry<Point>.CreatePolygon(ring, SridHelper.GeodeticWGS84));
            }
        }

        return result;
    }

    /// <summary>Circle-like polygons in WGS 84 — coordinates use the full double precision.</summary>
    static List<Geometry<Point>> CreateCircles(int count, int vertices, double radiusDegrees)
    {
        var random = new Random(42);
        var result = new List<Geometry<Point>>(count);

        for (int i = 0; i < count; i++)
        {
            double cx = 2 + random.NextDouble(), cy = 48 + random.NextDouble();

            var ring = new List<Point>(vertices + 1);

            for (int v = 0; v < vertices; v++)
            {
                double angle = 2 * Math.PI * v / vertices;
                ring.Add(new Point(cx + radiusDegrees * Math.Cos(angle), cy + radiusDegrees * Math.Sin(angle)));
            }

            ring.Add(ring[0]);

            result.Add(Geometry<Point>.CreatePolygon(ring, SridHelper.GeodeticWGS84));
        }

        return result;
    }
}
