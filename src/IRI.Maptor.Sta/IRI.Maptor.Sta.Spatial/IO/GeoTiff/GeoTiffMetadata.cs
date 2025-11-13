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

    /// <summary>
    /// Creates GeoTIFF metadata from a bounding box, image dimensions, and SRID.
    /// </summary>
    public static GeoTiffMetadata Create(BoundingBox boundingBox, int imageWidth, int imageHeight, int srid)
    {
        var metadata = new GeoTiffMetadata
        {
            ImageWidth = imageWidth,
            ImageHeight = imageHeight
        };

        // Calculate pixel sizes
        double pixelSizeX = boundingBox.Width / imageWidth;
        double pixelSizeY = boundingBox.Height / imageHeight;

        // Set pixel scale: [pixelSizeX, pixelSizeY, 0]
        metadata.PixelScale = new double[] { pixelSizeX, pixelSizeY, 0.0 };

        // Set tiepoints: [0, 0, 0, upperLeftX, upperLeftY, 0]
        // Upper-left corner world coordinates (pixel 0,0)
        double upperLeftX = boundingBox.XMin;
        double upperLeftY = boundingBox.YMax;
        metadata.TiePoints = new double[] { 0.0, 0.0, 0.0, upperLeftX, upperLeftY, 0.0 };

        // Build GeoKeyDirectory
        metadata.GeoKeyDirectory = BuildGeoKeyDirectory(srid);

        // Set GeoAsciiParams
        metadata.GeoAsciiParams = srid == SridHelper.WebMercator ? "WGS 84 / Pseudo-Mercator\0" : "WGS 84\0";

        return metadata;
    }

    /// <summary>
    /// Builds a GeoKeyDirectory array from an SRID.
    /// </summary>
    private static ushort[] BuildGeoKeyDirectory(int srid)
    {
        List<ushort> geoKeys = new List<ushort>();

        // Header: [version, revision, minor revision, number of keys]
        geoKeys.Add(1); // Version
        geoKeys.Add(1); // Revision
        geoKeys.Add(0); // Minor revision

        bool isGeographic = srid == SridHelper.GeodeticWGS84;
        int numKeys = isGeographic ? 3 : 4;
        geoKeys.Add((ushort)numKeys); // Number of keys

        if (isGeographic)
        {
            // Key 1: GTModelTypeGeoKey (1024) = ModelTypeGeographic (2)
            geoKeys.Add(1024); // Key ID
            geoKeys.Add(0); // Location (0 = value in tag)
            geoKeys.Add(1); // Count
            geoKeys.Add(2); // Value (ModelTypeGeographic)

            // Key 2: GeographicTypeGeoKey (2048) = WGS84
            geoKeys.Add(2048); // Key ID
            geoKeys.Add(0); // Location
            geoKeys.Add(1); // Count
            geoKeys.Add(4326); // Value (WGS84)

            // Key 3: GeogAngularUnitsGeoKey (2054) = degrees
            geoKeys.Add(2054); // Key ID
            geoKeys.Add(0); // Location
            geoKeys.Add(1); // Count
            geoKeys.Add(9102); // Value (degrees)
        }
        else
        {
            // Key 1: GTModelTypeGeoKey (1024) = ModelTypeProjected (1)
            geoKeys.Add(1024); // Key ID
            geoKeys.Add(0); // Location (0 = value in tag)
            geoKeys.Add(1); // Count
            geoKeys.Add(1); // Value (ModelTypeProjected)

            // Key 2: ProjectedCSTypeGeoKey (3072) = EPSG code
            geoKeys.Add(3072); // Key ID
            geoKeys.Add(0); // Location
            geoKeys.Add(1); // Count
            geoKeys.Add((ushort)srid); // Value (EPSG code, e.g., 3857)

            // Key 3: GeographicTypeGeoKey (2048) = WGS84
            geoKeys.Add(2048); // Key ID
            geoKeys.Add(0); // Location
            geoKeys.Add(1); // Count
            geoKeys.Add(4326); // Value (WGS84)

            // Key 4: GeogAngularUnitsGeoKey (2054) = degrees
            geoKeys.Add(2054); // Key ID
            geoKeys.Add(0); // Location
            geoKeys.Add(1); // Count
            geoKeys.Add(9102); // Value (degrees)
        }

        return geoKeys.ToArray();
    }
}
