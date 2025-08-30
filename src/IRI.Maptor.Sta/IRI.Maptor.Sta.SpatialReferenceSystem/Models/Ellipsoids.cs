// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using IRI.Maptor.Sta.Metrics;
using IRI.Maptor.Sta.SpatialReferenceSystem.Models;

namespace IRI.Maptor.Sta.SpatialReferenceSystem;

public static class Ellipsoids
{
    public static Ellipsoid<Meter, Degree> Airy1830
    {
        get
        {
            return new Ellipsoid<Meter, Degree>("Airy 1830", new Meter(6377563.396), 299.3249646, 7001) { EsriName = "Airy_1830" };
        }
    }

    public static Ellipsoid<Meter, Degree> ModifiedAiry
    {
        get
        {
            return new Ellipsoid<Meter, Degree>("Modified Airy", new Meter(6377340.189), 299.3249646, 7002) { EsriName = "Airy_Modified" };
        }
    }

    public static Ellipsoid<Meter, Degree> AustralianNational
    {
        get
        {
            return new Ellipsoid<Meter, Degree>("Australian National", new Meter(6378160.0), 298.25, 7003) { EsriName = "Australian" };
        }
    }

    public static Ellipsoid<Meter, Degree> Bessel1841Namibia
    {
        get
        {
            return new Ellipsoid<Meter, Degree>("Bessel 1841 (Namibia)", new Meter(6377483.865280418), 299.1528128, 7046) { EsriName = "Bessel_Namibia" };
        }
    }

    public static Ellipsoid<Meter, Degree> Bessel1841
    {
        get
        {
            return new Ellipsoid<Meter, Degree>("Bessel 1841", new Meter(6377397.155), 299.1528128, 7004) { EsriName = "Bessel_1841" };
        }
    }

    public static Ellipsoid<Meter, Degree> Clarke1866
    {
        get
        {
            return new Ellipsoid<Meter, Degree>("Clarke 1866", new Meter(6378206.4), 294.9786982, 7008) { EsriName = "Clarke_1866" };
        }
    }

    public static Ellipsoid<Meter, Degree> Clarke1880
    {
        get
        {
            return new Ellipsoid<Meter, Degree>("Clarke 1880", new Meter(6378249.144808011), 293.4663076556253, 7013) { EsriName = "Clarke_1880" };
        }
    }

    /// <summary>
    /// This is used as the horizontal datum of NIOC LCC Projection
    /// </summary>
    public static Ellipsoid<Meter, Degree> Clarke1880Rgs
    {
        get
        {
            return new Ellipsoid<Meter, Degree>("Clarke 1880 RGS", new Meter(6378249.145), 293.465, 7012) { EsriName = "Clarke_1880_RGS" };
        }
    }


    //EPSG not found
    public static Ellipsoid<Meter, Degree> Nahrawan
    {
        get
        {
            return new Ellipsoid<Meter, Degree>("Nahrwan 67 ", new Meter(6378249.145), 293.465,
                new Cartesian3DPoint<Meter>(new Meter(-245.0), new Meter(-153.9), new Meter(382.8)),
                new OrientationParameter(new Degree(), new Degree(), new Degree()),
                int.MinValue)
            { EsriName = "Nahrwan 1967 (deg)" };
        }
    }

    //https://epsg.io/4270
    public static Ellipsoid<Meter, Degree> NahrawanIraq
    {
        get
        {
            return new Ellipsoid<Meter, Degree>("Nahrwan 1967 Iraq", new Meter(6378249.145), 293.465,
                new Cartesian3DPoint<Meter>(new Meter(-242.2), new Meter(-144.9), new Meter(370.3)),
                new OrientationParameter(new Degree(), new Degree(), new Degree()),
                4270)
            { EsriName = "Nahrawan" };
        }
    }

    //https://epsg.io/4132
    public static Ellipsoid<Meter, Degree> FD58
    {
        get
        {
            return new Ellipsoid<Meter, Degree>("D_FD_1958", new Meter(6378249.145), 293.465,
                new Cartesian3DPoint<Meter>(new Meter(-241.54), new Meter(-163.64), new Meter(396.06)),
                new OrientationParameter(new Degree(), new Degree(), new Degree()),
                4132)
            { EsriName = "FD58" };
        }
    }


    //public static Ellipsoid<Meter, Degree> EverestSabahSarawak1967
    //{
    //    get
    //    {
    //        return new Ellipsoid<Meter, Degree>("Everest (Sabah Sarawak) 1967", new Meter(6377298.556), 300.8017) { EsriName = "Everest_Definition_1967" };
    //    }
    //}

    //public static Ellipsoid<Meter, Degree> EverestIndia1956
    //{
    //    get
    //    {
    //        return new Ellipsoid<Meter, Degree>("Everest (India 1956)", new Meter(6377301.243), 300.8017) { EsriName = "Everest_India_1956" };
    //    }
    //}

    //public static Ellipsoid<Meter, Degree> EverestMalaysia1969
    //{
    //    get
    //    {
    //        return new Ellipsoid<Meter, Degree>("Everest (Malaysia 1969)", new Meter(6377295.664), 300.8017) { EsriName = "Everest_Modified_1969" };
    //    }
    //}

    //public static Ellipsoid<Meter, Degree> EverestMalaySing1830
    //{
    //    get
    //    {
    //        return new Ellipsoid<Meter, Degree>("Everest (Malay. & Sing)", new Meter(6377304.063), 300.8017) { EsriName = "Everest_1830_Modified" };
    //    }
    //}

    //public static Ellipsoid<Meter, Degree> EverestPakistan
    //{
    //    get
    //    {
    //        return new Ellipsoid<Meter, Degree>("Everest (Pakistan)", new Meter(6377309.613), 300.8017) { EsriName = "Everest_Pakistan" };
    //    }
    //}

    //public static Ellipsoid<Meter, Degree> ModifiedFischer1960
    //{
    //    get
    //    {
    //        return new Ellipsoid<Meter, Degree>("Modified Fischer 1960", new Meter(6378155.0), 298.3) { EsriName = "Fischer_Modified" };
    //    }
    //}

    //public static Ellipsoid<Meter, Degree> Helmert1906
    //{
    //    get
    //    {
    //        return new Ellipsoid<Meter, Degree>("Helmert 1906", new Meter(6378200.0), 298.3) { EsriName = "Helmert_1906" };
    //    }
    //}

    //public static Ellipsoid<Meter, Degree> Hough1960
    //{
    //    get
    //    {
    //        return new Ellipsoid<Meter, Degree>("Hough 1960", new Meter(6378270.0), 297.0) { EsriName = "Hough_1960" };
    //    }
    //}

    public static Ellipsoid<Meter, Degree> Indonesian1974
    {
        get
        {
            return new Ellipsoid<Meter, Degree>("Indonesian 1974", new Meter(6378160.0), 298.247, 6238) { EsriName = "Indonesian" };
        }
    }

    /// <summary>
    /// Hayford 1924
    /// </summary>
    public static Ellipsoid<Meter, Degree> International1924
    {
        get
        {
            return new Ellipsoid<Meter, Degree>("International 1924", new Meter(6378388.0), 297, 7022) { EsriName = "International_1924" };
        }
    }

    //public static Ellipsoid<Meter, Degree> Krassovsky1940
    //{
    //    get
    //    {
    //        return new Ellipsoid<Meter, Degree>("Krassovsky 1940", new Meter(6378245.0), 298.3) { EsriName = "Krasovsky_1940" };
    //    }
    //}


    //Srid value may be wrong
    public static Ellipsoid<Meter, Degree> GRS80
    {
        get
        {
            return new Ellipsoid<Meter, Degree>("GRS 80", new Meter(6378137.0), 298.257222101, 4019) { EsriName = "GRS_1980" };
        }
    }

    public static Ellipsoid<Meter, Degree> SouthAmerican1969
    {
        get
        {
            return new Ellipsoid<Meter, Degree>("South American 1969", new Meter(6378160.0), 298.25, 6618) { EsriName = "South_American_1969" };
        }
    }

    public static Ellipsoid<Meter, Degree> WGS72
    {
        get
        {
            return new Ellipsoid<Meter, Degree>("WGS 72", new Meter(6378135.0), 298.26, 4322) { EsriName = "WGS_1972" };
        }
    }
                                                                                                             
    public static Ellipsoid<Meter, Degree> WGS84 { get; } = new Ellipsoid<Meter, Degree>("WGS 84", new Meter(6378137.0), 298.257223563, SridHelper.GeodeticWGS84) { EsriName = "WGS_1984" };

    public static Ellipsoid<Meter, Degree> Sphere
    {
        get
        {
            return new Ellipsoid<Meter, Degree>("Sphere", new Meter(6378137), double.PositiveInfinity, 1) { EsriName = "shpere" };
        }
    }
}