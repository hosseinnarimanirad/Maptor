using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Spatial.IO;

public class Worldfile
{ 
    public double XPixelSize { get; set; } 
     
    public double YPixelSize { get; set; } 
     
    public double XRotation { get; set; } 
      
    public double YRotation { get; set; }

    public Point CenterOfUpperLeftPixel { get; set; }

    public double GroundXMin => CenterOfUpperLeftPixel.X - XPixelSize / 2.0;

    public double GroundYMax => CenterOfUpperLeftPixel.Y + YPixelSize / 2.0;

    public Worldfile()
    {
    }

    public Worldfile(double xPixelSize, double yPixelSize, Point centerOfUpperLeftPixel)
        : this(xPixelSize, yPixelSize, 0, 0, centerOfUpperLeftPixel)
    {

    }

    public Worldfile(double xPixelSize, double yPixelSize, double xRotation, double yRotation, Point centerOfUpperLeftPixel)
    {
        XPixelSize = xPixelSize;

        YPixelSize = yPixelSize;

        CenterOfUpperLeftPixel = centerOfUpperLeftPixel;

        XRotation = xRotation;

        YRotation = yRotation;
    }


    public BoundingBox GetBoundingBox(int imagePixelWidth, int imagePixelHeight)
    {
        return new BoundingBox(xMin: GroundXMin,
                                yMin: CenterOfUpperLeftPixel.Y + YPixelSize / 2.0 - YPixelSize * imagePixelHeight,
                                xMax: CenterOfUpperLeftPixel.X - XPixelSize / 2.0 + XPixelSize * imagePixelWidth,
                                yMax: GroundYMax);
    }

    public Point ToImageCoordinate(Point groundCoordinate, int imagePixelWidth, int imagePixelHeight)
    {
        var groundWidth = XPixelSize * imagePixelWidth;

        var groundHeight = YPixelSize * imagePixelHeight;

        var x = (groundCoordinate.X - GroundXMin) * imagePixelWidth / groundWidth;

        var y = (GroundYMax - groundCoordinate.Y) * imagePixelHeight / groundHeight;

        return new Point(x, y);
    }
     
    public Point ToGroundCoordinate(Point imageCoordinate, int imagePixelWidth, int imagePixelHeight)
    {
        var groundWidth = XPixelSize * imagePixelWidth;

        var groundHeight = YPixelSize * imagePixelHeight;

        var x = imageCoordinate.X * groundWidth / imagePixelWidth + GroundXMin;

        var y = GroundYMax - imageCoordinate.Y * groundHeight / imagePixelHeight;

        return new Point(x, y);
    }

    public static Worldfile Read(string worldFileName)
    {
        string[] lines = System.IO.File.ReadAllLines(worldFileName);

        double xPixelSize = double.Parse(lines[0]);

        double rotationAboutY = double.Parse(lines[1]);

        double rotationAboutX = double.Parse(lines[2]);

        double yPixelSize = double.Parse(lines[3]);

        yPixelSize = yPixelSize > 0 ? yPixelSize : -yPixelSize;

        double xOfCenterOfUpperLeftPixel = double.Parse(lines[4]);

        double yOfCenterOfUpperLeftPixel = double.Parse(lines[5]);

        return new Worldfile(xPixelSize, yPixelSize, rotationAboutX, rotationAboutY, new Point(xOfCenterOfUpperLeftPixel, yOfCenterOfUpperLeftPixel));
    }

    public override string ToString()
    {
        return $"Upper Left Center: {CenterOfUpperLeftPixel}, XPixelSize: {XPixelSize}, YPixelSize: {YPixelSize}";
    }
}
