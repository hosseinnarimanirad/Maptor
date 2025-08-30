namespace IRI.Maptor.Sta.Spatial.AdvancedStructures;

public class KdTreeNode<T>
{
    public T Point { get; set; }

    //public Func<T, T, int> comparer;
    
    protected KdTreeNode<T>? _leftChild, _rightChild, _parent;

    public KdTreeNode<T> LeftChild
    {
        get { return this._leftChild; }

        set
        {
            if (value != null)
            {
                this._leftChild = value;

                value.Parent = this;
            }
        }
    }

    public KdTreeNode<T> RigthChild
    {
        get { return this._rightChild; }

        set
        {
            if (value != null)
            {
                this._rightChild = value;

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

        //this.comparer = comparer;
    }

    public override string ToString()
    {
        return string.Format("Key = '{0}', Left = '{1}', Rigth = '{2}'",
            Point.ToString(),
            LeftChild == null ? string.Empty : LeftChild.Point.ToString(),
            RigthChild == null ? string.Empty : RigthChild.Point.ToString());
    }
}
