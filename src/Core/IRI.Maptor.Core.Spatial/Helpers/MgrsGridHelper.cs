using System;
using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections.Mgrs;

namespace IRI.Maptor.Core.Spatial.Helpers;

/// <summary>
/// How coarse a step of the MGRS grid is. The metric values are deliberately the same numbers as
/// <see cref="MgrsPrecision"/>, so one casts to the other.
/// </summary>
public enum MgrsGridLevel
{
    /// <summary>Grid zone cells — one UTM zone by one latitude band, 6° × 8°.</summary>
    GridZone = -1,

    Km100 = 0,
    Km10 = 1,
    Km1 = 2,
    M100 = 3,
    M10 = 4,
}

/// <summary>One cell of the grid: the square, and the reference that names it.</summary>
public sealed class MgrsGridCell
{
    public MgrsGridCell(string reference, Geometry<Point> geometry)
    {
        Reference = reference;
        Geometry = geometry;
    }

    /// <summary>e.g. <c>39S</c> at grid zone level, <c>39S WV</c> at 100 km, <c>39S WV 53 39</c> at 1 km.</summary>
    public string Reference { get; }

    /// <summary>The cell outline, in Web Mercator.</summary>
    public Geometry<Point> Geometry { get; }
}

/// <summary>Which of the three things a label says.</summary>
public enum MgrsGridLabelKind
{
    /// <summary>The square's own name — <c>39S</c>, or <c>39S WV</c> — the context the digits are read against.</summary>
    SquareId,

    /// <summary>A value on a vertical line.</summary>
    Easting,

    /// <summary>A value on a horizontal line.</summary>
    Northing,
}

/// <summary>
/// One piece of text on the grid. A paper sheet carries the grid zone and 100 km square in its
/// collar and the principal digits up the side and along the bottom; a screen overlay has no
/// collar, so the square's name is drawn on the map instead.
/// </summary>
public sealed class MgrsGridLabel
{
    public MgrsGridLabel(string text, Point position, MgrsGridLabelKind kind)
    {
        Text = text;
        Position = position;
        Kind = kind;
    }

    public string Text { get; }

    /// <summary>Where to draw it, in Web Mercator.</summary>
    public Point Position { get; }

    public MgrsGridLabelKind Kind { get; }

    public bool IsEasting => Kind == MgrsGridLabelKind.Easting;
}

/// <summary>The cells covering one extent, at one level.</summary>
public sealed class MgrsGrid
{
    public MgrsGrid(MgrsGridLevel level, List<MgrsGridCell> cells, List<MgrsGridLabel> labels)
    {
        Level = level;
        Cells = cells;
        Labels = labels;
    }

    public MgrsGridLevel Level { get; }

    public List<MgrsGridCell> Cells { get; }

    /// <summary>
    /// Everything written on the grid: the name of each visible square, and — below 100 km — the
    /// principal digits on each line.
    /// </summary>
    public List<MgrsGridLabel> Labels { get; }

    /// <summary>Just the line values, for a caller that styles them apart from the square names.</summary>
    public IEnumerable<MgrsGridLabel> AxisLabels => Labels.Where(l => l.Kind != MgrsGridLabelKind.SquareId);

    /// <summary>Just the square names.</summary>
    public IEnumerable<MgrsGridLabel> SquareLabels => Labels.Where(l => l.Kind == MgrsGridLabelKind.SquareId);

    public static MgrsGrid Empty(MgrsGridLevel level) => new MgrsGrid(level, new List<MgrsGridCell>(), new List<MgrsGridLabel>());
}

/// <summary>
/// Builds the MGRS grid over a map extent, picking how fine to draw it from how much ground is on
/// screen — the same idea as <see cref="GraticuleHelper.ChooseInterval"/>, stepping through
/// 100 km / 10 km / 1 km / 100 m / 10 m squares instead of degrees.
/// </summary>
/// <remarks>
/// <para>
/// Cells are emitted as <em>polygons</em> rather than as grid lines, which is what makes the awkward
/// part tractable: MGRS squares are metric inside a UTM zone, so their edges are not straight in
/// Web Mercator and the grid restarts at every zone boundary. As polygons, each zone simply
/// contributes its own cells and the boundary falls out of clipping each cell to its zone's
/// longitude strip. Drawn as continuous lines the seams would have to be stitched by hand.
/// </para>
/// <para>
/// Cell edges are sampled rather than drawn corner to corner, because a straight line in UTM bows
/// in Web Mercator. Four samples an edge is far below a pixel at any zoom where the cell is big
/// enough to see.
/// </para>
/// </remarks>
public static class MgrsGridHelper
{
    /// <summary>Samples per cell edge. Enough that the bow is invisible; small enough to stay cheap.</summary>
    private const int SamplesPerEdge = 4;

    /// <summary>The metric ladder, coarse to fine.</summary>
    private static readonly MgrsGridLevel[] MetricLevels =
    {
        MgrsGridLevel.Km100, MgrsGridLevel.Km10, MgrsGridLevel.Km1, MgrsGridLevel.M100, MgrsGridLevel.M10,
    };

    /// <summary>The side of a cell at this level, in metres. Meaningless for the grid zone level.</summary>
    public static double GetCellSize(MgrsGridLevel level) => MgrsConverter.GetSquareSize((MgrsPrecision)(int)level);

    /// <summary>
    /// How fine to draw the grid for a given extent: the finest level that still puts no more than
    /// <paramref name="maxCellsAcross"/> cells across the view, falling back to grid zone cells
    /// when even 100 km squares would be too many.
    /// </summary>
    public static MgrsGridLevel ChooseLevel(BoundingBox geodeticExtent, int maxCellsAcross = 12)
    {
        var span = GetSpanInMetres(geodeticExtent);

        if (span <= 0 || double.IsNaN(span))
            return MgrsGridLevel.GridZone;

        if (span / GetCellSize(MgrsGridLevel.Km100) > maxCellsAcross)
            return MgrsGridLevel.GridZone;

        var chosen = MgrsGridLevel.Km100;

        foreach (var level in MetricLevels)
        {
            if (span / GetCellSize(level) > maxCellsAcross)
                break;

            chosen = level;
        }

        return chosen;
    }

    /// <summary>
    /// The grid over <paramref name="webMercatorExtent"/>. Pass <paramref name="level"/> to fix how
    /// fine it is; leave it null to let the extent decide.
    /// </summary>
    /// <param name="maxCells">
    /// A ceiling on how many cells are produced, so a pathological extent cannot generate millions.
    /// The grid is returned truncated rather than throwing.
    /// </param>
    public static MgrsGrid Create(BoundingBox webMercatorExtent, MgrsGridLevel? level = null, int maxCells = 4000)
    {
        if (webMercatorExtent.IsNaN() || !webMercatorExtent.IsValid())
            return MgrsGrid.Empty(level ?? MgrsGridLevel.GridZone);

        var geodetic = webMercatorExtent.Transform(MapProjects.WebMercatorToGeodeticWgs84);

        // MGRS stops at the polar circles; nothing below or above them has a cell.
        var south = Math.Max(geodetic.YMin, MinLatitude);
        var north = Math.Min(geodetic.YMax, MaxLatitude);
        var west = Math.Max(geodetic.XMin, -180.0);
        var east = Math.Min(geodetic.XMax, 180.0);

        if (south >= north || west >= east)
            return MgrsGrid.Empty(level ?? MgrsGridLevel.GridZone);

        var clipped = new BoundingBox(west, south, east, north);

        var theLevel = level ?? ChooseLevel(clipped);

        var cells = theLevel == MgrsGridLevel.GridZone
            ? CreateGridZoneCells(clipped, maxCells)
            : CreateSquareCells(clipped, theLevel, maxCells);

        // Every level names its squares; only the finer ones need values on their lines.
        var labels = CreateSquareLabels(clipped, theLevel, maxCells);

        if (theLevel > MgrsGridLevel.Km100)
            labels.AddRange(CreateAxisLabels(clipped, theLevel, maxCells));

        return new MgrsGrid(theLevel, cells, labels);
    }

    #region Grid zone cells

    private static List<MgrsGridCell> CreateGridZoneCells(BoundingBox geodetic, int maxCells)
    {
        var cells = new List<MgrsGridCell>();

        var firstZone = (int)MapProjects.FindUtmZone(geodetic.XMin);
        var lastZone = (int)MapProjects.FindUtmZone(Math.Min(geodetic.XMax, 179.999999));

        foreach (var band in MgrsConverter.BandLetters)
        {
            var (bandSouth, bandNorth) = MgrsConverter.GetBandLatitudeRange(band);

            if (bandNorth <= geodetic.YMin || bandSouth >= geodetic.YMax)
                continue;

            for (var zone = firstZone; zone <= lastZone; zone++)
            {
                var (cellWest, cellEast) = MgrsConverter.GetGridZoneLongitudeRange(zone, band);

                // 32X, 34X and 36X do not exist.
                if (double.IsNaN(cellWest))
                    continue;

                if (cellEast <= geodetic.XMin || cellWest >= geodetic.XMax)
                    continue;

                // A grid zone cell is bounded by meridians and parallels, and Web Mercator maps
                // both to straight lines, so four corners are exact here — no sampling needed.
                var outline = new List<Point>
                {
                    ToWebMercator(cellWest, bandSouth),
                    ToWebMercator(cellWest, bandNorth),
                    ToWebMercator(cellEast, bandNorth),
                    ToWebMercator(cellEast, bandSouth),
                };

                cells.Add(new MgrsGridCell(
                    zone.ToString(System.Globalization.CultureInfo.InvariantCulture) + band,
                    Geometry<Point>.Create(outline, GeometryType.Polygon, SridHelper.WebMercator)));

                if (cells.Count >= maxCells)
                    return cells;
            }
        }

        return cells;
    }

    #endregion

    #region Metric cells

    private static List<MgrsGridCell> CreateSquareCells(BoundingBox geodetic, MgrsGridLevel level, int maxCells)
    {
        var cells = new List<MgrsGridCell>();

        var size = GetCellSize(level);

        var precision = (MgrsPrecision)(int)level;

        var firstZone = (int)MapProjects.FindUtmZone(geodetic.XMin);
        var lastZone = (int)MapProjects.FindUtmZone(Math.Min(geodetic.XMax, 179.999999));

        for (var zone = firstZone; zone <= lastZone; zone++)
        {
            // Squares are metric within one zone, so each zone is walked in its own UTM plane.
            // The nominal strip is used rather than the Norway/Svalbard cell widths: those change
            // which zone a *position* belongs to, and that is already baked into the reference
            // each square reports.
            var stripWest = 6.0 * zone - 186.0;
            var stripEast = 6.0 * zone - 180.0;

            var west = Math.Max(geodetic.XMin, stripWest);
            var east = Math.Min(geodetic.XMax, stripEast);

            if (west >= east)
                continue;

            // North and south of the equator are different UTM origins, so they are walked apart.
            foreach (var isNorth in new[] { true, false })
            {
                var south = isNorth ? Math.Max(geodetic.YMin, 0.0) : geodetic.YMin;
                var north = isNorth ? geodetic.YMax : Math.Min(geodetic.YMax, 0.0);

                if (south >= north)
                    continue;

                AddSquaresForZone(cells, zone, isNorth, size, precision,
                                  new BoundingBox(west, south, east, north), stripWest, stripEast, maxCells);

                if (cells.Count >= maxCells)
                    return cells;
            }
        }

        return cells;
    }

    private static void AddSquaresForZone(
        List<MgrsGridCell> cells, int zone, bool isNorth, double size, MgrsPrecision precision,
        BoundingBox geodeticPart, double stripWest, double stripEast, int maxCells)
    {
        var utm = GetUtmBounds(zone, isNorth, geodeticPart);

        if (utm.IsNaN())
            return;

        var minEasting = Math.Floor(utm.XMin / size) * size;
        var maxEasting = Math.Ceiling(utm.XMax / size) * size;
        var minNorthing = Math.Floor(utm.YMin / size) * size;
        var maxNorthing = Math.Ceiling(utm.YMax / size) * size;

        // Guard before looping rather than after: the product is what could be enormous.
        var estimated = ((maxEasting - minEasting) / size) * ((maxNorthing - minNorthing) / size);

        if (estimated > maxCells * 4)
            return;

        for (var easting = minEasting; easting < maxEasting; easting += size)
        {
            for (var northing = minNorthing; northing < maxNorthing; northing += size)
            {
                var cell = TryCreateSquare(zone, isNorth, easting, northing, size, precision, stripWest, stripEast);

                if (cell is not null)
                    cells.Add(cell);

                if (cells.Count >= maxCells)
                    return;
            }
        }
    }

    private static MgrsGridCell? TryCreateSquare(
        int zone, bool isNorth, double easting, double northing, double size, MgrsPrecision precision,
        double stripWest, double stripEast)
    {
        if (!MgrsConverter.TryFromUtm(zone, isNorth, easting, northing, precision, out var reference))
            return null;

        var centralMeridian = MapProjects.CalculateCentralMeridian(zone);

        var outline = new List<Point>(SamplesPerEdge * 4);

        void Walk(double fromEasting, double fromNorthing, double toEasting, double toNorthing)
        {
            for (var i = 0; i < SamplesPerEdge; i++)
            {
                var t = (double)i / SamplesPerEdge;

                var e = fromEasting + (toEasting - fromEasting) * t;
                var n = fromNorthing + (toNorthing - fromNorthing) * t;

                var geodetic = MapProjects.UTMToGeodetic(new Point(e, n), Ellipsoids.WGS84, centralMeridian, isNorth);

                // Truncate the square at the zone boundary. Clamping the sampled vertices rather
                // than solving each edge against the meridian is exact in the limit and already
                // sub-pixel at four samples an edge.
                var longitude = Math.Min(Math.Max(geodetic.X, stripWest), stripEast);

                outline.Add(ToWebMercator(longitude, geodetic.Y));
            }
        }

        Walk(easting, northing, easting + size, northing);
        Walk(easting + size, northing, easting + size, northing + size);
        Walk(easting + size, northing + size, easting, northing + size);
        Walk(easting, northing + size, easting, northing);

        return new MgrsGridCell(reference, Geometry<Point>.Create(outline, GeometryType.Polygon, SridHelper.WebMercator));
    }

    /// <summary>The UTM bounds of a geodetic box in one zone, sampled around the box's edges.</summary>
    private static BoundingBox GetUtmBounds(int zone, bool isNorth, BoundingBox geodetic)
    {
        double minEasting = double.MaxValue, maxEasting = double.MinValue;
        double minNorthing = double.MaxValue, maxNorthing = double.MinValue;

        const int samples = 8;

        for (var i = 0; i <= samples; i++)
        {
            var t = (double)i / samples;

            var longitude = geodetic.XMin + geodetic.Width * t;
            var latitude = geodetic.YMin + geodetic.Height * t;

            foreach (var corner in new[]
            {
                new Point(longitude, geodetic.YMin),
                new Point(longitude, geodetic.YMax),
                new Point(geodetic.XMin, latitude),
                new Point(geodetic.XMax, latitude),
            })
            {
                var projected = MapProjects.GeodeticToUTM(corner, Ellipsoids.WGS84, zone, isNorth);

                if (double.IsNaN(projected.X) || double.IsNaN(projected.Y))
                    continue;

                minEasting = Math.Min(minEasting, projected.X);
                maxEasting = Math.Max(maxEasting, projected.X);
                minNorthing = Math.Min(minNorthing, projected.Y);
                maxNorthing = Math.Max(maxNorthing, projected.Y);
            }
        }

        if (minEasting > maxEasting || minNorthing > maxNorthing)
            return BoundingBox.NaN;

        return new BoundingBox(minEasting, minNorthing, maxEasting, maxNorthing);
    }

    #endregion

    #region Square names

    /// <summary>
    /// The name of every square visible in the view — <c>39S</c> at grid zone level, <c>39S WV</c>
    /// at 100 km and below — placed at the centre of the part of the square that is actually on
    /// screen.
    /// </summary>
    /// <remarks>
    /// Placing it at the centre of the <em>visible part</em> rather than of the square is what
    /// makes it work at both ends of the zoom. A grid zone cell is 6° by 8° and a 100 km square is
    /// 100 km across, so at most zooms one of them is larger than the view and its true centre is
    /// off screen; anchoring to the visible part keeps exactly one name per square in sight,
    /// whether the view is inside one square or spans four.
    /// </remarks>
    public static List<MgrsGridLabel> CreateSquareLabels(BoundingBox geodetic, MgrsGridLevel level, int maxCells = 4000)
    {
        var labels = new List<MgrsGridLabel>();

        // Grid zone cells name themselves; everything finer is read against its 100 km square.
        var squareLevel = level == MgrsGridLevel.GridZone ? MgrsGridLevel.GridZone : MgrsGridLevel.Km100;

        var squares = squareLevel == MgrsGridLevel.GridZone
            ? CreateGridZoneCells(geodetic, maxCells)
            : CreateSquareCells(geodetic, MgrsGridLevel.Km100, maxCells);

        foreach (var square in squares)
        {
            var box = square.Geometry.GetBoundingBox().Transform(MapProjects.WebMercatorToGeodeticWgs84);

            var visible = box.Intersect(geodetic);

            if (visible.IsNaN() || !visible.IsValid())
                continue;

            labels.Add(new MgrsGridLabel(
                square.Reference,
                ToWebMercator(visible.Center.X, visible.Center.Y),
                MgrsGridLabelKind.SquareId));

            if (labels.Count >= maxCells)
                break;
        }

        return labels;
    }

    #endregion

    #region Axis labels

    /// <summary>
    /// How far in from the edge of the view the labels sit, as a fraction of the view. Far enough
    /// not to be clipped, close enough to read as a margin.
    /// </summary>
    private const double EdgeInset = 0.04;

    /// <summary>
    /// The grid-line values along the bottom and left of the view: for each vertical line the
    /// principal digits of its easting, for each horizontal line those of its northing. This is
    /// what a topographic sheet prints in its margin, and putting them against the edge keeps them
    /// legible while panning instead of scattering a caption through every square.
    /// </summary>
    public static List<MgrsGridLabel> CreateAxisLabels(BoundingBox geodetic, MgrsGridLevel level, int maxCells = 4000)
    {
        var labels = new List<MgrsGridLabel>();

        // The first line met inside each 100 km square spells the reference out in full, so the
        // bare digits on the rest of the lines have something to be read against. This is the
        // convention a sheet follows in its corners.
        var spelledOutColumns = new HashSet<long>();
        var spelledOutRows = new HashSet<long>();

        if (level <= MgrsGridLevel.Km100)
            return labels;

        var size = GetCellSize(level);

        var digits = (int)level;

        var firstZone = (int)MapProjects.FindUtmZone(geodetic.XMin);
        var lastZone = (int)MapProjects.FindUtmZone(Math.Min(geodetic.XMax, 179.999999));

        // Where the two rows of labels sit, in geodetic terms.
        var labelLatitude = geodetic.YMin + geodetic.Height * EdgeInset;
        var labelLongitude = geodetic.XMin + geodetic.Width * EdgeInset;

        for (var zone = firstZone; zone <= lastZone; zone++)
        {
            var stripWest = 6.0 * zone - 186.0;
            var stripEast = 6.0 * zone - 180.0;

            var west = Math.Max(geodetic.XMin, stripWest);
            var east = Math.Min(geodetic.XMax, stripEast);

            if (west >= east)
                continue;

            var isNorth = geodetic.Center.Y >= 0;

            var centralMeridian = MapProjects.CalculateCentralMeridian(zone);

            var utm = GetUtmBounds(zone, isNorth, new BoundingBox(west, geodetic.YMin, east, geodetic.YMax));

            if (utm.IsNaN())
                continue;

            // The row of easting labels, and the column of northing labels, in this zone's UTM.
            var labelRow = MapProjects.GeodeticToUTM(new Point((west + east) / 2, labelLatitude), Ellipsoids.WGS84, zone, isNorth);

            var labelColumn = MapProjects.GeodeticToUTM(new Point(Math.Min(Math.Max(labelLongitude, west), east), geodetic.Center.Y), Ellipsoids.WGS84, zone, isNorth);

            for (var easting = Math.Ceiling(utm.XMin / size) * size; easting <= utm.XMax; easting += size)
            {
                var position = MapProjects.UTMToGeodetic(new Point(easting, labelRow.Y), Ellipsoids.WGS84, centralMeridian, isNorth);

                if (position.X < west || position.X > east)
                    continue;

                var text = GetPrincipalDigits(easting, size, digits);

                var column = (long)Math.Floor(easting / MgrsBands100Km);

                if (spelledOutColumns.Add(zone * 100L + column))
                    text = Spell(zone, isNorth, easting, labelRow.Y, text);

                labels.Add(new MgrsGridLabel(text, ToWebMercator(position.X, position.Y), MgrsGridLabelKind.Easting));

                if (labels.Count >= maxCells)
                    return labels;
            }

            for (var northing = Math.Ceiling(utm.YMin / size) * size; northing <= utm.YMax; northing += size)
            {
                var position = MapProjects.UTMToGeodetic(new Point(labelColumn.X, northing), Ellipsoids.WGS84, centralMeridian, isNorth);

                if (position.Y < geodetic.YMin || position.Y > geodetic.YMax)
                    continue;

                var text = GetPrincipalDigits(northing, size, digits);

                var row = (long)Math.Floor(northing / MgrsBands100Km);

                if (spelledOutRows.Add(zone * 1000L + row))
                    text = Spell(zone, isNorth, labelColumn.X, northing, text);

                labels.Add(new MgrsGridLabel(text, ToWebMercator(position.X, position.Y), MgrsGridLabelKind.Northing));

                if (labels.Count >= maxCells)
                    return labels;
            }
        }

        return labels;
    }

    /// <summary>
    /// The digits a grid line is known by: its offset inside the 100 km square, at this level's
    /// resolution. A 1 km line reads as two digits, a 10 km line as one — exactly the digits the
    /// same position contributes to a full reference.
    /// </summary>
    private static string GetPrincipalDigits(double value, double size, int digits)
    {
        var withinSquare = ((value % MgrsBands100Km) + MgrsBands100Km) % MgrsBands100Km;

        var index = (int)Math.Round(withinSquare / size);

        var wrap = (int)Math.Round(MgrsBands100Km / size);

        if (index >= wrap)
            index -= wrap;

        return index.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(digits, '0');
    }

    private const double MgrsBands100Km = 100000.0;

    /// <summary>
    /// Puts the grid zone and 100 km square in front of a line's digits — <c>36</c> becomes
    /// <c>39S WV 36</c> — so the reader can see how the number they are reading composes into a
    /// whole reference. Falls back to the bare digits if the position cannot be named.
    /// </summary>
    private static string Spell(int zone, bool isNorth, double easting, double northing, string digits)
    {
        if (!MgrsConverter.TryFromUtm(zone, isNorth, easting, northing, MgrsPrecision.Km100, out var square))
            return digits;

        return square + " " + digits;
    }

    #endregion

    #region Helpers

    private const double MinLatitude = -80.0;

    private const double MaxLatitude = 84.0;

    private static Point ToWebMercator(double longitude, double latitude)
        => MapProjects.GeodeticWgs84ToWebMercator(new Point(longitude, latitude));

    /// <summary>The larger of the extent's two sides, in metres.</summary>
    private static double GetSpanInMetres(BoundingBox geodeticExtent)
    {
        var latitude = Math.Min(Math.Max(geodeticExtent.Center.Y, MinLatitude), MaxLatitude);

        var width = geodeticExtent.Width * 111320.0 * Math.Cos(latitude * Math.PI / 180.0);

        var height = geodeticExtent.Height * 110574.0;

        return Math.Max(Math.Abs(width), Math.Abs(height));
    }

    #endregion
}
