// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.DataStructures;

public static class SortAlgorithm
{
    public static void BubbleSort<T>(T[] array, Func<T, T, int> comparer)
    {
        for (int i = 0; i < array.Length - 1; i++)
        {
            bool swapped = false;

            for (int j = 0; j < array.Length - i - 1; j++)
            {
                if (comparer(array[j], array[j + 1]) > 0)
                {
                    var temp = array[j];

                    array[j] = array[j + 1];

                    array[j + 1] = temp;

                    swapped = true;
                }
            }

            if (!swapped)
                break;
        }
    }

    public static T[] Heapsort<T>(T[] array, SortDirection direction) where T : IComparable<T>
    {
        T[] result = new T[array.Length];

        if (array.Length <= 1)
        {
            Array.Copy(array, result, array.Length);

            return result;
        }

        IBinaryHeap<T> heap = CreateHeap(array, direction);

        int counter = 0;

        while (heap.Length != 0)
        {
            result[counter] = heap.ReleaseValue();

            counter++;
        }

        return result;
    }

    public static void Heapsort<T>(ref T[] array, SortDirection direction) where T : IComparable<T>
    {
        if (array.Length <= 1)
            return;

        IBinaryHeap<T> heap = CreateHeap(array, direction);

        int counter = 0;

        while (heap.Length != 0)
        {
            array[counter] = heap.ReleaseValue();

            counter++;
        }
    }

    public static void Heapsort<T>(ref List<T> array, SortDirection direction) where T : IComparable<T>
    {
        if (array.Count <= 1)
            return;

        IBinaryHeap<T> heap = CreateHeap(array.ToArray(), direction);

        int counter = 0;

        while (heap.Length != 0)
        {
            array[counter] = heap.ReleaseValue();

            counter++;
        }
    }

    private static IBinaryHeap<T> CreateHeap<T>(T[] array, SortDirection direction) where T : IComparable<T>
    {
        // a min-heap releases smallest-first, which yields ascending output
        if (direction == SortDirection.Ascending)
        {
            return new MinBinaryHeap<T>(array);
        }
        else
        {
            return new MaxBinaryHeap<T>(array);
        }
    }

    /// <summary>
    /// Sorts in ascending comparer order and returns the result as a new array.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="array"></param>
    /// <param name="comparer">any negative/zero/positive result is honored</param>
    /// <returns></returns>
    public static T[] Heapsort<T>(T[] array, Func<T, T, int> comparer)
    {
        T[] result = new T[array.Length];

        if (array.Length <= 1)
        {
            Array.Copy(array, result, array.Length);

            return result;
        }

        BinaryHeap<T> heap = new BinaryHeap<T>(array, comparer);

        // the heap releases largest-first; filling from the end yields ascending order
        int counter = array.Length - 1;

        while (heap.Length != 0)
        {
            result[counter] = heap.ReleaseValue();

            counter--;
        }

        return result;
    }

    /// <summary>
    /// Sorts in ascending comparer order and returns the result as a new
    /// array; the input array is not modified. Stable.
    /// </summary>
    public static T[] MergeSort<T>(T[] array, Func<T, T, int> comparer)
    {
        T[] result = new T[array.Length];

        Array.Copy(array, result, array.Length);

        if (result.Length > 1)
        {
            MergeSortCore(result, new T[result.Length], 0, result.Length, comparer);
        }

        return result;
    }

    /// <summary>
    /// Counts the pairs (i, j) with i &lt; j and array[i] &gt; array[j]
    /// according to the comparer; the input array is not modified.
    /// </summary>
    public static long CountInversions<T>(T[] array, Func<T, T, int> comparer)
    {
        if (array.Length <= 1)
            return 0;

        T[] copy = new T[array.Length];

        Array.Copy(array, copy, array.Length);

        return MergeSortCore(copy, new T[copy.Length], 0, copy.Length, comparer);
    }

    /// <summary>
    /// Sorts array[start..end) using a shared scratch buffer and returns the
    /// number of inversions in the range.
    /// </summary>
    private static long MergeSortCore<T>(T[] array, T[] scratch, int start, int end, Func<T, T, int> comparer)
    {
        if (end - start <= 1)
            return 0;

        int middle = start + (end - start) / 2;

        long count = MergeSortCore(array, scratch, start, middle, comparer)
                   + MergeSortCore(array, scratch, middle, end, comparer);

        int left = start, right = middle, target = start;

        while (left < middle && right < end)
        {
            // taking from the left run on ties keeps the sort stable
            if (comparer(array[left], array[right]) <= 0)
            {
                scratch[target++] = array[left++];
            }
            else
            {
                count += middle - left;

                scratch[target++] = array[right++];
            }
        }

        while (left < middle)
        {
            scratch[target++] = array[left++];
        }

        while (right < end)
        {
            scratch[target++] = array[right++];
        }

        Array.Copy(scratch, start, array, start, end - start);

        return count;
    }

    public static void QuickSort<T>(T[] array, Func<T, T, int> comparer)
    {
        QuickSort(array, comparer, 0, array.Length - 1);
    }

    private static void QuickSort<T>(T[] array, Func<T, T, int> comparer, int startIndex, int endIndex)
    {
        // recurse into the smaller partition and loop on the larger one, so
        // the stack depth stays O(log n) even for adversarial inputs
        while (startIndex < endIndex)
        {
            int q = PartitionWithMedianElement(array, comparer, startIndex, endIndex);

            if (q - startIndex < endIndex - q)
            {
                QuickSort(array, comparer, startIndex, q - 1);

                startIndex = q + 1;
            }
            else
            {
                QuickSort(array, comparer, q + 1, endIndex);

                endIndex = q - 1;
            }
        }
    }

    private static int PartitionWithFirstElement<T>(T[] array, Func<T, T, int> comparer, int startIndex, int endIndex)
    {
        T pivot = array[startIndex];

        int i = startIndex + 1;

        for (int j = startIndex + 1; j <= endIndex; j++)
        {
            if (comparer(array[j], pivot) < 0)
            {
                T temp = array[j];
                array[j] = array[i];
                array[i] = temp;
                i++;
            }
        }

        T temp02 = array[startIndex];
        array[startIndex] = array[i - 1];
        array[i - 1] = temp02;

        return i - 1;
    }

    private static int PartitionWithMedianElement<T>(T[] array, Func<T, T, int> comparer, int startIndex, int endIndex)
    {
        int middleIndex = startIndex + (endIndex - startIndex) / 2;

        T first = array[startIndex];
        T middle = array[middleIndex];
        T last = array[endIndex];

        int index;

        if (comparer(first, middle) <= 0)
        {
            if (comparer(middle, last) <= 0)
            {
                index = middleIndex;
            }
            else
            {
                index = comparer(first, last) <= 0 ? endIndex : startIndex;
            }
        }
        else
        {
            if (comparer(first, last) <= 0)
            {
                index = startIndex;
            }
            else
            {
                index = comparer(middle, last) <= 0 ? endIndex : middleIndex;
            }
        }

        T temp02 = array[index];
        array[index] = array[startIndex];
        array[startIndex] = temp02;

        return PartitionWithFirstElement(array, comparer, startIndex, endIndex);
    }
}
