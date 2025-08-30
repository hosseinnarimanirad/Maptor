using System;

using IRI.Maptor.Sta.Common.Abstrations;

namespace IRI.Maptor.Extensions;

public static class PointExtensions
{
    public static System.Windows.Point AsWpfPoint<T>(this T value) where T : IPoint, new()
    {
        return new System.Windows.Point(value.X, value.Y);
    }

    public static System.Drawing.PointF AsGdiPointF(this System.Windows.Point point)
    {
        return new System.Drawing.PointF((float)point.X, (float)point.Y);
    }

    public static System.Drawing.Point AsGdiPoint(this System.Windows.Point point)
    {
        return new System.Drawing.Point(Convert.ToInt32(Math.Round(point.X)), Convert.ToInt32(Math.Round(point.Y)));
    }

    public static Sta.Common.Primitives.Point AsPoint(this System.Windows.Point point)
    {
        return new Sta.Common.Primitives.Point(point.X, point.Y);
    }
}
