using IRI.Maptor.Core.Common.Abstractions;

namespace IRI.Maptor.Core.Spatial.AdvancedStructures;

/// <summary>
/// Per-tree state shared between a <see cref="BalancedKdTree{T}"/> and its nodes.
/// </summary>
/// <remarks>
/// These used to be static members on <see cref="BalancedKdTree{T}"/>, which meant every
/// tree over the same <typeparamref name="T"/> shared one point accessor and one nil node:
/// constructing a second tree silently rebound the first one's accessor, and the nil node
/// was frozen from whichever tree happened to be built first. Holding them per instance
/// keeps trees isolated and makes construction thread-safe.
/// </remarks>
internal sealed class KdTreeContext<T>
{
    public Func<T, IPoint> PointFunc { get; }

    /// <summary>
    /// The shared leaf terminator. It carries no coordinates and is never visited by a query.
    /// </summary>
    public BalancedKdTreeNode<T> NilNode { get; }

    public KdTreeContext(Func<T, IPoint> pointFunc, T nilValue)
    {
        this.PointFunc = pointFunc;

        // the nil node deliberately does not evaluate pointFunc: nilValue is a sentinel
        // (Point.NaN, default(T), ...) and may not be a meaningful coordinate at all
        this.NilNode = BalancedKdTreeNode<T>.CreateNilNode(this, nilValue);
    }
}
