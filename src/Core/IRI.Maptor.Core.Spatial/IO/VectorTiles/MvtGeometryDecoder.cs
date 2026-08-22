using System;
using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.Spatial.IO.VectorTiles;

/// <summary>
/// Decodes the command/parameter integer stream of an <see cref="MvtFeature"/> into a Maptor
/// <see cref="Geometry{T}"/>. Coordinates are produced through <paramref name="toPoint"/>, which
/// converts a tile-local integer coordinate to the target CRS (web mercator), so the result never
/// holds tile-local coordinates.
/// </summary>
public static class MvtGeometryDecoder
{
    private const int CommandMoveTo = 1;
    private const int CommandLineTo = 2;
    private const int CommandClosePath = 7;

    public static Geometry<Point>? ToGeometry(MvtFeature feature, Func<int, int, Point> toPoint, int srid)
    {
        if (feature?.Geometry == null || feature.Geometry.Count == 0)
            return null;

        switch (feature.GeometryKind)
        {
            case MvtGeometryKind.Point:
                return DecodePoints(feature.Geometry, toPoint, srid);

            case MvtGeometryKind.LineString:
                return DecodeLines(feature.Geometry, toPoint, srid);

            case MvtGeometryKind.Polygon:
                return DecodePolygons(feature.Geometry, toPoint, srid);

            default:
                return null;
        }
    }

    private static Geometry<Point>? DecodePoints(List<uint> geometry, Func<int, int, Point> toPoint, int srid)
    {
        var points = new List<Point>();
        int cx = 0, cy = 0, i = 0;

        while (i < geometry.Count)
        {
            DecodeCommand(geometry[i++], out int command, out int count);

            if (command != CommandMoveTo)
                break;

            for (int k = 0; k < count && i + 1 < geometry.Count; k++)
            {
                cx += ZigZag(geometry[i++]);
                cy += ZigZag(geometry[i++]);
                points.Add(toPoint(cx, cy));
            }
        }

        if (points.Count == 0)
            return null;

        return points.Count == 1
            ? Geometry<Point>.Create(points, GeometryType.Point, srid)
            : Geometry<Point>.Create(points, GeometryType.MultiPoint, srid);
    }

    private static Geometry<Point>? DecodeLines(List<uint> geometry, Func<int, int, Point> toPoint, int srid)
    {
        var lines = new List<List<Point>>();
        List<Point>? current = null;
        int cx = 0, cy = 0, i = 0;

        while (i < geometry.Count)
        {
            DecodeCommand(geometry[i++], out int command, out int count);

            if (command == CommandMoveTo)
            {
                for (int k = 0; k < count && i + 1 < geometry.Count; k++)
                {
                    cx += ZigZag(geometry[i++]);
                    cy += ZigZag(geometry[i++]);
                    current = new List<Point> { toPoint(cx, cy) };
                    lines.Add(current);
                }
            }
            else if (command == CommandLineTo)
            {
                for (int k = 0; k < count && i + 1 < geometry.Count; k++)
                {
                    cx += ZigZag(geometry[i++]);
                    cy += ZigZag(geometry[i++]);
                    current?.Add(toPoint(cx, cy));
                }
            }
            else
            {
                break;
            }
        }

        var valid = lines.Where(l => l.Count >= 2).ToList();

        if (valid.Count == 0)
            return null;

        if (valid.Count == 1)
            return Geometry<Point>.Create(valid[0], GeometryType.LineString, srid);

        var geometries = valid.Select(l => Geometry<Point>.Create(l, GeometryType.LineString, srid)).ToList();
        return Geometry<Point>.Create(geometries, GeometryType.MultiLineString, srid);
    }

    private static Geometry<Point>? DecodePolygons(List<uint> geometry, Func<int, int, Point> toPoint, int srid)
    {
        var rings = new List<Geometry<Point>>();
        List<Point>? current = null;
        int cx = 0, cy = 0, i = 0;

        while (i < geometry.Count)
        {
            DecodeCommand(geometry[i++], out int command, out int count);

            if (command == CommandMoveTo)
            {
                for (int k = 0; k < count && i + 1 < geometry.Count; k++)
                {
                    cx += ZigZag(geometry[i++]);
                    cy += ZigZag(geometry[i++]);
                    current = new List<Point> { toPoint(cx, cy) };
                }
            }
            else if (command == CommandLineTo)
            {
                for (int k = 0; k < count && i + 1 < geometry.Count; k++)
                {
                    cx += ZigZag(geometry[i++]);
                    cy += ZigZag(geometry[i++]);
                    current?.Add(toPoint(cx, cy));
                }
            }
            else if (command == CommandClosePath)
            {
                // ClosePath implies a return to the ring start; no extra vertex is appended.
                if (current != null && current.Count >= 3)
                    rings.Add(Geometry<Point>.CreatePolygonRing(current, srid));

                current = null;
            }
        }

        if (rings.Count == 0)
            return null;

        // CreatePolygonOrMultiPolygon re-derives exterior/hole containment and fixes winding,
        // so MVT ring ordering/orientation does not need to be pre-classified here.
        return Geometry<Point>.CreatePolygonOrMultiPolygon(rings, srid, fixOrientation: true);
    }

    private static void DecodeCommand(uint commandInteger, out int command, out int count)
    {
        command = (int)(commandInteger & 0x7);
        count = (int)(commandInteger >> 3);
    }

    private static int ZigZag(uint value) => (int)(value >> 1) ^ -(int)(value & 1);
}
