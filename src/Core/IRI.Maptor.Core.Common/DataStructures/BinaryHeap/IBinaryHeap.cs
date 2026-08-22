using System;
namespace IRI.Maptor.Core.Common.DataStructures;

public interface IBinaryHeap<T> where T : IComparable<T>
{
    int Length { get; }
    T ReleaseValue();
}
