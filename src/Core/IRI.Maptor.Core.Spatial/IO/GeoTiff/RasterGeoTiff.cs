using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Common.Mathematics;

namespace IRI.Maptor.Core.Spatial.IO;

public class RasterGeoTiff
{
    public BoundingBox GeodeticWgs84BoundingBox { get; set; }

    public Matrix? Data { get; set; }

    public RasterGeoTiff(Matrix? data, BoundingBox geodeticBoundingBox)
    {
        this.Data = data;

        this.GeodeticWgs84BoundingBox = geodeticBoundingBox;


    }

    public static RasterGeoTiff NaN { get => new RasterGeoTiff(null, BoundingBox.NaN); }
}
