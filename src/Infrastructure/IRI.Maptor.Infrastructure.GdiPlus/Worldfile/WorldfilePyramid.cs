using System.Linq;
using IRI.Maptor.Core.Spatial.Primitives;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Compression;
using IRI.Maptor.Core.Common.Model;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Model;
using IRI.Maptor.Core.Common.Helpers;

namespace IRI.Maptor.Infrastructure.GdiPlus.WorldfileFormat;

public static class WorldfilePyramid
{

    public static void Create(string imageFileName, double pixelSize, string outputDirectory = null)
    {

        using (var bitmap = new Bitmap(imageFileName))
        //using (var bitmap = (Bitmap)Image.FromStream(new System.IO.StreamReader(imageFileName).BaseStream, false, false))
        {
            var worldfileName = WorldfileManager.TryGetAssociatedWorldfileName(imageFileName);

            if (worldfileName == null)
            {
                System.Diagnostics.Debug.Print($"Worldfile not found: '{worldfileName}'");
                return;
            }

            var webMercatorBoundingBox = WorldfileManager.ReadImageBoundingBox(worldfileName, bitmap.Width, bitmap.Height);

            int zoomLevel = WebMercatorUtility.GetZoomLevel(bitmap.Height * pixelSize / webMercatorBoundingBox.Height);

            if (outputDirectory == null)
            {
                outputDirectory = $"{System.IO.Path.GetDirectoryName(imageFileName)}\\{System.IO.Path.GetFileNameWithoutExtension(imageFileName)}";
            }

            while (zoomLevel > 0 && zoomLevel < 20)
            {
                var tiles = WebMercatorUtility.WebMercatorBoundingBoxToGoogleTileRegions(webMercatorBoundingBox, zoomLevel);

                if (tiles.Count < 1)
                    break;

                if (!System.IO.Directory.Exists($"{outputDirectory}\\{zoomLevel}"))
                {
                    System.IO.Directory.CreateDirectory($"{outputDirectory}\\{zoomLevel}");
                }

                SplitIntoGoogleTiles(bitmap, webMercatorBoundingBox, tiles, $"{outputDirectory}\\{zoomLevel}");

                zoomLevel--;
            }

            var zipFileName = System.IO.Path.ChangeExtension(imageFileName, "pyrmd");

            if (System.IO.File.Exists(zipFileName))
                System.IO.File.Delete(zipFileName);

            ZipFile.CreateFromDirectory(outputDirectory, System.IO.Path.ChangeExtension(imageFileName, "pyrmd"));

            IOHelper.DeleteDirectory(outputDirectory);
        }
    }

    private static void SplitIntoGoogleTiles(Bitmap bitmap, BoundingBox webMercatorBoundingBox, List<TileInfo> googleTiles, string outputDirectory)
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

            var tileImage = CopyRegionIntoImage(bitmap, sourceBoundingBox, destinationBoundingBox);

            //For boundary images use bitmap format
            //if (tile.ColumnNumber == maxColumn || tile.ColumnNumber == minColumn || tile.RowNumber == minRow || tile.RowNumber == maxRow)
            //{
            //    tileImage.Save($"{outputDirectory}\\{tile.ToShortString()}.jpg", System.Drawing.Imaging.ImageFormat.Png);
            //}
            //else
            //{
            //    tileImage.Save($"{outputDirectory}\\{tile.ToShortString()}.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
            //}

            if (tile.ColumnNumber == maxColumn || tile.ColumnNumber == minColumn || tile.RowNumber == minRow || tile.RowNumber == maxRow)
            {
                tileImage.Save(System.IO.Path.Combine(outputDirectory, $"{tile.ToShortString()}.jpg"), System.Drawing.Imaging.ImageFormat.Png);
            }
            else
            {
                tileImage.Save(System.IO.Path.Combine(outputDirectory, $"{tile.ToShortString()}.jpg"), System.Drawing.Imaging.ImageFormat.Jpeg);
            }

        }
    }

    private static Bitmap CopyRegionIntoImage(Bitmap srcBitmap, RectangleF srcRegion, RectangleF destRegion)
    {
        Bitmap result = new Bitmap(256, 256);

        using (Graphics grD = Graphics.FromImage(result))
        {
            grD.DrawImage(srcBitmap, destRegion, srcRegion, GraphicsUnit.Pixel);
        }

        return result;
    }

    private static RectangleF GetImageBoundary(BoundingBox mapExtent, BoundingBox subMapExtent, double imageWidth, double imageHeight)
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

        return new RectangleF((float)xPixelMin, (float)(imageHeight - yPixelMax), (float)(xPixelMax - xPixelMin), (float)(yPixelMax - yPixelMin));
    }
}
