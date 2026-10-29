namespace IRI.Maptor.Core.Satellite.Propagation;

/// <summary>
/// The Earth constants SGP4 runs with. TLEs and OMMs are fitted with <see cref="Wgs72"/>, so use it
/// unless the elements' producer says otherwise; the others exist for comparison with other software.
/// </summary>
public enum Sgp4GravityModel
{
    /// <summary>WGS-72 with the original Spacetrack Report #3 value of XKE.</summary>
    Wgs72Old,

    /// <summary>WGS-72: the standard for published elements. The default.</summary>
    Wgs72,

    Wgs84,
}

/// <summary>The constants of one <see cref="Sgp4GravityModel"/> (Vallado's getgravconst).</summary>
internal readonly struct GravityConstants
{
    /// <summary>km³/s².</summary>
    public double Mu { get; }

    /// <summary>Equatorial radius, km.</summary>
    public double RadiusEarthKm { get; }

    /// <summary>√(μ) in Earth radii^1.5 per minute.</summary>
    public double Xke { get; }

    public double TuMin { get; }

    public double J2 { get; }

    public double J3 { get; }

    public double J4 { get; }

    public double J3OverJ2 => J3 / J2;

    private GravityConstants(double mu, double radiusEarthKm, double xke, double j2, double j3, double j4)
    {
        Mu = mu;
        RadiusEarthKm = radiusEarthKm;
        Xke = xke;
        TuMin = 1.0 / xke;
        J2 = j2;
        J3 = j3;
        J4 = j4;
    }

    public static GravityConstants For(Sgp4GravityModel model)
    {
        switch (model)
        {
            case Sgp4GravityModel.Wgs72Old:
                return new GravityConstants(398600.79964, 6378.135, 0.0743669161, 0.001082616, -0.00000253881, -0.00000165597);

            case Sgp4GravityModel.Wgs72:
            {
                const double mu = 398600.8, radius = 6378.135;
                return new GravityConstants(mu, radius, 60.0 / Math.Sqrt(radius * radius * radius / mu), 0.001082616, -0.00000253881, -0.00000165597);
            }

            case Sgp4GravityModel.Wgs84:
            {
                const double mu = 398600.5, radius = 6378.137;
                return new GravityConstants(mu, radius, 60.0 / Math.Sqrt(radius * radius * radius / mu), 0.00108262998905, -0.00000253215306, -0.00000161098761);
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(model));
        }
    }
}
