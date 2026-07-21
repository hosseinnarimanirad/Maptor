using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.DataStructures;

public class MinBinaryHeap<T> : IBinaryHeap<T> where T : IComparable<T>
{
    public T[] values;

    private int pointer;

    public int Length
    {
        get { return this.pointer; }
    }

    public MinBinaryHeap(T[] array)
    {
        values = new T[array.Length];

        Array.Copy(array, values, array.Length);

        pointer = array.Length;

        for (int i = pointer / 2 - 1; i >= 0; i--)
        {
            SiftDown(i);
        }
    }

    public T ReleaseValue()
    {
        if (pointer == 0)
            throw new InvalidOperationException("the heap is empty");

        T result = values[0];

        pointer--;

        values[0] = values[pointer];

        values[pointer] = default!;

        SiftDown(0);

        return result;
    }

    private void SiftDown(int index)
    {
        while (true)
        {
            int left = 2 * index + 1;

            if (left >= pointer)
                return;

            int child = left;

            int right = left + 1;

            if (right < pointer && values[right].CompareTo(values[left]) < 0)
                child = right;

            if (values[child].CompareTo(values[index]) >= 0)
                return;

            T temp = values[index];

            values[index] = values[child];

            values[child] = temp;

            index = child;
        }
    }
}
