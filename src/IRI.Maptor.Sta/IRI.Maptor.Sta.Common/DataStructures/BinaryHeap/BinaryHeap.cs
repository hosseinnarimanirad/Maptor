using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Sta.DataStructures;

public class BinaryHeap<T>
{
    public T[] values;

    private int pointer;

    public int Length
    {
        get { return this.pointer; }
    }

    Func<T, T, int> comparer;

    public BinaryHeap(T[] array, Func<T, T, int> comparer)
    {
        this.comparer = comparer;

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

    public T SeekValue()
    {
        return this.values[0];
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

            if (right < pointer && comparer(values[right], values[left]) > 0)
                child = right;

            if (comparer(values[child], values[index]) <= 0)
                return;

            T temp = values[index];

            values[index] = values[child];

            values[child] = temp;

            index = child;
        }
    }
}
