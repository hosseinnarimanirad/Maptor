using System.Drawing;

using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Presentation.Wpf.Helpers;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Res.LRSimplification.Common;

public static class GeneralHelper
{
    /// <summary>Renders the geometries at the given level, saves them and their difference image against the original, returns the difference.</summary>
    public static (Bitmap image, double percent) CreateImages(
        BoundingBox groundBoundingBox,
        int level,
        Bitmap originalBitmap,
        List<Geometry<Point>> geometries,
        string outputDirectory,
        string fileName,
        string simplificationTypeMethod,
        double coef = 1,
        bool saveImages = true)
    {
        var bitmap = Render(geometries, groundBoundingBox, level);

        var diff = IRI.Maptor.Infrastructure.GdiPlus.Helpers.ImageHelper.CalculateDifPlusBitmaps(originalBitmap, bitmap, true);

        if (saveImages)
        {
            bitmap.Save($"{outputDirectory}\\{fileName}-{level}-{coef:N2}-{simplificationTypeMethod}.png", System.Drawing.Imaging.ImageFormat.Tiff);
            diff.image.Save($"{outputDirectory}\\{fileName}-{level}-{coef:N2}-{simplificationTypeMethod}-diff.png", System.Drawing.Imaging.ImageFormat.Tiff);
        }

        return diff;
    }

    /// <summary>
    /// The raster of the visual metric: <paramref name="geometries"/> drawn on the screen of zoom level
    /// <paramref name="level"/> with <see cref="SimplificationHelper.RenderOptions"/> (not with the map renderer).
    /// </summary>
    public static Bitmap Render(List<Geometry<Point>> geometries, BoundingBox groundBoundingBox, int level)
    {
        var screenSize = WebMercatorUtility.ToScreenSize(level, groundBoundingBox);

        return SimplificationHelper.RenderForMetric(geometries, groundBoundingBox, screenSize);
    }
}
