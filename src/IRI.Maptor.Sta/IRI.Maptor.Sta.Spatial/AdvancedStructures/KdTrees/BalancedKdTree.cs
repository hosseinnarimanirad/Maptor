using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Common.Abstractions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.DataStructures.Trees;

namespace IRI.Maptor.Sta.Spatial.AdvancedStructures;

/// <summary>
/// A point index that keeps itself shallow with red-black rotations and caches, at every node,
/// the bounding box of that node's subtree. Answers nearest-neighbour and radius queries.
/// </summary>
/// <remarks>
/// The comparer used at a node is chosen from its depth (<c>comparers[level % comparers.Count]</c>),
/// but rotations move nodes between depths. With more than one comparer the k-d ordering invariant
/// therefore does not survive balancing. Queries stay correct regardless, because they prune on the
/// cached bounding boxes and never consult the comparers — what degrades is how tight those boxes
/// are, and so how much a query can skip. Pass a single total-order comparer (a Hilbert rank, see
/// <c>Analysis/SFC</c>) to keep the ordering exact.
/// </remarks>
public class BalancedKdTree<T>
{
    private BalancedKdTreeNode<T> root;

    private readonly KdTreeContext<T> _context;

    private readonly List<Func<T, T, int>> comparers;

    public BalancedKdTreeNode<T> Root
    {
        get { return this.root; }
        set
        {
            this.root = value;

            if (this.root != null)
            {
                this.root.Parent = null;
            }
        }
    }

    /// <summary>
    /// How coordinates are read out of <typeparamref name="T"/>.
    /// </summary>
    public Func<T, IPoint> PointFunc => _context.PointFunc;

    /// <summary>
    /// The leaf terminator shared by this tree's nodes.
    /// </summary>
    public BalancedKdTreeNode<T> NilNode => _context.NilNode;

    public BalancedKdTree(IEnumerable<T> values, List<Func<T, T, int>> comparers, T nilValue, Func<T, IPoint> pointFunc)
    {
        if (comparers == null || comparers.Count == 0)
        {
            throw new ArgumentException("At least one comparer is required.", nameof(comparers));
        }

        if (pointFunc == null && !typeof(IPoint).IsAssignableFrom(typeof(T)))
        {
            throw new ArgumentException(
                $"'{typeof(T).Name}' does not implement IPoint, so a pointFunc is required to read its coordinates.",
                nameof(pointFunc));
        }

        this.comparers = comparers;

        this._context = new KdTreeContext<T>(pointFunc ?? (i => (IPoint)i), nilValue);

        if (values == null)
        {
            return;
        }

        // single pass: values may be a lazy or single-use sequence
        foreach (var item in values)
        {
            Insert(item);
        }
    }

    public void Insert(T value)
    {
        var node = new BalancedKdTreeNode<T>(_context, value, NodeColor.Red);

        if (this.root == null)
        {
            node.Color = NodeColor.Black;

            this.Root = node;

            return;
        }

        Add(this.root, node, 0);

        // the new node widened the boxes of everything above it
        node.RepairBoundingBoxUpward();

        InsertFixup(node);
    }

    /// <summary>
    /// Walks down from <paramref name="parent"/> and hangs <paramref name="child"/> where it falls off.
    /// </summary>
    /// <remarks>
    /// The depth is threaded through rather than recomputed. It used to be derived by walking back up
    /// to the root at every step, which made a single insertion cost O(depth^2).
    /// </remarks>
    private void Add(BalancedKdTreeNode<T> parent, BalancedKdTreeNode<T> child, int depth)
    {
        while (true)
        {
            var comparer = this.comparers[depth % this.comparers.Count];

            if (comparer(parent.Point, child.Point) >= 0)
            {
                if (parent.LeftChild != null && !parent.LeftChild.IsNilNode())
                {
                    parent = parent.LeftChild;

                    depth++;

                    continue;
                }

                parent.LeftChild = child;

                return;
            }
            else
            {
                if (parent.RightChild != null && !parent.RightChild.IsNilNode())
                {
                    parent = parent.RightChild;

                    depth++;

                    continue;
                }

                parent.RightChild = child;

                return;
            }
        }
    }

    private void InsertFixup(BalancedKdTreeNode<T> node)
    {
        while (node.Parent != null && node.Parent.Parent != null && node.Parent.Color == NodeColor.Red)
        {
            var parent = node.Parent;

            var grandParent = parent.Parent;

            if (object.ReferenceEquals(parent, grandParent.LeftChild))
            {
                BalancedKdTreeNode<T> uncle = grandParent.RightChild;

                if (uncle != null && uncle.Color == NodeColor.Red)
                {
                    parent.Color = NodeColor.Black;

                    uncle.Color = NodeColor.Black;

                    grandParent.Color = NodeColor.Red;

                    node = grandParent;
                }
                else if (object.ReferenceEquals(node, parent.RightChild))
                {
                    node = parent;

                    LeftRotate(node);
                }
                else
                {
                    parent.Color = NodeColor.Black;

                    grandParent.Color = NodeColor.Red;

                    RightRotate(grandParent);
                }
            }
            else if (object.ReferenceEquals(parent, grandParent.RightChild))
            {
                BalancedKdTreeNode<T> uncle = grandParent.LeftChild;

                if (uncle != null && uncle.Color == NodeColor.Red)
                {
                    parent.Color = NodeColor.Black;

                    uncle.Color = NodeColor.Black;

                    grandParent.Color = NodeColor.Red;

                    node = grandParent;
                }
                else if (object.ReferenceEquals(node, parent.LeftChild))
                {
                    node = parent;

                    RightRotate(node);
                }
                else
                {
                    parent.Color = NodeColor.Black;

                    grandParent.Color = NodeColor.Red;

                    LeftRotate(grandParent);
                }
            }
            else
            {
                // the parent is neither child of the grandparent: the links are inconsistent.
                // bail out rather than spin forever.
                break;
            }
        }

        this.Root.Color = NodeColor.Black;
    }

    private void LeftRotate(BalancedKdTreeNode<T> node)
    {
        BalancedKdTreeNode<T> rightChild = node.RightChild;

        if (rightChild == null || rightChild.IsNilNode())
        {
            throw new InvalidOperationException("A left rotation needs a right child to promote.");
        }

        node.RightChild = rightChild.LeftChild;

        // read node.Parent before the last link overwrites it
        if (node.Parent == null)
        {
            this.Root = rightChild;
        }
        else if (object.ReferenceEquals(node, node.Parent.LeftChild))
        {
            node.Parent.LeftChild = rightChild;
        }
        else
        {
            node.Parent.RightChild = rightChild;
        }

        rightChild.LeftChild = node;

        // the demoted node lost a subtree, so repair it before its new parent reads its box
        node.RepairBoundingBox();

        rightChild.RepairBoundingBoxUpward();
    }

    private void RightRotate(BalancedKdTreeNode<T> node)
    {
        BalancedKdTreeNode<T> leftChild = node.LeftChild;

        if (leftChild == null || leftChild.IsNilNode())
        {
            throw new InvalidOperationException("A right rotation needs a left child to promote.");
        }

        node.LeftChild = leftChild.RightChild;

        if (node.Parent == null)
        {
            this.Root = leftChild;
        }
        else if (object.ReferenceEquals(node, node.Parent.LeftChild))
        {
            node.Parent.LeftChild = leftChild;
        }
        else
        {
            node.Parent.RightChild = leftChild;
        }

        leftChild.RightChild = node;

        node.RepairBoundingBox();

        leftChild.RepairBoundingBoxUpward();
    }

    public int GetNodeLevel(BalancedKdTreeNode<T> node)
    {
        int level = 0;

        while (node?.Parent != null)
        {
            level++;

            node = node.Parent;
        }

        return level;
    }

    private double GetDefaultDistance(T first, T second)
    {
        return SpatialUtility.GetEuclideanLength(PointFunc(first), PointFunc(second));
    }

    //Nearest Neighbour
    public T FindNearestNeighbour(T point, Func<T, T, double> distanceFunc = null)
    {
        if (this.root == null)
        {
            throw new InvalidOperationException("The tree is empty; there is no nearest neighbour.");
        }

        distanceFunc ??= GetDefaultDistance;

        var best = this.root.Point;

        var bestDistance = double.PositiveInfinity;

        FindNearestNeighbour(point, this.root, distanceFunc, ref best, ref bestDistance);

        return best;
    }

    /// <remarks>
    /// The best distance found so far is carried by reference, so every candidate is measured once
    /// and the pruning radius tightens for the second child instead of being thrown away.
    /// </remarks>
    private void FindNearestNeighbour(
        T point,
        BalancedKdTreeNode<T> node,
        Func<T, T, double> distanceFunc,
        ref T best,
        ref double bestDistance)
    {
        var distance = distanceFunc(point, node.Point);

        if (distance < bestDistance)
        {
            bestDistance = distance;

            best = node.Point;
        }

        SearchChildForNearest(point, node.LeftChild, distanceFunc, ref best, ref bestDistance);

        SearchChildForNearest(point, node.RightChild, distanceFunc, ref best, ref bestDistance);
    }

    private void SearchChildForNearest(
        T point,
        BalancedKdTreeNode<T> child,
        Func<T, T, double> distanceFunc,
        ref T best,
        ref double bestDistance)
    {
        if (child == null || child.IsNilNode())
        {
            return;
        }

        if (!SpatialUtility.CircleRectangleIntersects(PointFunc(point), bestDistance, child.MinimumBoundingBox))
        {
            return;
        }

        FindNearestNeighbour(point, child, distanceFunc, ref best, ref bestDistance);
    }

    //Range
    public List<T> FindNeighbours(T point, double distance, Func<T, T, double> distanceFunc = null)
    {
        var result = new List<T>();

        if (this.root == null)
        {
            return result;
        }

        distanceFunc ??= GetDefaultDistance;

        FindNeighbours(point, distance, this.root, distanceFunc, result);

        return result;
    }

    private void FindNeighbours(
        T point,
        double radius,
        BalancedKdTreeNode<T> node,
        Func<T, T, double> distanceFunc,
        List<T> result)
    {
        // the point held at this node is part of the answer too. Leaving it out is what
        // made this query under-report: only whole subtrees falling inside the radius
        // ever contributed, so every node the search descended through was skipped.
        if (distanceFunc(point, node.Point) <= radius)
        {
            result.Add(node.Point);
        }

        CollectNeighboursFromChild(point, radius, node.LeftChild, distanceFunc, result);

        CollectNeighboursFromChild(point, radius, node.RightChild, distanceFunc, result);
    }

    private void CollectNeighboursFromChild(
        T point,
        double radius,
        BalancedKdTreeNode<T> child,
        Func<T, T, double> distanceFunc,
        List<T> result)
    {
        if (child == null || child.IsNilNode())
        {
            return;
        }

        var relation = SpatialUtility.GetAxisAlignedRectangleRelationToCircle(PointFunc(point), radius, child.MinimumBoundingBox);

        if (relation == SpatialRelation.Contained)
        {
            AddAllValues(child, result);
        }
        else if (relation == SpatialRelation.Intersects)
        {
            FindNeighbours(point, radius, child, distanceFunc, result);
        }
    }

    public List<T> GetAllValues()
    {
        var result = new List<T>();

        if (this.root != null)
        {
            AddAllValues(this.root, result);
        }

        return result;
    }

    private void AddAllValues(BalancedKdTreeNode<T> node, List<T> result)
    {
        result.Add(node.Point);

        if (node.LeftChild != null && !node.LeftChild.IsNilNode())
        {
            AddAllValues(node.LeftChild, result);
        }

        if (node.RightChild != null && !node.RightChild.IsNilNode())
        {
            AddAllValues(node.RightChild, result);
        }
    }
}
