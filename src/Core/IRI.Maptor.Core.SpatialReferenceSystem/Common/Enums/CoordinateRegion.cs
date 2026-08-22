// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Core.Common.Metrics;

namespace IRI.Maptor.Core.SpatialReferenceSystem;

enum FirstAxisRegion
{
    Negative = 1,
    Zero = 2,
    Positive = 4
}

enum SecondAxisRegion
{
    Negative = 8,
    Zero = 16,
    Positive = 32
}

[Flags]
public enum CoordinateRegion
{

    Base = FirstAxisRegion.Zero | SecondAxisRegion.Zero,
    PositiveX1 = FirstAxisRegion.Positive | SecondAxisRegion.Zero,
    Region1 = FirstAxisRegion.Positive | SecondAxisRegion.Positive,
    PositiveX2 = FirstAxisRegion.Zero | SecondAxisRegion.Positive,
    Region2 = FirstAxisRegion.Negative | SecondAxisRegion.Positive,
    NegativeX1 = FirstAxisRegion.Negative | SecondAxisRegion.Zero,
    Region3 = FirstAxisRegion.Negative | SecondAxisRegion.Negative,
    NegativeX2 = FirstAxisRegion.Zero | SecondAxisRegion.Negative,
    Region4 = FirstAxisRegion.Positive | SecondAxisRegion.Negative

}
