using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.DataStructures.Trees;

namespace IRI.Maptor.Sta.Spatial.AdvancedStructures;

public class BalancedKdTreeNode<T>
{  
    public T Point { get; set; }

    //public static BalancedKdTreeNode<T> nilNode = new BalancedKdTreeNode<T>(default(T));

    public NodeColor Color { get; set; }

    protected BalancedKdTreeNode<T> leftChild, rightChild, parent;

    public BalancedKdTreeNode<T> LeftChild
    {
        get { return this.leftChild; }

        set
        {
            if (value != null)
            {
                this.leftChild = value;

                if (!value.IsNilNode())
                {
                    value.Parent = this;
                }
            }

            //RepairBoundingBox();

        }
    }

    public BalancedKdTreeNode<T> RightChild
    {
        get { return this.rightChild; }

        set
        {
            if (value != null)
            {
                this.rightChild = value;

                if (!value.IsNilNode())
                {
                    value.Parent = this;
                }
            }

            //RepairBoundingBox();

        }
    }

    private void RepairBoundingBox()
    {
        var pointValue = BalancedKdTree<T>.PointFunc(Point);

        this.MinimumBoundingBox = new BoundingBox(pointValue.X, pointValue.Y, pointValue.X, pointValue.Y);

        if (LeftChild != null && !LeftChild.IsNilNode())
        {
            this.MinimumBoundingBox = this.MinimumBoundingBox.Add(LeftChild.MinimumBoundingBox);
        }
        if (RightChild != null && !RightChild.IsNilNode())
        {
            this.MinimumBoundingBox = this.MinimumBoundingBox.Add(RightChild.MinimumBoundingBox);
        }

        if (Parent != null)
        {
            Parent.RepairBoundingBox();
        }

    }

    public BalancedKdTreeNode<T> Parent
    {
        get { return this.parent; }

        set
        {
            this.parent = value;

            this.parent?.RepairBoundingBox();
        }
    }


    private BoundingBox _minimumBoundingBox;
    public BoundingBox MinimumBoundingBox
    {
        get { return _minimumBoundingBox; }
        set
        {
            this._minimumBoundingBox = value;
        }
    }

    private BalancedKdTreeNode(T point)
    {
        this.Point = point;

        this.Color = NodeColor.Black;
    }

    public BalancedKdTreeNode(T point, NodeColor color)
    {
        this.Point = point;

        this.Color = color;

        this.LeftChild = BalancedKdTree<T>.NilNode;
        this.RightChild = BalancedKdTree<T>.NilNode;

        var pointValue = BalancedKdTree<T>.PointFunc(point);

        this.MinimumBoundingBox = new BoundingBox(pointValue.X, pointValue.Y, pointValue.X, pointValue.Y);
    }

    internal static BalancedKdTreeNode<T> CreateNilNode(T nilValue)
    {
        return new BalancedKdTreeNode<T>(nilValue);
    }

    public override string ToString()
    {
        return string.Format("Key = '{0}', Left = '{1}', Rigth = '{2}'",
            Point.ToString(),
            LeftChild == null ? string.Empty : LeftChild.Point.ToString(),
            RightChild == null ? string.Empty : RightChild.Point.ToString());
    }

    public bool IsNilNode()
    {
        return this == BalancedKdTree<T>.NilNode;
    }

    public BoundingBox CalculateBoundingBox()
    {
        BoundingBox result = this.MinimumBoundingBox;

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

    //public static void SetNilNode(T nilNodevalue)
    //{
    //    nilNode = new BalancedKdTreeNode<T>(nilNodevalue);
    //}
}
