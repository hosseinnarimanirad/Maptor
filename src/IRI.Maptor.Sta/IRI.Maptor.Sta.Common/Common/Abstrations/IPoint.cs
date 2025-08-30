namespace IRI.Maptor.Sta.Common.Abstrations;

public interface IPoint
{
    double X { get; set; }
    double Y { get; set; }

    bool AreExactlyTheSame(object obj);

    //double DistanceTo(IPoint point);

    byte[] AsWkb();

    bool IsNaN();

    byte[] AsByteArray();

     
    //T Transform<T>(Func<T, T> transform, int newSrid) where T : IPoint, new();
}
