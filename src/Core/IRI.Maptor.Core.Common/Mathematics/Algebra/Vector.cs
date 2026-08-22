using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;

[assembly: CLSCompliant(true)]

namespace IRI.Maptor.Core.Common.Mathematics;

[DataContract]
public class UnequalVectorSizeException : Exception
{

    public UnequalVectorSizeException() : base("Not Equal Vector Size") { }
    public UnequalVectorSizeException(string message) : base(message) { }
    public UnequalVectorSizeException(string message, Exception inner) : base(message, inner) { }
    //protected UnequalVectorSizeException(
    //  SerializationInfo info,
    //  StreamingContext context)
    //    : base(info, context) { }
}

public class Vector
{

    #region Field, Properties, Indexers

    List<double> Values = new List<double>();

    public int Length
    {

        get { return this.Values.Count; }

    }

    public double this[int index]
    {
        get { return this.GetValue(index); }

        set { this.SetValue(index, value); }
    }

    public double Norm
    {
        get
        {

            return CalculateNorm();

        }
    }

    public Vector Abs
    {

        get { return this.CalculateAbsVector(); }

    }

    #endregion

    #region Constructors

    public Vector()
    {

    }

    public Vector(int size)
    {

        for (int i = 0; i < size; i++)
        {

            this.Values.Add(0);

        }

    }

    //public Vector(int[] values)
    //{
    //    for (int i = 0; i < values.Length; i++)
    //    {
    //        this.Values.Add(values[i]);
    //    }
    //}

    public Vector(double[] values)
    {
        Values.AddRange(values);
    }

    //public Vector(float[] values)
    //{

    //    for (int i = 0; i < values.Length; i++)
    //    {

    //        this.Values.Add(values[i]);

    //    }

    //}

    #endregion

    #region Methods

    #region Private

    private double CalculateNorm()
    {

        return Math.Sqrt(DotMultiply(this, this));

    }

    private Vector CalculateAbsVector()
    {

        Vector resultVector = new Vector(this.Length);

        for (int i = 0; i < this.Length; i++)
        {

            if (this[i] < 0)
                resultVector[i] = -this[i];

            resultVector[i] = this[i];

        }

        return resultVector;

    }

    private double GetValue(int index)
    {
        if (index >= this.Length || index < 0)
        {
            throw new OutOfBoundIndexException();
        }
        else
        {
            return this.Values[index];
        }
    }

    private void SetValue(int index, double value)
    {
        if (index >= this.Length || index < 0)
        {
            throw new OutOfBoundIndexException();
        }
        else
        {
            this.Values[index] = value;
        }
    }

    #endregion

    #region Public

    public Vector Clone()
    {

        Vector resultVector = new Vector(this.Length);

        for (int i = 0; i < this.Length; i++)
        {
            resultVector[i] = this[i];
        }

        return resultVector;

    }

    public double[] ToArray()
    {

        return this.Values.ToArray();

    }

    public void AddValue(double value)
    {

        this.Values.Add(value);

    }

    public Vector Negate()
    {
        return this.Multiply(-1);
    }

    public Vector Add(Vector value)
    {
        if (!AreTheSameSize(this, value))
            throw new UnequalVectorSizeException();

        Vector result = new Vector();

        for (int i = 0; i < this.Length; i++)
        {

            result.AddValue(this[i] + value[i]);

        }

        return result;

    }

    public Vector Multiply(double scalar)
    {

        Vector result = new Vector();

        for (int i = 0; i < this.Length; i++)
        {

            result.AddValue(scalar * this[i]);

        }

        return result;
    }

    public double DotMultiply(Vector value)
    {
        if (value.Length!=this.Length)
        {
            throw new NotImplementedException();
        }

        double result = 0;

        for (int i = 0; i < this.Length; i++)
        {
            result += this[i] * value[i];
        }

        return result;
    }


    public Vector Subtract(Vector value)
    {
        if (!AreTheSameSize(this, value))
            throw new UnequalVectorSizeException();

        Vector result = new Vector();

        for (int i = 0; i < value.Length; i++)
        {

            result.AddValue(this[i] - value[i]);

        }

        return result;
    }

    #endregion

    #endregion

    #region Statics

    public static bool AreIndependent(Vector vector1, Vector vector2)
    {

        return (DotMultiply(vector1, vector2) == 0);

    }

    public static bool AreTheSameSize(Vector vector1, Vector vector2)
    {

        return (vector1.Length == vector2.Length);

    }

    public static double DotMultiply(Vector vector1, Vector vector2)
    {
        if (vector1.Length != vector2.Length)
            throw new UnequalVectorSizeException();

        double result = 0;

        for (int i = 0; i < vector1.Length; i++)
        {

            result += vector1[i] * vector2[i];

        }

        return result;

    }

    #endregion

    #region Operators

    public static Vector operator -(Vector vector)
    {
        return vector.Negate();
    }

    public static Vector operator -(Vector vector1, Vector vector2)
    {

        return vector1.Subtract(vector2);

    }

    public static Vector operator +(Vector vector1, Vector vector2)
    {
        return vector1.Add(vector2);
    }

    public static Vector operator *(double scalar, Vector vector)
    {
        return vector.Multiply(scalar);
    }

    public static Vector operator *(Vector vector, double scalar)
    {
        return vector.Multiply(scalar);
    }

    public static bool operator ==(Vector vector1, Vector vector2)
    {
        return vector1.Values.ToString() == vector2.Values.ToString();
    }

    public static bool operator !=(Vector vector1, Vector vector2)
    {
        return !(vector1 == vector2);
    }

    #endregion

    #region Overrides

    public override string ToString()
    {

        System.Text.StringBuilder result = new System.Text.StringBuilder();

        for (int i = 0; i < this.Values.Count - 1; i++)
        {

            result.Append(string.Format(CultureInfo.CurrentCulture, "{0}, ", this.Values[i]));

        }

        result.Append(this.Values[this.Length - 1]);

        return result.ToString();
    }

    public override bool Equals(object obj)
    {
        return (this.ToString() == obj.ToString());
    }

    public override int GetHashCode()
    {
        // Equals compares ToString() output, so the hash must be derived from it too
        return this.ToString().GetHashCode();
    }

    #endregion

}

