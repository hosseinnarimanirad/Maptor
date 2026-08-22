//besmellahe rahmane rahim
//Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Core.Common.Mathematics;
using System.Collections.Generic;

namespace IRI.Maptor.Core.Common.Metrics;

public class LinearCollection<T> : ILinearCollection  where T : LinearUnit, new()
{
    #region Fields&Properties

    private List<double> values;

    public int Length
    {
        get { return values.Count; }
    }

    public LinearMode Mode
    {
        get { return this.GetMode(); }
    }

    #endregion

    #region Indexers&Constructors

    public LinearUnit this[int index]
    {
        get { return GetValue(index); }
        set { SetValue(index, value); }
    }

    public LinearCollection(double[] values)
    {
        this.values = new List<double>(values);
    }

    public LinearCollection(int size)
    {
        this.values = new List<double>(new double[size]);
    }

    #endregion

    #region Methods

    private LinearMode GetMode()
    {
        for (int i = 0; i < LinearUnitBuilder.UnitPairs.Count; i++)
        {
            if (LinearUnitBuilder.UnitPairs[((LinearMode)i)] == typeof(T))
            { return (LinearMode)i; }
        }
        throw new NotImplementedException();
    }

    public void SetValue(int index, LinearUnit value)
    {
        if (index >= this.Length || index < 0)
        {
            throw new OutOfBoundIndexException();
        }
        else
        {
            LinearUnit tempValue = value.ChangeTo<T>();

            this.values[index] = tempValue.Value;
        }
    }

    public LinearUnit GetValue(int index)
    {
        if (index >= this.Length || index < 0)
        {
            throw new OutOfBoundIndexException();
        }
        else
        {
            LinearUnit tempValue = new T();

            tempValue.Value = this.values[index];

            return tempValue;
        }
    }

    public void SetTheValue(int index, double value)
    {
        if (index >= this.Length || index < 0)
        {
            throw new OutOfBoundIndexException();
        }
        this.values[index] = value;
    }

    public double GetTheValue(int index)
    {
        if (index >= this.Length || index < 0)
        {
            throw new OutOfBoundIndexException();
        }
        return this.values[index];
    }

    public ILinearCollection AddAllValuesWith(double value)
    {
        LinearCollection<T> result = new LinearCollection<T>(this.Length);

        for (int i = 0; i < this.Length; i++)
        {
            result.SetTheValue(i, this.GetTheValue(i) + value);
        }

        return result;
    }

    public ILinearCollection SubtractAllValuesWith(double value)
    {
        return this.AddAllValuesWith(-value);
    }

    public ILinearCollection SubtractAllValuesFrom(double value)
    {
        LinearCollection<T> result = new LinearCollection<T>(this.Length);

        for (int i = 0; i < this.Length; i++)
        {
            result.SetTheValue(i, value - this.GetTheValue(i));
        }

        return result;
    }

    public ILinearCollection MultiplyAllValuesWith(double value)
    {
        LinearCollection<T> result = new LinearCollection<T>(this.Length);

        for (int i = 0; i < this.Length; i++)
        {
            result.SetTheValue(i, this.GetTheValue(i) * value);
        }

        return result;
    }

    public ILinearCollection DivideAllValuesAsNumerator(double denominator)
    {
        return MultiplyAllValuesWith(1 / denominator);
    }

    public ILinearCollection DivideAllValuesAsDenominator(double numerator)
    {
        LinearCollection<T> result = new LinearCollection<T>(this.Length);

        for (int i = 0; i < this.Length; i++)
        {
            result.SetTheValue(i, numerator / this.GetTheValue(i));
        }

        return result;
    }

    public ILinearCollection Add(ILinearCollection array)
    {
        if (this.Length != array.Length)
        {
            throw new NotImplementedException();
        }

        LinearCollection<T> result = new LinearCollection<T>(this.Length);

        for (int i = 0; i < this.Length; i++)
        {
            result.SetValue(i, this.GetValue(i).Add(array.GetValue(i)));
        }

        return result;
    }

    public ILinearCollection Subtract(ILinearCollection array)
    {
        if (this.Length != array.Length)
        {
            throw new NotImplementedException();
        }

        LinearCollection<T> result = new LinearCollection<T>(this.Length);

        for (int i = 0; i < this.Length; i++)
        {
            result.SetValue(i, this.GetValue(i).Subtract(array.GetValue(i)));
        }

        return result;
    }

    public ILinearCollection Negate()
    {
        return this.MultiplyAllValuesWith(-1);
    }

    public double[] ToArray()
    {
        return this.values.ToArray();
    }

    public Vector ToVector()
    {
        return new Vector(this.ToArray());
    }

    public ILinearCollection Clone()
    {
        return new LinearCollection<T>(this.ToArray());
    }

    public ILinearCollection ChangeTo<TNewAngleArrayType>() where TNewAngleArrayType : LinearUnit, new()
    {
        if (typeof(T) == typeof(TNewAngleArrayType))
        {
            return this.Clone();
        }

        double[] tempValues = new double[this.Length];

        for (int i = 0; i < this.Length; i++)
        {
            tempValues[i] = this[i].ChangeTo<TNewAngleArrayType>().Value;
        }

        return new LinearCollection<TNewAngleArrayType>(tempValues);
    }

    #endregion

    #region Operators

    public static LinearCollection<T> operator +(LinearCollection<T> firstArray, ILinearCollection secondArray)
    {
        return (LinearCollection<T>)firstArray.Add(secondArray);
    }

    public static LinearCollection<T> operator -(LinearCollection<T> firstArray, ILinearCollection secondArray)
    {
        return (LinearCollection<T>)firstArray.Subtract(secondArray);
    }

    public static LinearCollection<T> operator -(LinearCollection<T> array)
    {
        return (LinearCollection<T>)array.Negate();
    }

    #endregion

    #region IEnumerable<ILinearUnit> Members

    public IEnumerator<LinearUnit> GetEnumerator()
    {
        for (int i = 0; i < values.Count; i++)
        {
            yield return this[i];
        }
    }

    #endregion

    #region IEnumerable Members

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    #endregion
}