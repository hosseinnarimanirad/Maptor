//besmellahe rahmane rahim
//Allahoma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Mathematics;

namespace IRI.Maptor.Core.Common.Metrics;

public interface ILinearCollection : IEnumerable<LinearUnit>
{
    int Length { get; }
    LinearMode Mode { get; }
    LinearUnit this[int index] { get; set; }
    ILinearCollection Add(ILinearCollection array);
    ILinearCollection AddAllValuesWith(double value);
    ILinearCollection MultiplyAllValuesWith(double value);
    ILinearCollection Subtract(ILinearCollection array);
    ILinearCollection SubtractAllValuesFrom(double value);
    ILinearCollection SubtractAllValuesWith(double value);
    ILinearCollection DivideAllValuesAsDenominator(double numerator);
    ILinearCollection DivideAllValuesAsNumerator(double denominator);
    ILinearCollection Negate();
    //IEnumerator<LinearUnit> GetEnumerator();
    double GetTheValue(int index);
    LinearUnit GetValue(int index);
    void SetTheValue(int index, double value);
    void SetValue(int index, LinearUnit value);
    ILinearCollection ChangeTo<TNewAngleArrayType>() where TNewAngleArrayType : LinearUnit, new();
    ILinearCollection Clone();
    double[] ToArray();
    Vector ToVector();
}
