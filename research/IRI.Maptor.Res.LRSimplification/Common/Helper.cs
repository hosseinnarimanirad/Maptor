using IRI.Maptor.Jab.Common;
using IRI.Maptor.Sta.Mathematics;
using System.Drawing;
using System.Threading.Tasks;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Helpers;

namespace IRI.Maptor.Res.LRSimplification.Common;

public static class Helper
{

    public static async Task<(Bitmap image, ConfusionMatrix confusionMatrix)> CreateImagesAndCM(
      BoundingBox groundBoundingBox,
      int level,
      Bitmap originalBitmap,
      VectorLayer vectorLayer,
      string layerName,
      string simplificationTypeMethod,
      bool saveImages = true)
    {
        var currentScreenSize = WebMercatorUtility.ToScreenSize(level, groundBoundingBox);

        var scale = WebMercatorUtility.GetGoogleMapScale(level);

        var bitmap = await vectorLayer.AsGdiBitmapAsync(groundBoundingBox, scale, currentScreenSize.Width, currentScreenSize.Height);

        var diff = IRI.Maptor.Ket.GdiPlus.Helpers.ImageHelper.CalculateConfusionMatrixBitmaps(originalBitmap, bitmap);

        if (saveImages)
        {
            bitmap.Save($"{layerName}-{simplificationTypeMethod}.png", System.Drawing.Imaging.ImageFormat.Tiff);
            diff.image.Save($"{layerName}-{simplificationTypeMethod}-diff.png", System.Drawing.Imaging.ImageFormat.Tiff);
        }

        return diff;
    }
}
