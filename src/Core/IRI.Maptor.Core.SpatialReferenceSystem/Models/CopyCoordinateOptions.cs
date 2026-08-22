using Ellipsoid = IRI.Maptor.Core.SpatialReferenceSystem.Ellipsoid<IRI.Maptor.Core.Common.Metrics.Meter, IRI.Maptor.Core.Common.Metrics.Degree>;

namespace IRI.Maptor.Core.SpatialReferenceSystem;

public class CopyCoordinateOptions
{
    //public static readonly CopyCoordinateOptions Default = new CopyCoordinateOptions();

    public int? UtmZone { get; set; }

    public int LatLongPrecision { get; set; } = 5;

    public int XyPrecision { get; set; } = 2;

    public Ellipsoid Ellipsoid { get; set; } = Ellipsoids.WGS84;

    public bool UseThousandSeparator { get; set; } = false;

    public static CopyCoordinateOptions Create(int latLongPrecision = 5, int xyPrecision = 2)
    {
        return new CopyCoordinateOptions()
        {
            LatLongPrecision = latLongPrecision,
            XyPrecision = xyPrecision
        };
    }
}
