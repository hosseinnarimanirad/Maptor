using IRI.Maptor.Core.Satellite.Propagation;

namespace IRI.Maptor.Core.Satellite.Passes;

/// <summary>What a pass is about.</summary>
public enum PassKind
{
    /// <summary>The satellite is above the minimum elevation from somewhere in the region.</summary>
    Visible,

    /// <summary>The imager's swath reaches the region.</summary>
    Imaged,
}

/// <summary>One pass of a satellite over a <see cref="PassRegion"/>, found by <see cref="PassFinder"/>.</summary>
public sealed class SatellitePass
{
    internal SatellitePass(Sgp4Propagator satellite, PassKind kind, DateTime start, DateTime peak, DateTime end)
    {
        Satellite = satellite;
        Kind = kind;
        Start = start;
        Peak = peak;
        End = end;
    }

    public Sgp4Propagator Satellite { get; }

    /// <summary>The satellite's name, or its catalog number.</summary>
    public string Name => Satellite.Elements.Name ?? Satellite.Elements.CatalogNumber;

    public PassKind Kind { get; }

    /// <summary>When the pass begins, UTC; the window's start if it was already under way.</summary>
    public DateTime Start { get; }

    /// <summary>The closest approach to the region, UTC: the highest elevation and the middle of the pass.</summary>
    public DateTime Peak { get; }

    /// <summary>When the pass ends, UTC; the window's end if it was still under way.</summary>
    public DateTime End { get; }

    public TimeSpan Duration => End - Start;

    /// <summary>The pass was already under way when the search window began.</summary>
    public bool StartsBeforeWindow { get; internal set; }

    /// <summary>The pass was still under way when the search window ended.</summary>
    public bool EndsAfterWindow { get; internal set; }

    /// <summary>
    /// The pass lasts the whole window: a geostationary satellite always in view of the region, for
    /// instance. Its peak is then the moment of the highest elevation within the window.
    /// </summary>
    public bool CoversWholeWindow => StartsBeforeWindow && EndsAfterWindow;

    /// <summary>
    /// The highest elevation seen from anywhere in the region, degrees. For a point it is exact; for a
    /// polygon it is taken on a sphere from the closest approach, good to a few tenths of a degree.
    /// </summary>
    public double PeakElevation { get; internal set; }

    /// <summary>Where the satellite appears and where it is highest, degrees from north; point regions only.</summary>
    public double? RiseAzimuth { get; internal set; }

    public double? PeakAzimuth { get; internal set; }

    /// <summary>Where it disappears; point regions only.</summary>
    public double? SetAzimuth { get; internal set; }

    /// <summary>The satellite is heading north at the peak.</summary>
    public bool IsNorthbound { get; internal set; }

    /// <summary>The Sun's elevation at the region's centre at the peak, degrees.</summary>
    public double SunElevation { get; internal set; }

    /// <summary>The Sun is up at the region's centre at the peak: what an optical imager needs.</summary>
    public bool IsDaylight => SunElevation > 0;

    /// <summary>
    /// For a visible pass: at some moment the satellite is in sunlight while the region's centre is past
    /// civil dusk (Sun below −6°), so it can be seen with the naked eye. Always false for deep-space
    /// orbits (GPS, Galileo, geostationary): they are far too faint.
    /// </summary>
    public bool IsNakedEyeVisible { get; internal set; }

    /// <summary>For an imaged pass: the share of the region inside the swath, 0–1 (1 for a point).</summary>
    public double? CoveredFraction { get; internal set; }

    public override string ToString()
        => FormattableString.Invariant($"{Name} {Kind} {Start:yyyy-MM-dd HH:mm:ss}–{End:HH:mm:ss}, peak {PeakElevation:F1}°");
}
