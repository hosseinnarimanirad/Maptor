using System.Text;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.IO;
using IRI.Maptor.Sta.Common.Model;
using IRI.Maptor.Sta.Spatial.Model;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Ket.GdiPlus.WorldfileFormat;

public static class WorldfileManager
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="worldFileName"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static BoundingBox ReadImageBoundingBox(string worldFileName, int width, int height)
    {
        if (!System.IO.File.Exists(worldFileName))
        {
            throw new NotImplementedException();
        }

        string[] lines = System.IO.File.ReadAllLines(worldFileName);

        double xPixelSize = double.Parse(lines[0]);

        double rotationAboutY = double.Parse(lines[1]);

        double rotationAboutX = double.Parse(lines[2]);

        double yPixelSize = double.Parse(lines[3]);

        yPixelSize = (yPixelSize > 0) ? yPixelSize : -yPixelSize;

        double xOfCenterOfUpperLeftPixel = double.Parse(lines[4]);

        double yOfCenterOfUpperLeftPixel = double.Parse(lines[5]);

        return new BoundingBox(xMin: xOfCenterOfUpperLeftPixel - xPixelSize / 2.0,
                                            yMin: (yOfCenterOfUpperLeftPixel + yPixelSize / 2.0) - yPixelSize * height,
                                            xMax: (xOfCenterOfUpperLeftPixel - xPixelSize / 2.0) + xPixelSize * width,
                                            yMax: yOfCenterOfUpperLeftPixel + yPixelSize / 2.0);
    }

    public static void SaveWorldFile(string fileName, Worldfile worldfile)
    {
        StringBuilder lines = new StringBuilder();

        lines.AppendLine(worldfile.XPixelSize.ToInvariantString());
        lines.AppendLine(worldfile.YRotation.ToInvariantString());
        lines.AppendLine(worldfile.XRotation.ToInvariantString());
        lines.AppendLine((-worldfile.YPixelSize).ToInvariantString());

        lines.AppendLine(worldfile.CenterOfUpperLeftPixel.X.ToInvariantString());
        lines.AppendLine(worldfile.CenterOfUpperLeftPixel.Y.ToInvariantString());

        System.IO.File.WriteAllText(fileName, lines.ToString());
    }

    public static GeoReferencedImage? ReadWorldfile(string imageFileName, int srid)
    {
        if (!System.IO.File.Exists(imageFileName))
            return null;

        var size = IRI.Maptor.Ket.GdiPlus.Helpers.ImageHelper.GetSize(imageFileName);

        var worldFileName = TryGetAssociatedWorldfileName(imageFileName);
         
        try
        {
            BoundingBox boundingBox;

            if (srid == SridHelper.GeodeticWGS84)
            {
                boundingBox = ReadImageBoundingBox(worldFileName, size.Width, size.Height);
            }
            else if (srid == SridHelper.WebMercator)
            {
                boundingBox = ReadImageBoundingBox(worldFileName, size.Width, size.Height).Transform(MapProjects.WebMercatorToGeodeticWgs84);
            }
            else
            {
                throw new NotImplementedException("not supported srid for worldfile");
            }

            return new GeoReferencedImage(System.IO.File.ReadAllBytes(imageFileName), boundingBox);
        }
        catch (Exception)
        {
            return null;
        }

    }

    public static Worldfile? TryReadAssociatedWorldfile(string imageFileName)
    {
        var worldFileName = TryGetAssociatedWorldfileName(imageFileName);

        if (worldFileName != null)
        {
            return Worldfile.Read(worldFileName);
        }

        return null;
    }

    public static string? TryGetAssociatedWorldfileName(string imageFileName)
    {
        string worldFileName = string.Empty;

        var extension = System.IO.Path.GetExtension(imageFileName).ToLower();

        switch (extension)
        {
            case ".bmp":
                worldFileName = System.IO.Path.ChangeExtension(imageFileName, "bpw");

                if (!System.IO.File.Exists(worldFileName))
                    worldFileName = System.IO.Path.ChangeExtension(imageFileName, "bmpw");
                break;

            case ".jpg":
            case ".jpeg":
                worldFileName = System.IO.Path.ChangeExtension(imageFileName, "jpgw");

                if (!System.IO.File.Exists(worldFileName))
                    worldFileName = System.IO.Path.ChangeExtension(imageFileName, "jpegw");

                if (!System.IO.File.Exists(worldFileName))
                    worldFileName = System.IO.Path.ChangeExtension(imageFileName, "jgw");
                break;

            case ".tif":
                worldFileName = System.IO.Path.ChangeExtension(imageFileName, "tfw");
                break;

            case ".png":
                worldFileName = System.IO.Path.ChangeExtension(imageFileName, "pgw");
                break;

            default:
                break;
        }

        if (System.IO.File.Exists(worldFileName))
        {
            return worldFileName;
        }

        return null;
    }

    public static string MakeAssociatedWorldfileName(string imageFileName)
    {
        var extension = IOHelper.GetExtensionWithoutDot(imageFileName);

        return System.IO.Path.ChangeExtension(imageFileName, $"{extension}w");
    }

    public static Worldfile Create(BoundingBox mapBoundingBox, int imagePixelWidth, int imagePixelHeight)
    {
        var xPixelSize = mapBoundingBox.Width / imagePixelWidth;

        var yPixelSize = mapBoundingBox.Height / imagePixelHeight;

        return new Worldfile(xPixelSize, yPixelSize, new Point(mapBoundingBox.XMin + xPixelSize / 2.0, mapBoundingBox.YMax - yPixelSize / 2.0));
    }

    public static Worldfile CreateByGeodeticValues(TileInfo tile, int imagePixelWidth = 256, int imagePixelHeight = 256)
    {
        return Create(tile.GeodeticExtent, imagePixelWidth, imagePixelHeight);
    }

    public static Worldfile CreateByWebMercatorValues(TileInfo tile, int imagePixelWidth = 256, int imagePixelHeight = 256)
    {
        return Create(tile.WebMercatorExtent, imagePixelWidth, imagePixelHeight);
    }
}

