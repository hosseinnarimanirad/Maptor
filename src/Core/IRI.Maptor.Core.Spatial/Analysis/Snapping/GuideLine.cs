using IRI.Maptor.Core.Common.Abstractions;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>What a guide follows.</summary>
public enum GuideKind
{
    /// <summary>Straight on from a segment, beyond its end.</summary>
    Extension = 1,

    /// <summary>At 90° to a segment.</summary>
    Perpendicular = 2,

    /// <summary>Along a segment's direction, through another point.</summary>
    Parallel = 3,

    /// <summary>A fixed angle step from a segment's direction (or from north).</summary>
    AngleStep = 4,
}

/// <summary>
/// A direction to draw along: a line through <see cref="Origin"/>, or only its forward half (a ray). It only
/// describes the guide; <see cref="GuideBuilder"/> makes guides and <see cref="GuideEngine"/> searches them.
/// Coordinates are flat (a projected SRS such as Web Mercator).
/// </summary>
public sealed class GuideLine<T> where T : IPoint, new()
{
    internal GuideLine(GuideKind kind, T origin, double directionX, double directionY, bool isRay, T? referenceStart = default, T? referenceEnd = default)
    {
        var norm = Math.Sqrt(directionX * directionX + directionY * directionY);

        Kind = kind;
        Origin = origin;
        DirectionX = directionX / norm;
        DirectionY = directionY / norm;
        IsRay = isRay;
        ReferenceStart = referenceStart;
        ReferenceEnd = referenceEnd;
    }

    public GuideKind Kind { get; }

    /// <summary>A point of the guide; a ray starts here.</summary>
    public T Origin { get; }

    /// <summary>The guide's direction as a unit vector.</summary>
    public double DirectionX { get; }

    public double DirectionY { get; }

    /// <summary>True when only the forward half counts (an extension, an angle step).</summary>
    public bool IsRay { get; }

    /// <summary>The existing segment the guide was built from, when it was built from one; else null.</summary>
    public T? ReferenceStart { get; }

    public T? ReferenceEnd { get; }

    public bool HasReference => ReferenceStart is not null && ReferenceEnd is not null;

    /// <summary>The point at <paramref name="along"/> coordinate units from the origin (negative is behind it).</summary>
    public T At(double along) => new T { X = Origin.X + along * DirectionX, Y = Origin.Y + along * DirectionY };

    /// <summary>How far along the guide the point nearest to <paramref name="point"/> is (negative is behind the origin).</summary>
    public double Along(T point) => (point.X - Origin.X) * DirectionX + (point.Y - Origin.Y) * DirectionY;
}
