using System;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;

namespace IRI.Maptor.Presentation.Wpf.Snapping;

/// <summary>What the drawing aids made of one pointer position.</summary>
public sealed class DrawingAidResult
{
    internal DrawingAidResult(Point point, SnapResult<Point>? snap, GuideResult<Point>? guide, bool isTyped = false, bool isDirectionFixed = false)
    {
        Point = point;
        Snap = snap;
        Guide = guide;
        IsTyped = isTyped;
        IsDirectionFixed = isDirectionFixed;
    }

    /// <summary>The point to use.</summary>
    public Point Point { get; }

    /// <summary>The snap that gave <see cref="Point"/>, when it was a snap.</summary>
    public SnapResult<Point>? Snap { get; }

    /// <summary>The guide that gave <see cref="Point"/>, when it was a guide.</summary>
    public GuideResult<Point>? Guide { get; }

    /// <summary>True when a typed length or angle gave <see cref="Point"/>; <see cref="Snap"/> and <see cref="Guide"/> are then null.</summary>
    public bool IsTyped { get; }

    /// <summary>
    /// For a typed result: true when the segment's direction is held (a typed angle, or a typed length along a
    /// guide or an angle step), false when it still follows the pointer.
    /// </summary>
    public bool IsDirectionFixed { get; }
}

/// <summary>
/// Decides the point for a pointer position while a segment is being drawn, from snapping and guides together.
/// The order: a stored point (line end, vertex) first; then a guide (its crossing with an edge before the plain
/// guide); then the other snap kinds (intersection, midpoint, edge); else the pointer as it is. With the direction
/// locked to the angle steps, the point lies on the nearest step, at its crossing with an edge when one is in range.
/// <para>
/// Typed values come before all of that. A typed angle fixes the direction, and the point is the pointer brought
/// onto it. A typed length alone is measured along the direction the rules above give: towards the snapped point,
/// along the guide or the angle step, else towards the pointer. Typed values are not snapping: they hold while
/// snapping is suspended too.
/// </para>
/// <para>
/// It only combines <see cref="MapSnapper"/> and the guide engine; it does not load features, read the mouse or draw.
/// Everything is suspended with the snapper (snapping off, or Ctrl held).
/// </para>
/// </summary>
public sealed class MapDrawingAids
{
    /// <summary>The angle step the direction locks to (the same as rotating a text label with Shift).</summary>
    public const double AngleStepDegrees = 15;

    private readonly MapSnapper _snapper;
    private readonly Func<bool> _guidesEnabled;

    /// <param name="snapper">Snapping for the same map.</param>
    /// <param name="guidesEnabled">Whether guides are on, read at each call; on when null.</param>
    public MapDrawingAids(MapSnapper snapper, Func<bool>? guidesEnabled = null)
    {
        _snapper = snapper ?? throw new ArgumentNullException(nameof(snapper));
        _guidesEnabled = guidesEnabled ?? (() => true);
    }

    /// <param name="pointer">The map point under the pointer.</param>
    /// <param name="previous">The placed vertex before <paramref name="last"/>, or null (the first segment of a part).</param>
    /// <param name="last">The placed vertex the segment starts from, or null when no vertex is placed yet.</param>
    /// <param name="lockToSteps">True to lock the direction to the angle steps (Shift held).</param>
    /// <param name="typed">The length and/or angle typed for the segment, or null.</param>
    public DrawingAidResult Resolve(Point pointer, Point? previous, Point? last, bool lockToSteps, SegmentConstraint? typed = null)
    {
        if (last is null || typed is not { IsActive: true })
            return Untyped(pointer, previous, last, lockToSteps);

        if (typed.HasAngle)
            return new DrawingAidResult(typed.Resolve(previous, last, pointer), null, null, isTyped: true, isDirectionFixed: true);

        // only the length: along the direction snapping and guides give
        var free = Untyped(pointer, previous, last, lockToSteps);

        return new DrawingAidResult(typed.Resolve(previous, last, free.Point), null, null, isTyped: true,
                                    isDirectionFixed: free.Guide is { } onGuide && PassesThrough(onGuide.Guide, last));
    }

    private DrawingAidResult Untyped(Point pointer, Point? previous, Point? last, bool lockToSteps)
    {
        if (_snapper.IsSuspended)
            return new DrawingAidResult(pointer, null, null);

        var guidesOn = last is not null && _guidesEnabled();

        if (guidesOn && lockToSteps)
        {
            var steps = GuideBuilder.AngleSteps(previous, last!, AngleStepDegrees);

            if (GuideEngine.Nearest(steps, pointer) is { } locked)
            {
                // on the locked direction, a crossing with an edge near the point is still worth landing on
                var crossing = _snapper.Guide(new[] { locked.Guide }, locked.Location);

                var result = crossing is { Hit: GuideHit.Crossing } ? crossing : locked;

                return new DrawingAidResult(result.Location, null, result);
            }
        }

        var snap = _snapper.Snap(pointer);

        if (snap is { Kind: SnapKind.End or SnapKind.Vertex })
            return new DrawingAidResult(Copy(snap.Location), snap, null);

        if (guidesOn && previous is not null)
        {
            if (_snapper.Guide(GuideBuilder.FromPreviousSegment(previous, last!), pointer) is { } guide)
                return new DrawingAidResult(guide.Location, null, guide);
        }

        return snap is null
            ? new DrawingAidResult(pointer, null, null)
            : new DrawingAidResult(Copy(snap.Location), snap, null);
    }

    // a guide through the start of the segment keeps the segment's direction, wherever on it the point is
    private static bool PassesThrough(GuideLine<Point> guide, Point point)
    {
        var dx = point.X - guide.Origin.X;
        var dy = point.Y - guide.Origin.Y;

        var across = Math.Abs(dx * guide.DirectionY - dy * guide.DirectionX);

        return across <= 1e-9 * Math.Max(1, Math.Abs(dx) + Math.Abs(dy)) && (!guide.IsRay || guide.Along(point) >= -1e-9);
    }

    // a snapped vertex is the layer's own point object; the sketch must get its own
    private static Point Copy(Point point) => new Point(point.X, point.Y);
}
