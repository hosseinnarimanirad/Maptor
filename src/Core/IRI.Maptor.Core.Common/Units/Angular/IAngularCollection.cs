//besmellahe rahmane rahim
//Allahoma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Mathematics;

namespace IRI.Maptor.Core.Common.Metrics;

public interface IAngularCollection : IEnumerable<AngularUnit>
{
    int Length { get; }
    AngleMode Mode { get; }
    AngleRange Range { get; set; }
    AngularUnit this[int index] { get; set; }

    IAngularCollection Add(IAngularCollection array);
    IAngularCollection AddAllValuesWith(double value);
    IAngularCollection Subtract(IAngularCollection array);
    IAngularCollection SubtractAllValuesFrom(double value);
    IAngularCollection SubtractAllValuesWith(double value);
    IAngularCollection DivideAllValuesAsDenominator(double numerator);
    IAngularCollection DivideAllValuesAsNumerator(double denominator);
    IAngularCollection MultiplyAllValuesWith(double value);
    IAngularCollection Negate();

    //IEnumerator<AngularUnit> GetEnumerator();

    double GetTheValue(int index);
    AngularUnit GetValue(int index);
    void SetValue(int index, AngularUnit value);
    double[] ToArray();
    Vector ToVector();

    AngularCollection<TNewAngleArrayType> ChangeTo<TNewAngleArrayType>() where TNewAngleArrayType : AngularUnit, new();
    IAngularCollection Clone();

}
