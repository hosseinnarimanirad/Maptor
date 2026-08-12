using IRI.Maptor.Sta.Common.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Wpf.Helpers;

public static class Utility
{

    public static Func<Point, Point> CreateMapToScreenMapFunc(BoundingBox mapExtent, double screenWidth, double screenHeight)
    {
        double xScale = screenWidth / mapExtent.Width;
        double yScale = screenHeight / mapExtent.Height;
        double scale = xScale > yScale ? yScale : xScale;

        return new Func<Point, Point>(p => new Point((p.X - mapExtent.XMin) * scale, -(p.Y - mapExtent.YMax) * scale));
    }


}
