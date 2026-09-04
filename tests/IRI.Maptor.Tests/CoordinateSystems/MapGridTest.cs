using System;
using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Spatial.Helpers.MapGrids;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;

using Xunit;

namespace IRI.Maptor.Tests.CoordinateSystems;

/// <summary>
/// The map-grid engine: which lines cover a view, how fine they are, and what is written on them.
/// </summary>
/// <remarks>
/// Nothing here touches the map. The parts worth pinning before any layer depends on them are the
/// two that are easy to get subtly wrong and hard to see afterwards: the abbreviation rule for the
/// values along the edges, and the cutting of a polyline at a UTM zone seam.
/// </remarks>
public class MapGridTest
{
    #region Helpers

    private static BoundingBox WebMercator(double west, double south, double east, double north)
        => new BoundingBox(west, south, east, north).Transform(MapProjects.GeodeticWgs84ToWebMercator);

    /// <summary>A square-in-degrees view centred on a place.</summary>
    private static BoundingBox Around(double longitude, double latitude, double degrees)
        => WebMercator(longitude - degrees / 2, latitude - degrees / 2, longitude + degrees / 2, latitude + degrees / 2);

    private static Point ToGeodetic(Point webMercator) => MapProjects.WebMercatorToGeodeticWgs84(webMercator);

    private static IEnumerable<Point> GeodeticVertices(MapGridLine line) => line.WebMercatorPoints.Select(ToGeodetic);

    private static List<MapGridLabel> LabelsOn(MapGrid grid, MapGridAxis axis, MapGridSide side)
        => grid.Labels.Where(l => l.Axis == axis && l.Side == side).ToList();

    /// <summary>Tehran, and a view wide enough that the graticule lands on whole degrees.</summary>
    private const double TehranLongitude = 51.4;

    private const double TehranLatitude = 35.7;

    #endregion

    #region Ladders

    /// <summary>
    /// Zooming in never steps the grid back to a coarser interval. The step-back rule inside
    /// <see cref="MapGridLadders.ChooseMajor"/> is the part that could break this, and a grid that
    /// coarsened as you zoomed in would look like a bug in the map rather than in the ladder.
    /// </summary>
    [Fact]
    public void ChooseMajor_IsMonotonicAsTheSpanNarrows()
    {
        foreach (var ladder in new[] { MapGridLadders.Degrees, MapGridLadders.Metres })
        {
            var previous = double.MaxValue;

            for (var span = ladder[0] * 4; span > ladder[ladder.Count - 1] / 2; span /= 1.3)
            {
                var chosen = MapGridLadders.ChooseMajor(span, ladder);

                Assert.True(chosen <= previous, $"span {span} chose {chosen} after {previous}");

                previous = chosen;
            }
        }
    }

    [Fact]
    public void ChooseMajor_KeepsTheLineCountInRangeWhereTheLadderAllows()
    {
        // 111 km of ground: the metric ladder has a 100 km step, so 3-6 lines is reachable.
        var chosen = MapGridLadders.ChooseMajor(111_000, MapGridLadders.Metres);

        var count = 111_000 / chosen;

        Assert.InRange(count, 3, 6);
    }

    [Theory]
    [InlineData(1_000.0, 200.0)]
    [InlineData(2_000.0, 500.0)]
    [InlineData(5_000.0, 1_000.0)]
    [InlineData(10_000.0, 2_000.0)]
    [InlineData(20_000.0, 5_000.0)]
    [InlineData(50_000.0, 10_000.0)]
    [InlineData(100_000.0, 20_000.0)]
    public void MinorOf_SubdividesAMetricIntervalIntoRoundParts(double major, double expected)
        => Assert.Equal(expected, MapGridLadders.MinorOf(major, MapGridLadders.Metres));

    [Theory]
    [InlineData(1.0, 15 / 60.0)]
    [InlineData(2.0, 30 / 60.0)]
    [InlineData(5.0, 1.0)]
    [InlineData(30 / 60.0, 10 / 60.0)]
    [InlineData(10 / 60.0, 2 / 60.0)]
    public void MinorOf_SubdividesADegreeIntervalIntoRoundParts(double major, double expected)
    {
        var minor = MapGridLadders.MinorOf(major, MapGridLadders.Degrees);

        Assert.NotNull(minor);
        Assert.Equal(expected, minor!.Value, 12);
    }

    /// <summary>
    /// A subdivision must itself be a ladder step, so zooming in promotes minor lines to major ones
    /// instead of shifting the whole pattern sideways.
    /// </summary>
    [Fact]
    public void MinorOf_AlwaysReturnsALadderStepThatDividesEvenly()
    {
        foreach (var ladder in new[] { MapGridLadders.Degrees, MapGridLadders.Metres })
        {
            foreach (var major in ladder)
            {
                var minor = MapGridLadders.MinorOf(major, ladder);

                if (minor is null)
                    continue;

                Assert.Contains(ladder, step => step == minor.Value);

                var count = major / minor.Value;

                Assert.InRange(count, 1.999, 5.001);
                Assert.Equal(Math.Round(count), count, 6);
            }
        }
    }

    [Fact]
    public void MinorOf_IsNullAtTheFinestStepOfALadder()
    {
        Assert.Null(MapGridLadders.MinorOf(MapGridLadders.Metres[MapGridLadders.Metres.Count - 1], MapGridLadders.Metres));

        Assert.Null(MapGridLadders.MinorOf(MapGridLadders.Degrees[MapGridLadders.Degrees.Count - 1], MapGridLadders.Degrees));
    }

    #endregion

    #region Label text

    [Fact]
    public void FormatGeodetic_DropsZeroPartsFromTheSpelledOutForm()
    {
        Assert.Equal("51°E", MapGridLabelFormatter.FormatGeodetic(51.0, isLatitude: false, interval: 1.0, full: true));

        Assert.Equal($"51°30{DegreeHelper.minuteSign}E", MapGridLabelFormatter.FormatGeodetic(51.5, isLatitude: false, interval: 30 / 60.0, full: true));

        Assert.Equal($"35°15{DegreeHelper.minuteSign}S", MapGridLabelFormatter.FormatGeodetic(-35.25, isLatitude: true, interval: 15 / 60.0, full: true));

        // The equator and the prime meridian carry no hemisphere letter.
        Assert.Equal("0°", MapGridLabelFormatter.FormatGeodetic(0.0, isLatitude: true, interval: 1.0, full: true));
    }

    [Fact]
    public void FormatGeodetic_AbbreviatesToTheChangingPart()
    {
        Assert.Equal($"30{DegreeHelper.minuteSign}", MapGridLabelFormatter.FormatGeodetic(51.5, isLatitude: false, interval: 30 / 60.0, full: false));

        Assert.Equal($"30{DegreeHelper.secondSign}", MapGridLabelFormatter.FormatGeodetic(51.008333333333333, isLatitude: false, interval: 30 / 3600.0, full: false));

        // At a whole degree there is nothing above the degrees to abbreviate away, so both forms
        // are the same and every label reads in full.
        Assert.Equal(
            MapGridLabelFormatter.FormatGeodetic(51.0, isLatitude: false, interval: 1.0, full: true),
            MapGridLabelFormatter.FormatGeodetic(51.0, isLatitude: false, interval: 1.0, full: false));
    }

    /// <summary>
    /// A metric value is written out in full on every line. The topographic-sheet abbreviation was
    /// built first — <c>⁵34⁰⁰⁰ mE</c> once per edge, then <c>35 36 37</c> — and rejected by the user
    /// on sight: two bare digits with no unit and no anchor is a puzzle, not an abbreviation.
    /// </summary>
    [Theory]
    [InlineData(534_000.0, "534000")]
    [InlineData(535_000.0, "535000")]
    [InlineData(600_000.0, "600000")]
    [InlineData(3_950_000.0, "3950000")]
    [InlineData(534_200.0, "534200")]
    public void FormatMetric_WritesTheWholeNumberOfMetres(double value, string expected)
        => Assert.Equal(expected, MapGridLabelFormatter.FormatMetric(value));

    /// <summary>
    /// Culture never leaks in: no thousands separator, no Persian digits. The digits have to survive
    /// being read off the map and typed back into a coordinate box.
    /// </summary>
    [Fact]
    public void FormatMetric_IsCultureInvariant()
    {
        var original = System.Threading.Thread.CurrentThread.CurrentCulture;

        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("fa-IR");

            Assert.Equal("534000", MapGridLabelFormatter.FormatMetric(534_000));
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = original;
        }
    }

    #endregion

    #region The geodetic graticule

    /// <summary>
    /// The whole point of the feature: lines, not cells. A meridian and a parallel are straight in
    /// Web Mercator, so they are two-vertex polylines and never rings.
    /// </summary>
    [Fact]
    public void Geodetic_LinesAreTwoVertexPolylines()
    {
        var grid = MapGridHelper.Create(Around(TehranLongitude, TehranLatitude, 4), MapGridDefinition.Geodetic());

        Assert.NotEmpty(grid.Lines);

        Assert.All(grid.Lines, line => Assert.Equal(2, line.WebMercatorPoints.Count));
    }

    [Fact]
    public void Geodetic_ChoosesAWholeDegreeForAFourDegreeView()
    {
        var grid = MapGridHelper.Create(Around(TehranLongitude, TehranLatitude, 4), MapGridDefinition.Geodetic());

        Assert.Equal(1.0, grid.MajorInterval, 9);
        Assert.Equal(15 / 60.0, grid.MinorInterval!.Value, 9);
    }

    [Fact]
    public void Geodetic_EveryValueIsAMultipleOfItsInterval()
    {
        var grid = MapGridHelper.Create(Around(TehranLongitude, TehranLatitude, 4), MapGridDefinition.Geodetic());

        foreach (var line in grid.Lines)
        {
            var step = line.Kind == MapGridLineKind.Major ? grid.MajorInterval : grid.MinorInterval!.Value;

            var ratio = line.Value / step;

            Assert.Equal(Math.Round(ratio), ratio, 6);
        }

        // A minor line never lands on a major one, or the map would draw the same line twice.
        var majorValues = grid.MajorLines.Select(l => (l.Axis, Value: Math.Round(l.Value, 9))).ToHashSet();

        Assert.DoesNotContain(grid.MinorLines, l => majorValues.Contains((l.Axis, Math.Round(l.Value, 9))));
    }

    [Fact]
    public void Geodetic_NumbersAllFourEdgesByDefault()
    {
        var grid = MapGridHelper.Create(Around(TehranLongitude, TehranLatitude, 4), MapGridDefinition.Geodetic());

        Assert.NotEmpty(LabelsOn(grid, MapGridAxis.X, MapGridSide.Bottom));
        Assert.NotEmpty(LabelsOn(grid, MapGridAxis.X, MapGridSide.Top));
        Assert.NotEmpty(LabelsOn(grid, MapGridAxis.Y, MapGridSide.Left));
        Assert.NotEmpty(LabelsOn(grid, MapGridAxis.Y, MapGridSide.Right));

        // Every meridian crossing the view is numbered top and bottom, and every parallel left and
        // right — the two rows of a side hold the same values.
        Assert.Equal(
            LabelsOn(grid, MapGridAxis.X, MapGridSide.Bottom).Select(l => l.Value).ToList(),
            LabelsOn(grid, MapGridAxis.X, MapGridSide.Top).Select(l => l.Value).ToList());
    }

    [Fact]
    public void Geodetic_MaskingSidesRemovesExactlyThoseLabels()
    {
        var extent = Around(TehranLongitude, TehranLatitude, 4);

        var definition = MapGridDefinition.Geodetic();
        definition.LabelSides = MapGridSide.Bottom | MapGridSide.Left;

        var grid = MapGridHelper.Create(extent, definition);

        Assert.NotEmpty(grid.Labels);
        Assert.All(grid.Labels, label => Assert.True(label.Side == MapGridSide.Bottom || label.Side == MapGridSide.Left));

        var all = MapGridHelper.Create(extent, MapGridDefinition.Geodetic());

        Assert.Equal(
            all.Labels.Count(l => l.Side == MapGridSide.Bottom || l.Side == MapGridSide.Left),
            grid.Labels.Count);
    }

    /// <summary>
    /// The rule a printed sheet follows: the first value met along an edge is spelled out, and the
    /// ones after it carry only the digits that changed.
    /// </summary>
    [Fact]
    public void Geodetic_SpellsOutTheFirstValueOnEachEdgeAndAbbreviatesTheRest()
    {
        // Half a degree across, so the interval lands below a degree and there is something to
        // abbreviate away.
        var grid = MapGridHelper.Create(Around(TehranLongitude, TehranLatitude, 0.5), MapGridDefinition.Geodetic());

        Assert.True(grid.MajorInterval < 1.0, $"expected a sub-degree interval, got {grid.MajorInterval}");

        foreach (var side in new[] { MapGridSide.Bottom, MapGridSide.Top })
        {
            var labels = LabelsOn(grid, MapGridAxis.X, side);

            Assert.True(labels.Count >= 2, $"{side} carried {labels.Count} labels");

            Assert.True(labels[0].IsFull);
            Assert.Contains("°", labels[0].Text);

            // Everything after it is short, because the whole view sits inside one degree.
            Assert.All(labels.Skip(1), label =>
            {
                Assert.False(label.IsFull);
                Assert.DoesNotContain("°", label.Text);
            });
        }
    }

    /// <summary>
    /// And it starts over when the degree rolls over, so a bare <c>20′</c> is never left without a
    /// degree to belong to.
    /// </summary>
    [Fact]
    public void Geodetic_SpellsTheValueOutAgainWhenTheDegreeChanges()
    {
        // Straddles longitude 51 so the run of minutes crosses into a new degree.
        var grid = MapGridHelper.Create(WebMercator(50.5, 35.2, 51.6, 36.0), MapGridDefinition.Geodetic());

        Assert.True(grid.MajorInterval < 1.0);

        var labels = LabelsOn(grid, MapGridAxis.X, MapGridSide.Bottom);

        Assert.True(labels.Count >= 3);

        var respelled = labels.Skip(1).Where(l => l.IsFull).ToList();

        Assert.NotEmpty(respelled);

        Assert.All(respelled, label => Assert.Equal((long)Math.Floor(label.Value), (long)Math.Floor(label.Value)));

        // Exactly the labels whose degree differs from the previous one are spelled out.
        for (var i = 1; i < labels.Count; i++)
        {
            var changed = Math.Floor(labels[i].Value) != Math.Floor(labels[i - 1].Value);

            Assert.Equal(changed, labels[i].IsFull);
        }
    }

    #endregion

    #region UTM

    [Fact]
    public void Utm_UsesOnlyTheZoneTheViewSitsIn()
    {
        var grid = MapGridHelper.Create(Around(51.4, 35.7, 1), MapGridDefinition.Utm());

        Assert.NotEmpty(grid.Lines);

        Assert.All(grid.Lines, line => Assert.Equal(39, line.Zone));

        Assert.Empty(grid.ZoneSeams);
    }

    /// <summary>
    /// The case the whole UTM scheme exists for. At a zone boundary the grid restarts: each side
    /// is walked in its own plane, its lines are <em>cut</em> at the meridian rather than clamped
    /// to it, and the seam itself is drawn.
    /// </summary>
    [Fact]
    public void Utm_CutsItsLinesAtTheZoneSeam()
    {
        const double seam = 48.0;
        const double tolerance = 1e-6;

        var grid = MapGridHelper.Create(WebMercator(47.5, 34.0, 48.5, 35.0), MapGridDefinition.Utm());

        var gridLines = grid.Lines.Where(l => l.Kind != MapGridLineKind.ZoneSeam).ToList();

        Assert.Contains(gridLines, l => l.Zone == 38);
        Assert.Contains(gridLines, l => l.Zone == 39);

        foreach (var line in gridLines.Where(l => l.Zone == 38))
        {
            Assert.All(GeodeticVertices(line), point => Assert.True(point.X <= seam + tolerance, $"zone 38 vertex at {point.X}"));
        }

        foreach (var line in gridLines.Where(l => l.Zone == 39))
        {
            Assert.All(GeodeticVertices(line), point => Assert.True(point.X >= seam - tolerance, $"zone 39 vertex at {point.X}"));
        }

        var seams = grid.ZoneSeams.ToList();

        Assert.Single(seams);
        Assert.Equal(seam, ToGeodetic(seams[0].WebMercatorPoints[0]).X, 6);
        Assert.Contains(grid.Labels, l => l.Kind == MapGridLineKind.ZoneSeam && l.Text.Contains("38") && l.Text.Contains("39"));
    }

    /// <summary>
    /// Cutting rather than clamping is the whole difference from the MGRS overlay: a clamped
    /// polyline would run along the seam meridian, drawing a line the grid does not have.
    /// </summary>
    [Fact]
    public void Utm_DoesNotLeaveASegmentRunningAlongTheSeam()
    {
        var grid = MapGridHelper.Create(WebMercator(47.5, 34.0, 48.5, 35.0), MapGridDefinition.Utm());

        foreach (var line in grid.Lines.Where(l => l.Axis == MapGridAxis.Y && l.Kind != MapGridLineKind.ZoneSeam))
        {
            var vertices = GeodeticVertices(line).ToList();

            // A parallel-ish line that had been clamped would show two or more consecutive
            // vertices sharing the seam longitude.
            var consecutiveAtSeam = 0;

            foreach (var vertex in vertices)
            {
                consecutiveAtSeam = Math.Abs(vertex.X - 48.0) < 1e-7 ? consecutiveAtSeam + 1 : 0;

                Assert.True(consecutiveAtSeam < 2, "a line was clamped to the seam instead of cut at it");
            }
        }
    }

    [Fact]
    public void Utm_LinesAreSampledPolylinesBecauseTheyBowInWebMercator()
    {
        var grid = MapGridHelper.Create(Around(51.4, 35.7, 1), MapGridDefinition.Utm());

        var line = grid.MajorLines.First(l => l.Axis == MapGridAxis.X);

        Assert.True(line.WebMercatorPoints.Count > 2, "a UTM line must be sampled, not drawn corner to corner");
    }

    [Fact]
    public void Utm_SouthOfTheEquatorUsesTheSouthernFalseNorthing()
    {
        var grid = MapGridHelper.Create(Around(51.4, -35.0, 1), MapGridDefinition.Utm());

        var northings = grid.Lines.Where(l => l.Axis == MapGridAxis.Y).Select(l => l.Value).ToList();

        Assert.NotEmpty(northings);

        // Northings count down from 10 000 000 m at the equator in the southern hemisphere.
        Assert.All(northings, value => Assert.InRange(value, 5_000_000, 10_000_000));
    }

    [Fact]
    public void Utm_IntervalFollowsTheZoomAndNeverCoarsens()
    {
        var previous = double.MaxValue;

        for (var degrees = 3.0; degrees > 0.002; degrees /= 1.5)
        {
            var interval = MapGridHelper.ChooseMajorInterval(Around(51.4, 35.7, degrees), MapGridDefinition.Utm());

            Assert.True(interval <= previous, $"a {degrees}° view chose {interval} after {previous}");

            previous = interval;
        }
    }

    [Fact]
    public void Utm_NumbersItsLinesInMetresOnAllFourEdges()
    {
        var grid = MapGridHelper.Create(Around(51.4, 35.7, 1), MapGridDefinition.Utm());

        foreach (var (axis, side) in new[]
        {
            (MapGridAxis.X, MapGridSide.Bottom),
            (MapGridAxis.X, MapGridSide.Top),
            (MapGridAxis.Y, MapGridSide.Left),
            (MapGridAxis.Y, MapGridSide.Right),
        })
        {
            var labels = LabelsOn(grid, axis, side);

            Assert.NotEmpty(labels);

            // Every value in full, on every line: no unit, no abbreviation, and the same form
            // whichever edge it is written against.
            Assert.All(labels, label => Assert.Equal(MapGridLabelFormatter.FormatMetric(label.Value), label.Text));

            Assert.All(labels, label => Assert.DoesNotContain("m", label.Text));
        }
    }

    #endregion

    #region Projected

    [Fact]
    public void Projected_WebMercatorLinesAreStraight()
    {
        var definition = MapGridDefinition.Projected(SrsBases.WebMercator, "webMercator");

        var grid = MapGridHelper.Create(Around(51.4, 35.7, 2), definition);

        Assert.NotEmpty(grid.Lines);

        foreach (var line in grid.Lines)
        {
            var values = line.Axis == MapGridAxis.X
                ? line.WebMercatorPoints.Select(p => p.X)
                : line.WebMercatorPoints.Select(p => p.Y);

            var list = values.ToList();

            Assert.True(list.Max() - list.Min() < 1e-3, "a Web Mercator grid line must be straight on a Web Mercator map");
        }
    }

    /// <summary>
    /// And a conic one is not. A Lambert parallel bows across the map, which is exactly why the
    /// engine samples lines instead of drawing them end to end.
    /// </summary>
    [Fact]
    public void Projected_LambertLinesBowAcrossTheMap()
    {
        var definition = MapGridDefinition.Projected(SrsBases.LccNiocWithWgs84, "lccNioc");

        var grid = MapGridHelper.Create(WebMercator(50.0, 33.0, 54.0, 36.0), definition);

        var line = grid.MajorLines.First(l => l.Axis == MapGridAxis.Y);

        var ys = line.WebMercatorPoints.Select(p => p.Y).ToList();

        Assert.True(ys.Max() - ys.Min() > 100, "a Lambert parallel should not come out as a straight Web Mercator line");
    }

    /// <summary>
    /// A sampled vertex lands on its line to within a few micrometres — double-precision noise
    /// through the projection chain, and nothing else.
    /// </summary>
    /// <remarks>
    /// Worth pinning because it localizes every visible deviation to one place. Neither conversion
    /// the engine leans on loses anything measurable (a 4° box around Iran round-trips through the
    /// Lambert grid and through Web Mercator with zero error at double precision), so what the next
    /// test measures is the cutting, not the projecting. Getting here also took fixing the clipper:
    /// it used to hand back an untouched segment end as <c>a + 1·(b - a)</c>, which is a rounding
    /// error away from <c>b</c> and nudged every vertex in the grid by about a tenth of a
    /// millimetre.
    /// </remarks>
    [Fact]
    public void Projected_SampledVerticesSitExactlyOnTheirLine()
    {
        var srs = SrsBases.LccNiocWithWgs84;

        var grid = MapGridHelper.Create(WebMercator(50.0, 33.0, 54.0, 36.0), MapGridDefinition.Projected(srs, "lccNioc"));

        Assert.NotEmpty(grid.Lines);

        var checkedAny = false;

        foreach (var line in grid.Lines)
        {
            // The ends of a run may be cut points rather than samples; everything between them is
            // a sample.
            for (var i = 1; i < line.WebMercatorPoints.Count - 1; i++)
            {
                checkedAny = true;

                var deviation = DeviationFromLine(srs, line, line.WebMercatorPoints[i]);

                Assert.True(deviation < 1e-3, $"a sampled vertex is {deviation:E3} m off its line");
            }
        }

        Assert.True(checkedAny);
    }

    /// <summary>
    /// Where a line is <em>cut</em> at the edge of the view the crossing is interpolated along a
    /// sampled chord, so the cut point sits a little off the true curve. This pins how little.
    /// </summary>
    /// <remarks>
    /// The error is worst on the widest view — a 4° box gives about 2 m — and it falls away
    /// quadratically as the samples shorten, while a pixel only halves. So the deviation measured
    /// in pixels is largest where a pixel is hundreds of metres wide, and there it is a twentieth
    /// of one. This is what makes 32 samples a line enough, and it is the reason the engine cuts
    /// polylines instead of snapping their ends back onto the curve.
    /// </remarks>
    [Fact]
    public void Projected_CutPointsStayFarBelowAPixel()
    {
        var srs = SrsBases.LccNiocWithWgs84;

        const double assumedWindowPixels = 1000.0;

        foreach (var degrees in new[] { 4.0, 1.0, 0.25, 0.05 })
        {
            var grid = MapGridHelper.Create(Around(52.0, 34.5, degrees), MapGridDefinition.Projected(srs, "lccNioc"));

            var worst = 0.0;

            foreach (var line in grid.Lines)
            {
                foreach (var vertex in line.WebMercatorPoints)
                {
                    worst = Math.Max(worst, DeviationFromLine(srs, line, vertex));
                }
            }

            var metresPerPixel = degrees * 110_574.0 / assumedWindowPixels;

            Assert.True(
                worst < metresPerPixel * 0.05,
                $"a {degrees}° view deviates {worst:N3} m, which is {worst / metresPerPixel:N3} px");
        }
    }

    private static double DeviationFromLine(SrsBase srs, MapGridLine line, Point webMercatorVertex)
    {
        var projected = srs.FromWgs84Geodetic(ToGeodetic(webMercatorVertex));

        var actual = line.Axis == MapGridAxis.X ? projected.X : projected.Y;

        return Math.Abs(actual - line.Value);
    }

    /// <summary>
    /// A geographic system has no metres in it, so a "projected" grid over one would draw a grid of
    /// degrees while every label called them metres. Rejected at construction rather than silently.
    /// </summary>
    [Fact]
    public void Projected_RejectsASystemThatDoesNotProject()
        => Assert.Throws<ArgumentException>(() => MapGridDefinition.Projected(SrsBases.GeodeticWgs84, "wgs84"));

    #endregion

    #region Crowding

    /// <summary>
    /// No two values are written on top of each other. A fixed interval far finer than the view
    /// wants is the blunt way to force the collision; the real ones are a UTM zone seam and the
    /// corners, and they are the same code path.
    /// </summary>
    [Fact]
    public void Labels_NeverPrintOnTopOfEachOther()
    {
        var options = MapGridOptions.Default;

        foreach (var definition in new[] { MapGridDefinition.Geodetic(), MapGridDefinition.Utm() })
        {
            definition.MajorInterval = definition.IsAngular ? 1 / 60.0 : 500;

            var view = WebMercator(51.0, 35.5, 51.4, 35.9);

            var grid = MapGridHelper.Create(view, definition, options);

            var geodetic = MapGridHelper.ToClippedGeodetic(view);

            var minX = geodetic.Width * options.MinLabelSeparationX;
            var minY = geodetic.Height * options.MinLabelSeparationY;

            var placed = grid.Labels.Select(l => ToGeodetic(l.Position)).ToList();

            Assert.NotEmpty(placed);

            for (var i = 0; i < placed.Count; i++)
            {
                for (var j = i + 1; j < placed.Count; j++)
                {
                    var overlaps = Math.Abs(placed[i].X - placed[j].X) < minX
                                && Math.Abs(placed[i].Y - placed[j].Y) < minY;

                    Assert.False(overlaps, $"{definition.Key}: two values overlap near {placed[i].X:N4}, {placed[i].Y:N4}");
                }
            }
        }
    }

    /// <summary>
    /// Suppressing a crowded value must not consume the spelled-out slot: whichever value actually
    /// survives on an edge is the one that carries the full reference. Otherwise a graticule margin
    /// could end up showing nothing but bare minutes.
    /// </summary>
    /// <remarks>
    /// Geodetic, because that is the only family that still abbreviates — a metric value is written
    /// out in full on every line, so it has no slot to consume.
    /// </remarks>
    [Fact]
    public void TheFirstValueThatSurvivesOnAnEdgeIsStillSpelledOut()
    {
        var definition = MapGridDefinition.Geodetic();

        definition.MajorInterval = 1 / 60.0;

        var grid = MapGridHelper.Create(WebMercator(51.0, 35.5, 51.4, 35.9), definition);

        foreach (var (axis, side) in new[]
        {
            (MapGridAxis.X, MapGridSide.Bottom),
            (MapGridAxis.X, MapGridSide.Top),
            (MapGridAxis.Y, MapGridSide.Left),
            (MapGridAxis.Y, MapGridSide.Right),
        })
        {
            var labels = LabelsOn(grid, axis, side);

            if (labels.Count == 0)
                continue;

            Assert.True(labels[0].IsFull, $"the first surviving value on {side} was abbreviated");
            Assert.Contains("°", labels[0].Text);
        }
    }

    /// <summary>
    /// Where the margin is crowded it is a grid value that gives way, never the caption naming the
    /// two zones — the one label on the map a reader cannot work out from the others.
    /// </summary>
    [Fact]
    public void TheZoneSeamCaptionSurvivesACrowdedMargin()
    {
        var definition = MapGridDefinition.Utm();

        // Fine enough that eastings crowd each other right across the seam.
        definition.MajorInterval = 1_000;

        var grid = MapGridHelper.Create(WebMercator(47.8, 34.0, 48.2, 34.4), definition);

        Assert.Single(grid.ZoneSeams);

        var seamLabels = grid.Labels.Where(l => l.Kind == MapGridLineKind.ZoneSeam).ToList();

        Assert.NotEmpty(seamLabels);
        Assert.All(seamLabels, label => Assert.Equal("38 | 39", label.Text));
    }

    /// <summary>Crowding never removes the lines themselves — only the numbers on them.</summary>
    [Fact]
    public void SuppressingValuesLeavesEveryLineDrawn()
    {
        var definition = MapGridDefinition.Geodetic();

        definition.MajorInterval = 1 / 60.0;

        var grid = MapGridHelper.Create(WebMercator(51.0, 35.5, 51.4, 35.9), definition);

        var majorCount = grid.MajorLines.Count();

        // A 0.4° view at one-arc-minute spacing carries about 24 lines on each axis, and every one
        // of them is drawn.
        Assert.True(majorCount >= 40, $"only {majorCount} lines were drawn");

        // With all four edges on, an uncrowded grid would write two values per line. Fewer means
        // the margin was thinned — which is the point of the test.
        Assert.True(grid.Labels.Count < majorCount * 2, "this view should crowd its margin, or the test proves nothing");
    }

    #endregion

    #region Data sources

    /// <summary>
    /// The layer's lines arrive as <strong>LineString</strong> features. This is the difference
    /// from the MGRS overlay that the whole feature turns on: an MGRS square is a named region and
    /// is properly a polygon, a grid line is a line.
    /// </summary>
    [Fact]
    public void DataSource_ProducesPolylineFeaturesNeverPolygons()
    {
        var source = MapGridDataSource.Create(MapGridDefinition.Geodetic());

        var featureSet = source.GetAsFeatureSetAsync(Around(TehranLongitude, TehranLatitude, 4)).Result;

        Assert.NotEmpty(featureSet.Features);

        Assert.Equal(Core.Common.Enums.GeometryType.LineString, source.GeometryType);

        var lines = featureSet.Features
            .Where(f => (string)f.Attributes[MapGridDataSource.KindFieldName] != MapGridDataSource.LabelKind)
            .ToList();

        Assert.NotEmpty(lines);

        Assert.All(lines, feature =>
        {
            Assert.Equal(Core.Common.Enums.GeometryType.LineString, feature.TheGeometry.Type);

            // A weight to filter on, so major / minor / seam can be three symbolizers on one layer.
            Assert.True(feature.Attributes.ContainsKey(MapGridDataSource.KindFieldName));
        });

        Assert.Contains(featureSet.Features, f => (string)f.Attributes[MapGridDataSource.KindFieldName] == nameof(MapGridLineKind.Major));
    }

    /// <summary>
    /// The values ride in the same feature set as the lines, told apart by the <c>Kind</c> attribute
    /// every symbolizer filters on. One source, therefore one layer, therefore one legend row — and,
    /// critically, a layer that can be removed by identity.
    /// </summary>
    [Fact]
    public void DataSource_AlsoCarriesTheValuesAsPointFeatures()
    {
        var source = MapGridDataSource.Create(MapGridDefinition.Utm());

        var featureSet = source.GetAsFeatureSetAsync(Around(TehranLongitude, TehranLatitude, 1)).Result;

        var labels = featureSet.Features
            .Where(f => (string)f.Attributes[MapGridDataSource.KindFieldName] == MapGridDataSource.LabelKind)
            .ToList();

        Assert.NotEmpty(labels);

        Assert.All(labels, feature =>
        {
            Assert.Equal(Core.Common.Enums.GeometryType.Point, feature.TheGeometry.Type);
            Assert.Equal(MapGridDataSource.LabelFieldName, feature.LabelAttribute);
            Assert.False(string.IsNullOrEmpty((string)feature.Attributes[MapGridDataSource.LabelFieldName]));
        });

        var majorCount = featureSet.Features.Count(f => (string)f.Attributes[MapGridDataSource.KindFieldName] == nameof(MapGridLineKind.Major));

        // Four edges, so a principal line is numbered at most twice; never more values than that.
        Assert.True(labels.Count <= majorCount * 2 + 8, $"{labels.Count} values for {majorCount} principal lines");

        // A line never carries text, so the line symbolizers and the label symbolizer never overlap.
        Assert.All(
            featureSet.Features.Where(f => (string)f.Attributes[MapGridDataSource.KindFieldName] != MapGridDataSource.LabelKind),
            feature => Assert.Equal(Core.Common.Enums.GeometryType.LineString, feature.TheGeometry.Type));
    }

    [Fact]
    public void DataSource_RejectsANullDefinition()
        => Assert.Throws<ArgumentNullException>(() => new MapGridDataSource(null!));

    #endregion

    #region Guards and ceilings

    [Fact]
    public void Create_RejectsANullDefinition()
        => Assert.Throws<ArgumentNullException>(() => MapGridHelper.Create(Around(51.4, 35.7, 1), null!));

    /// <summary>
    /// A layer asks for a grid on every pan, including while the map is still settling, so a
    /// nonsense extent must come back empty rather than throw.
    /// </summary>
    [Theory]
    [InlineData(MapGridKind.Geodetic)]
    [InlineData(MapGridKind.Utm)]
    public void Create_ReturnsEmptyForAnUnusableExtent(MapGridKind kind)
    {
        var definition = kind == MapGridKind.Geodetic ? MapGridDefinition.Geodetic() : MapGridDefinition.Utm();

        Assert.Empty(MapGridHelper.Create(BoundingBox.NaN, definition).Lines);

        // Degenerate: zero width and height.
        Assert.Empty(MapGridHelper.Create(new BoundingBox(0, 0, 0, 0), definition).Lines);
    }

    [Theory]
    [InlineData(MapGridKind.Geodetic)]
    [InlineData(MapGridKind.Utm)]
    public void Create_SurvivesWorldAndPolarExtents(MapGridKind kind)
    {
        var definition = kind == MapGridKind.Geodetic ? MapGridDefinition.Geodetic() : MapGridDefinition.Utm();

        foreach (var extent in new[]
        {
            WebMercator(-179.9, -85.0, 179.9, 85.0),
            WebMercator(-10, 84.0, 10, 85.0),
            WebMercator(-10, -85.0, 10, -84.0),
        })
        {
            var grid = MapGridHelper.Create(extent, definition);

            Assert.All(grid.Lines, line => Assert.All(line.WebMercatorPoints, point =>
            {
                Assert.False(double.IsNaN(point.X) || double.IsNaN(point.Y));
                Assert.False(double.IsInfinity(point.X) || double.IsInfinity(point.Y));
            }));
        }
    }

    /// <summary>A fixed interval far too fine for the view truncates rather than trying to build millions of lines.</summary>
    [Fact]
    public void Create_TruncatesRatherThanExploding()
    {
        var definition = MapGridDefinition.Utm();
        definition.MajorInterval = 10;

        var options = MapGridOptions.Default;

        var grid = MapGridHelper.Create(Around(51.4, 35.7, 1), definition, options);

        Assert.True(grid.Lines.Count <= options.MaxLines);
        Assert.True(grid.Labels.Count <= options.MaxLabels);
    }

    [Fact]
    public void Create_HonoursAFixedInterval()
    {
        var definition = MapGridDefinition.Utm();
        definition.MajorInterval = 10_000;

        var grid = MapGridHelper.Create(Around(51.4, 35.7, 1), definition);

        Assert.Equal(10_000, grid.MajorInterval);
        Assert.NotEmpty(grid.Lines);
        Assert.All(grid.MajorLines, line => Assert.Equal(0, Math.Round(line.Value % 10_000, 3)));
    }

    /// <summary>
    /// The cache is keyed on the definition's mutable parts too, because a caller edits one
    /// definition instance in place when the user changes the interval.
    /// </summary>
    [Fact]
    public void Create_CacheNoticesAnIntervalChangeOnTheSameDefinition()
    {
        var extent = Around(51.4, 35.7, 1);

        var definition = MapGridDefinition.Utm();

        var first = MapGridHelper.Create(extent, definition);

        Assert.Same(first, MapGridHelper.Create(extent, definition));

        definition.MajorInterval = 5_000;

        var second = MapGridHelper.Create(extent, definition);

        Assert.NotSame(first, second);
        Assert.Equal(5_000, second.MajorInterval);
    }

    #endregion
}
