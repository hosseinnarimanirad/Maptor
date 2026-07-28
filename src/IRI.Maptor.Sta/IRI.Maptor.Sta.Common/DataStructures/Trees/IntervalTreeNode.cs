using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.DataStructures.Trees;

public class IntervalTreeNode<T> where T : IComparable
{
    public static IntervalTreeNode<T> nilNode = new IntervalTreeNode<T>();

    public NodeColor Color { get; set; }

    public T Low { get; set; }

    public T High { get; set; }

    public T Max { get; set; }

    private IntervalTreeNode<T> leftChild, rightChild;

    public IntervalTreeNode<T> LeftChild
    {
        get { return this.leftChild; }

        set
        {
            if (value != null)
            {
                this.leftChild = value;

                value.Parent = this;

                if (this.Max.CompareTo(value.Max) < 0)
                {
                    this.Max = value.Max;
                }
            }
        }
    }

    public IntervalTreeNode<T> RightChild
    {
        get { return this.rightChild; }

        set
        {
            if (value != null)
            {
                this.rightChild = value;

                value.Parent = this;

                if (this.Max.CompareTo(value.Max) < 0)
                {
                    this.Max = value.Max;
                }
            }
        }
    }

    public IntervalTreeNode<T> Parent { get; set; }

    private IntervalTreeNode()
    {
        this.Low = default(T);

        this.High = default(T);

        this.Max = default(T);

        this.Color = NodeColor.Black;
    }

    public IntervalTreeNode(T low, T high, NodeColor color)
    {
        this.Color = color;

        this.Low = low;

        this.High = high;

        this.Max = high;

        this.LeftChild = nilNode;

        this.RightChild = nilNode;
    }

    public override string ToString()
    {
        return string.Format("[{0},{1}], Max = '{2}', Color = '{3}', Left = [{4}], Right = [{5}]",
            Low.ToString(),
            High.ToString(),
            Max.ToString(),
            this.Color.ToString(),
            LeftChild == null ? string.Empty : string.Format("{0},{1}", LeftChild.Low.ToString(), LeftChild.High.ToString()),
            RightChild == null ? string.Empty : string.Format("{0},{1}", RightChild.Low.ToString(), RightChild.High.ToString())
            );
    }
}
