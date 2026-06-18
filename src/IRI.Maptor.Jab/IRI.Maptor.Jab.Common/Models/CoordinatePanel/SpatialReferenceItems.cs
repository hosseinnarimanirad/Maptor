using System;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using static IRI.Maptor.Jab.Core.Properties.Resources;
//using static IRI.Maptor.Jab.Core.Localization.LocalizationResourceKeys;

namespace IRI.Maptor.Jab.Common.Models.CoordinatePanel;

public static class SpatialReferenceItems
{
    //static readonly Func<double, string> toStringForGeodetic = d => d.ToString("#,#.#####");
    //static readonly Func<double, string> toStringForGeodeticDms = d => DegreeHelper.ToDms(d, true);
    //static readonly Func<double, string> toStringForDefault = d => d.ToString("#,#.###");

    static readonly SpatialReferenceItem _geodeticWgs84
        = new SpatialReferenceItem(/*p => p,*/
                                    //toStringForGeodetic,
                                    CoordinateDisplayMode.GeodeticDecimal,
                                    nameof(srs_geodeticTitle),
                                    nameof(srs_geodeticSubTitle),
                                    nameof(srs_defaultLongitude),
                                    nameof(srs_defaultLatitude));

    static readonly SpatialReferenceItem _geodeticDmsWgs84
        = new SpatialReferenceItem(/*p => p,*/
                                    //toStringForGeodeticDms,
                                    CoordinateDisplayMode.GeodeticDms,
                                    nameof(srs_geodeticDmsTitle),
                                    nameof(srs_geodeticDmsSubTitle),
                                    nameof(srs_defaultLongitude),
                                    nameof(srs_defaultLatitude));

    static readonly SpatialReferenceItem _utmWgs84
        = new SpatialReferenceItem(/*p => MapProjects.GeodeticToUTM(p, p.Y > 0),*/
                                    //toStringForDefault,
                                    CoordinateDisplayMode.UTM,
                                    nameof(srs_utmTitle),
                                    nameof(srs_utmSubTitle),
                                    nameof(srs_defaultX),
                                    nameof(srs_defaultY),
                                    nameof(srs_utmZone))
        { IsZoneVisible = true };


    static readonly SpatialReferenceItem _mercatorWgs84
        = new SpatialReferenceItem(/*p => MapProjects.GeodeticToMercator(p),*/
                                    //toStringForDefault,
                                    CoordinateDisplayMode.Mercator,
                                    nameof(srs_mercatorTitle),
                                    nameof(srs_mercatorSubTitle),
                                    nameof(srs_defaultX),
                                    nameof(srs_defaultY));

    static readonly SpatialReferenceItem _webMercator
        = new SpatialReferenceItem(/*p => MapProjects.GeodeticWgs84ToWebMercator(p),*/
                                    //toStringForDefault,
                                    CoordinateDisplayMode.WebMercator,
                                    nameof(srs_webMercatorTitle),
                                    nameof(srs_webMercatorSubTitle),
                                    nameof(srs_defaultX),
                                    nameof(srs_defaultY));


    static readonly SpatialReferenceItem _tmWgs84
        = new SpatialReferenceItem(/*p => MapProjects.GeodeticToTransverseMercator(p),*/
                                    //toStringForDefault,
                                    CoordinateDisplayMode.TM,
                                    nameof(srs_tmTitle),
                                    nameof(srs_tmSubTitle),
                                    nameof(srs_defaultX),
                                    nameof(srs_defaultY));


    static readonly SpatialReferenceItem _cylindricalEqualAreaWgs84
        = new SpatialReferenceItem(/*p => MapProjects.GeodeticToCylindricalEqualArea(p),*/
                                    //toStringForDefault,
                                    CoordinateDisplayMode.CylindricalEqualArea,
                                    nameof(srs_ceaTitle),
                                    nameof(srs_ceaSubTitle),
                                    nameof(srs_defaultX),
                                    nameof(srs_defaultY));

    public static SpatialReferenceItem GeodeticWgs84
    {
        get { return _geodeticWgs84; }
    }

    public static SpatialReferenceItem GeodeticDmsWgs84
    {
        get { return _geodeticDmsWgs84; }
    }

    public static SpatialReferenceItem UtmWgs84
    {
        get { return _utmWgs84; }
    }

    public static SpatialReferenceItem MercatorWgs84
    {
        get { return _mercatorWgs84; }
    }

    public static SpatialReferenceItem WebMercator
    {
        get { return _webMercator; }
    }

    public static SpatialReferenceItem TmWgs84
    {
        get { return _tmWgs84; }
    }

    public static SpatialReferenceItem CylindricalEqualAreaWgs84
    {
        get { return _cylindricalEqualAreaWgs84; }
    }

}
