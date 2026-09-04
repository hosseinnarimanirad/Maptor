using System;
using System.Linq;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections.Mgrs;

using Xunit;

namespace IRI.Maptor.Tests.CoordinateSystems;

/// <summary>
/// The MGRS grid overlay: which squares cover an extent, and how fine they are.
/// </summary>
/// <remarks>
/// Cells are polygons rather than grid lines, which is what makes the zone seams tractable —
/// each zone contributes its own cells and the boundary falls out of clipping each cell to its
/// zone's longitude strip.
/// </remarks>
public class MgrsGridTest
{
    private static BoundingBox WebMercator(double west, double south, double east, double north)
        => new BoundingBox(west, south, east, north).Transform(MapProjects.GeodeticWgs84ToWebMercator);

    private static BoundingBox Around(double longitude, double latitude, double degrees)
        => WebMercator(longitude - degrees / 2, latitude - degrees / 2, longitude + degrees / 2, latitude + degrees / 2);

    private static BoundingBox GeodeticOf(MgrsGridCell cell)
        => cell.Geometry.GetBoundingBox().Transform(MapProjects.WebMercatorToGeodeticWgs84);

    #region Choosing the level

    /// <summary>
    /// The grid gets finer as the view narrows, and never coarser. Spans are around Tehran, so a
    /// degree of longitude is about 90 km.
    /// </summary>
    [Theory]
    [InlineData(60.0, MgrsGridLevel.GridZone)]
    [InlineData(20.0, MgrsGridLevel.GridZone)]
    [InlineData(6.0, MgrsGridLevel.Km100)]
    [InlineData(2.0, MgrsGridLevel.Km100)]
    [InlineData(0.5, MgrsGridLevel.Km10)]
    [InlineData(0.02, MgrsGridLevel.Km1)]
    [InlineData(0.004, MgrsGridLevel.M100)]
    [InlineData(0.0006, MgrsGridLevel.M10)]
    public void ChooseLevel_FollowsTheExtent(double degrees, MgrsGridLevel expected)
    {
        var extent = new BoundingBox(51.4 - degrees / 2, 35.7 - degrees / 2, 51.4 + degrees / 2, 35.7 + degrees / 2);

        Assert.Equal(expected, MgrsGridHelper.ChooseLevel(extent));
    }

    /// <summary>Zooming in never steps the grid back to a coarser square.</summary>
    [Fact]
    public void ChooseLevel_IsMonotonicAsTheViewNarrows()
    {
        var previous = MgrsGridLevel.GridZone;

        for (var degrees = 90.0; degrees > 0.0002; degrees /= 1.5)
        {
            var extent = new BoundingBox(51.4 - degrees / 2, 35.7 - degrees / 2, 51.4 + degrees / 2, 35.7 + degrees / 2);

            var level = MgrsGridHelper.ChooseLevel(extent);

            Assert.True((int)level >= (int)previous, $"level went backwards at {degrees}°: {previous} -> {level}");

            previous = level;
        }
    }

    [Theory]
    [InlineData(MgrsGridLevel.Km100, 100000.0)]
    [InlineData(MgrsGridLevel.Km10, 10000.0)]
    [InlineData(MgrsGridLevel.Km1, 1000.0)]
    [InlineData(MgrsGridLevel.M100, 100.0)]
    [InlineData(MgrsGridLevel.M10, 10.0)]
    public void GetCellSize_MatchesTheLevelName(MgrsGridLevel level, double expected)
    {
        Assert.Equal(expected, MgrsGridHelper.GetCellSize(level), 6);
    }

    #endregion

    #region Cells

    [Fact]
    public void Create_ProducesCellsWhoseReferencesParse()
    {
        var grid = MgrsGridHelper.Create(Around(51.4, 35.7, 0.5));

        Assert.NotEmpty(grid.Cells);

        foreach (var cell in grid.Cells)
        {
            Assert.True(MgrsConverter.TryGetBoundingBox(cell.Reference, out _),
                $"'{cell.Reference}' is not a reference that resolves");

            Assert.NotNull(cell.Geometry);
        }
    }

    /// <summary>Every cell of a metric grid names a square at that level, so every reference is the same length.</summary>
    [Theory]
    [InlineData(MgrsGridLevel.Km100)]
    [InlineData(MgrsGridLevel.Km10)]
    [InlineData(MgrsGridLevel.Km1)]
    public void Create_AtAFixedLevel_EveryReferenceHasTheSamePrecision(MgrsGridLevel level)
    {
        var grid = MgrsGridHelper.Create(Around(51.4, 35.7, 0.2), level);

        Assert.NotEmpty(grid.Cells);
        Assert.Equal(level, grid.Level);

        var expected = (MgrsPrecision)(int)level;

        foreach (var cell in grid.Cells)
        {
            Assert.True(MgrsConverter.TryParse(cell.Reference, out var parsed), cell.Reference);

            Assert.Equal(expected, parsed.Precision);
        }
    }

    /// <summary>
    /// The seam. Longitude 48 divides zones 38 and 39, and cells on either side must meet there
    /// exactly — neither overlapping across the meridian nor leaving a gap.
    /// </summary>
    [Fact]
    public void Create_AcrossAZoneBoundary_ClipsCellsToTheirOwnZone()
    {
        var grid = MgrsGridHelper.Create(WebMercator(47.5, 35.5, 48.5, 36.0), MgrsGridLevel.Km100);

        Assert.NotEmpty(grid.Cells);

        var west = grid.Cells.Where(c => c.Reference.StartsWith("38", StringComparison.Ordinal)).ToList();
        var east = grid.Cells.Where(c => c.Reference.StartsWith("39", StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(west);
        Assert.NotEmpty(east);

        // no cell of zone 38 reaches past the meridian, and none of zone 39 falls short of it
        foreach (var cell in west)
            Assert.True(GeodeticOf(cell).XMax <= 48.0 + 1e-6, $"{cell.Reference} spills east of the zone boundary");

        foreach (var cell in east)
            Assert.True(GeodeticOf(cell).XMin >= 48.0 - 1e-6, $"{cell.Reference} spills west of the zone boundary");

        // and they actually meet: something on each side touches the seam
        Assert.Contains(west, c => Math.Abs(GeodeticOf(c).XMax - 48.0) < 1e-6);
        Assert.Contains(east, c => Math.Abs(GeodeticOf(c).XMin - 48.0) < 1e-6);
    }

    /// <summary>Grid zone cells are 6° by 8°, and the ones that do not exist are not emitted.</summary>
    [Fact]
    public void Create_AtGridZoneLevel_MatchesTheCellLattice()
    {
        var grid = MgrsGridHelper.Create(WebMercator(-179.0, -79.0, 179.0, 83.0));

        Assert.Equal(MgrsGridLevel.GridZone, grid.Level);

        var references = grid.Cells.Select(c => c.Reference).ToHashSet();

        Assert.Contains("39S", references);
        Assert.Contains("31X", references);   // Svalbard, widened
        Assert.Contains("32V", references);   // Norway, widened

        // 32X, 34X and 36X are not part of the grid
        Assert.DoesNotContain("32X", references);
        Assert.DoesNotContain("34X", references);
        Assert.DoesNotContain("36X", references);
    }

    /// <summary>The widened Norway cell really is nine degrees across, not six.</summary>
    [Fact]
    public void Create_TheNorwayCell_IsWiderThanASixDegreeZone()
    {
        var grid = MgrsGridHelper.Create(WebMercator(0.0, 56.0, 12.0, 64.0), MgrsGridLevel.GridZone);

        var norway = grid.Cells.Single(c => c.Reference == "32V");

        var box = GeodeticOf(norway);

        Assert.Equal(3.0, box.XMin, 6);
        Assert.Equal(12.0, box.XMax, 6);
    }

    [Fact]
    public void Create_SouthernHemisphere_Works()
    {
        var grid = MgrsGridHelper.Create(WebMercator(151.0, -34.0, 151.5, -33.6), MgrsGridLevel.Km10);

        Assert.NotEmpty(grid.Cells);

        // Sydney is zone 56, band H
        Assert.All(grid.Cells, c => Assert.StartsWith("56H", c.Reference, StringComparison.Ordinal));
    }

    /// <summary>An extent straddling the equator has to produce cells from both UTM origins.</summary>
    [Fact]
    public void Create_AcrossTheEquator_ProducesBothHemispheres()
    {
        var grid = MgrsGridHelper.Create(WebMercator(50.8, -0.2, 51.2, 0.2), MgrsGridLevel.Km10);

        Assert.Contains(grid.Cells, c => c.Reference.StartsWith("39N", StringComparison.Ordinal));
        Assert.Contains(grid.Cells, c => c.Reference.StartsWith("39M", StringComparison.Ordinal));
    }

    #endregion

    #region Labelling — what a map would actually print

    /// <summary>
    /// Every visible square is named, at every level: <c>39S</c> for a grid zone cell,
    /// <c>39S WV</c> for a 100 km square. Without it the digits on the lines mean nothing, because
    /// a screen overlay has no map collar to carry the designation.
    /// </summary>
    [Fact]
    public void SquareLabels_AtGridZoneLevel_AreTheDesignator()
    {
        var grid = MgrsGridHelper.Create(Around(51.4, 35.7, 40.0));

        Assert.Equal(MgrsGridLevel.GridZone, grid.Level);

        Assert.Contains(grid.SquareLabels, l => l.Text == "39S");
    }

    [Theory]
    [InlineData(MgrsGridLevel.Km100)]
    [InlineData(MgrsGridLevel.Km10)]
    [InlineData(MgrsGridLevel.Km1)]
    [InlineData(MgrsGridLevel.M100)]
    public void SquareLabels_AtEveryMetricLevel_NameTheHundredKilometreSquare(MgrsGridLevel level)
    {
        var grid = MgrsGridHelper.Create(Around(51.4, 35.7, 0.05), level);

        Assert.NotEmpty(grid.SquareLabels);

        // "39S WV" — a grid zone designator and a 100 km square, nothing finer
        foreach (var label in grid.SquareLabels)
            Assert.True(MgrsConverter.TryParse(label.Text, out var parsed) && parsed.Precision == MgrsPrecision.Km100,
                $"'{label.Text}' is not a 100 km square reference");
    }

    /// <summary>
    /// The square name is placed inside the part of the square that is on screen, not at the
    /// square's own centre. Zoomed inside one 100 km square the centre is tens of kilometres away,
    /// so anchoring to the square would put the only piece of context off the display.
    /// </summary>
    [Fact]
    public void SquareLabels_SitInsideTheView_EvenWhenTheSquareIsLargerThanIt()
    {
        // about two kilometres across, deep inside a single 100 km square
        var view = WebMercator(51.40, 35.70, 51.42, 35.72);

        var grid = MgrsGridHelper.Create(view, MgrsGridLevel.Km1);

        var geodetic = view.Transform(MapProjects.WebMercatorToGeodeticWgs84);

        Assert.NotEmpty(grid.SquareLabels);

        foreach (var label in grid.SquareLabels)
        {
            var point = MapProjects.WebMercatorToGeodeticWgs84(label.Position);

            Assert.InRange(point.X, geodetic.XMin, geodetic.XMax);
            Assert.InRange(point.Y, geodetic.YMin, geodetic.YMax);
        }
    }

    /// <summary>Grid zone and 100 km levels are named by their squares, so they need no line values.</summary>
    [Theory]
    [InlineData(MgrsGridLevel.GridZone)]
    [InlineData(MgrsGridLevel.Km100)]
    public void AxisLabels_AtCoarseLevels_AreNotProduced(MgrsGridLevel level)
    {
        var grid = MgrsGridHelper.Create(Around(51.4, 35.7, 3.0), level);

        Assert.Empty(grid.AxisLabels);
    }

    /// <summary>
    /// A line value is the principal digits of its easting or northing inside the 100 km square,
    /// padded to the level's digit count — one digit at 10 km, two at 1 km, three at 100 m. The
    /// first line met inside each square is spelled out in full instead, so the bare digits have
    /// something to be read against.
    /// </summary>
    [Theory]
    [InlineData(MgrsGridLevel.Km10, 1)]
    [InlineData(MgrsGridLevel.Km1, 2)]
    [InlineData(MgrsGridLevel.M100, 3)]
    public void AxisLabels_AreThePrincipalDigits_ExceptTheFirstInEachSquare(MgrsGridLevel level, int expectedDigits)
    {
        var grid = MgrsGridHelper.Create(Around(51.4, 35.7, 0.04), level);

        Assert.NotEmpty(grid.AxisLabels);

        var spelledOut = 0;

        foreach (var label in grid.AxisLabels)
        {
            if (label.Text.Contains(' '))
            {
                // "39S WV 36" — a square reference, then the same digits the bare labels carry
                spelledOut++;

                var lastSpace = label.Text.LastIndexOf(' ');

                Assert.Equal(expectedDigits, label.Text.Length - lastSpace - 1);
                Assert.True(MgrsConverter.TryParse(label.Text.Substring(0, lastSpace), out _), label.Text);
            }
            else
            {
                Assert.Equal(expectedDigits, label.Text.Length);
                Assert.True(label.Text.All(char.IsDigit), $"'{label.Text}' is not all digits");
            }
        }

        // one per axis per visible square, so at least one and never many
        Assert.InRange(spelledOut, 1, 8);
    }

    /// <summary>
    /// Both axes are labelled, and both sets hug the edge they belong to: eastings along the
    /// bottom, northings up the left. That is where a sheet prints them, and against the edge they
    /// stay put while the map is panned.
    /// </summary>
    [Fact]
    public void AxisLabels_SitAgainstTheBottomAndLeftEdges()
    {
        var view = WebMercator(51.3, 35.6, 51.5, 35.8);

        var grid = MgrsGridHelper.Create(view, MgrsGridLevel.Km1);

        var geodetic = view.Transform(MapProjects.WebMercatorToGeodeticWgs84);

        var eastings = grid.Labels.Where(l => l.Kind == MgrsGridLabelKind.Easting).ToList();
        var northings = grid.Labels.Where(l => l.Kind == MgrsGridLabelKind.Northing).ToList();

        Assert.NotEmpty(eastings);
        Assert.NotEmpty(northings);

        foreach (var label in eastings)
        {
            var point = MapProjects.WebMercatorToGeodeticWgs84(label.Position);

            Assert.InRange(point.Y, geodetic.YMin, geodetic.YMin + geodetic.Height * 0.1);
        }

        foreach (var label in northings)
        {
            var point = MapProjects.WebMercatorToGeodeticWgs84(label.Position);

            Assert.InRange(point.X, geodetic.XMin, geodetic.XMin + geodetic.Width * 0.1);
        }
    }

    /// <summary>
    /// Every line value names a square that is actually on screen. The two sets are not equal:
    /// cells snap <em>outward</em> so they cover the view, while line values snap <em>inward</em>
    /// so they stay visible, which leaves the outermost cell's line off the edge and unlabelled.
    /// The invariant is containment, not equality.
    /// </summary>
    [Fact]
    public void AxisLabels_NameSquaresThatAreOnScreen()
    {
        var grid = MgrsGridHelper.Create(Around(51.4, 35.7, 0.04), MgrsGridLevel.Km1);

        Assert.NotEmpty(grid.AxisLabels);

        var parsed = grid.Cells.Select(c => MgrsConverter.Parse(c.Reference)).ToList();

        var cellEastings = parsed.Select(c => c.Easting.ToString().PadLeft(2, '0')).ToHashSet();
        var cellNorthings = parsed.Select(c => c.Northing.ToString().PadLeft(2, '0')).ToHashSet();

        foreach (var label in grid.Labels.Where(l => l.Kind == MgrsGridLabelKind.Easting))
            Assert.Contains(TrailingDigits(label), cellEastings);

        foreach (var label in grid.Labels.Where(l => l.Kind == MgrsGridLabelKind.Northing))
            Assert.Contains(TrailingDigits(label), cellNorthings);
    }

    /// <summary>The digits of a line value, whether or not it carries a spelled-out prefix.</summary>
    private static string TrailingDigits(MgrsGridLabel label)
    {
        var lastSpace = label.Text.LastIndexOf(' ');

        return lastSpace < 0 ? label.Text : label.Text.Substring(lastSpace + 1);
    }

    #endregion

    #region Limits

    [Fact]
    public void Create_RespectsTheCellCeiling()
    {
        var grid = MgrsGridHelper.Create(Around(51.4, 35.7, 2.0), MgrsGridLevel.Km1, maxCells: 25);

        Assert.True(grid.Cells.Count <= 25, $"produced {grid.Cells.Count} cells past the ceiling");
    }

    /// <summary>Nothing outside 80°S–84°N has an MGRS cell, and a nonsense extent is not an error.</summary>
    [Fact]
    public void Create_OutsideTheGrid_IsEmptyRatherThanThrowing()
    {
        Assert.Empty(MgrsGridHelper.Create(BoundingBox.NaN).Cells);

        Assert.Empty(MgrsGridHelper.Create(WebMercator(50.0, 85.0, 51.0, 89.0)).Cells);
    }

    #endregion
}
