using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Common.DataStructures.Trees;

public class BTree<T> where T : IComparable
{
    public BTreeNode<T> Root;

    public BTree(BTreeNode<T> root, int minimumDegree)
    {
        if (minimumDegree < 2)
        {
            throw new NotImplementedException();
        }

        this.Root = root;

        this.Root.IsLeaf = true;

        this.minimumDegree = minimumDegree;
    }

    int minimumDegree;

    public int MinimumDegree
    {
        get { return this.minimumDegree; }
    }

    public BTreeNode<T> Search(BTreeNode<T> parent, T key, out int resultKeyIndex)
    {
        resultKeyIndex = 1;

        while (resultKeyIndex < parent.NumberOfKeys && key.CompareTo(parent.Keys[resultKeyIndex]) > 0)
        {
            resultKeyIndex++;
        }

        if (resultKeyIndex < parent.NumberOfKeys && key.CompareTo(parent.Keys[resultKeyIndex]) == 0)
        {
            return parent;
        }
        if (parent.IsLeaf)
        {
            resultKeyIndex = -1;
            return null;
        }

        return Search(parent.pointers[resultKeyIndex], key, out resultKeyIndex);
    }

    public void Insert(T key)
    {
        BTreeNode<T> r = this.Root;

        if (r.NumberOfKeys == 2 * this.MinimumDegree - 1)
        {
            BTreeNode<T> s = new BTreeNode<T>();

            this.Root = s;

            s.IsLeaf = false;

            s.pointers.Add(r);

            SplitTheNode(s, 0);

            InsertNonFull(s, key);
        }
        else
        {
            InsertNonFull(r, key);
        }
    }

    public void SplitTheNode(BTreeNode<T> parent, int childIndex)
    {
        BTreeNode<T> y = parent.pointers[childIndex];

        //int t = y.pointers.Count / 2;
        int t = this.MinimumDegree;

        BTreeNode<T> z = new BTreeNode<T>();

        z.IsLeaf = y.IsLeaf;

        //for (int i = 0; i < t - 1; i++)
        //{
        //    z.Keys.Add(y.Keys[i + t]);
        //}
        z.Keys.AddRange(y.Keys.Skip<T>(t));

        if (!y.IsLeaf)
        {
            //for (int i = 0; i < t; i++)
            //{
            //    z.pointers[i] = y.pointers[i + t];
            //}
            z.pointers.AddRange(y.pointers.Skip<BTreeNode<T>>(t));
            //y.pointers.RemoveRange(t, y.pointers.Count - t);
            y.pointers.RemoveRange(t, t);
        }

        //for (int i = parent.NumberOfKeys; i >= childIndex + 1; i--)
        //{
        //    parent.pointers[i + 1] = parent.pointers[i];
        //}

        //parent.pointers[childIndex] = z;
        parent.pointers.Insert(childIndex + 1, z);
        //for (int i = parent.NumberOfKeys - 1; i >= childIndex; i--)
        //{
        //    parent.Keys[i + 1] = parent.Keys[i];
        //}

        //parent.Keys[childIndex] = y.Keys[t - 1];
        parent.Keys.Insert(childIndex, y.Keys[t - 1]);

        //y.NumberOfKeys = t - 1;
        //y.Keys.RemoveRange(t - 1, y.Keys.Count - t + 1);
        y.Keys.RemoveRange(t - 1, t);

    }

    public void InsertNonFull(BTreeNode<T> x, T k)
    {
        int i = x.NumberOfKeys;

        if (x.IsLeaf)
        {
            while (i > 0 && k.CompareTo(x.Keys[i - 1]) < 0)
            {
                i--;
            }

            x.Keys.Insert(i, k);
        }
        else
        {
            while (i > 0 && k.CompareTo(x.Keys[i - 1]) < 0)
            {
                i--;
            }

            //   i++;

            if (x.pointers[i].NumberOfKeys == 2 * this.MinimumDegree - 1)
            {
                SplitTheNode(x, i);

                if (k.CompareTo(x.Keys[i]) > 0)
                {
                    i++;
                }
            }

            InsertNonFull(x.pointers[i], k);
        }
    }

}
