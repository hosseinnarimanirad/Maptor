using System;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using static IRI.Maptor.Jab.Common.Localization.LocalizationResourceKeys;

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
                                    srs_geodeticTitle.ToString(), 
                                    srs_geodeticSubTitle.ToString(), 
                                    srs_defaultLongitude.ToString(), 
                                    srs_defaultLatitude.ToString());

    static readonly SpatialReferenceItem _geodeticDmsWgs84 
        = new SpatialReferenceItem(p => p, 
                                    toStringForGeodeticDms, 
                                    srs_geodeticDmsTitle.ToString(), 
                                    srs_geodeticDmsSubTitle.ToString(), 
                                    srs_defaultLongitude.ToString(), 
                                    srs_defaultLatitude.ToString());

    static readonly SpatialReferenceItem _utmWgs84 
        = new SpatialReferenceItem(p => MapProjects.GeodeticToUTM(p, p.Y > 0), 
                                    toStringForDefault,
                                    srs_utmTitle.ToString(), 
                                    srs_utmSubTitle.ToString(), 
                                    srs_defaultX.ToString(), 
                                    srs_defaultY.ToString(), 
                                    srs_utmZone.ToString()) { IsZoneVisible = true };


    static readonly SpatialReferenceItem _mercatorWgs84 
        = new SpatialReferenceItem(p => MapProjects.GeodeticToMercator(p), 
                                    toStringForDefault,
                                    srs_mercatorTitle.ToString(), 
                                    srs_mercatorSubTitle.ToString(), 
                                    srs_defaultX.ToString(), 
                                    srs_defaultY.ToString());

    static readonly SpatialReferenceItem _webMercator
        = new SpatialReferenceItem(p => MapProjects.GeodeticWgs84ToWebMercator(p),
                                    toStringForDefault,
                                    srs_webMercatorTitle.ToString(),
                                    srs_webMercatorSubTitle.ToString(),
                                    srs_defaultX.ToString(),
                                    srs_defaultY.ToString());


    static readonly SpatialReferenceItem _tmWgs84 
        = new SpatialReferenceItem(p => MapProjects.GeodeticToTransverseMercator(p), 
                                    toStringForDefault,
                                    srs_tmTitle.ToString(), 
                                    srs_tmSubTitle.ToString(), 
                                    srs_defaultX.ToString(), 
                                    srs_defaultY.ToString());


    static readonly SpatialReferenceItem _cylindricalEqualAreaWgs84 
        = new SpatialReferenceItem(p => MapProjects.GeodeticToCylindricalEqualArea(p), 
                                    toStringForDefault,
                                    srs_ceaTitle.ToString(), 
                                    srs_ceaSubTitle.ToString(), 
                                    srs_defaultX.ToString(), 
                                    srs_defaultY.ToString());

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
