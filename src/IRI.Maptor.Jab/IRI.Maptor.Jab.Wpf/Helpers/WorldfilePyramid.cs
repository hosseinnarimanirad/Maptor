using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using IRI.Maptor.Sta.Spatial.Model;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Ket.GdiPlus.WorldfileFormat;
using IRI.Maptor.Sta.Persistence.RasterDataSources;

namespace IRI.Maptor.Jab.Wpf.Helpers;

public static class WorldfilePyramidFactory
{
    public const string _extentFileName = ZippedImagePyramidDataSource._extentFileName;
    
    public static void CreateFromWebMercatorWorldfile(string imageFileName, double pixelSize, string outputDirectory = null)
    {
        var bitmap = new BitmapImage(new Uri(imageFileName));

        var worldfileName = WorldfileManager.TryGetAssociatedWorldfileName(imageFileName);

        if (worldfileName == null)
        {
            System.Diagnostics.Debug.Print($"Worldfile not found: '{worldfileName}'");
            return;
        }

        var webMercatorBoundingBox = WorldfileManager.ReadImageBoundingBox(worldfileName, (int)bitmap.Width, (int)bitmap.Height);

        int zoomLevel = WebMercatorUtility.GetZoomLevel(bitmap.Height * pixelSize / webMercatorBoundingBox.Height);

        if (zoomLevel >= 20)
        {
            zoomLevel = 19;
        }

        if (outputDirectory == null)
        {
            //outputDirectory = $"{System.IO.Path.GetDirectoryName(imageFileName)}\\{System.IO.Path.GetFileNameWithoutExtension(imageFileName)}";
            outputDirectory = Path.Combine(Path.GetDirectoryName(imageFileName), Path.GetFileNameWithoutExtension(imageFileName));
        }

        if (!Directory.Exists($"{outputDirectory}"))
        {
            Directory.CreateDirectory($"{outputDirectory}");
        }

        WriteBoundingBox(webMercatorBoundingBox, outputDirectory);

        while (zoomLevel > 0 && zoomLevel < 20)
        {
            var tiles = WebMercatorUtility.WebMercatorBoundingBoxToGoogleTileRegions(webMercatorBoundingBox, zoomLevel);

            if (tiles.Count < 1)
                break;

            var subDirectory = Path.Combine(outputDirectory, zoomLevel.ToString());

            if (!Directory.Exists(subDirectory))
            {
                Directory.CreateDirectory(subDirectory);
            }

            SplitIntoGoogleTiles(bitmap, webMercatorBoundingBox, tiles, subDirectory);

            zoomLevel--;
        }

        var zipFileName = Path.ChangeExtension(imageFileName, "pyrmd");

        if (File.Exists(zipFileName))
            File.Delete(zipFileName);

        ZipFile.CreateFromDirectory(outputDirectory, Path.ChangeExtension(imageFileName, "pyrmd"));

        IOHelper.DeleteDirectory(outputDirectory);
    }

    private static void WriteBoundingBox(BoundingBox webMercatorBoundingBox, string outputDirectory)
    {
        System.IO.File.WriteAllText(System.IO.Path.Combine(outputDirectory, _extentFileName), JsonHelper.Serialize(webMercatorBoundingBox));
    }



    private static void SplitIntoGoogleTiles(BitmapImage bitmap, BoundingBox webMercatorBoundingBox, List<TileInfo> googleTiles, string outputDirectory)
    {
        var minRow = googleTiles.Min(i => i.RowNumber);

        var maxRow = googleTiles.Max(i => i.RowNumber);

        var minColumn = googleTiles.Min(i => i.ColumnNumber);

        var maxColumn = googleTiles.Max(i => i.ColumnNumber);

        foreach (var tile in googleTiles)
        {
            var imageSubRegion = tile.WebMercatorExtent.Intersect(webMercatorBoundingBox);

            if (!imageSubRegion.IsValid())
                throw new NotImplementedException();

            //pixels must be copy from this region on the original image
            var sourceBoundingBox = GetImageBoundary(webMercatorBoundingBox, tile.WebMercatorExtent, bitmap.Width, bitmap.Height);

            //pixels must be copy to this region on the tile time
            var destinationBoundingBox = GetImageBoundary(tile.WebMercatorExtent, imageSubRegion, 256, 256);

            //For boundary images use bitmap format

            BitmapEncoder encoder;

            if (tile.ColumnNumber == maxColumn || tile.ColumnNumber == minColumn || tile.RowNumber == minRow || tile.RowNumber == maxRow)
            {
                encoder = CopyRegionIntoImage(bitmap, sourceBoundingBox, destinationBoundingBox, () => new PngBitmapEncoder());
            }
            else
            {
                encoder = CopyRegionIntoImage(bitmap, sourceBoundingBox, destinationBoundingBox, () => new JpegBitmapEncoder());
            }

            Save(encoder, $"{outputDirectory}\\{tile.ToShortString()}.jpg");
        }
    }

    private static void Save(BitmapEncoder encoder, string path)
    {
        using (var filestream = new FileStream(path, FileMode.Create))
        {
            encoder.Save(filestream);
        }
    }

    private static BitmapEncoder CopyRegionIntoImage(BitmapImage srcBitmap, Rect srcRegion, Rect destRegion, Func<BitmapEncoder> encoderMaker)
    {
        CroppedBitmap cropped = new CroppedBitmap(srcBitmap, ToInt32Rect(srcRegion));

        cropped.Freeze();

        var scale = 256.0 / srcRegion.Width;

        var transform = new ScaleTransform(scale, scale);

        DrawingVisual drawingVisual = new DrawingVisual();

        using (DrawingContext drawingContext = drawingVisual.RenderOpen())
        {
            drawingContext.DrawImage(new TransformedBitmap(cropped, transform), destRegion);
        }

        RenderTargetBitmap bmp = new RenderTargetBitmap(256, 256, 96, 96, PixelFormats.Pbgra32);

        bmp.Render(drawingVisual);

        bmp.Freeze();

        var encoder = encoderMaker();

        encoder.Frames.Add(BitmapFrame.Create(bmp));

        return encoder;

    }

    private static Int32Rect ToInt32Rect(Rect rect)
    {
        return new Int32Rect((int)rect.Left, (int)rect.Top, (int)rect.Width, (int)rect.Height);
    }

    private static Rect GetImageBoundary(BoundingBox mapExtent, BoundingBox subMapExtent, double imageWidth, double imageHeight)
    {
        double scale = imageWidth / mapExtent.Width;

        double scaleY = imageHeight / mapExtent.Height;

        if (Math.Abs(scale - scaleY) > 10E-10)
        {
            throw new NotImplementedException();
        }

        //
        double xPixelMin = (subMapExtent.XMin - mapExtent.XMin) * scale;

        xPixelMin = xPixelMin < 0 ? 0 : xPixelMin;

        //
        double xPixelMax = (subMapExtent.XMax - mapExtent.XMin) * scale;

        xPixelMax = xPixelMax > imageWidth ? imageWidth : xPixelMax;

        //
        double yPixelMin = (subMapExtent.YMin - mapExtent.YMin) * scale;

        yPixelMin = yPixelMin < 0 ? 0 : yPixelMin;

        //
        double yPixelMax = (subMapExtent.YMax - mapExtent.YMin) * scale;

        yPixelMax = yPixelMax > imageHeight ? imageHeight : yPixelMax;
        //new RectangleF()
        //return new RectangleF((float)xPixelMin, (float)(imageHeight - yPixelMax), (float)(xPixelMax - xPixelMin), (float)(yPixelMax - yPixelMin));
        return new Rect(xPixelMin, imageHeight - yPixelMax, xPixelMax - xPixelMin, yPixelMax - yPixelMin);
    }
}
