using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Common.Abstractions;

public interface IPoint
{
    double X { get; set; }
    double Y { get; set; }

    //PointType Type { get; }

    bool AreExactlyTheSame(object obj);

    bool HaveTheSameXY(object obj);

    //double DistanceTo(IPoint point);

    byte[]? AsWkb();

    bool IsNaN();

    byte[] AsByteArray();

    string AsDelimited(char delimiter, int precision, bool useThousandSeparator);

    //bool HasM();

    //bool HasZ();

}
