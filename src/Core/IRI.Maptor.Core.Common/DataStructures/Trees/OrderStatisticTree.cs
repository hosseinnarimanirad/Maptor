using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.DataStructures.Trees;

public class OrderStatisticTree<T> where T : IComparable
{
    private int count;

    private OrderStatisticNode<T> root;

    public OrderStatisticNode<T> Root
    {
        get { return this.root; }
        set { this.root = value; this.root.Parent = null; }
    }

    public OrderStatisticTree()
    {

    }

    public OrderStatisticNode<T> Select(OrderStatisticNode<T> subtreeRoot, int i)
    {
        int r = subtreeRoot.LeftChild.Size + 1;

        if (r == i)
        {
            return subtreeRoot;
        }
        else if (i < r)
        {
            return Select(subtreeRoot.LeftChild, i);
        }
        else
        {
            return Select(subtreeRoot.RightChild, i - r);
        }
    }

    public int Rank(OrderStatisticNode<T> node)
    {
        int r = node.LeftChild.Size + 1;

        OrderStatisticNode<T> temp = node;

        while (!object.ReferenceEquals(this.Root, temp))
        {
            if (object.ReferenceEquals(temp, temp.Parent.RightChild))
            {
                r += temp.Parent.LeftChild.Size + 1;
            }

            temp = temp.Parent;
        }

        return r;
    }


    public OrderStatisticTree(T[] values)
    {
        if (values == null)
            return;

        this.count = values.Length;

        this.Root = new OrderStatisticNode<T>(values[0], NodeColor.Black);
        //this.Parent.LeftChild = nilNode;
        //this.Parent.RightChild = nilNode;

        for (int i = 1; i < values.Length; i++)
        {
            OrderStatisticNode<T> node = new OrderStatisticNode<T>(values[i], NodeColor.Red) { Parent = this.Root };

            //node.LeftChild = nilNode;

            //node.RightChild = nilNode;

            Add(this.Root, node);

            InsertFixup(node);
        }
    }

    private static void Add(OrderStatisticNode<T> parent, OrderStatisticNode<T> node)
    {
        parent.Size++;

        if (parent.Key.CompareTo(node.Key) >= 0)
        {
            if (parent.LeftChild != OrderStatisticNode<T>.nilNode)
            {
                Add(parent.LeftChild, node);
            }
            else
            {
                parent.LeftChild = node;
            }
        }
        else
        {
            if (parent.RightChild != OrderStatisticNode<T>.nilNode)
            {
                Add(parent.RightChild, node);
            }
            else
            {
                parent.RightChild = node;
            }
        }
    }

    public void Insert(T value)
    {
        this.count++;

        OrderStatisticNode<T> node = new OrderStatisticNode<T>(value, NodeColor.Red) { Parent = this.Root };

        //node.LeftChild = nilNode;

        //node.RightChild = nilNode;

        Add(this.Root, node);

        InsertFixup(node);
    }

    private void InsertFixup(OrderStatisticNode<T> node)
    {
        while (node.Parent.Color == NodeColor.Red)
        {
            if (object.ReferenceEquals(node.Parent, node.Parent.Parent.LeftChild))
            {
                OrderStatisticNode<T> y = node.Parent.Parent.RightChild;

                if (y.Color == NodeColor.Red)
                {
                    node.Parent.Color = NodeColor.Black;

                    y.Color = NodeColor.Black;

                    node.Parent.Parent.Color = NodeColor.Red;

                    node = node.Parent.Parent;
                }
                else if (object.ReferenceEquals(node, node.Parent.RightChild))
                {
                    node = node.Parent;

                    LeftRotate(node);
                }
                else
                {
                    node.Parent.Color = NodeColor.Black;

                    node.Parent.Parent.Color = NodeColor.Red;

                    RightRotate(node.Parent.Parent);
                }
            }
            else if (object.ReferenceEquals(node.Parent, node.Parent.Parent.RightChild))
            {
                OrderStatisticNode<T> y = node.Parent.Parent.LeftChild;

                if (y.Color == NodeColor.Red)
                {
                    node.Parent.Color = NodeColor.Black;

                    y.Color = NodeColor.Black;

                    node.Parent.Parent.Color = NodeColor.Red;

                    node = node.Parent.Parent;
                }
                else if (object.ReferenceEquals(node, node.Parent.LeftChild))
                {
                    node = node.Parent;

                    RightRotate(node);
                }
                else
                {
                    node.Parent.Color = NodeColor.Black;

                    node.Parent.Parent.Color = NodeColor.Red;

                    LeftRotate(node.Parent.Parent);
                }
            }
            if (node.Parent == null)
            {
                break;
            }
        }

        this.Root.Color = NodeColor.Black;
    }

    public void LeftRotate(OrderStatisticNode<T> node)
    {
        if (node.LeftChild == null || node.RightChild == null)
        {
            throw new NotImplementedException();
        }

        OrderStatisticNode<T> rightChild = node.RightChild;

        node.RightChild = rightChild.LeftChild;

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

        rightChild.Size = node.Size;

        node.Size = node.LeftChild.Size + node.RightChild.Size + 1;
    }

    public void RightRotate(OrderStatisticNode<T> node)
    {
        if (node.LeftChild == null || node.RightChild == null)
        {
            throw new NotImplementedException();
        }

        OrderStatisticNode<T> leftChild = node.LeftChild;

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

        leftChild.Size = node.Size;

        node.Size = node.LeftChild.Size + node.RightChild.Size + 1;
    }
}
