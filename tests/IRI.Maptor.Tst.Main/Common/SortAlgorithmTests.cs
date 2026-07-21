using System.Collections.Generic;
using System.Linq;
using IRI.Maptor.Sta.DataStructures;

namespace IRI.Maptor.Tst.Main.Common;

/// <summary>
/// Tests for SortAlgorithm (BubbleSort, MergeSort, QuickSort, Heapsort,
/// CountInversions) and the binary heap classes it relies on.
/// </summary>
public class SortAlgorithmTests
{
    private static readonly Func<int, int, int> Ascending = (a, b) => a.CompareTo(b);

    private static int[] RandomArray(int length, int seed)
    {
        var random = new Random(seed);

        var result = new int[length];

        for (int i = 0; i < length; i++)
        {
            result[i] = random.Next(-1000, 1000);
        }

        return result;
    }

    public static IEnumerable<object[]> EdgeCaseArrays()
    {
        yield return new object[] { new int[0] };
        yield return new object[] { new[] { 42 } };
        yield return new object[] { new[] { 7, 7, 7, 7, 7 } };
        yield return new object[] { new[] { 1, 2, 3, 4, 5, 6 } };
        yield return new object[] { new[] { 6, 5, 4, 3, 2, 1 } };
        yield return new object[] { new[] { 3, 1, 3, 2, 1, 2, 3 } };
    }

    #region BubbleSort

    [Theory]
    [MemberData(nameof(EdgeCaseArrays))]
    public void BubbleSort_EdgeCases_MatchesArraySort(int[] array)
    {
        var expected = array.OrderBy(i => i).ToArray();

        SortAlgorithm.BubbleSort(array, Ascending);

        Assert.Equal(expected, array);
    }

    [Fact]
    public void BubbleSort_RandomArray_MatchesArraySort()
    {
        var array = RandomArray(500, seed: 1);
        var expected = array.OrderBy(i => i).ToArray();

        SortAlgorithm.BubbleSort(array, Ascending);

        Assert.Equal(expected, array);
    }

    #endregion

    #region MergeSort

    [Theory]
    [MemberData(nameof(EdgeCaseArrays))]
    public void MergeSort_EdgeCases_MatchesArraySort(int[] array)
    {
        var expected = array.OrderBy(i => i).ToArray();

        var result = SortAlgorithm.MergeSort(array, Ascending);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void MergeSort_RandomArray_MatchesArraySort()
    {
        var array = RandomArray(1000, seed: 2);
        var expected = array.OrderBy(i => i).ToArray();

        var result = SortAlgorithm.MergeSort(array, Ascending);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void MergeSort_DoesNotModifyInput()
    {
        var array = RandomArray(100, seed: 3);
        var snapshot = (int[])array.Clone();

        SortAlgorithm.MergeSort(array, Ascending);

        Assert.Equal(snapshot, array);
    }

    [Fact]
    public void MergeSort_IsStable()
    {
        // equal keys must keep their original relative order
        var array = new[] { (key: 1, order: 0), (key: 0, order: 1), (key: 1, order: 2), (key: 0, order: 3), (key: 1, order: 4) };

        var result = SortAlgorithm.MergeSort(array, (a, b) => a.key.CompareTo(b.key));

        Assert.Equal(new[] { (0, 1), (0, 3), (1, 0), (1, 2), (1, 4) }, result);
    }

    #endregion

    #region QuickSort

    [Theory]
    [MemberData(nameof(EdgeCaseArrays))]
    public void QuickSort_EdgeCases_MatchesArraySort(int[] array)
    {
        var expected = array.OrderBy(i => i).ToArray();

        SortAlgorithm.QuickSort(array, Ascending);

        Assert.Equal(expected, array);
    }

    [Fact]
    public void QuickSort_RandomArray_MatchesArraySort()
    {
        var array = RandomArray(1000, seed: 4);
        var expected = array.OrderBy(i => i).ToArray();

        SortAlgorithm.QuickSort(array, Ascending);

        Assert.Equal(expected, array);
    }

    [Fact]
    public void QuickSort_LargeSortedArray_DoesNotOverflowStack()
    {
        var array = Enumerable.Range(0, 200_000).ToArray();
        var expected = (int[])array.Clone();

        SortAlgorithm.QuickSort(array, Ascending);

        Assert.Equal(expected, array);
    }

    #endregion

    #region Heapsort

    [Theory]
    [MemberData(nameof(EdgeCaseArrays))]
    public void Heapsort_Directional_EdgeCases(int[] array)
    {
        var ascending = SortAlgorithm.Heapsort((int[])array.Clone(), SortDirection.Ascending);
        var descending = SortAlgorithm.Heapsort((int[])array.Clone(), SortDirection.Descending);

        Assert.Equal(array.OrderBy(i => i).ToArray(), ascending);
        Assert.Equal(array.OrderByDescending(i => i).ToArray(), descending);
    }

    [Fact]
    public void Heapsort_Ascending_ReturnsAscendingOrder()
    {
        var array = RandomArray(1000, seed: 5);

        var result = SortAlgorithm.Heapsort(array, SortDirection.Ascending);

        Assert.Equal(array.OrderBy(i => i).ToArray(), result);
    }

    [Fact]
    public void Heapsort_Descending_ReturnsDescendingOrder()
    {
        var array = RandomArray(1000, seed: 6);

        var result = SortAlgorithm.Heapsort(array, SortDirection.Descending);

        Assert.Equal(array.OrderByDescending(i => i).ToArray(), result);
    }

    [Fact]
    public void Heapsort_RefArray_SortsInPlace()
    {
        var array = RandomArray(500, seed: 7);
        var expected = array.OrderBy(i => i).ToArray();

        SortAlgorithm.Heapsort(ref array, SortDirection.Ascending);

        Assert.Equal(expected, array);
    }

    [Fact]
    public void Heapsort_RefArray_EmptyArray_DoesNotThrow()
    {
        var array = new int[0];

        SortAlgorithm.Heapsort(ref array, SortDirection.Ascending);

        Assert.Empty(array);
    }

    [Fact]
    public void Heapsort_RefList_SortsInPlace()
    {
        var list = RandomArray(500, seed: 8).ToList();
        var expected = list.OrderByDescending(i => i).ToList();

        SortAlgorithm.Heapsort(ref list, SortDirection.Descending);

        Assert.Equal(expected, list);
    }

    [Fact]
    public void Heapsort_RefList_EmptyList_DoesNotThrow()
    {
        var list = new List<int>();

        SortAlgorithm.Heapsort(ref list, SortDirection.Ascending);

        Assert.Empty(list);
    }

    [Theory]
    [MemberData(nameof(EdgeCaseArrays))]
    public void Heapsort_Comparer_EdgeCases_ReturnsAscendingComparerOrder(int[] array)
    {
        var expected = array.OrderBy(i => i).ToArray();

        var result = SortAlgorithm.Heapsort(array, Ascending);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Heapsort_Comparer_RandomArray_ReturnsAscendingComparerOrder()
    {
        var array = RandomArray(1000, seed: 9);

        var result = SortAlgorithm.Heapsort(array, Ascending);

        Assert.Equal(array.OrderBy(i => i).ToArray(), result);
    }

    [Fact]
    public void Heapsort_Comparer_ReversedComparer_ReturnsDescendingOrder()
    {
        var array = RandomArray(1000, seed: 10);

        var result = SortAlgorithm.Heapsort(array, (a, b) => b.CompareTo(a));

        Assert.Equal(array.OrderByDescending(i => i).ToArray(), result);
    }

    #endregion

    #region CountInversions

    [Fact]
    public void CountInversions_SortedArray_ReturnsZero()
    {
        var array = Enumerable.Range(0, 100).ToArray();

        Assert.Equal(0, SortAlgorithm.CountInversions(array, Ascending));
    }

    [Fact]
    public void CountInversions_ReversedArray_ReturnsMaximum()
    {
        const int n = 100;
        var array = Enumerable.Range(0, n).Reverse().ToArray();

        Assert.Equal((long)n * (n - 1) / 2, SortAlgorithm.CountInversions(array, Ascending));
    }

    [Fact]
    public void CountInversions_KnownCase_ReturnsExpectedCount()
    {
        // inversions: (2,1), (4,3), (4,1), (3,1) => 4
        var array = new[] { 2, 4, 3, 1, 5 };

        Assert.Equal(4, SortAlgorithm.CountInversions(array, Ascending));
    }

    [Fact]
    public void CountInversions_DoesNotModifyInput()
    {
        var array = RandomArray(100, seed: 11);
        var snapshot = (int[])array.Clone();

        SortAlgorithm.CountInversions(array, Ascending);

        Assert.Equal(snapshot, array);
    }

    [Fact]
    public void CountInversions_MatchesBruteForce()
    {
        var array = RandomArray(200, seed: 12);

        long expected = 0;

        for (int i = 0; i < array.Length; i++)
        {
            for (int j = i + 1; j < array.Length; j++)
            {
                if (array[i] > array[j])
                    expected++;
            }
        }

        Assert.Equal(expected, SortAlgorithm.CountInversions(array, Ascending));
    }

    #endregion

    #region Binary heaps

    [Fact]
    public void MinBinaryHeap_ReleasesSmallestFirst()
    {
        var heap = new MinBinaryHeap<int>(new[] { 5, 1, 4, 2, 3 });

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, new[] { heap.ReleaseValue(), heap.ReleaseValue(), heap.ReleaseValue(), heap.ReleaseValue(), heap.ReleaseValue() });
    }

    [Fact]
    public void MaxBinaryHeap_ReleasesLargestFirst()
    {
        var heap = new MaxBinaryHeap<int>(new[] { 5, 1, 4, 2, 3 });

        Assert.Equal(new[] { 5, 4, 3, 2, 1 }, new[] { heap.ReleaseValue(), heap.ReleaseValue(), heap.ReleaseValue(), heap.ReleaseValue(), heap.ReleaseValue() });
    }

    [Fact]
    public void BinaryHeaps_EmptyArray_ConstructionSucceeds()
    {
        Assert.Equal(0, new MinBinaryHeap<int>(new int[0]).Length);
        Assert.Equal(0, new MaxBinaryHeap<int>(new int[0]).Length);
        Assert.Equal(0, new BinaryHeap<int>(new int[0], Ascending).Length);
    }

    [Fact]
    public void BinaryHeaps_ReleaseValueOnEmptyHeap_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new MinBinaryHeap<int>(new int[0]).ReleaseValue());
        Assert.Throws<InvalidOperationException>(() => new MaxBinaryHeap<int>(new int[0]).ReleaseValue());
        Assert.Throws<InvalidOperationException>(() => new BinaryHeap<int>(new int[0], Ascending).ReleaseValue());
    }

    #endregion
}
