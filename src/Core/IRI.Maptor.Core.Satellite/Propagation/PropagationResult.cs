using IRI.Maptor.Core.Satellite.Frames;

namespace IRI.Maptor.Core.Satellite.Propagation;

/// <summary>What SGP4 says about a propagation; the numbers are Vallado's error codes.</summary>
public enum Sgp4Status
{
    Ok = 0,

    /// <summary>The mean eccentricity left [0, 1): the elements are no longer meaningful.</summary>
    MeanEccentricityOutOfRange = 1,

    /// <summary>The mean motion fell to zero or below.</summary>
    MeanMotionNotPositive = 2,

    /// <summary>Deep space: the eccentricity with the lunar-solar periodics left [0, 1].</summary>
    PerturbedEccentricityOutOfRange = 3,

    /// <summary>The semi-latus rectum went negative.</summary>
    SemiLatusRectumNegative = 4,

    /// <summary>The orbit has decayed: the satellite is below the Earth's surface.</summary>
    Decayed = 6,
}

/// <summary>
/// A satellite's state at one instant, in the TEME frame SGP4 works in (True Equator, Mean Equinox
/// of date): position in km, velocity in km/s.
/// </summary>
public sealed class PropagationResult
{
    internal PropagationResult(DateTime utc, double minutesSinceEpoch, Sgp4Status status, Vector3 position, Vector3 velocity)
    {
        Utc = utc;
        MinutesSinceEpoch = minutesSinceEpoch;
        Status = status;
        Position = position;
        Velocity = velocity;
    }

    public DateTime Utc { get; }

    public double MinutesSinceEpoch { get; }

    public Sgp4Status Status { get; }

    public bool IsValid => Status == Sgp4Status.Ok;

    /// <summary>TEME position, km. Zero when <see cref="Status"/> is <see cref="Sgp4Status.SemiLatusRectumNegative"/>.</summary>
    public Vector3 Position { get; }

    /// <summary>TEME velocity, km/s.</summary>
    public Vector3 Velocity { get; }
}
