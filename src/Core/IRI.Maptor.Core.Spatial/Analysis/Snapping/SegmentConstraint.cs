using IRI.Maptor.Core.Common.Abstractions;

namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>
/// A length and/or an angle the user fixed for the segment being drawn, and the point they allow for a pointer
/// position. It holds the values and works out the point; it does not read keys or draw.
/// <para>
/// The length is in ground metres (<see cref="WebMercatorGround"/>). The angle is in degrees: a turn from the
/// previous segment (0 straight on, positive to the right), or, when <see cref="AngleIsBearing"/> is set or there
/// is no previous segment, a bearing clockwise from north. Coordinates are Web Mercator.
/// </para>
/// </summary>
public sealed class SegmentConstraint
{
    /// <summary>The fixed ground length in metres, or null when the length is free.</summary>
    public double? Length { get; set; }

    /// <summary>The fixed angle in degrees, or null when the direction is free.</summary>
    public double? Angle { get; set; }

    /// <summary>True when <see cref="Angle"/> is a bearing from north; false when it is a turn from the previous segment.</summary>
    public bool AngleIsBearing { get; set; }

    public bool HasLength => Length is > 0;

    public bool HasAngle => Angle is { } angle && !double.IsNaN(angle) && !double.IsInfinity(angle);

    /// <summary>True when a length or an angle is fixed.</summary>
    public bool IsActive => HasLength || HasAngle;

    /// <summary>Frees both.</summary>
    public void Clear()
    {
        Length = null;
        Angle = null;
    }

    /// <summary>
    /// The point for the segment from <paramref name="last"/> that the fixed values allow, given where the pointer
    /// is: with a fixed angle the direction is that angle, else it is <paramref name="direction"/> when given (the
    /// guide the pointer is on), else towards the pointer; with a fixed length the point is at that ground length
    /// along the direction, else it is the pointer projected onto the direction (never behind
    /// <paramref name="last"/>). With nothing fixed the pointer itself is returned.
    /// </summary>
    /// <param name="previous">The point before <paramref name="last"/>, or null for the first segment of a shape.</param>
    /// <param name="last">The point the segment starts from.</param>
    /// <param name="pointer">Where the pointer is.</param>
    /// <param name="direction">A direction to keep while only the length is fixed (it need not be a unit vector), or null.</param>
    public T Resolve<T>(T? previous, T last, T pointer, (double X, double Y)? direction = null) where T : IPoint, new()
    {
        if (!IsActive)
            return pointer;

        double dx, dy;

        if (HasAngle)
        {
            var bearing = AngleIsBearing || previous is null || (previous.X == last.X && previous.Y == last.Y)
                ? Angle!.Value
                : WebMercatorGround.Bearing(previous, last) + Angle!.Value;

            (dx, dy) = WebMercatorGround.DirectionOf(bearing);
        }
        else if (direction is { } given && (given.X != 0 || given.Y != 0))
        {
            var norm = Math.Sqrt(given.X * given.X + given.Y * given.Y);

            (dx, dy) = (given.X / norm, given.Y / norm);
        }
        else
        {
            dx = pointer.X - last.X;
            dy = pointer.Y - last.Y;

            var norm = Math.Sqrt(dx * dx + dy * dy);

            // the pointer is on the start point: no direction to measure the length along
            if (norm <= 0)
                return new T { X = last.X, Y = last.Y };

            (dx, dy) = (dx / norm, dy / norm);
        }

        if (HasLength)
            return WebMercatorGround.PointAt(last, dx, dy, Length!.Value);

        var along = Math.Max(0, (pointer.X - last.X) * dx + (pointer.Y - last.Y) * dy);

        return new T { X = last.X + along * dx, Y = last.Y + along * dy };
    }
}
