using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.DataStructures.Trees;

namespace IRI.Maptor.Sta.Spatial.AdvancedStructures;

public class BalancedKdTreeNode<T>
{
    private readonly KdTreeContext<T> _context;

    private readonly bool _isNil;

    public T Point { get; set; }

    public NodeColor Color { get; set; }

    protected BalancedKdTreeNode<T> leftChild, rightChild, parent;

    public BalancedKdTreeNode<T> LeftChild
    {
        get { return this.leftChild; }

        set
        {
            this.leftChild = value;

            if (value != null && !value.IsNilNode())
            {
                value.Parent = this;
            }
        }
    }

    public BalancedKdTreeNode<T> RightChild
    {
        get { return this.rightChild; }

        set
        {
            this.rightChild = value;

            if (value != null && !value.IsNilNode())
            {
                value.Parent = this;
            }
        }
    }

    public BalancedKdTreeNode<T> Parent
    {
        get { return this.parent; }

        set { this.parent = value; }
    }

    private BoundingBox _minimumBoundingBox;

    /// <summary>
    /// The extent of this node's whole subtree. Maintained by <see cref="BalancedKdTree{T}"/>;
    /// it is what the spatial queries prune on, so it is not writable from outside.
    /// </summary>
    public BoundingBox MinimumBoundingBox
    {
        get { return _minimumBoundingBox; }

        internal set { this._minimumBoundingBox = value; }
    }

    private BalancedKdTreeNode(KdTreeContext<T> context, T nilValue)
    {
        this._context = context;

        this._isNil = true;

        this.Point = nilValue;

        this.Color = NodeColor.Black;
    }

    internal BalancedKdTreeNode(KdTreeContext<T> context, T point, NodeColor color)
    {
        this._context = context;

        this.Point = point;

        this.Color = color;

        this.LeftChild = context.NilNode;
        this.RightChild = context.NilNode;

        var pointValue = context.PointFunc(point);

        this.MinimumBoundingBox = new BoundingBox(pointValue.X, pointValue.Y, pointValue.X, pointValue.Y);
    }

    internal static BalancedKdTreeNode<T> CreateNilNode(KdTreeContext<T> context, T nilValue)
    {
        return new BalancedKdTreeNode<T>(context, nilValue);
    }

    public bool IsNilNode()
    {
        return this._isNil;
    }

    /// <summary>
    /// Recomputes this node's box from its own point and its children's boxes.
    /// </summary>
    internal void RepairBoundingBox()
    {
        if (_isNil)
        {
            return;
        }

        var pointValue = _context.PointFunc(Point);

        var box = new BoundingBox(pointValue.X, pointValue.Y, pointValue.X, pointValue.Y);

        if (leftChild != null && !leftChild.IsNilNode())
        {
            box = box.Add(leftChild.MinimumBoundingBox);
        }

        if (rightChild != null && !rightChild.IsNilNode())
        {
            box = box.Add(rightChild.MinimumBoundingBox);
        }

        this._minimumBoundingBox = box;
    }

    /// <summary>
    /// Repairs this node and then every ancestor up to the root.
    /// </summary>
    internal void RepairBoundingBoxUpward()
    {
        var node = this;

        while (node != null && !node.IsNilNode())
        {
            node.RepairBoundingBox();

            node = node.parent;
        }
    }

    /// <summary>
    /// Computes the true extent of this subtree from its points, ignoring the cached boxes.
    /// Use it to verify <see cref="MinimumBoundingBox"/>; it is deliberately independent of it.
    /// </summary>
    public BoundingBox CalculateBoundingBox()
    {
        var pointValue = _context.PointFunc(Point);

        BoundingBox result = new BoundingBox(pointValue.X, pointValue.Y, pointValue.X, pointValue.Y);

        if (LeftChild != null && !LeftChild.IsNilNode())
        {
            result = result.Add(LeftChild.CalculateBoundingBox());
        }

        if (RightChild != null && !RightChild.IsNilNode())
        {
            result = result.Add(RightChild.CalculateBoundingBox());
        }

        return result;
    }

    public override string ToString()
    {
        return string.Format("Key = '{0}', Left = '{1}', Right = '{2}'",
            Point?.ToString(),
            LeftChild == null || LeftChild.IsNilNode() ? string.Empty : LeftChild.Point?.ToString(),
            RightChild == null || RightChild.IsNilNode() ? string.Empty : RightChild.Point?.ToString());
    }
}
