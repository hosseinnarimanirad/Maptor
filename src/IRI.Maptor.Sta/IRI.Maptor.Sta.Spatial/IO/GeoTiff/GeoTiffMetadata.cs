using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Sta.Spatial.IO;

public class GeoTiffMetadata
{
    /// <summary>
    /// Pixel scale: [scaleX, scaleY, scaleZ]
    /// </summary>
    public double[] PixelScale { get; set; }

    /// <summary>
    /// Tiepoints array, stored in groups of 6:
    /// [i, j, k, X, Y, Z, i2, j2, k2, X2, Y2, Z2, ...]
    /// </summary>
    public double[] TiePoints { get; set; }

    /// <summary>
    /// Optional 4x4 transformation matrix (16 values).
    /// If present, this overrides PixelScale/TiePoints.
    /// </summary>
    public double[] Transformation { get; set; }

    /// <summary>
    /// Raw GeoKeyDirectory (defines CRS, projection, etc.).
    /// </summary>
    public ushort[] GeoKeyDirectory { get; set; }

    /// <summary>
    /// GeoTIFF double parameter values.
    /// </summary>
    public double[] GeoDoubleParams { get; set; }

    /// <summary>
    /// GeoTIFF ASCII parameter values.
    /// </summary>
    public string GeoAsciiParams { get; set; }

    // Dimensions (from TIFF tags 256, 257)
    // in pixels
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }

    /// <summary>
    /// Pixel size in X direction (map units per pixel).
    /// </summary>
    public double XPixelSize => HasPixelScale ? PixelScale[0] : 0;

    /// <summary>
    /// Pixel size in Y direction (map units per pixel).
    /// Usually negative if Y increases upward in map coordinates.
    /// </summary>
    public double YPixelSize => HasPixelScale ? PixelScale[1] : 0;

    /// <summary>
    /// Center of the upper-left pixel in world coordinates.
    /// Uses tiepoints and pixel scale if available.
    /// </summary>
    public Point? CenterOfUpperLeftPixel
    {
        get
        {
            if (!HasPixelScale || !HasTiePoints)
                return null;

            double X0 = TiePoints[3]; // world X of pixel (0,0)

            double Y0 = TiePoints[4]; // world Y of pixel (0,0)

            return new Point(
                X0 + XPixelSize / 2.0,
                Y0 - YPixelSize / 2.0
            );
        }
    }


    public bool HasPixelScale => PixelScale != null && PixelScale.Length >= 2;
    public bool HasTiePoints => TiePoints != null && TiePoints.Length >= 6;
    public bool HasTransformation => Transformation != null && Transformation.Length == 16;


    /// <summary>
    /// Builds an affine transform (pixel → world).
    /// Uses Transformation matrix if available, otherwise PixelScale + TiePoints.
    /// </summary>
    public double[,] GetAffineTransform()
    {
        if (HasTransformation)
        {
            // 4x4 affine stored row-major
            var M = new double[4, 4];
            for (int i = 0; i < 16; i++)
                M[i / 4, i % 4] = Transformation[i];
            return M;
        }
        else if (HasPixelScale && HasTiePoints)
        {
            // Standard affine from tiepoint (0,0) and pixel scale
            // X = X0 + i*scaleX
            // Y = Y0 + j*scaleY
            double X0 = TiePoints[3];

            double Y0 = TiePoints[4];

            return new double[,]
            {
                { XPixelSize, 0, 0, X0 },
                { 0, YPixelSize, 0, Y0 },
                { 0, 0, 1, 0 },
                { 0, 0, 0, 1 }
            };
        }
        else
        {
            throw new InvalidOperationException("Cannot compute affine transform (missing metadata).");
        }
    }

    /// <summary>
    /// Maps a pixel coordinate (col, row) to world coordinates.
    /// </summary>
    public Point PixelToWorld(int col, int row)
    {
        var M = GetAffineTransform();

        double X = M[0, 0] * col + M[0, 1] * row + M[0, 3];

        double Y = M[1, 0] * col + M[1, 1] * row + M[1, 3];

        return new Point(X, Y);
    }

    public BoundingBox GetGeodeticWgs84BoundingBox()
    {
        if (string.IsNullOrEmpty(this.GeoAsciiParams) || 
            !this.GeoAsciiParams.Contains(SridHelper.GeodeticWGS84Name))
            throw new NotImplementedException();

        if (CenterOfUpperLeftPixel is null)
            throw new InvalidOperationException("Missing tiepoints or pixel scale.");
         
        double xMin = CenterOfUpperLeftPixel.X - XPixelSize / 2.0;
        double yMax = CenterOfUpperLeftPixel.Y + YPixelSize / 2.0;

        double xMax = xMin + XPixelSize * ImageWidth;
        double yMin = yMax - YPixelSize * ImageHeight;

        return new BoundingBox(xMin, yMin, xMax, yMax);

        //return new BoundingBox(xMin: xOfCenterOfUpperLeftPixel - xPixelSize / 2.0,
        //                        yMin: (yOfCenterOfUpperLeftPixel + yPixelSize / 2.0) - yPixelSize * ImageHeight,
        //                        xMax: (xOfCenterOfUpperLeftPixel - xPixelSize / 2.0) + xPixelSize * ImageWidth,
        //                        yMax: yOfCenterOfUpperLeftPixel + yPixelSize / 2.0);
    }
}
