namespace IRI.Maptor.Core.Astronomy.Coordinates;

/// <summary>
/// A direction as seen by an observer on the ground.
/// </summary>
public readonly struct HorizontalCoordinate
{
    /// <summary>Azimuth, degrees in [0, 360), measured from north through east.</summary>
    public double Azimuth { get; }

    /// <summary>Geometric elevation above the horizon, degrees in [-90, 90]. No atmospheric refraction is applied.</summary>
    public double Elevation { get; }

    public HorizontalCoordinate(double azimuth, double elevation)
    {
        Azimuth = azimuth;
        Elevation = elevation;
    }

    public override string ToString() => FormattableString.Invariant($"Az {Azimuth:F3}°, El {Elevation:F3}°");
}
