// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;


namespace IRI.Maptor.Core.Common.DataStructures;

//Not implemented
public class DisjointSet<T>
{
    public List<LinkedList<T>> values;

    public DisjointSet(T[] nodes)
    {
        this.values = new List<LinkedList<T>>();

        for (int i = 0; i < nodes.Length; i++)
        {
            LinkedList<T> temp = new LinkedList<T>();

            temp.AddFirst(nodes[i]);

            this.values.Add(temp);
        }
    }

    public DisjointSet(List<T> nodes)
    {
        this.values = new List<LinkedList<T>>();

        for (int i = 0; i < nodes.Count; i++)
        {
            LinkedList<T> temp = new LinkedList<T>();

            temp.AddFirst(nodes[i]);

            this.values.Add(temp);
        }
    }

    public DisjointSet(List<List<T>> nodes)
    {
        this.values = new List<LinkedList<T>>();

        for (int i = 0; i < nodes.Count; i++)
        {
            LinkedList<T> temp = new LinkedList<T>();

            foreach (T item in nodes[i])
            {
                temp.AddFirst(item);
            }

            this.values.Add(temp);
        }
    }

    public DisjointSet(List<LinkedList<T>> nodes)
    {

    }

    public void Join(DisjointSet<T> firstSet, DisjointSet<T> secondSet)
    {
        //if (!(this.values.Contains(firstSet) && this.values.Contains(secondSet)))
        //{

        //}
    }
}
