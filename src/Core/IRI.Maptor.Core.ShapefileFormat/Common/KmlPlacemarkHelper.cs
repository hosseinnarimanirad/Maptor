using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Ogc.Kml.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.ShapefileFormat;

static class KmlPlacemarkHelper
{
    private const string CoordinateFormat = "{0:G17},{1:G17}";

    /// <summary>
    /// Projects a coordinate if projection function is provided
    /// </summary>
    public static Point ProjectCoordinate(Point point, Func<Point, Point> projectFunc)
    {
        return projectFunc != null ? projectFunc(point) : point;
    }

    /// <summary>
    /// Formats a coordinate point as a string
    /// </summary>
    public static string FormatCoordinate(Point point)
    {
        return string.Format(CultureInfo.InvariantCulture, CoordinateFormat, point.X, point.Y);
    }

    /// <summary>
    /// Formats x,y coordinates as a string
    /// </summary>
    public static string FormatCoordinateString(double x, double y)
    {
        return string.Format(CultureInfo.InvariantCulture, CoordinateFormat, x, y);
    }

    /// <summary>
    /// Adds IconStyle to a placemark
    /// </summary>
    public static void AddIconStyle(PlacemarkType placemark, byte[] color)
    {
        if (color is null)
            return;

        var style = new StyleType();
        var iconStyle = new IconStyleType();
        iconStyle.Color = color;
        style.IconStyle = iconStyle;
        placemark.AbstractStyleSelectorGroup.Add(style);
    }

    /// <summary>
    /// Adds LineStyle to a placemark
    /// </summary>
    public static void AddLineStyle(PlacemarkType placemark, byte[] color)
    {
        if (color is null)
            return;

        var style = new StyleType();
        var lineStyle = new LineStyleType();
        lineStyle.Color = color;
        style.LineStyle = lineStyle;
        placemark.AbstractStyleSelectorGroup.Add(style);
    }

    /// <summary>
    /// Adds PolyStyle to a placemark
    /// </summary>
    public static void AddPolyStyle(PlacemarkType placemark, byte[] color)
    {
        if (color is null)
            return;

        var style = new StyleType();
        var polyStyle = new PolyStyleType();
        polyStyle.Color = color;
        style.PolyStyle = polyStyle;
        placemark.AbstractStyleSelectorGroup.Add(style);
    }

    /// <summary>
    /// Creates a placemark for a single point
    /// </summary>
    public static PlacemarkType CreatePointPlacemark(Point point, Func<Point, Point> projectFunc, byte[] color)
    {
        var placemark = new PlacemarkType();
        var kmlPoint = new PointType();

        var projectedPoint = ProjectCoordinate(point, projectFunc);
        kmlPoint.Coordinates.Add(FormatCoordinate(projectedPoint));

        placemark.AbstractGeometryGroup = kmlPoint;
        AddIconStyle(placemark, color);

        return placemark;
    }

    /// <summary>
    /// Creates a placemark for multiple points
    /// </summary>
    public static PlacemarkType CreateMultiPointPlacemark(IEnumerable<Point> points, Func<Point, Point> projectFunc, byte[] color)
    {
        var placemark = new PlacemarkType();
        var pointsList = points.ToList();

        if (pointsList.Count == 0)
            return placemark;

        var multiGeometry = new MultiGeometryType();

        foreach (var point in pointsList)
        {
            var kmlPoint = new PointType();
            var projectedPoint = ProjectCoordinate(point, projectFunc);
            kmlPoint.Coordinates.Add(FormatCoordinate(projectedPoint));
            multiGeometry.AbstractGeometryGroup.Add(kmlPoint);
        }

        placemark.AbstractGeometryGroup = multiGeometry;
        AddIconStyle(placemark, color);

        return placemark;
    }

    /// <summary>
    /// Formats ring coordinates, optionally closing the ring
    /// </summary>
    public static string FormatRingCoordinates(IEnumerable<Point> points, Func<Point, Point> projectFunc, bool closeRing)
    {
        var pointsList = points.ToList();
        if (pointsList.Count == 0)
            return string.Empty;

        var coordinateStrings = pointsList.Select(p =>
        {
            var projected = ProjectCoordinate(p, projectFunc);
            return FormatCoordinate(projected);
        });

        var coordinates = string.Join(" ", coordinateStrings);

        if (closeRing && pointsList.Count > 0)
        {
            var firstPoint = ProjectCoordinate(pointsList[0], projectFunc);
            coordinates += " " + FormatCoordinate(firstPoint);
        }

        return coordinates;
    }

    /// <summary>
    /// Creates a placemark with LineString(s) from coordinate strings
    /// </summary>
    public static PlacemarkType CreateLineStringPlacemark(IEnumerable<string> coordinateStrings, byte[] color)
    {
        var placemark = new PlacemarkType();
        var coordinateStringsList = coordinateStrings.ToList();

        if (coordinateStringsList.Count == 0)
            return placemark;

        var multiGeometry = new MultiGeometryType();

        foreach (var coordString in coordinateStringsList)
        {
            var linestring = new LineStringType();
            linestring.Coordinates.Add(coordString);
            multiGeometry.AbstractGeometryGroup.Add(linestring);
        }

        placemark.AbstractGeometryGroup = multiGeometry;
        AddLineStyle(placemark, color);

        return placemark;
    }

    /// <summary>
    /// Creates a polygon placemark with outer ring and optional inner rings
    /// </summary>
    public static PlacemarkType CreatePolygonPlacemark(
        IEnumerable<Point> outerRing,
        IEnumerable<IEnumerable<Point>> innerRings,
        Func<Point, Point> projectFunc,
        byte[] color)
    {
        var placemark = new PlacemarkType();
        var outerRingList = outerRing.ToList();

        if (outerRingList.Count == 0)
            return placemark;

        var polygon = new PolygonType();

        // Outer boundary
        var outerBoundary = new BoundaryType();
        var outerRingKml = new LinearRingType();
        outerRingKml.Coordinates.Add(FormatRingCoordinates(outerRingList, projectFunc, closeRing: true));
        outerBoundary.LinearRing = outerRingKml;
        polygon.OuterBoundaryIs = outerBoundary;

        // Inner boundaries (holes)
        if (innerRings != null)
        {
            foreach (var innerRing in innerRings)
            {
                var innerRingList = innerRing.ToList();
                if (innerRingList.Count == 0)
                    continue;

                var innerBoundary = new BoundaryType();
                var innerRingKml = new LinearRingType();
                innerRingKml.Coordinates.Add(FormatRingCoordinates(innerRingList, projectFunc, closeRing: true));
                innerBoundary.LinearRing = innerRingKml;
                polygon.InnerBoundaryIs.Add(innerBoundary);
            }
        }

        placemark.AbstractGeometryGroup = polygon;
        AddPolyStyle(placemark, color);

        return placemark;
    }
}

