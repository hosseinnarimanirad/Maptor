using System.Windows;

namespace IRI.Maptor.Jab.Common.Models;

public static class AncherFunctionHandlers
{
    public static AncherFunctionHandler CenterLeft = (point, width, height) => new Point(point.X, point.Y - height / 2.0);
    public static AncherFunctionHandler TopLeft = (point, width, height) => new Point(point.X, point.Y);
    public static AncherFunctionHandler BottomLeft = (point, width, height) => new Point(point.X, point.Y - height);

    public static AncherFunctionHandler CenterCenter = (point, width, height) => new Point(point.X - width / 2.0, point.Y - height / 2.0);
    public static AncherFunctionHandler TopCenter = (point, width, height) => new Point(point.X - width / 2.0, point.Y);
    public static AncherFunctionHandler BottomCenter = (point, width, height) => new Point(point.X - width / 2.0, point.Y - height);

    public static AncherFunctionHandler CenterRight = (point, width, height) => new Point(point.X + width / 2.0, point.Y - height / 2.0);
    public static AncherFunctionHandler TopRight = (point, width, height) => new Point(point.X + width / 2.0, point.Y);
    public static AncherFunctionHandler BottomRight = (point, width, height) => new Point(point.X + width / 2.0, point.Y - height);

}
