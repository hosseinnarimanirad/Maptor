using System;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using static IRI.Maptor.Jab.Common.Properties.Resources;
//using static IRI.Maptor.Jab.Common.Localization.LocalizationResourceKeys;

namespace IRI.Maptor.Jab.Common.Models.CoordinatePanel;

public static class SpatialReferenceItems
{
    static readonly Func<double, string> toStringForGeodetic = d => d.ToString("#,#.#####");
    static readonly Func<double, string> toStringForGeodeticDms = d => DegreeHelper.ToDms(d, true);
    static readonly Func<double, string> toStringForDefault = d => d.ToString("#,#.###");

    //static readonly PersianEnglishItem geodeticTitle = new PersianEnglishItem("سیستم مختصات ژئودتیک، WGS84 (اعشار درجه)", "Geodetic, WGS84 (decimal degree)");
    //static readonly PersianEnglishItem geodeticDmsTitle = new PersianEnglishItem("سیستم مختصات ژئودتیک، WGS84 (درجه، دقیقه، ثانیه)", "Geodetic, WGS84 (dms)");
    //static readonly PersianEnglishItem utmTitle = new PersianEnglishItem("سیستم تصویر Univarsal Transverse Mercator - UTM", "Universal Transeverse Mercator (UTM)");
    //static readonly PersianEnglishItem mercatorTitle = new PersianEnglishItem("سیستم تصویر Mercator", "Mercator");
    //static readonly PersianEnglishItem tmTitle = new PersianEnglishItem("سیستم تصویر Transeverse Mercator", "Transeverse Mercator");
    //static readonly PersianEnglishItem ceaTitle = new PersianEnglishItem("سیستم تصویر Cylindrical Equal Area", "Cylindrical Equal Area");


    //static readonly PersianEnglishItem geodeticSubTitle = new PersianEnglishItem("ژئودتیک", "Geodetic, WGS84");
    //static readonly PersianEnglishItem geodeticDmsSubTitle = new PersianEnglishItem("ژئودتیک", "Geodetic, WGS84");
    //static readonly PersianEnglishItem utmSubTitle = new PersianEnglishItem("UTM", "UTM");
    //static readonly PersianEnglishItem mercatorSubTitle = new PersianEnglishItem("Mercator", "Mercator");
    //static readonly PersianEnglishItem tmSubTitle = new PersianEnglishItem("Transeverse Mercator", "Transeverse Mercator");
    //static readonly PersianEnglishItem ceaSubTitle = new PersianEnglishItem("Cylindrical Equal Area", "Cylindrical Equal Area");


    ////static readonly PersianEnglishItem _defaultZone = new PersianEnglishItem("ناحیه", "Zone");
    //static readonly PersianEnglishItem _defaultX = new PersianEnglishItem("X:", "X:");
    //static readonly PersianEnglishItem _defaultY = new PersianEnglishItem("Y:", "Y:");
    //static readonly PersianEnglishItem _defaultLatitude = new PersianEnglishItem("عرض جغرافیایی:", "Latitude:");
    //static readonly PersianEnglishItem _defaultLongitude = new PersianEnglishItem("طول جغرافیایی:", "Longitude:");

    static readonly SpatialReferenceItem _geodeticWgs84 
        = new SpatialReferenceItem(p => p, 
                                    toStringForGeodetic, 
                                    nameof(srs_geodeticTitle), 
                                    nameof(srs_geodeticSubTitle), 
                                    nameof(srs_defaultLongitude),
                                    nameof(srs_defaultLatitude));

    static readonly SpatialReferenceItem _geodeticDmsWgs84 
        = new SpatialReferenceItem(p => p, 
                                    toStringForGeodeticDms, 
                                    nameof(srs_geodeticDmsTitle), 
                                    nameof(srs_geodeticDmsSubTitle), 
                                    nameof(srs_defaultLongitude),
                                    nameof(srs_defaultLatitude));

    static readonly SpatialReferenceItem _utmWgs84 
        = new SpatialReferenceItem(p => MapProjects.GeodeticToUTM(p, p.Y > 0), 
                                    toStringForDefault,
                                    nameof(srs_utmTitle), 
                                    nameof(srs_utmSubTitle), 
                                    nameof(srs_defaultX), 
                                    nameof(srs_defaultY),
                                    nameof(srs_utmZone)) { IsZoneVisible = true };


    static readonly SpatialReferenceItem _mercatorWgs84 
        = new SpatialReferenceItem(p => MapProjects.GeodeticToMercator(p), 
                                    toStringForDefault,
                                    nameof(srs_mercatorTitle), 
                                    nameof(srs_mercatorSubTitle), 
                                    nameof(srs_defaultX),
                                    nameof(srs_defaultY));

    static readonly SpatialReferenceItem _webMercator
        = new SpatialReferenceItem(p => MapProjects.GeodeticWgs84ToWebMercator(p),
                                    toStringForDefault,
                                    nameof(srs_webMercatorTitle),
                                    nameof(srs_webMercatorSubTitle),
                                    nameof(srs_defaultX),
                                    nameof(srs_defaultY));


    static readonly SpatialReferenceItem _tmWgs84 
        = new SpatialReferenceItem(p => MapProjects.GeodeticToTransverseMercator(p), 
                                    toStringForDefault,
                                    nameof(srs_tmTitle), 
                                    nameof(srs_tmSubTitle), 
                                    nameof(srs_defaultX),
                                    nameof(srs_defaultY));


    static readonly SpatialReferenceItem _cylindricalEqualAreaWgs84 
        = new SpatialReferenceItem(p => MapProjects.GeodeticToCylindricalEqualArea(p), 
                                    toStringForDefault,
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
