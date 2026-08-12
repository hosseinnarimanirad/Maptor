using System.Windows;

namespace IRI.Maptor.Extensions;

public static class RectExtensions
{
    public static Point GetCenter(this Rect rect)
    {
        return new Point(rect.Left + rect.Width / 2,
                          rect.Top + rect.Height / 2);
    }
}
