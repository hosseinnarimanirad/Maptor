using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Sta.Common.Abstrations;

public interface IPoint
{
    double X { get; set; }
    double Y { get; set; }

    //PointType Type { get; }

    bool AreExactlyTheSame(object obj);

    //double DistanceTo(IPoint point);

    byte[] AsWkb();

    bool IsNaN();

    byte[] AsByteArray();

    //bool HasM();

    //bool HasZ();
     
}
