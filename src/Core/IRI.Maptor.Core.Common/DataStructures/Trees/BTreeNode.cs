using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Common.DataStructures.Trees;

public class BTreeNode<T> where T : IComparable
{
    //private int numberOfKeys;

    //public int NumberOfKeys { get; set; }
    public int NumberOfKeys { get { return this.Keys.Count; } }

    public bool IsLeaf { get; set; }

    public List<T> Keys;

    public List<BTreeNode<T>> pointers;

    public BTreeNode()
    {
        this.Keys = new List<T>();

        this.pointers = new List<BTreeNode<T>>();

        this.IsLeaf = true;
    }

    public override string ToString()
    {
        string result = string.Empty;

        foreach (T item in Keys)
        {
            result += string.Format("{0}, ", item.ToString());
        }

        if (result.Length > 0)
        {
            result.Remove(result.Length - 2, 2);
        }

        return result;
    }
}
