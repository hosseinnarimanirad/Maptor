using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Extensions;

public static class PointExtensions
{ 
    public static Geometry<Point> AsGeometry(this Point point, int srid)
    {
        return Geometry<Point>.Create(point.X, point.Y, srid);
    } 
}
