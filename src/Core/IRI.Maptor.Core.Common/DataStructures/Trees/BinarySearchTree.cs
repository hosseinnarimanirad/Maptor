using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.DataStructures.Trees;

public class BinarySearchTree<T> where T : IComparable
{
    private int count;

    List<T> treeWalk;

    public BinarySearchNode<T> Parent { get; set; }

    public BinarySearchTree(T[] values)
    {
        if (values == null)
            return;

        this.count = values.Length;

        this.Parent = new BinarySearchNode<T>(values[0]);

        for (int i = 1; i < values.Length; i++)
        {
            Add(this.Parent, values[i]);
        }
    }

    private static void Add(BinarySearchNode<T> parent, T childValue)
    {
        if (parent.Key.CompareTo(childValue) >= 0)
        {
            if (parent.LeftChild != null)
            {
                Add(parent.LeftChild, childValue);
            }
            else
            {
                parent.LeftChild = new BinarySearchNode<T>(childValue) { Parent = parent };
            }
        }
        else
        {
            if (parent.RightChild != null)
            {
                Add(parent.RightChild, childValue);
            }
            else
            {
                parent.RightChild = new BinarySearchNode<T>(childValue) { Parent = parent };
            }
        }
    }

    public T[] InorderTreeWalk
    {
        get
        {
            this.treeWalk = new List<T>(count);

            GetInorderTreeWalk(this.Parent);

            return this.treeWalk.ToArray();
        }
    }

    //preorder tree walk prints the root before the values in either subtree
    public T[] PreorderTreeWalk
    {
        get
        {
            this.treeWalk = new List<T>(count);

            GetPreorderTreeWalk(this.Parent);

            return this.treeWalk.ToArray();
        }
    }

    //postorder tree walk prints the root after the values in its subtrees
    public T[] PostorderTreeWalk
    {
        get
        {
            this.treeWalk = new List<T>(count);

            GetPostorderTreeWalk(this.Parent);

            return this.treeWalk.ToArray();
        }
    }

    private void GetInorderTreeWalk(BinarySearchNode<T> node)
    {
        if (node.LeftChild != null)
        {
            GetInorderTreeWalk(node.LeftChild);
        }

        treeWalk.Add(node.Key);

        if (node.RightChild != null)
        {
            GetInorderTreeWalk(node.RightChild);
        }
    }

    private void GetPreorderTreeWalk(BinarySearchNode<T> node)
    {
        treeWalk.Add(node.Key);

        if (node.LeftChild != null)
        {
            GetPreorderTreeWalk(node.LeftChild);
        }

        if (node.RightChild != null)
        {
            GetPreorderTreeWalk(node.RightChild);
        }
    }

    private void GetPostorderTreeWalk(BinarySearchNode<T> node)
    {
        if (node.LeftChild != null)
        {
            GetPostorderTreeWalk(node.LeftChild);
        }

        if (node.RightChild != null)
        {
            GetPostorderTreeWalk(node.RightChild);
        }

        treeWalk.Add(node.Key);
    }

    
    public BinarySearchNode<T> Search(T value)
    {
        return TreeOperation.LookFor(this.Parent, value);
    }

    public BinarySearchNode<T> Minimum
    {
        get { return TreeOperation.GetMinimum(this.Parent); }
    }

    public BinarySearchNode<T> Maximum
    {
        get { return TreeOperation.GetMaximum(this.Parent); }
    }

    public BinarySearchNode<T> GetSuccessor(BinarySearchNode<T> node)
    {
        return TreeOperation.GetSuccessor(node);
    }

    public BinarySearchNode<T> GetPredecessor(BinarySearchNode<T> node)
    {
        return TreeOperation.GetPredecessor(node);
    }

    //public virtual void Insert(BinarySearchTreeNode<T> newNode)
    //{
    //    this.count++;

    //    Add(this.Parent, newNode);
    //}

    public virtual void Insert(T value)
    {
        this.count++;

        Add(this.Parent, value);
    }

    public virtual void Delete(BinarySearchNode<T> newNode)
    {

    }

}
