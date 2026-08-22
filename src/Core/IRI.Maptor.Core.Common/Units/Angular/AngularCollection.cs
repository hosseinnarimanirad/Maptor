//besmellahe rahmane rahim
//Allahoma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Mathematics;

namespace IRI.Maptor.Core.Common.Metrics;

public class AngularCollection<T> : IAngularCollection where T : AngularUnit, new()
{
    #region Fields&Properties

    private List<double> values;

    private AngleRange m_Range;

    public int Length
    {
        get { return this.values.Count; }
    }

    public AngleMode Mode
    {
        get { return this.GetMode(); }
    }

    public AngleRange Range
    {
        get
        {
            return this.m_Range;
        }

        set
        {
            this.m_Range = value;

            this.AdaptValues();
        }
    }

    #endregion

    #region Indexers&Constructors

    public AngularUnit this[int index]
    {
        get { return GetValue(index); }

        set { SetValue(index, value); }
    }

    public AngularCollection(int size, AngleRange range)
        : this(new double[size], range) { }

    public AngularCollection(double[] values, AngleRange range)
    {
        this.values = new List<double>(values);
        
        this.m_Range = range;

        this.AdaptValues();
    }

    #endregion

    #region Methods

    private AngleMode GetMode()
    {
        for (int i = 0; i < AngularUnitBuilder.UnitPairs.Count; i++)
        {
            if (AngularUnitBuilder.UnitPairs[(AngleMode)i] == typeof(T))
            { return (AngleMode)i; }
        }
        throw new NotImplementedException();
    }

    private void AdaptValues()
    {
        T tempValue = new T();

        tempValue.Range = this.Range;

        AngleAdapter adapter = tempValue.Adapter;

        for (int i = 0; i < this.values.Count; i++)
        {
            this.values[i] = adapter.Adopt(this.values[i]);
        }
    }

    public void SetValue(int index, AngularUnit value)
    {
        if (index >= this.Length || index < 0)
        {
            throw new OutOfBoundIndexException();
        }
        else
        {
            AngularUnit tempValue = value.ChangeTo<T>();

            tempValue.Range = this.Range;

            this.values[index] = tempValue.Value;
        }
    }

    public AngularUnit GetValue(int index)
    {
        if (index >= this.Length || index < 0)
        {
            throw new OutOfBoundIndexException();
        }
        else
        {
            T tempValue = new T();

            tempValue.Range = this.Range;

            tempValue.Value = this.values[index];

            return tempValue;
        }
    }

    public double GetTheValue(int index)
    {
        if (index >= this.Length || index < 0)
        {
            throw new OutOfBoundIndexException();
        }

        return this.values[index];
    }

    //public void SetTheValue(int index, double value)
    //{
    //    if (index > this.Length || index < 0)
    //    {
    //        throw new OutOfBoundIndexException();
    //    }
    //    else
    //    {
    //        this.values[index] = this.atempValue.Value;
    //    }
    //}

    //public double GetTheValue(int index)
    //{
    //    if (index > this.Length || index < 0)
    //    {
    //        throw new OutOfBoundIndexException();
    //    }
    //    else
    //    {
    //        T tempValue = new T();

    //        tempValue.Range = this.Range;

    //        tempValue.Value = this.values[index];

    //        return tempValue;
    //    }
    //}


    public IAngularCollection AddAllValuesWith(double value)
    {
        AngularCollection<T> result = new AngularCollection<T>(this.Length, this.Range);

        for (int i = 0; i < this.Length; i++)
        {
            T tempValue = new T();

            tempValue.Range = this.Range;

            tempValue.Value = this.GetTheValue(i) + value;

            result.SetValue(i, tempValue);
        }

        return result;
    }

    public IAngularCollection SubtractAllValuesWith(double value)
    {
        return this.AddAllValuesWith(-value);
    }

    public IAngularCollection SubtractAllValuesFrom(double value)
    {
        AngularCollection<T> result = new AngularCollection<T>(this.Length, this.Range);

        for (int i = 0; i < this.Length; i++)
        {
            T tempValue = new T();

            tempValue.Range = this.Range;

            tempValue.Value = value - this.GetTheValue(i);

            result.SetValue(i, tempValue);
        }

        return result;
    }

    public IAngularCollection MultiplyAllValuesWith(double value)
    {
        AngularCollection<T> result = new AngularCollection<T>(this.Length, this.Range);

        for (int i = 0; i < this.Length; i++)
        {
            T tempValue = new T();

            tempValue.Range = this.Range;

            tempValue.Value = this.GetTheValue(i) * value;

            result.SetValue(i, tempValue);
        }

        return result;
    }

    public IAngularCollection DivideAllValuesAsNumerator(double denominator)
    {
        return this.MultiplyAllValuesWith(1 / denominator);
    }

    public IAngularCollection DivideAllValuesAsDenominator(double numerator)
    {
        AngularCollection<T> result = new AngularCollection<T>(this.Length, this.Range);

        for (int i = 0; i < this.Length; i++)
        {
            T tempValue = new T();

            tempValue.Range = this.Range;

            tempValue.Value = numerator / this.GetTheValue(i);

            result.SetValue(i, tempValue);
        }

        return result;
    }

    public IAngularCollection Add(IAngularCollection array)
    {
        if (this.Length != array.Length)
        {
            throw new NotImplementedException();
        }

        AngularCollection<T> result = new AngularCollection<T>(this.Length, this.Range);

        for (int i = 0; i < this.Length; i++)
        {
            result.SetValue(i, this[i].Add(array[i]));
        }

        return result;
    }

    public IAngularCollection Subtract(IAngularCollection array)
    {
        if (this.Length != array.Length)
        {
            throw new NotImplementedException();
        }

        AngularCollection<T> result = new AngularCollection<T>(this.Length, this.Range);

        for (int i = 0; i < this.Length; i++)
        {
            result.SetValue(i, this[i].Subtract(array[i]));
        }

        return result;
    }

    public IAngularCollection Negate()
    {
        return this.MultiplyAllValuesWith(-1);
    }

    public Vector ToVector()
    {
        return new Vector(this.ToArray());
    }

    public double[] ToArray()
    {
        return this.values.ToArray();
    }

    public IAngularCollection Clone()
    {
        return new AngularCollection<T>(this.ToArray(), this.Range);
    }

    public AngularCollection<TNewAngleArrayType> ChangeTo<TNewAngleArrayType>() where TNewAngleArrayType : AngularUnit, new()
    {
        double[] tempValues = new double[this.Length];

        for (int i = 0; i < this.Length; i++)
        {
            tempValues[i] = this[i].ChangeTo<TNewAngleArrayType>().Value;
        }

        return new AngularCollection<TNewAngleArrayType>(tempValues, this.Range);
    }

    #endregion

    #region Operators

    public static AngularCollection<T> operator +(AngularCollection<T> firstArray, IAngularCollection secondArray)
    {
        return (AngularCollection<T>)firstArray.Add(secondArray);
    }

    public static AngularCollection<T> operator -(AngularCollection<T> firstArray, IAngularCollection secondArray)
    {
        return (AngularCollection<T>)firstArray.Subtract(secondArray);
    }

    public static AngularCollection<T> operator -(AngularCollection<T> array)
    {
        return (AngularCollection<T>)array.Negate();
    }

    #endregion

    #region IEnumerable<ILinearUnit> Members

    public IEnumerator<AngularUnit> GetEnumerator()
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
