namespace IRI.Maptor.Core.Spatial.AdvancedStructures;

public class KdTreeNode<T>
{
    public T Point { get; set; }

    protected KdTreeNode<T>? _leftChild, _rightChild, _parent;

    public KdTreeNode<T> LeftChild
    {
        get { return this._leftChild; }

        set
        {
            this._leftChild = value;

            if (value != null)
            {
                value.Parent = this;
            }
        }
    }

    public KdTreeNode<T> RightChild
    {
        get { return this._rightChild; }

        set
        {
            this._rightChild = value;

            if (value != null)
            {
                value.Parent = this;
            }
        }
    }

    public KdTreeNode<T> Parent
    {
        get { return this._parent; }

        set
        {
            this._parent = value;
        }
    }

    public KdTreeNode(T point)
    {
        this.Point = point;
    }

    public override string ToString()
    {
        return string.Format("Key = '{0}', Left = '{1}', Right = '{2}'",
            Point?.ToString(),
            LeftChild == null ? string.Empty : LeftChild.Point?.ToString(),
            RightChild == null ? string.Empty : RightChild.Point?.ToString());
    }
}
