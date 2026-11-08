using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Ogc.Kml;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Tests.TheGeometry;

/// <summary>
/// Rings in Maptor never repeat the first point as the last one; CreatePolygonRing strips the closing
/// point(s) that formats such as KML write. Some files repeat the closing point more than once.
/// </summary>
public class CreatePolygonRingTest
{
    [Fact]
    public void OpenRing_IsKeptAsIs()
    {
        var ring = Geometry<Point>.CreatePolygonRing(
            new List<Point> { new(0, 0), new(10, 0), new(10, 10), new(0, 10) }, 0);

        Assert.Equal(4, ring.Points.Count);
    }

    [Fact]
    public void ClosedRing_DropsTheClosingPoint()
    {
        var ring = Geometry<Point>.CreatePolygonRing(
            new List<Point> { new(0, 0), new(10, 0), new(10, 10), new(0, 10), new(0, 0) }, 0);

        Assert.Equal(4, ring.Points.Count);
    }

    [Fact]
    public void ClosingPointRepeatedTwice_DropsBothCopies()
    {
        var ring = Geometry<Point>.CreatePolygonRing(
            new List<Point> { new(0, 0), new(10, 0), new(10, 10), new(0, 10), new(0, 0), new(0, 0) }, 0);

        Assert.Equal(4, ring.Points.Count);
        Assert.False(ring.Points[0].HaveTheSameXY(ring.Points[^1]));
    }

    [Fact]
    public void Kml_PolygonWithDoubledClosingPoint_HasArea()
    {
        const string kml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <kml xmlns="http://www.opengis.net/kml/2.2">
              <Document>
                <Placemark>
                  <MultiGeometry>
                    <Polygon>
                      <outerBoundaryIs><LinearRing><coordinates>
                        0,0,0 10,0,0 10,10,0 0,10,0 0,0,0 0,0,0
                      </coordinates></LinearRing></outerBoundaryIs>
                    </Polygon>
                  </MultiGeometry>
                </Placemark>
              </Document>
            </kml>
            """;

        var geometry = KmlReader.Parse(kml, 0).Single();

        var polygon = Assert.IsType<Geometry<PointZ>>(geometry);

        Assert.Equal(GeometryType.MultiPolygon, polygon.Type);
        Assert.Equal(100, polygon.EuclideanArea, 6);
    }

    [Theory]
    [InlineData("0,0,0 10,0,0 10,10,0 0,10,0 0,0")]   // first point 3D, closing point 2D
    [InlineData("0,0 10,0,0 10,10,0 0,10,0 0,0,0")]   // first point 2D, closing point 3D
    public void Kml_RingMixing2DAnd3DPoints_DropsTheClosingPoint(string coordinates)
    {
        var kml = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <kml xmlns="http://www.opengis.net/kml/2.2">
              <Document>
                <Placemark>
                  <Polygon>
                    <outerBoundaryIs><LinearRing><coordinates>{coordinates}</coordinates></LinearRing></outerBoundaryIs>
                  </Polygon>
                </Placemark>
              </Document>
            </kml>
            """;

        var polygon = Assert.IsType<Geometry<Point>>(KmlReader.Parse(kml, 0).Single());

        Assert.Equal(GeometryType.Polygon, polygon.Type);
        Assert.Equal(4, polygon.Geometries[0].Points.Count);
        Assert.Equal(100, polygon.EuclideanArea, 6);
    }
}
