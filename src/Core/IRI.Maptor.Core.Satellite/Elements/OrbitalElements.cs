namespace IRI.Maptor.Core.Satellite.Elements;

/// <summary>
/// One satellite's mean orbital elements at an epoch, as published in a TLE or an OMM: the input of
/// <see cref="Propagation.Sgp4Propagator"/>.
/// </summary>
/// <remarks>
/// These are SGP4 <em>mean</em> elements, not osculating ones; they only give correct positions when
/// fed back through SGP4. Angles are in degrees, mean motion in revolutions per day, as published.
/// </remarks>
public sealed class OrbitalElements
{
    public OrbitalElements(
        string catalogNumber,
        DateTime epoch,
        double inclination,
        double rightAscension,
        double eccentricity,
        double argumentOfPerigee,
        double meanAnomaly,
        double meanMotion,
        double bStar,
        double meanMotionDot = 0,
        double meanMotionDdot = 0,
        string? name = null,
        string? internationalDesignator = null,
        char classification = 'U',
        int elementSetNumber = 0,
        int revolutionNumber = 0)
    {
        if (string.IsNullOrWhiteSpace(catalogNumber))
            throw new ArgumentException("A catalog number is required.", nameof(catalogNumber));

        if (!(eccentricity >= 0 && eccentricity < 1))
            throw new ArgumentOutOfRangeException(nameof(eccentricity), "The eccentricity must be in [0, 1).");

        if (!(meanMotion > 0))
            throw new ArgumentOutOfRangeException(nameof(meanMotion), "The mean motion must be positive.");

        CatalogNumber = catalogNumber.Trim();
        Epoch = epoch.Kind == DateTimeKind.Local ? epoch.ToUniversalTime() : DateTime.SpecifyKind(epoch, DateTimeKind.Utc);
        Inclination = inclination;
        RightAscension = rightAscension;
        Eccentricity = eccentricity;
        ArgumentOfPerigee = argumentOfPerigee;
        MeanAnomaly = meanAnomaly;
        MeanMotion = meanMotion;
        BStar = bStar;
        MeanMotionDot = meanMotionDot;
        MeanMotionDdot = meanMotionDdot;
        Name = string.IsNullOrWhiteSpace(name) ? null : name!.Trim();
        InternationalDesignator = string.IsNullOrWhiteSpace(internationalDesignator) ? null : internationalDesignator!.Trim();
        Classification = classification;
        ElementSetNumber = elementSetNumber;
        RevolutionNumber = revolutionNumber;
    }

    /// <summary>Object name, from a 3-line TLE's name line or an OMM's OBJECT_NAME; null when not given.</summary>
    public string? Name { get; }

    /// <summary>
    /// NORAD catalog number as published: digits, or the Alpha-5 form (a letter then four digits, e.g.
    /// <c>A0001</c> = 100001) used once five digits run out. See <see cref="CatalogNumberValue"/>.
    /// </summary>
    public string CatalogNumber { get; }

    /// <summary>The catalog number as an integer, decoding Alpha-5 (A = 10 … Z = 33, skipping I and O).</summary>
    public int CatalogNumberValue => CatalogNumbers.Decode(CatalogNumber);

    /// <summary>U unclassified, C classified, S secret.</summary>
    public char Classification { get; }

    /// <summary>COSPAR designator, e.g. "98067A" (TLE) or "1998-067A" (OMM); null when not given.</summary>
    public string? InternationalDesignator { get; }

    /// <summary>The instant the elements describe, UTC.</summary>
    public DateTime Epoch { get; }

    /// <summary>Degrees.</summary>
    public double Inclination { get; }

    /// <summary>Right ascension of the ascending node, degrees.</summary>
    public double RightAscension { get; }

    public double Eccentricity { get; }

    /// <summary>Degrees.</summary>
    public double ArgumentOfPerigee { get; }

    /// <summary>Degrees.</summary>
    public double MeanAnomaly { get; }

    /// <summary>Revolutions per day (Kozai mean motion, as published).</summary>
    public double MeanMotion { get; }

    /// <summary>Drag term, in inverse Earth radii.</summary>
    public double BStar { get; }

    /// <summary>First derivative of mean motion divided by two, revolutions per day². Not used by SGP4.</summary>
    public double MeanMotionDot { get; }

    /// <summary>Second derivative of mean motion divided by six, revolutions per day³. Not used by SGP4.</summary>
    public double MeanMotionDdot { get; }

    public int ElementSetNumber { get; }

    /// <summary>Revolutions completed at epoch.</summary>
    public int RevolutionNumber { get; }

    /// <summary>Orbital period from the published mean motion, minutes.</summary>
    public double PeriodMinutes => 1440.0 / MeanMotion;

    /// <summary>How old the elements are at <paramref name="utc"/>: SGP4 errors grow by roughly a few km a day.</summary>
    public TimeSpan Age(DateTime utc) => (utc.Kind == DateTimeKind.Local ? utc.ToUniversalTime() : utc) - Epoch;

    public override string ToString() => Name is null ? CatalogNumber : $"{Name} ({CatalogNumber})";
}
