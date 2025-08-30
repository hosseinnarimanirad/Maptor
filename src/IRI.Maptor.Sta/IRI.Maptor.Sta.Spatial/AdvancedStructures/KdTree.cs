namespace IRI.Maptor.Sta.Spatial.AdvancedStructures;

public class KdTree<T>
{
    public KdTreeNode<T> Root { get; set; }

    List<Func<T, T, int>> comparers;

    public KdTree(T[] values, List<Func<T, T, int>> comparers)
    {
        if (values.Equals(null) || comparers.Equals(null))
        {
            return;
        }

        this.Root = new KdTreeNode<T>(values[0]);

        this.comparers = comparers;

        for (int i = 1; i < values.Length; i++)
        {
            Insert(values[i]);
        }
    }

    private void Insert(T value)
    {
        Add(this.Root, value);
    }

    private void Add(KdTreeNode<T> parent, T childValue)
    {
        int functionIndex = GetNodeLevel(parent) % comparers.Count;

        if (this.comparers[functionIndex](parent.Point, childValue) >= 0)
        {
            if (parent.LeftChild != null)
            {
                Add(parent.LeftChild, childValue);
            }
            else
            {
                //int nodeLevel = 1 + GetNodeLevel(parent);

                parent.LeftChild = new KdTreeNode<T>(childValue);
            }
        }
        else
        {
            if (parent.RigthChild != null)
            {
                Add(parent.RigthChild, childValue);
            }
            else
            {
                //int nodeLevel = 1 + GetNodeLevel(parent);

                parent.RigthChild = new KdTreeNode<T>(childValue);
            }
        }

    }

    public int GetNodeLevel(KdTreeNode<T> node)
    {
        if (object.Equals(null, node.Parent))
        {
            return 0;
        }
        else
        {
            return GetNodeLevel(node.Parent) + 1;
        }
    }
}
