using System;
using System.Windows.Threading;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Presentation.Wpf.Layers;

namespace IRI.Maptor.Presentation.Wpf.Snapping;

/// <summary>One segment of an existing line or polygon boundary (Web Mercator), and the layer it belongs to.</summary>
public sealed class ReferenceEdge
{
    public ReferenceEdge(Point start, Point end, VectorLayer? source = null)
    {
        Start = start;
        End = end;
        Source = source;
    }

    public Point Start { get; }

    public Point End { get; }

    /// <summary>The map layer (for a drawing, its drawing item), or null when unknown.</summary>
    public VectorLayer? Source { get; }

    /// <summary>True when <paramref name="other"/> is the same segment, in either direction.</summary>
    public bool SameAs(ReferenceEdge? other)
        => other is not null &&
           ((Same(Start, other.Start) && Same(End, other.End)) || (Same(Start, other.End) && Same(End, other.Start)));

    private static bool Same(Point a, Point b) => a.X == b.X && a.Y == b.Y;
}

/// <summary>
/// Holds the existing edge the user picked to draw guides from (its extension, the perpendicular to it and the
/// parallel to it), and picks one when the pointer rests on an edge for <see cref="Pause"/>. It does not find the
/// edge under the pointer (<see cref="MapSnapper.EdgeAt"/>), build guides or draw.
/// </summary>
public sealed class ReferenceEdgePicker
{
    /// <summary>How long the pointer must rest on an edge for it to become the reference.</summary>
    public static readonly TimeSpan Pause = TimeSpan.FromMilliseconds(500);

    // moving further than this (pixels) is not resting: the wait starts again
    private const double RestDistance = 4;

    private readonly DispatcherTimer _timer;

    private ReferenceEdge? _pending;
    private System.Windows.Point _pendingAt;

    public ReferenceEdgePicker()
    {
        _timer = new DispatcherTimer { Interval = Pause };

        _timer.Tick += (sender, e) => PauseElapsed();
    }

    /// <summary>The edge guides are drawn from, or null.</summary>
    public ReferenceEdge? Reference { get; private set; }

    /// <summary>Raised when <see cref="Reference"/> changes.</summary>
    public event Action? Changed;

    /// <summary>
    /// Tells the picker what the pointer is on: an edge, or null (nothing, a typed value, a pan). The edge becomes
    /// the reference once the pointer has rested on it for <see cref="Pause"/>; moving on, or onto another edge,
    /// starts the wait again. Resting on the current reference changes nothing.
    /// </summary>
    /// <param name="edge">The edge under the pointer, or null.</param>
    /// <param name="screenPoint">Where the pointer is, in pixels.</param>
    public void Hover(ReferenceEdge? edge, System.Windows.Point screenPoint = default)
    {
        if (edge is null || edge.SameAs(Reference))
        {
            _pending = null;
            _timer.Stop();

            return;
        }

        if (edge.SameAs(_pending) && (screenPoint - _pendingAt).Length <= RestDistance)
            return;

        _pending = edge;
        _pendingAt = screenPoint;

        _timer.Stop();
        _timer.Start();
    }

    /// <summary>The wait is over: the edge the pointer rested on becomes the reference. The timer calls it; tests may too.</summary>
    public void PauseElapsed()
    {
        _timer.Stop();

        if (_pending is not { } edge)
            return;

        _pending = null;

        Set(edge);
    }

    /// <summary>Makes <paramref name="edge"/> the reference at once.</summary>
    public void Set(ReferenceEdge edge)
    {
        _pending = null;
        _timer.Stop();

        Reference = edge ?? throw new ArgumentNullException(nameof(edge));

        Changed?.Invoke();
    }

    /// <summary>Drops the reference and any wait. False when there was no reference.</summary>
    public bool Clear()
    {
        _pending = null;
        _timer.Stop();

        if (Reference is null)
            return false;

        Reference = null;

        Changed?.Invoke();

        return true;
    }
}
