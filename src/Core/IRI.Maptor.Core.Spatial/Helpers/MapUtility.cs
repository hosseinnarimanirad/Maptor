using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Spatial.Helpers;

public class MapUtility
{
    public static Func<Point, Point> GetMapToScreen(BoundingBox mapExtent, double screenWidth, double screenHeight)
    {
        double scaleX = screenWidth / mapExtent.Width;

        double scaleY = screenHeight / mapExtent.Height;

        return p => new Point((p.X - mapExtent.XMin) * scaleX, screenHeight - (p.Y - mapExtent.YMin) * scaleY);
    }

}
