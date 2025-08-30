using System;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;

using IRI.Maptor.Sta.Spatial.Model;
using IRI.Maptor.Ket.GdiPlus.WorldfileFormat;


namespace IRI.Maptor.Jab.Common.Helpers;

public static class ImageUtility
{
    public static BitmapImage CreateBitmapImage(Uri uri)
    {
        BitmapImage result = new BitmapImage();

        result.BeginInit();

        result.UriSource = uri;

        result.CacheOption = BitmapCacheOption.OnLoad;

        result.EndInit();

        return result;
    }

    public static BitmapImage CreateBitmapImage(string uri, int decodePixelHeight)
    {
        BitmapImage result = new BitmapImage();

        result.BeginInit();

        result.DecodePixelHeight = decodePixelHeight;

        result.UriSource = new Uri(uri);

        result.CacheOption = BitmapCacheOption.OnLoad;

        result.EndInit();

        return result;
    }
      
    public static BitmapImage? CreateBitmapImage(byte[] array, int? decodePixelWidth = null, int? decodePixelHeight = null)
    {
        BitmapImage image = new BitmapImage();

        try
        {
            using (System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(array))
            {
                memoryStream.Position = 0;

                image.BeginInit();

                if (decodePixelHeight.HasValue)
                    image.DecodePixelHeight = decodePixelHeight.Value;

                if (decodePixelWidth.HasValue)
                    image.DecodePixelWidth = decodePixelWidth.Value;

                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;

                image.CacheOption = BitmapCacheOption.OnLoad;

                image.StreamSource = memoryStream;

                image.EndInit();

                image.Freeze();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"IRI.Maptor.Jab.Common > Helpers > ImageUtility > ToImage {ex.Message}");
            return null;
        }

        return image;
    }

    public static BitmapImage CreateBitmapImage(System.Drawing.Bitmap bitmap, System.Drawing.Imaging.ImageFormat format)
    {
        BitmapImage result = new BitmapImage();

        using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
        {
            bitmap.Save(stream, format);

            stream.Position = 0;

            result.BeginInit();

            result.CacheOption = BitmapCacheOption.OnLoad;

            result.StreamSource = stream;

            result.EndInit();
        }

        return result;
    }

    public static BitmapImage CreateBitmapImage(WriteableBitmap writeableBitmap)
    {
        BitmapImage bmImage = new BitmapImage();

        using (MemoryStream stream = new MemoryStream())
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(writeableBitmap));
            encoder.Save(stream);
            bmImage.BeginInit();
            bmImage.CacheOption = BitmapCacheOption.OnLoad;
            bmImage.StreamSource = stream;
            bmImage.EndInit();
            bmImage.Freeze();
        }
        return bmImage;
    }

    public static BitmapImage CreateBitmapImage(this RenderTargetBitmap rtb)
    {
        var bitmapImage = new BitmapImage();

        var bitmapEncoder = new PngBitmapEncoder();

        bitmapEncoder.Frames.Add(BitmapFrame.Create(rtb));

        using (var stream = new MemoryStream())
        {
            bitmapEncoder.Save(stream);
            stream.Seek(0, SeekOrigin.Begin);

            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
        }

        return bitmapImage;
    }


    public static BitmapSource AsBitmapSource(System.Drawing.Bitmap bitmap)
    {
        return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                                bitmap.GetHbitmap(), 
                                                IntPtr.Zero,
                                                System.Windows.Int32Rect.Empty, 
                                                BitmapSizeOptions.FromWidthAndHeight(bitmap.Width, bitmap.Height));
    }

    public static RenderTargetBitmap Render(List<DrawingVisual> drawingVisuals, int screenWidth, int screenHeight)
    {
        RenderTargetBitmap image = new RenderTargetBitmap(screenWidth, screenHeight, 96, 96, PixelFormats.Pbgra32);

        foreach (var item in drawingVisuals)
        {
            image.Render(item);
        }

        image.Freeze();

        return image;
    }

    public static void MergeAndSave(string fileName, List<DrawingVisual> drawingVisuals, int width, int height, BitmapEncoder? preferedEncoder = null)
    {
        if (width == 0 || height == 0)
            return;

        RenderTargetBitmap image = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

        foreach (var drawingVisual in drawingVisuals)
        {
            image.Render(drawingVisual);
        }

        Save(fileName, image, preferedEncoder); 
    }

    public static void Save(string fileName, RenderTargetBitmap? image, BitmapEncoder? preferedEncoder = null)
    {
        if (image is null)
            return;

        var frame = BitmapFrame.Create(image);

        BitmapEncoder encoder = preferedEncoder ?? new PngBitmapEncoder();

        encoder.Frames.Add(frame);

        using (Stream stream = File.Create(fileName))
        {
            encoder.Save(stream);
        }
    }


    public static Image Create(double width, double height, DrawingVisual drawing)
    {
        Image result = new Image();

        RenderTargetBitmap bmp = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);

        bmp.Render(drawing);

        bmp.Freeze();

        result.Source = bmp;

        return result;
    }

    public static Image Create(double width, double height, DrawingVisual drawing, Transform transform)
    {
        Image result = new Image();

        RenderTargetBitmap bmp = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);

        bmp.Render(drawing);

        bmp.Freeze();

        result.Source = bmp;

        result.RenderTransform = transform;

        return result;
    }

    public static System.Drawing.Image AsGdiPlusImage(this System.Windows.Media.ImageSource image)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        {
            var encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image as BitmapSource));
            encoder.Save(memoryStream);
            memoryStream.Flush();
            return System.Drawing.Image.FromStream(memoryStream);
        }

    }

    #region Byte array

    public static System.Drawing.Bitmap ToGdiImage(byte[] array)
    {
        using (var ms = new MemoryStream(array))
        {
            return (System.Drawing.Bitmap)System.Drawing.Image.FromStream(ms);
        }
    }

    public static byte[] AsByteArray(System.Drawing.Image image)
    {
        try
        {
            System.Drawing.ImageConverter converter = new System.Drawing.ImageConverter();

            return (byte[])converter.ConvertTo(image, typeof(byte[]));
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static byte[] AsByteArray(BitmapImage image, BitmapEncoder encoder)
    {
        byte[] result;

        encoder.Frames.Add(BitmapFrame.Create(image));

        using (MemoryStream ms = new MemoryStream())
        {
            encoder.Save(ms);

            result = ms.ToArray();
        }

        return result;
    }

    public static byte[] AsByteArray(BitmapImage imageSource)
    {
        Stream stream = imageSource.StreamSource;

        Byte[] buffer = null;

        if (stream != null && stream.Length > 0)
        {
            using (BinaryReader br = new BinaryReader(stream))
            {
                buffer = br.ReadBytes((int)stream.Length);
            }
        }

        return buffer;
    }

    #endregion


    #region TileInfo

    public static void MergeTilesAndSaveByWpf(List<TileInfo> tiles, Func<TileInfo, string> fileNameFunc, string outputFileName)
    {
        if (tiles == null)
        {
            return;
        }

        var minX = tiles.Min(t => t.ColumnNumber);

        var minY = tiles.Min(t => t.RowNumber);

        var width = tiles.Max(t => t.ColumnNumber) - minX + 1;

        var height = tiles.Max(t => t.RowNumber) - minY + 1;

        DrawingVisual drawingVisual = new DrawingVisual();

        using (DrawingContext drawingContext = drawingVisual.RenderOpen())
        {
            foreach (var tile in tiles)
            {
                var fileName = fileNameFunc(tile);

                if (!File.Exists(fileName))
                    continue;

                BitmapFrame frame = BitmapDecoder.Create(new Uri(fileName), BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames.First();

                drawingContext.DrawImage(frame, new System.Windows.Rect((tile.ColumnNumber - minX) * 256, (tile.RowNumber - minY) * 256, 256, 256));
            }
        }

        RenderTargetBitmap bmp = new RenderTargetBitmap(width * 256, height * 256, 96, 96, PixelFormats.Pbgra32);

        bmp.Render(drawingVisual);

        // Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder
        JpegBitmapEncoder encoder = new JpegBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bmp));

        // Saves the image into a file using the encoder
        using (Stream stream = File.Create(outputFileName))
            encoder.Save(stream);

    }

    public static void MergeTilesAndSaveByGdiplus(List<TileInfo> tiles, Func<TileInfo, string> fileNameFunc, string outputFileName,
        System.Drawing.Imaging.ImageFormat imageFormat,
        System.Drawing.Imaging.PixelFormat format = System.Drawing.Imaging.PixelFormat.Format24bppRgb,
        string? waterMarkText = null)
    {
        if (tiles == null || tiles.Count < 1)
        {
            return;
        }

        //var sizeoff = System.Drawing.Image.GetPixelFormatSize(System.Drawing.Imaging.PixelFormat.Format24bppRgb);

        var minX = tiles.Min(t => t.ColumnNumber);

        var minY = tiles.Min(t => t.RowNumber);

        var width = tiles.Max(t => t.ColumnNumber) - minX + 1;

        var height = tiles.Max(t => t.RowNumber) - minY + 1;

        var outputImage = new System.Drawing.Bitmap(width * 256, height * 256, format);

        using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(outputImage))
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                var fileName = fileNameFunc(tiles[i]);

                if (!File.Exists(fileName))
                    continue;

                var image = System.Drawing.Image.FromFile(fileName);

                graphics.DrawImage(image, new System.Drawing.Rectangle((tiles[i].ColumnNumber - minX) * 256, (tiles[i].RowNumber - minY) * 256, 256, 256),
                                        new System.Drawing.Rectangle(0, 0, 256, 256), System.Drawing.GraphicsUnit.Pixel);
            }

        }

        //if (!string.IsNullOrWhiteSpace(waterMarkText))
        //{
        //    AddWaterMark(outputImage, waterMarkText, Extensions.BrushHelper.AsGdiBrush(System.Drawing.Color.White, .7));
        //}

        //if imageFormat not provided, then it will use png format
        outputImage.Save(outputFileName, imageFormat);

        var worldFile = WorldfileManager.Create(tiles.GetTotalImageBoundsInWebMercator(), width * 256, height * 256);

        WorldfileManager.SaveWorldFile(WorldfileManager.MakeAssociatedWorldfileName(outputFileName), worldFile);
    }

    public static bool HasAnyImage(List<TileInfo> tiles, Func<TileInfo, string> fileNameFunc)
    {
        if (tiles == null || tiles.Count < 1)
        {
            return false;
        }

        for (int i = 0; i < tiles.Count; i++)
        {
            var fileName = fileNameFunc(tiles[i]);

            if (File.Exists(fileName))
                return true;
        }

        return false;
    }

    public static bool HasAllImage(string folderPath, List<TileInfo> tiles, Func<TileInfo, string> fileNameFunc)
    {
        if (tiles == null || tiles.Count < 1 || !System.IO.Directory.Exists(folderPath))
        {
            return false;
        }

        var files = new HashSet<string>(System.IO.Directory.EnumerateFiles(folderPath));

        for (int i = 0; i < tiles.Count; i++)
        {
            var fileName = fileNameFunc(tiles[i]);

            if (!files.Contains(fileName))
            {
                return false;
            }
        }

        return true;
    }

    public static void MergeTilesAndSaveByGdiplusInGeodetic(List<TileInfo> tiles, Func<TileInfo, string> fileNameFunc, string outputFileName, string? waterMarkText = null)
    {
        if (tiles == null || tiles.Count < 1)
        {
            return;
        }

        var minX = tiles.Min(t => t.ColumnNumber);

        var minY = tiles.Min(t => t.RowNumber);

        var width = tiles.Max(t => t.ColumnNumber) - minX + 1;

        var height = tiles.Max(t => t.RowNumber) - minY + 1;

        var outputImage = new System.Drawing.Bitmap(width * 256, height * 256, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        var geodeticRegionHeight = tiles.Max(i => i.GeodeticExtent.YMax) - tiles.Min(i => i.GeodeticExtent.YMin);

        using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(outputImage))
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                var fileName = fileNameFunc(tiles[i]);

                if (!File.Exists(fileName))
                    continue;

                var image = System.Drawing.Image.FromFile(fileName);

                var subImageHeight = outputImage.Height * tiles[i].GeodeticExtent.Height / geodeticRegionHeight;

                graphics.DrawImage(image, new System.Drawing.Rectangle((tiles[i].ColumnNumber - minX) * 256, (tiles[i].RowNumber - minY) * 256, 256, 256),
                                        new System.Drawing.Rectangle(0, 0, 256, 256), System.Drawing.GraphicsUnit.Pixel);

            }

        }

        //if (!string.IsNullOrWhiteSpace(waterMarkText))
        //{
        //    AddWaterMark(outputImage, waterMarkText, Extensions.BrushHelper.AsGdiBrush(System.Drawing.Color.White, .7));
        //}

        outputImage.Save(outputFileName);

        var worldFile = WorldfileManager.Create(tiles.GetTotalImageBoundsInGeodetic(), width * 256, height * 256);

        WorldfileManager.SaveWorldFile(WorldfileManager.MakeAssociatedWorldfileName(outputFileName), worldFile);
    }

    #endregion


    #region Other

    public static void AddWaterMark(System.Drawing.Bitmap image, string waterMarkText, System.Drawing.Brush brush)
    {
        var font = new System.Drawing.Font("Times New Roman", 14);

        using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(image))
        {
            for (int i = 0; i < image.Width; i += 256)
            {
                for (int j = 0; j < image.Height; j += 256)
                {
                    graphics.DrawString(waterMarkText, font, brush, i, j);
                }
            }

        }
    }

    public static long CalculateBitmapSize(int width, int height, int bitsPerPixel = 24)
    {
        //54 byte: bmp header size
        return (long)(54.0 + (bitsPerPixel / 8.0) * width * height);
    }

    public static bool CompareBitmaps(System.Drawing.Image left, System.Drawing.Image right)
    {
        if (object.Equals(left, right))
            return true;
        if (left == null || right == null)
            return false;
        if (!left.Size.Equals(right.Size) || !left.PixelFormat.Equals(right.PixelFormat))
            return false;

        System.Drawing.Bitmap leftBitmap = left as System.Drawing.Bitmap;
        System.Drawing.Bitmap rightBitmap = right as System.Drawing.Bitmap;
        if (leftBitmap == null || rightBitmap == null)
            return true;

        #region Optimized code for performance

        int bytes = left.Width * left.Height * (System.Drawing.Image.GetPixelFormatSize(left.PixelFormat) / 8);

        bool result = true;
        byte[] b1bytes = new byte[bytes];
        byte[] b2bytes = new byte[bytes];

        System.Drawing.Imaging.BitmapData bmd1 = leftBitmap.LockBits(new System.Drawing.Rectangle(0, 0, leftBitmap.Width - 1, leftBitmap.Height - 1), System.Drawing.Imaging.ImageLockMode.ReadOnly, leftBitmap.PixelFormat);
        System.Drawing.Imaging.BitmapData bmd2 = rightBitmap.LockBits(new System.Drawing.Rectangle(0, 0, rightBitmap.Width - 1, rightBitmap.Height - 1), System.Drawing.Imaging.ImageLockMode.ReadOnly, rightBitmap.PixelFormat);

        Marshal.Copy(bmd1.Scan0, b1bytes, 0, bytes);
        Marshal.Copy(bmd2.Scan0, b2bytes, 0, bytes);

        for (int n = 0; n <= bytes - 1; n++)
        {
            if (b1bytes[n] != b2bytes[n])
            {
                result = false;
                break;
            }
        }

        leftBitmap.UnlockBits(bmd1);
        rightBitmap.UnlockBits(bmd2);

        #endregion

        return result;
    }

    public static System.Drawing.Bitmap ChangeOpacity(System.Drawing.Bitmap image, double opacityvalue)
    {
        System.Drawing.Bitmap bmp = new System.Drawing.Bitmap((int)image.Width, (int)image.Height); // Determining Width and Height of Source Image

        System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bmp);

        System.Drawing.Imaging.ColorMatrix colormatrix = new System.Drawing.Imaging.ColorMatrix();

        colormatrix.Matrix33 = (float)GetOpacity(opacityvalue);

        System.Drawing.Imaging.ImageAttributes imgAttribute = new System.Drawing.Imaging.ImageAttributes();

        imgAttribute.SetColorMatrix(colormatrix, System.Drawing.Imaging.ColorMatrixFlag.Default, System.Drawing.Imaging.ColorAdjustType.Bitmap);

        graphics.DrawImage(image, new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, image.Width, image.Height, System.Drawing.GraphicsUnit.Pixel, imgAttribute);

        graphics.Dispose();   // Releasing all resource used by graphics 

        return bmp;
    }

    private static double GetOpacity(double opacity) => opacity < 0 ? 0 : opacity > 1 ? 1 : opacity;
     
    #endregion
}
