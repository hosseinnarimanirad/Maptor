// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.DataStructures.CustomStructures;

public class UniquePairs<FirstType, SecondType>
{
    List<FirstType> firstValues;

    List<SecondType> secondValues;

    public int Count
    {
        get { return this.firstValues.Count; }
    }

    public UniquePairs()
        : this(1)
    {
    }

    public UniquePairs(int capacity)
    {
        this.firstValues = new List<FirstType>(capacity);

        this.secondValues = new List<SecondType>(capacity);
    }

    public void Add(FirstType firstValue, SecondType secondValue)
    {
        if (firstValues.Contains(firstValue) || secondValues.Contains(secondValue))
        {
            throw new NotImplementedException();
        }

        firstValues.Add(firstValue);

        secondValues.Add(secondValue);
    }

    public void Add(Pair<FirstType, SecondType> value)
    {
        if (firstValues.Contains(value.First) || secondValues.Contains(value.Second))
        {
            throw new NotImplementedException();
        }

        firstValues.Add(value.First);

        secondValues.Add(value.Second);
    }

    public FirstType this[SecondType secondValue]
    {
        get
        {
            return GetFirstValue(secondValue);
        }
    }

    public SecondType this[FirstType firstValue]
    {
        get
        {
            int index = this.firstValues.IndexOf(firstValue);

            return this.secondValues[index];
        }
    }

    public FirstType GetFirstValue(SecondType secondValue)
    {
        int index = this.secondValues.IndexOf(secondValue);

        return this.firstValues[index];
    }

    public SecondType GetSecondValue(FirstType firstValue)
    {
        int index = this.firstValues.IndexOf(firstValue);

        return this.secondValues[index];
    }

    public FirstType GetFirstValue(int index)
    {
        return this.firstValues[index];
    }

    public SecondType GetSecondValue(int index)
    {
        return this.secondValues[index];
    }

    public bool FirstValuesContains(FirstType item)
    {
        return this.firstValues.Contains(item);
    }

    public bool SecondValuesContains(SecondType item)
    {
        return this.secondValues.Contains(item);
    }
}
