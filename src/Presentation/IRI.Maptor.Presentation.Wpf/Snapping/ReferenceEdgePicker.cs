using System;

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
/// parallel to it) and tells when it changes. The edge is picked on purpose, from the right-click options while
/// drawing; nothing is picked by moving the pointer. It does not find the edge under the pointer
/// (<see cref="MapSnapper.EdgeAt"/>), build guides or draw.
/// </summary>
public sealed class ReferenceEdgePicker
{
    /// <summary>The edge guides are drawn from, or null.</summary>
    public ReferenceEdge? Reference { get; private set; }

    /// <summary>Raised when <see cref="Reference"/> changes.</summary>
    public event Action? Changed;

    /// <summary>Makes <paramref name="edge"/> the reference.</summary>
    public void Set(ReferenceEdge edge)
    {
        Reference = edge ?? throw new ArgumentNullException(nameof(edge));

        Changed?.Invoke();
    }

    /// <summary>Drops the reference. False when there was none.</summary>
    public bool Clear()
    {
        if (Reference is null)
            return false;

        Reference = null;

        Changed?.Invoke();

        return true;
    }
}
