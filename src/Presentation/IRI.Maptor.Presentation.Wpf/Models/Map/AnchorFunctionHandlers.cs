using System.Windows;

namespace IRI.Maptor.Presentation.Wpf.Models;

public static class AnchorFunctionHandlers
{
    public static AnchorFunctionHandler CenterLeft = (point, width, height) => new Point(point.X, point.Y - height / 2.0);
    public static AnchorFunctionHandler TopLeft = (point, width, height) => new Point(point.X, point.Y);
    public static AnchorFunctionHandler BottomLeft = (point, width, height) => new Point(point.X, point.Y - height);

    public static AnchorFunctionHandler CenterCenter = (point, width, height) => new Point(point.X - width / 2.0, point.Y - height / 2.0);
    public static AnchorFunctionHandler TopCenter = (point, width, height) => new Point(point.X - width / 2.0, point.Y);
    public static AnchorFunctionHandler BottomCenter = (point, width, height) => new Point(point.X - width / 2.0, point.Y - height);

    public static AnchorFunctionHandler CenterRight = (point, width, height) => new Point(point.X + width / 2.0, point.Y - height / 2.0);
    public static AnchorFunctionHandler TopRight = (point, width, height) => new Point(point.X + width / 2.0, point.Y);
    public static AnchorFunctionHandler BottomRight = (point, width, height) => new Point(point.X + width / 2.0, point.Y - height);

}
