using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;

namespace IRI.Maptor.Extensions;

public static class SqlGeometryExtensions
{
    //public static System.Windows.Point AsWpfPoint(this SqlGeometry pointGeometry)
    //{
    //    return new System.Windows.Point(pointGeometry.STX.Value, pointGeometry.STY.Value);
    //}

    public static System.Windows.Point AsWpfPoint<T>(this Geometry<T> pointGeometry) where T : IPoint, new()
    {
        return new System.Windows.Point(pointGeometry.Points[0].X, pointGeometry.Points[0].Y);
    }


}
