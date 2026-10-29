namespace IRI.Maptor.Core.Astronomy.Coordinates;

/// <summary>
/// A direction on the celestial sphere in the equatorial frame of date.
/// </summary>
public readonly struct EquatorialCoordinate
{
    /// <summary>Right ascension, degrees in [0, 360). Divide by 15 for hours.</summary>
    public double RightAscension { get; }

    /// <summary>Declination, degrees in [-90, 90].</summary>
    public double Declination { get; }

    public EquatorialCoordinate(double rightAscension, double declination)
    {
        RightAscension = rightAscension;
        Declination = declination;
    }

    public override string ToString() => FormattableString.Invariant($"RA {RightAscension:F5}°, Dec {Declination:F5}°");
}
