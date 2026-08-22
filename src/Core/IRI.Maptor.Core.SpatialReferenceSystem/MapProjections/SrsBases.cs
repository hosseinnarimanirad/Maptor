
using IRI.Maptor.Core.Common.Metrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;

public static class SrsBases
{
    static SrsBases()
    {
        GeodeticWgs84 = new NoProjection("Wgs84", Ellipsoids.WGS84);

        WebMercator = new WebMercator();

        //https://epsg.io/3200
        //LccFd58 = new LambertConformalConic(
        //   ellipsoid: Ellipsoids.FD58,
        //   standardParallel1: 29.655083333333333333333333333333,
        //   standardParallel2: 35.314694444444444444444444444444,
        //   centralMeridian: 45.0,
        //   latitudeOfOrigin: 32.5,
        //   falseEasting: 1500000.0,
        //   falseNorthing: 1166200.0,
        //   scaleFactor: 0.9987864078,
        //   srid: 3200);

        LccFd58 = new LambertConformalConic1P(
          ellipsoid: Ellipsoids.FD58,
          centralMeridian: 45.0,
          latitudeOfOrigin: 32.5,
          falseEasting: 1500000.0,
          falseNorthing: 1166200.0,
          scaleFactor: 0.9987864078,
          srid: 3200);


        LccNahrawanIraq = new LambertConformalConic2P(
           ellipsoid: Ellipsoids.NahrawanIraq,
           standardParallel1: 29.655083333333333333333333333333,
           standardParallel2: 35.314694444444444444444444444444,
           centralMeridian: 45.0,
           latitudeOfOrigin: 32.5,
           falseEasting: 1500000.0,
           falseNorthing: 1166200.0,
           scaleFactor: 1);


        LccNahrawan = new LambertConformalConic2P(
           ellipsoid: Ellipsoids.Nahrawan,
           standardParallel1: 29.655083333333333333333333333333,
           standardParallel2: 35.314694444444444444444444444444,
           centralMeridian: 45.0,
           latitudeOfOrigin: 32.5,
           falseEasting: 1500000.0,
           falseNorthing: 1166200.0,
           scaleFactor: 1);

        LccNiocWithClarke1880Rgs = new LambertConformalConic2P(
            ellipsoid: Ellipsoids.Clarke1880Rgs,
            standardParallel1: 29.655083333333333333333333333333,
            standardParallel2: 35.314694444444444444444444444444,
            centralMeridian: 45.0,
            latitudeOfOrigin: 32.5,
            falseEasting: 1500000.0,
            falseNorthing: 1166200.0,
            scaleFactor: 0.9987864078);

        LccNiocWithWgs84 = new LambertConformalConic2P(
            ellipsoid: Ellipsoids.WGS84,
            standardParallel1: 29.65508274166,
            standardParallel2: 35.31468809166,
            centralMeridian: 45.0,
            latitudeOfOrigin: 32.5,
            falseEasting: 1500000.0,
            falseNorthing: 1166200.0,
            scaleFactor: 0.9987864078);
                          
        UtmNorthZone38 = new UTM(Ellipsoids.WGS84, MapProjects.CalculateCentralMeridian(38));

        UtmNorthZone39 = new UTM(Ellipsoids.WGS84, MapProjects.CalculateCentralMeridian(39));

        UtmNorthZone40 = new UTM(Ellipsoids.WGS84, MapProjects.CalculateCentralMeridian(40));

        UtmNorthZone41 = new UTM(Ellipsoids.WGS84, MapProjects.CalculateCentralMeridian(41));
         
    }

    public static LambertConformalConic2P LccNiocWithWgs84 { get; private set; }
     
    public static LambertConformalConic2P LccNiocWithClarke1880Rgs { get; private set; }

    public static LambertConformalConic2P LccNahrawan { get; private set; }

    public static LambertConformalConic2P LccNahrawanIraq { get; private set; }

    //https://epsg.io/3200
    public static LambertConformalConic1P LccFd58 { get; private set; }

    public static NoProjection GeodeticWgs84 { get; private set; }

    public static WebMercator WebMercator { get; private set; }

    public static UTM UtmNorthZone38 { get; private set; }

    public static UTM UtmNorthZone39 { get; private set; }

    public static UTM UtmNorthZone40 { get; private set; }

    public static UTM UtmNorthZone41 { get; private set; }



    //  case MapProjectionType.None:
    //        

    //    case MapProjectionType.AlbersEqualAreaConic:
    //    case MapProjectionType.AzimuthalEquidistant:
    //        throw new NotImplementedException();

    //    case MapProjectionType.CylindricalEqualArea:
    //        return new CylindricalEqualArea(this.Title, this.Ellipsoid, Srid) { DatumName = this.Geogcs.Values?.First() };

    //    case MapProjectionType.LambertConformalConic:
    //        return new LambertConformalConic2P(
    //            this.Ellipsoid,
    //            GetParameter(EsriPrjParameterType.StandardParallel_1, double.NaN),
    //            GetParameter(EsriPrjParameterType.StandardParallel_2, double.NaN),
    //            GetParameter(EsriPrjParameterType.CentralMeridian, 0),
    //            GetParameter(EsriPrjParameterType.LatitudeOfOrigin, 0),
    //            GetParameter(EsriPrjParameterType.FalseEasting, 0),
    //            GetParameter(EsriPrjParameterType.FalseNorthing, 0),
    //            GetParameter(EsriPrjParameterType.ScaleFactor, 1),
    //            Srid)
    //        {
    //            Title = this.Title,
    //            DatumName = this.Geogcs.Values?.First()
    //        };

    //    case MapProjectionType.Mercator:
    //        return new Mercator(this.Ellipsoid, Srid)
    //        {
    //            Title = this.Title,
    //            DatumName = this.Geogcs.Values?.First()
    //        };

    //    case MapProjectionType.TransverseMercator:
    //    case MapProjectionType.UTM:
    //        return new TransverseMercator(
    //            this.Ellipsoid,
    //            GetParameter(EsriPrjParameterType.CentralMeridian, 0),
    //            GetParameter(EsriPrjParameterType.LatitudeOfOrigin, 0),
    //            GetParameter(EsriPrjParameterType.FalseEasting, 0),
    //            GetParameter(EsriPrjParameterType.FalseNorthing, 0),
    //            GetParameter(EsriPrjParameterType.ScaleFactor, 1),
    //            Srid)
    //        {
    //            Title = this.Title,
    //            DatumName = this.Geogcs.Values?.First()
    //        };

    //    case MapProjectionType.WebMercator:
    //        return new WebMercator()
    //        {
    //            Title = this.Title,
    //            DatumName = this.Geogcs.Values?.First()
    //        };

    //default:
    //        throw new NotImplementedException();
}

