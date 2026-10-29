using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Satellite;
using IRI.Maptor.Core.Satellite.Passes;
using IRI.Maptor.Core.Satellite.Propagation;
using IRI.Maptor.Core.Satellite.Tracks;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Helpers.MapGrids;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Core.Persistence.DataSources;

/// <summary>
/// Satellites at an instant, as Web Mercator features: the point beneath each one and, for the
/// selected one, its ground track, the area it can see and its imaging swath.
/// </summary>
/// <remarks>
/// <para>
/// Built like <see cref="DayNightDataSource"/>: computed with SGP4 on every request, never cached,
/// and told apart by <see cref="KindFieldName"/> so it is one layer. Set <see cref="Time"/> and redraw
/// the layer to move everything.
/// </para>
/// <para>
/// Features come out in drawing order: swath, footprint, region, past track, future track, the
/// highlighted pass, then the other satellites and the selected one on top.
/// </para>
/// </remarks>
public class SatellitesDataSource : VectorDataSource
{
    public const string KindFieldName = "Kind";

    /// <summary>The satellite's name, or its catalog number; the label of the position points.</summary>
    public const string NameFieldName = "Name";

    public const string CatalogNumberFieldName = "CatalogNumber";

    public const string SelectedKind = "Selected";

    public const string OtherKind = "Other";

    public const string PastTrackKind = "PastTrack";

    public const string FutureTrackKind = "FutureTrack";

    public const string FootprintKind = "Footprint";

    public const string SwathKind = "Swath";

    /// <summary>The region passes are found for: a polygon, or a point.</summary>
    public const string RegionKind = "Region";

    /// <summary>The part of the track flown during <see cref="HighlightedPass"/>.</summary>
    public const string PassTrackKind = "PassTrack";

    private static readonly List<Field> _fields = new()
    {
        new() { IsNullable = false, Name = KindFieldName, Length = 0, TypeFullName = typeof(string).FullName },
        new() { IsNullable = false, Name = NameFieldName, Length = 0, TypeFullName = typeof(string).FullName },
        new() { IsNullable = false, Name = CatalogNumberFieldName, Length = 0, TypeFullName = typeof(int).FullName },
    };

    public SatellitesDataSource() : base(_fields)
    {
    }

    /// <summary>The satellites drawn; replace the list to change them.</summary>
    public IReadOnlyList<Sgp4Propagator> Satellites { get; set; } = Array.Empty<Sgp4Propagator>();

    /// <summary>The satellite whose track, footprint and swath are drawn; null for none.</summary>
    public Sgp4Propagator? Selected { get; set; }

    private DateTime _time = DateTime.UtcNow;

    /// <summary>The instant drawn, UTC.</summary>
    public DateTime Time
    {
        get => _time;
        set => _time = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public bool ShowTrack { get; set; } = true;

    /// <summary>Orbits of track drawn behind the satellite.</summary>
    public double PastOrbits { get; set; } = 1;

    /// <summary>Orbits of track drawn ahead of the satellite.</summary>
    public double FutureOrbits { get; set; } = 2;

    public bool ShowFootprint { get; set; } = true;

    /// <summary>Elevation above the horizon that counts as visible, degrees.</summary>
    public double MinimumElevation { get; set; } = Footprint.DefaultMinimumElevation;

    /// <summary>Draw the imaging swath along the track, for satellites in <see cref="Sensors"/>.</summary>
    public bool ShowSwath { get; set; } = true;

    public SensorCatalog Sensors { get; set; } = SensorCatalog.CreateDefault();

    /// <summary>The region passes are found for, drawn as an outline (or a point); null for none.</summary>
    public PassRegion? Region { get; set; }

    /// <summary>A pass to show: the track flown during it is drawn bold. Its satellite need not be selected.</summary>
    public SatellitePass? HighlightedPass { get; set; }

    /// <summary>Which satellites are named on the map.</summary>
    public SatelliteLabelMode LabelMode { get; set; } = SatelliteLabelMode.Auto;

    /// <summary>With <see cref="SatelliteLabelMode.Auto"/>, every satellite is named up to this many; above it only the selected one.</summary>
    public int AutoLabelLimit { get; set; } = 50;

    /// <summary>Pixels between a click and a satellite's point that still count as a hit.</summary>
    public const double DefaultHitTolerance = 10;

    public override string SourceAddress => "Satellites (SGP4)";

    public override int Srid => SridHelper.WebMercator;

    public override BoundingBox WebMercatorExtent
    {
        get => new BoundingBox(-180.0, -MapGridHelper.MaxWebMercatorLatitude, 180.0, MapGridHelper.MaxWebMercatorLatitude)
            .Transform(MapProjects.GeodeticWgs84ToWebMercator);
        protected set => _ = value;
    }

    public override GeometryType? GeometryType
    {
        get => Common.Enums.GeometryType.Point;
        protected set => _ = value;
    }

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundingBox) => Task.FromResult(Build());

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry) => Task.FromResult(Build());

    /// <summary>Nothing to search: the features are computed.</summary>
    public override Task<FeatureSet<Point>> SearchAsync(string searchText) => throw new NotImplementedException();

    public List<Feature<Point>> GetFeatures()
    {
        var features = new List<Feature<Point>>();

        // the tracks are computed with the swath, but drawn above the region
        var tracks = Selected is not null ? AddSelectedOverlays(features, Selected) : default;

        if (Region is not null)
            AddRegion(features, Region);

        if (Selected is not null && ShowTrack)
        {
            AddTrack(features, tracks.Past, PastTrackKind, Selected);
            AddTrack(features, tracks.Future, FutureTrackKind, Selected);
        }

        if (HighlightedPass is SatellitePass pass)
            AddPassTrack(features, pass);

        foreach (var satellite in Satellites)
            if (satellite != Selected)
                AddPosition(features, satellite, OtherKind);

        if (Selected is not null)
            AddPosition(features, Selected, SelectedKind);

        return features;
    }

    /// <summary>
    /// The satellite drawn nearest to <paramref name="screenPoint"/>, within <paramref name="tolerance"/>;
    /// null if there is none. Measured on the screen, so a click feels the same at every zoom.
    /// </summary>
    /// <param name="screenPoint">The click, in screen units.</param>
    /// <param name="geographicToScreen">Longitude/latitude (X/Y) to the same screen units.</param>
    /// <param name="tolerance">Largest distance that still counts, in screen units.</param>
    /// <remarks>
    /// Each point is also tried a world to the west and to the east, so a map that repeats the world
    /// finds a satellite on the far side of ±180°. Satellites that have decayed, or that are beyond the
    /// ±85.05° Web Mercator edge (and so not drawn), are never hit.
    /// </remarks>
    public Sgp4Propagator? HitTest(Point screenPoint, Func<Point, Point> geographicToScreen, double tolerance = DefaultHitTolerance)
    {
        if (geographicToScreen is null)
            throw new ArgumentNullException(nameof(geographicToScreen));

        Sgp4Propagator? nearest = null;

        var best = tolerance * tolerance;

        foreach (var satellite in Satellites)
        {
            var position = satellite.GetPosition(Time);

            if (position is null || Math.Abs(position.SubSatellitePoint.Y) > MapGridHelper.MaxWebMercatorLatitude)
                continue;

            foreach (var shift in new[] { 0.0, -360.0, 360.0 })
            {
                var screen = geographicToScreen(new Point(position.SubSatellitePoint.X + shift, position.SubSatellitePoint.Y));

                var dx = screen.X - screenPoint.X;
                var dy = screen.Y - screenPoint.Y;
                var distance = dx * dx + dy * dy;

                if (distance <= best)
                {
                    best = distance;
                    nearest = satellite;
                }
            }
        }

        return nearest;
    }

    private bool IsLabeled(string kind) => LabelMode switch
    {
        SatelliteLabelMode.All => true,
        SatelliteLabelMode.SelectedOnly => kind == SelectedKind,
        _ => kind == SelectedKind || Satellites.Count <= AutoLabelLimit,
    };

    private FeatureSet<Point> Build() => FeatureSet<Point>.Create(string.Empty, GetFeatures());

    /// <summary>Adds the swath and footprint; returns the past and future track for drawing later.</summary>
    private (GroundTrack? Past, GroundTrack? Future) AddSelectedOverlays(List<Feature<Point>> features, Sgp4Propagator satellite)
    {
        var period = TimeSpan.FromMinutes(satellite.PeriodMinutes);

        var needTrack = ShowTrack || ShowSwath;

        var past = needTrack && PastOrbits > 0 ? GroundTrack.Compute(satellite, Time - Scale(period, PastOrbits), Time) : null;

        var future = needTrack && FutureOrbits > 0 ? GroundTrack.Compute(satellite, Time, Time + Scale(period, FutureOrbits)) : null;

        if (ShowSwath && Sensors.TryGet(satellite.Elements.CatalogNumberValue, out var sensor))
        {
            foreach (var track in new[] { past, future })
                if (track is not null)
                    foreach (var ring in Swath.Compute(track, sensor.SwathWidthKm))
                        AddPolygon(features, ring, SwathKind, satellite);
        }

        if (ShowFootprint && satellite.GetPosition(Time) is SatellitePosition now)
        {
            var footprint = Footprint.Of(now, MinimumElevation);

            foreach (var ring in SphericalCapHelper.ToGeographicRings(footprint.Center, footprint.AngularRadius, MapGridHelper.MaxWebMercatorLatitude))
                AddPolygon(features, ring, FootprintKind, satellite);
        }

        return (past, future);
    }

    private void AddRegion(List<Feature<Point>> features, PassRegion region)
    {
        if (region.IsPoint)
        {
            var point = Geometry<Point>.Create(new List<Point> { ToWebMercator(region.Center) }, Common.Enums.GeometryType.Point, SridHelper.WebMercator);

            features.Add(CreateFeature(features.Count, point, RegionKind, null, label: false));

            return;
        }

        var ring = region.Vertices.Select(ToWebMercator).ToList();

        features.Add(CreateFeature(features.Count, Geometry<Point>.Create(ring, Common.Enums.GeometryType.Polygon, SridHelper.WebMercator), RegionKind, null, label: false));
    }

    private static void AddPassTrack(List<Feature<Point>> features, SatellitePass pass)
    {
        if (pass.End <= pass.Start)
            return;

        // a fine step: a pass over a point can last well under a minute
        var step = TimeSpan.FromTicks(Math.Max(TimeSpan.FromSeconds(1).Ticks, (pass.End - pass.Start).Ticks / 60));

        var track = GroundTrack.Compute(pass.Satellite, pass.Start, pass.End, step);

        AddTrack(features, track, PassTrackKind, pass.Satellite);
    }

    private static TimeSpan Scale(TimeSpan span, double factor) => TimeSpan.FromTicks((long)(span.Ticks * factor));

    private static void AddTrack(List<Feature<Point>> features, GroundTrack? track, string kind, Sgp4Propagator satellite)
    {
        if (track is null)
            return;

        foreach (var segment in track.Segments)
        {
            if (segment.Count < 2)
                continue;

            var points = segment.Select(p => ToWebMercator(p.Location)).ToList();

            features.Add(CreateFeature(features.Count, Geometry<Point>.Create(points, Common.Enums.GeometryType.LineString, SridHelper.WebMercator), kind, satellite, label: false));
        }
    }

    private static void AddPolygon(List<Feature<Point>> features, List<Point> ring, string kind, Sgp4Propagator satellite)
    {
        if (ring.Count < 3)
            return;

        var points = ring.Select(ToWebMercator).ToList();

        features.Add(CreateFeature(features.Count, Geometry<Point>.Create(points, Common.Enums.GeometryType.Polygon, SridHelper.WebMercator), kind, satellite, label: false));
    }

    private void AddPosition(List<Feature<Point>> features, Sgp4Propagator satellite, string kind)
    {
        var position = satellite.GetPosition(Time);

        // decayed, or beyond what Web Mercator shows
        if (position is null || Math.Abs(position.SubSatellitePoint.Y) > MapGridHelper.MaxWebMercatorLatitude)
            return;

        var point = Geometry<Point>.Create(new List<Point> { ToWebMercator(position.SubSatellitePoint) }, Common.Enums.GeometryType.Point, SridHelper.WebMercator);

        features.Add(CreateFeature(features.Count, point, kind, satellite, label: IsLabeled(kind)));
    }

    private static Point ToWebMercator(Point geographic)
    {
        // Web Mercator has no poles: keep strips that reach past ±85.05° on the map's edge
        var latitude = Math.Max(-MapGridHelper.MaxWebMercatorLatitude, Math.Min(MapGridHelper.MaxWebMercatorLatitude, geographic.Y));

        return MapProjects.GeodeticWgs84ToWebMercator(new Point(geographic.X, latitude));
    }

    private static Feature<Point> CreateFeature(int id, Geometry<Point> geometry, string kind, Sgp4Propagator? satellite, bool label)
    {
        var feature = new Feature<Point>
        {
            Id = id,
            TheGeometry = geometry,
            Attributes = new Dictionary<string, object>
            {
                { KindFieldName, kind },
                { NameFieldName, satellite is null ? string.Empty : satellite.Elements.Name ?? satellite.Elements.CatalogNumber },
                { CatalogNumberFieldName, satellite?.Elements.CatalogNumberValue ?? 0 },
            },
        };

        if (label)
            feature.LabelAttribute = NameFieldName;

        return feature;
    }
}

/// <summary>Which satellites <see cref="SatellitesDataSource"/> names on the map.</summary>
public enum SatelliteLabelMode
{
    /// <summary>Every satellite up to <see cref="SatellitesDataSource.AutoLabelLimit"/>, above that only the selected one.</summary>
    Auto,

    All,

    SelectedOnly,
}
