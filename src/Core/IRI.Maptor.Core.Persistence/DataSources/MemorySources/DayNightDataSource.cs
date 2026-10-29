using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Core.Astronomy.Solar;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Helpers.MapGrids;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Core.Persistence.DataSources;

/// <summary>
/// The parts of the Earth where the Sun is below the horizon and below each twilight limit, at one
/// instant, as Web Mercator polygons.
/// </summary>
/// <remarks>
/// <para>
/// Each band is the spherical cap around the anti-solar point where the Sun's elevation is below its
/// limit, so the caps nest: night lies inside astronomical twilight, inside nautical, inside civil.
/// They are deliberately <strong>stacked, not cut into rings</strong>. Drawn with a translucent fill,
/// every cap a place lies in darkens it once more, and the map shades smoothly from the terminator to
/// deep night without any polygon holes, which would break wherever a cap is split at the antimeridian.
/// </para>
/// <para>
/// Built like <see cref="MapGridDataSource"/>: generated per request, never cached, told apart by
/// <see cref="KindFieldName"/>, so the whole overlay is one layer. Set <see cref="Time"/> and redraw
/// the layer to move it.
/// </para>
/// </remarks>
public class DayNightDataSource : VectorDataSource
{
    /// <summary>The <see cref="DayNightBand"/> a polygon belongs to.</summary>
    public const string KindFieldName = "Kind";

    /// <summary>The band's limit: the Sun's elevation, in degrees, is below this inside the polygon.</summary>
    public const string SunElevationFieldName = "SunElevation";

    private static readonly List<Field> _fields = new List<Field>
    {
        new() { IsNullable = false, Name = KindFieldName, Length = 0, TypeFullName = typeof(string).FullName },
        new() { IsNullable = false, Name = SunElevationFieldName, Length = 0, TypeFullName = typeof(double).FullName },
    };

    private DateTime _time = DateTime.UtcNow;

    /// <summary>The instant drawn, in UTC (an Unspecified kind is taken as UTC, a Local one converted).</summary>
    public DateTime Time
    {
        get => _time;
        set => _time = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    /// <summary>The <see cref="KindFieldName"/> of the point where the Sun is at the zenith.</summary>
    public const string SubSolarPointKind = "SubSolarPoint";

    /// <summary>Also return the sub-solar point, as a point feature of kind <see cref="SubSolarPointKind"/>, after the bands.</summary>
    public bool IncludeSubSolarPoint { get; set; }

    /// <summary>The bands drawn, in any order; all four by default.</summary>
    public IReadOnlyList<DayNightBand> Bands { get; set; } = AllBands;

    public static IReadOnlyList<DayNightBand> AllBands { get; } = new[]
    {
        DayNightBand.BelowHorizon,
        DayNightBand.BelowCivilTwilight,
        DayNightBand.BelowNauticalTwilight,
        DayNightBand.BelowAstronomicalTwilight,
    };

    public override string SourceAddress => "Day/Night Data Source";

    public override BoundingBox WebMercatorExtent
    {
        get => new BoundingBox(-180.0, -MapGridHelper.MaxWebMercatorLatitude, 180.0, MapGridHelper.MaxWebMercatorLatitude)
            .Transform(MapProjects.GeodeticWgs84ToWebMercator);
        protected set => _ = value;
    }

    public override int Srid => SridHelper.WebMercator;

    public override GeometryType? GeometryType
    {
        get => Common.Enums.GeometryType.Polygon;
        protected set => _ = value;
    }

    public DayNightDataSource() : base(_fields)
    {
    }

    public static DayNightDataSource Create(DateTime time) => new DayNightDataSource { Time = time };

    /// <summary>The Sun's elevation, in degrees, below which a place is in <paramref name="band"/>.</summary>
    public static double GetSunElevationLimit(DayNightBand band)
    {
        return band switch
        {
            DayNightBand.BelowHorizon => SunAltitudes.Sunrise,
            DayNightBand.BelowCivilTwilight => SunAltitudes.CivilTwilight,
            DayNightBand.BelowNauticalTwilight => SunAltitudes.NauticalTwilight,
            DayNightBand.BelowAstronomicalTwilight => SunAltitudes.AstronomicalTwilight,
            _ => throw new ArgumentOutOfRangeException(nameof(band)),
        };
    }

    public override string ToString() => $"{nameof(DayNightDataSource)} {Time:u}";

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundingBox) => Task.FromResult(Build());

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry) => Task.FromResult(Build());

    /// <summary>The overlay has nothing to search: its polygons are computed, not stored.</summary>
    public override Task<FeatureSet<Point>> SearchAsync(string searchText) => throw new NotImplementedException();

    /// <summary>The bands at <see cref="Time"/>, widest first so the darker ones draw on top, then the optional sub-solar point.</summary>
    public List<Feature<Point>> GetFeatures()
    {
        var subSolar = Sun.GetSubSolarPoint(Time);

        var antiSolar = new Point(subSolar.X >= 0 ? subSolar.X - 180 : subSolar.X + 180, -subSolar.Y);

        var features = new List<Feature<Point>>();

        foreach (var band in Bands.Distinct().OrderByDescending(GetSunElevationLimit))
        {
            var limit = GetSunElevationLimit(band);

            // Elevation e < limit  ⇔  distance from the sub-solar point > 90° − limit
            //                     ⇔  distance from the anti-solar point < 90° + limit
            var rings = SphericalCapHelper.ToGeographicRings(antiSolar, 90 + limit, MapGridHelper.MaxWebMercatorLatitude);

            foreach (var ring in rings)
            {
                var webMercator = ring.Select(MapProjects.GeodeticWgs84ToWebMercator<Point>).ToList();

                features.Add(new Feature<Point>
                {
                    Id = features.Count,
                    TheGeometry = Geometry<Point>.Create(webMercator, Common.Enums.GeometryType.Polygon, SridHelper.WebMercator),
                    Attributes = new Dictionary<string, object>
                    {
                        { KindFieldName, band.ToString() },
                        { SunElevationFieldName, limit },
                    },
                });
            }
        }

        if (IncludeSubSolarPoint && Math.Abs(subSolar.Y) <= MapGridHelper.MaxWebMercatorLatitude)
        {
            features.Add(new Feature<Point>
            {
                Id = features.Count,
                TheGeometry = Geometry<Point>.Create(
                    new List<Point> { MapProjects.GeodeticWgs84ToWebMercator(subSolar) }, Common.Enums.GeometryType.Point, SridHelper.WebMercator),
                Attributes = new Dictionary<string, object>
                {
                    { KindFieldName, SubSolarPointKind },
                    { SunElevationFieldName, 90.0 },
                },
            });
        }

        return features;
    }

    private FeatureSet<Point> Build() => FeatureSet<Point>.Create(string.Empty, GetFeatures());
}

/// <summary>A region of the day/night overlay, by how far the Sun is below the horizon there.</summary>
public enum DayNightBand
{
    /// <summary>Sun below the horizon (after sunset, before sunrise).</summary>
    BelowHorizon,

    /// <summary>Sun more than 6° below: past civil twilight.</summary>
    BelowCivilTwilight,

    /// <summary>Sun more than 12° below: past nautical twilight.</summary>
    BelowNauticalTwilight,

    /// <summary>Sun more than 18° below: full (astronomical) night.</summary>
    BelowAstronomicalTwilight,
}
