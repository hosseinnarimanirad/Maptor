using IRI.Maptor.Sta.SpatialReferenceSystem;

using static IRI.Maptor.Jab.Core.Properties.Resources;

namespace IRI.Maptor.Jab.Core.Models;

public static class SpatialReferenceItems
{
    static readonly SpatialReferenceItem _geodeticWgs84
        = new SpatialReferenceItem(CoordinateDisplayMode.GeodeticDecimal,
                                    nameof(srs_geodeticTitle),
                                    nameof(srs_geodeticSubTitle),
                                    nameof(srs_defaultLongitude),
                                    nameof(srs_defaultLatitude));

    static readonly SpatialReferenceItem _geodeticDmsWgs84
        = new SpatialReferenceItem(CoordinateDisplayMode.GeodeticDms,
                                    nameof(srs_geodeticDmsTitle),
                                    nameof(srs_geodeticDmsSubTitle),
                                    nameof(srs_defaultLongitude),
                                    nameof(srs_defaultLatitude));

    static readonly SpatialReferenceItem _utmWgs84
        = new SpatialReferenceItem(CoordinateDisplayMode.UTM,
                                    nameof(srs_utmTitle),
                                    nameof(srs_utmSubTitle),
                                    nameof(srs_defaultX),
                                    nameof(srs_defaultY),
                                    nameof(srs_utmZone))
        { IsZoneVisible = true };


    static readonly SpatialReferenceItem _mercatorWgs84
        = new SpatialReferenceItem(CoordinateDisplayMode.Mercator,
                                    nameof(srs_mercatorTitle),
                                    nameof(srs_mercatorSubTitle),
                                    nameof(srs_defaultX),
                                    nameof(srs_defaultY));

    static readonly SpatialReferenceItem _webMercator
        = new SpatialReferenceItem(CoordinateDisplayMode.WebMercator,
                                    nameof(srs_webMercatorTitle),
                                    nameof(srs_webMercatorSubTitle),
                                    nameof(srs_defaultX),
                                    nameof(srs_defaultY));


    static readonly SpatialReferenceItem _tmWgs84
        = new SpatialReferenceItem(CoordinateDisplayMode.TM,
                                    nameof(srs_tmTitle),
                                    nameof(srs_tmSubTitle),
                                    nameof(srs_defaultX),
                                    nameof(srs_defaultY));


    static readonly SpatialReferenceItem _cylindricalEqualAreaWgs84
        = new SpatialReferenceItem(CoordinateDisplayMode.CylindricalEqualArea,
                                    nameof(srs_ceaTitle),
                                    nameof(srs_ceaSubTitle),
                                    nameof(srs_defaultX),
                                    nameof(srs_defaultY));

    public static SpatialReferenceItem GeodeticWgs84 => _geodeticWgs84;

    public static SpatialReferenceItem GeodeticDmsWgs84 => _geodeticDmsWgs84;

    public static SpatialReferenceItem UtmWgs84 => _utmWgs84;

    public static SpatialReferenceItem MercatorWgs84 => _mercatorWgs84;

    public static SpatialReferenceItem WebMercator => _webMercator;

    public static SpatialReferenceItem TmWgs84 => _tmWgs84;

    public static SpatialReferenceItem CylindricalEqualAreaWgs84 => _cylindricalEqualAreaWgs84;

}
