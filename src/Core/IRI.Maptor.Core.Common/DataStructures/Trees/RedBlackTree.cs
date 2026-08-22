using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.DataStructures.Trees;

public class RedBlackTree<T> where T : IComparable
{
    private int count;

    private RedBlackNode<T> root;

    public RedBlackNode<T> Root
    {
        get { return this.root; }
        set
        {
            this.root = value;

            this.root.Parent = null;
        }
    }

    public static RedBlackNode<T> nilNode = RedBlackNode<T>.nilNode;

    public RedBlackTree(T parentValue)
    {
        this.count = 1;

        this.Root = new RedBlackNode<T>(parentValue, NodeColor.Black);

    }

    public RedBlackTree(T[] values)
    {
        if (values == null)
            return;

        this.count = values.Length;

        this.Root = new RedBlackNode<T>(values[0], NodeColor.Black);
        //this.Root.LeftChild = nilNode;
        //this.Root.RightChild = nilNode;

        for (int i = 1; i < values.Length; i++)
        {
            RedBlackNode<T> node = new RedBlackNode<T>(values[i], NodeColor.Red) { Parent = this.Root };

            //node.LeftChild = nilNode;

            //node.RightChild = nilNode;

            Add(this.Root, node);

            InsertFixup(node);
        }
    }

    private static void Add(RedBlackNode<T> parent, RedBlackNode<T> node)
    {
        if (parent.Key.CompareTo(node.Key) >= 0)
        {
            if (parent.LeftChild != nilNode)
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
            if (parent.RightChild != nilNode)
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

        RedBlackNode<T> node = new RedBlackNode<T>(value, NodeColor.Red) { Parent = this.Root };

        Add(this.Root, node);

        InsertFixup(node);
    }

    private void InsertFixup(RedBlackNode<T> node)
    {
        while (node.Parent.Color == NodeColor.Red)
        {
            if (object.ReferenceEquals(node.Parent, node.Parent.Parent.LeftChild))
            {
                RedBlackNode<T> y = node.Parent.Parent.RightChild;

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
                RedBlackNode<T> y = node.Parent.Parent.LeftChild;

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

    public void LeftRotate(RedBlackNode<T> node)
    {
        if (node.LeftChild == null || node.RightChild == null)
        {
            throw new NotImplementedException();
        }

        RedBlackNode<T> rightChild = node.RightChild;

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
    }

    public void RightRotate(RedBlackNode<T> node)
    {
        if (node.LeftChild == null || node.RightChild == null)
        {
            throw new NotImplementedException();
        }

        RedBlackNode<T> leftChild = node.LeftChild;

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
    }
}
