namespace IRI.Maptor.Core.Spatial.AdvancedStructures;

/// <summary>
/// The plain k-d tree: it cycles through the supplied comparers, one per level, and hangs each
/// new point wherever it falls off the tree. Nothing rebalances, so its shape follows insertion
/// order — sorted input degenerates it into a linked list. Use <see cref="BalancedKdTree{T}"/>
/// for anything that has to answer queries.
/// </summary>
public class KdTree<T>
{
    public KdTreeNode<T> Root { get; set; }

    List<Func<T, T, int>> comparers;

    public KdTree(T[] values, List<Func<T, T, int>> comparers)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        if (comparers is null || comparers.Count == 0)
        {
            throw new ArgumentException("At least one comparer is required.", nameof(comparers));
        }

        this.comparers = comparers;

        if (values.Length == 0)
        {
            return;
        }

        this.Root = new KdTreeNode<T>(values[0]);

        for (int i = 1; i < values.Length; i++)
        {
            Insert(values[i]);
        }
    }

    private void Insert(T value)
    {
        Add(this.Root, value);
    }

    /// <remarks>
    /// Iterative, and the depth is threaded through rather than recomputed. Recursion would
    /// overflow the stack on exactly the input this class is documented to handle badly —
    /// sorted points, where the depth reaches n — and deriving the depth by walking back up
    /// to the root at every step made a single insertion cost O(depth^2).
    /// </remarks>
    private void Add(KdTreeNode<T> parent, T childValue)
    {
        int depth = 0;

        while (true)
        {
            var comparer = this.comparers[depth % this.comparers.Count];

            if (comparer(parent.Point, childValue) >= 0)
            {
                if (parent.LeftChild != null)
                {
                    parent = parent.LeftChild;

                    depth++;

                    continue;
                }

                parent.LeftChild = new KdTreeNode<T>(childValue);

                return;
            }
            else
            {
                if (parent.RightChild != null)
                {
                    parent = parent.RightChild;

                    depth++;

                    continue;
                }

                parent.RightChild = new KdTreeNode<T>(childValue);

                return;
            }
        }
    }

    public int GetNodeLevel(KdTreeNode<T> node)
    {
        int level = 0;

        while (node?.Parent != null)
        {
            level++;

            node = node.Parent;
        }

        return level;
    }
}
