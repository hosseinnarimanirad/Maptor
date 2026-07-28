using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.DataStructures.Trees;

public class RedBlackNode<T> where T : IComparable
{
    public static RedBlackNode<T> nilNode = new RedBlackNode<T>();

    public NodeColor Color { get; set; }

    public T Key { get; set; }

    private RedBlackNode<T> _leftChild, _rightChild;

    public RedBlackNode<T> LeftChild
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

    public RedBlackNode<T> RightChild
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

    public RedBlackNode<T> Parent { get; set; }

    private RedBlackNode()
    {
        this.Key = default(T);

        this.Color = NodeColor.Black;
    }

    public RedBlackNode(T key, NodeColor color)
    {
        this.Color = color;

        this.Key = key;

        this.LeftChild = nilNode;

        this.RightChild = nilNode;
    }

    public override string ToString()
    {
        return string.Format("Key = '{0}', Color = '{3}', Left = '{1}', Right = '{2}'",
            Key.ToString(),
            LeftChild == null ? string.Empty : LeftChild.Key.ToString(),
            RightChild == null ? string.Empty : RightChild.Key.ToString(),
            this.Color.ToString());
    }
}
