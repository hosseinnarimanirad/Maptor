using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Spatial.Helpers.MapGrids;

/// <summary>Which of a grid's two families a line belongs to.</summary>
public enum MapGridAxis
{
    /// <summary>Constant easting or longitude — a roughly vertical line, numbered along the bottom and top.</summary>
    X,

    /// <summary>Constant northing or latitude — a roughly horizontal line, numbered up the left and right.</summary>
    Y,
}

/// <summary>How heavily a line is drawn.</summary>
public enum MapGridLineKind
{
    /// <summary>A line at the chosen interval; these are the ones that carry values.</summary>
    Major,

    /// <summary>A subdivision of the major interval, drawn lighter and left unnumbered.</summary>
    Minor,

    /// <summary>A UTM zone boundary. Not a grid line at all — the meridian where the grid restarts.</summary>
    ZoneSeam,
}

/// <summary>
/// One grid line, as a polyline in Web Mercator.
/// </summary>
/// <remarks>
/// A line is <em>always</em> a polyline and never a ring: that is the whole difference from the
/// MGRS overlay, which emits cells as polygons. A line of constant easting bows in Web Mercator,
/// so it carries many vertices; a meridian or parallel is straight there and carries two.
/// </remarks>
public sealed class MapGridLine
{
    public MapGridLine(MapGridAxis axis, MapGridLineKind kind, double value, List<Point> webMercatorPoints, int? zone = null)
    {
        Axis = axis;
        Kind = kind;
        Value = value;
        WebMercatorPoints = webMercatorPoints;
        Zone = zone;
    }

    public MapGridAxis Axis { get; }

    public MapGridLineKind Kind { get; }

    /// <summary>The line's own value, in the grid's units: degrees for a graticule, metres otherwise.</summary>
    public double Value { get; }

    /// <summary>The UTM zone this line belongs to; null for every other kind of grid.</summary>
    public int? Zone { get; }

    public List<Point> WebMercatorPoints { get; }
}

/// <summary>
/// One number written on the map: the value of a line, placed where that line meets an edge of the
/// view.
/// </summary>
/// <remarks>
/// A sheet prints a grid value <em>once per line</em> against the margin, not once per cell, and
/// abbreviates it to the digits that change — which is why <see cref="IsFull"/> exists. The first
/// line met on each side is spelled out so the short forms after it have something to be read
/// against.
/// </remarks>
public sealed class MapGridLabel
{
    public MapGridLabel(string text, Point position, MapGridAxis axis, MapGridSide side, MapGridLineKind kind, bool isFull, double value, int? zone = null)
    {
        Text = text;
        Position = position;
        Axis = axis;
        Side = side;
        Kind = kind;
        IsFull = isFull;
        Value = value;
        Zone = zone;
    }

    public string Text { get; }

    /// <summary>Where to draw it, in Web Mercator.</summary>
    public Point Position { get; }

    public MapGridAxis Axis { get; }

    /// <summary>Exactly one of <see cref="MapGridSide"/>'s four edges.</summary>
    public MapGridSide Side { get; }

    public MapGridLineKind Kind { get; }

    /// <summary>True when the value is spelled out in full rather than abbreviated to its changing digits.</summary>
    public bool IsFull { get; }

    public double Value { get; }

    public int? Zone { get; }
}

/// <summary>The lines and numbers of one grid over one view.</summary>
public sealed class MapGrid
{
    public MapGrid(MapGridDefinition definition, double majorInterval, double? minorInterval, List<MapGridLine> lines, List<MapGridLabel> labels)
    {
        Definition = definition;
        MajorInterval = majorInterval;
        MinorInterval = minorInterval;
        Lines = lines;
        Labels = labels;
    }

    public MapGridDefinition Definition { get; }

    /// <summary>The interval actually used — the one asked for, or the one the extent chose.</summary>
    public double MajorInterval { get; }

    /// <summary>The subdivision interval, or null when the grid is at the finest step of its ladder.</summary>
    public double? MinorInterval { get; }

    /// <summary>
    /// Every line, in one list. Callers that style major and minor apart filter on
    /// <see cref="MapGridLine.Kind"/> rather than reading separate collections, because the layer
    /// draws them from a single feature set.
    /// </summary>
    public List<MapGridLine> Lines { get; }

    public List<MapGridLabel> Labels { get; }

    public IEnumerable<MapGridLine> MajorLines => Lines.Where(l => l.Kind == MapGridLineKind.Major);

    public IEnumerable<MapGridLine> MinorLines => Lines.Where(l => l.Kind == MapGridLineKind.Minor);

    public IEnumerable<MapGridLine> ZoneSeams => Lines.Where(l => l.Kind == MapGridLineKind.ZoneSeam);

    public static MapGrid Empty(MapGridDefinition definition, double majorInterval = 0)
        => new MapGrid(definition, majorInterval, null, new List<MapGridLine>(), new List<MapGridLabel>());

    public override string ToString()
        => $"{Definition.Key}: {Lines.Count} lines, {Labels.Count} labels @ {MajorInterval}";
}
