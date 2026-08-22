using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common;

public class RandomItem<T>
{
    public T Item { get; private set; }

    public double Weight { get; private set; }

    public double MinInclusive { get; set; }

    public double MaxExclusive { get { return MinInclusive + Weight; } }

    public bool IsInRange(double value)
    {
        return value >= MinInclusive && value < MaxExclusive;
    }

    private RandomItem(T item, double weight)
    {
        this.Item = item;
        this.Weight = weight;
    }
     
    public static RandomItem<T> Create(T item, double weight)
    {
        if (weight < 0)
            throw new ArgumentOutOfRangeException("RandomItem > Create > weight must be greater than zero");

        if (item is null)
            throw new ArgumentOutOfRangeException("RandomItem > Create > item cannot be null");

        return new RandomItem<T>(item, weight);
    }
}