using System;

using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;

namespace IRI.Maptor.Core.Spatial.Helpers.MapGrids;

/// <summary>What a grid's lines are lines of.</summary>
public enum MapGridKind
{
    /// <summary>Meridians and parallels — constant longitude and latitude, in degrees.</summary>
    Geodetic,

    /// <summary>
    /// Constant easting and northing in UTM. Each zone strip in view is walked in its own plane
    /// and its lines are cut at the zone boundary, so the grid restarts at every seam — which is
    /// what a UTM grid *is*.
    /// </summary>
    Utm,

    /// <summary>Constant x and y in one named projection, over the whole view.</summary>
    Projected,
}

/// <summary>
/// Which edges of the view carry the values read off the lines. A printed sheet numbers all four,
/// so the reader can start from whichever edge is nearest.
/// </summary>
[Flags]
public enum MapGridSide
{
    None = 0,

    /// <summary>Along the bottom: the values of the vertical lines.</summary>
    Bottom = 1,

    /// <summary>Along the top: the values of the vertical lines.</summary>
    Top = 2,

    /// <summary>Up the left: the values of the horizontal lines.</summary>
    Left = 4,

    /// <summary>Up the right: the values of the horizontal lines.</summary>
    Right = 8,

    All = Bottom | Top | Left | Right,
}

/// <summary>
/// One grid the user has asked for: what it is lines of, how far apart, and where its values are
/// written. Several may be on the map at once — a geodetic graticule and a UTM grid together is
/// the case this whole design exists for.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MajorInterval"/> is in the grid's own units — degrees for
/// <see cref="MapGridKind.Geodetic"/>, metres otherwise — and is null by default, meaning the
/// interval follows the zoom.
/// </para>
/// <para>
/// This is deliberately *not* a projection wrapper. <see cref="Srs"/> is only consulted for
/// <see cref="MapGridKind.Projected"/>; UTM needs a zone per line and so cannot be described by a
/// single <see cref="SrsBase"/>, and the geodetic graticule needs no projection at all.
/// </para>
/// </remarks>
public sealed class MapGridDefinition
{
    private MapGridDefinition(MapGridKind kind, SrsBase? srs, string key, string title)
    {
        Kind = kind;
        Srs = srs;
        Key = key;
        Title = title;
    }

    public MapGridKind Kind { get; }

    /// <summary>The projection the grid is drawn in; null for <see cref="MapGridKind.Geodetic"/> and <see cref="MapGridKind.Utm"/>.</summary>
    public SrsBase? Srs { get; }

    /// <summary>A stable identifier — used as the settings key and to match a saved selection back to a catalogue entry.</summary>
    public string Key { get; }

    /// <summary>The legend and picker text. Callers localize; the core has no resources.</summary>
    public string Title { get; set; }

    /// <summary>
    /// Degrees for a geodetic grid, metres otherwise. Null — the default — lets the visible extent
    /// choose from the ladder, which is what makes the grid finer as the map zooms in.
    /// </summary>
    public double? MajorInterval { get; set; }

    /// <summary>Which edges the values are written against. All four by default.</summary>
    public MapGridSide LabelSides { get; set; } = MapGridSide.All;

    /// <summary>
    /// How far in from the edge this grid's row of values sits: 0 for the first grid on the map,
    /// 1 for the second, and so on. Two grids at the same tier would print their numbers on top of
    /// each other.
    /// </summary>
    public int LabelTier { get; set; }

    /// <summary>True when the grid's units are degrees rather than metres.</summary>
    public bool IsAngular => Kind == MapGridKind.Geodetic;

    /// <summary>Meridians and parallels.</summary>
    public static MapGridDefinition Geodetic(string? title = null)
        => new MapGridDefinition(MapGridKind.Geodetic, null, "geodetic", title ?? "Lat/Long");

    /// <summary>The UTM grid of whichever zones are in view.</summary>
    public static MapGridDefinition Utm(string? title = null)
        => new MapGridDefinition(MapGridKind.Utm, null, "utm", title ?? "UTM");

    /// <summary>A grid of constant x and y in one projection.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="srs"/> or <paramref name="key"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="srs"/> does not project — a geographic "projection" would produce a grid
    /// whose units are degrees while every label called them metres. Use <see cref="Geodetic"/>.
    /// </exception>
    public static MapGridDefinition Projected(SrsBase srs, string key, string? title = null)
    {
        if (srs is null)
            throw new ArgumentNullException(nameof(srs));

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        if (srs.Type == SpatialReferenceType.None)
            throw new ArgumentException(
                $"'{key}' does not project; its coordinates are degrees. Use {nameof(MapGridDefinition)}.{nameof(Geodetic)}() instead.",
                nameof(srs));

        return new MapGridDefinition(MapGridKind.Projected, srs, key, title ?? srs.Title ?? key);
    }

    public override string ToString()
        => $"{Key} ({Kind}, {(MajorInterval.HasValue ? MajorInterval.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "auto")})";
}
