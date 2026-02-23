using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;

namespace IRI.Maptor.Sta.SpatialReferenceSystem;

public static class SridHelper
{
    public const string GeodeticWGS84Name = "GCS_WGS_1984";

    public const int GeodeticWGS84 = 4326;

    public const int WebMercator = 3857;

    public const int UtmNorthZone38 = 32638;

    public const int UtmNorthZone39 = 32639;

    public const int UtmNorthZone40 = 32640;

    public const int UtmNorthZone41 = 32641;

    // https://epsg.io/3395
    public const int Mercator = 3395;

    // https://epsg.io/54034
    public const int CylindricalEqualArea = 54034;

    public static int GetUtmSrid(int zone) => int.Parse($"326{zone}");

    /// <summary>
    /// Returns EPSG SRID for UTM Southern hemisphere (32700 + zone).
    /// </summary>
    public static int GetUtmSouthSrid(int zone) => 32700 + zone;

    public static SrsBase AsSrsBase(int srid)
    {
        switch (srid)
        {
            case GeodeticWGS84:
                return new NoProjection("Wgs84", Ellipsoids.WGS84);// { DatumName = this.Geogcs.Values?.First() };

            case WebMercator:
                return new WebMercator();

            case UtmNorthZone38:
                return new UTM(Ellipsoids.WGS84, MapProjects.CalculateCentralMeridian(38));

            case UtmNorthZone39:
                return new UTM(Ellipsoids.WGS84, MapProjects.CalculateCentralMeridian(39));

            case UtmNorthZone40:
                return new UTM(Ellipsoids.WGS84, MapProjects.CalculateCentralMeridian(40));

            case UtmNorthZone41:
                return new UTM(Ellipsoids.WGS84, MapProjects.CalculateCentralMeridian(41));

            case Mercator:
                return new Mercator();

            case CylindricalEqualArea:
                return new CylindricalEqualArea();

            default:
                if (srid >= 32601 && srid <= 32660)
                {
                    int zone = srid - 32600;
                    return UTM.CreateForZone(Ellipsoids.WGS84, zone);
                }
                if (srid >= 32701 && srid <= 32760)
                {
                    int zone = srid - 32700;
                    return UTM.CreateForZone(Ellipsoids.WGS84, zone);
                }
                return null;
        }
    }
}