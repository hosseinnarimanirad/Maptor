using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Spatial.IO.Prj;

namespace IRI.Maptor.Extensions;

public static class CoordinateSystemExtensions
{
    public static string AsEsriCrsWkt(this SrsBase srsBase)
    {
        return AsEsriPrj(srsBase).AsEsriCrsWkt();   
    }

    public static EsriPrjFile AsEsriPrj(this SrsBase mapProjection)
    {
        switch (mapProjection.Type)
        {
            case SpatialReferenceType.None:
            case SpatialReferenceType.Geodetic:
                return new EsriPrjFile(new EsriPrjTreeNode(mapProjection.Ellipsoid, mapProjection.Title, mapProjection.Srid));

            case SpatialReferenceType.AlbersEqualAreaConic:
            case SpatialReferenceType.AzimuthalEquidistant:
                throw new NotImplementedException();

            case SpatialReferenceType.CylindricalEqualArea:
                return AsEsriPrjFile((CylindricalEqualArea)mapProjection);

            case SpatialReferenceType.LambertConformalConic:
                return AsEsriPrjFile((LambertConformalConic2P)mapProjection);

            case SpatialReferenceType.Mercator:
                return AsEsriPrjFile((Mercator)mapProjection);

            case SpatialReferenceType.TransverseMercator:
                return AsEsriPrjFile((TransverseMercator)mapProjection);

            case SpatialReferenceType.UTM:
                return AsEsriPrjFile((UTM)mapProjection);

            case SpatialReferenceType.WebMercator:
                return AsEsriPrjFile((WebMercator)mapProjection);

            default:
                throw new NotImplementedException();
        }
    }

    private static EsriPrjFile AsEsriPrjFile(LambertConformalConic2P lcc)
    {
        EsriPrjTreeNode root = new EsriPrjTreeNode();

        root.Name = EsriPrjFile._projcs;

        root.Values = new List<string>() { string.IsNullOrWhiteSpace(lcc.Title) ? "LAMBERT CONFORMAL CONIC" : lcc.Title };

        var geogcs = new EsriPrjTreeNode(lcc.Ellipsoid, lcc.DatumName, lcc.Ellipsoid.Srid);

        var projection = new EsriPrjTreeNode(EsriPrjFile._projection, EsriPrjFile._esriLambertConformalConic);

        var falseEasting = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._falseEasting, lcc.FalseEasting.AsExactString());

        var falseNorthing = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._falseNorthing, lcc.FalseNorthing.AsExactString());

        var centralMeridian = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._centralMeridian, lcc.CentralMeridian.AsExactString());

        var standardParallel1 = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._standardParallel1, lcc.StandardParallel1.AsExactString());

        var standardParallel2 = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._standardParallel2, lcc.StandardParallel2.AsExactString());

        var scaleFactor = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._scaleFactor, lcc.StandardParallel2.AsExactString());

        var latitudeOfOrigin = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._latitudeOfOrigin, lcc.StandardParallel2.AsExactString());

        var unit = EsriPrjTreeNode.MeterUnit;

        root.Children = new List<EsriPrjTreeNode>() { geogcs, projection, falseEasting, falseNorthing, centralMeridian, standardParallel1, standardParallel2, scaleFactor, latitudeOfOrigin, unit };

        return new EsriPrjFile(root);
    }

    private static EsriPrjFile AsEsriPrjFile(UTM utm)
    {
        EsriPrjTreeNode root = new EsriPrjTreeNode();

        root.Name = EsriPrjFile._projcs;

        string ns = utm.FalseNorthing == 0 ? "N" : "S";

        var zone = MapProjects.FindUtmZone(utm.CentralMeridian);

        root.Values = new List<string>() { string.IsNullOrWhiteSpace(utm.Title) ? $"WGS_1984_UTM_Zone_{zone}{ns}" : utm.Title };

        var geogcs = new EsriPrjTreeNode(utm.Ellipsoid, utm.DatumName, utm.Ellipsoid.Srid);

        var projection = new EsriPrjTreeNode(EsriPrjFile._projection, EsriPrjFile._esriTransverseMercator);

        var falseEasting = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._falseEasting, utm.FalseEasting.AsExactString());

        var falseNorthing = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._falseNorthing, utm.FalseNorthing.AsExactString());

        var centralMeridian = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._centralMeridian, utm.CentralMeridian.AsExactString());

        var scaleFactor = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._scaleFactor, utm.ScaleFactor.AsExactString());

        var latitudeOfOrigin = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._latitudeOfOrigin, utm.StandardParallel2.AsExactString());

        var unit = EsriPrjTreeNode.MeterUnit;

        root.Children = new List<EsriPrjTreeNode>() { geogcs, projection, falseEasting, falseNorthing, centralMeridian, scaleFactor, latitudeOfOrigin, unit };

        return new EsriPrjFile(root);
    }

    private static EsriPrjFile AsEsriPrjFile(WebMercator webMercator)
    {
        EsriPrjTreeNode root = new EsriPrjTreeNode();

        root.Name = EsriPrjFile._projcs;

        root.Values = new List<string>() { string.IsNullOrWhiteSpace(webMercator.Title) ? $"{webMercator.Ellipsoid.EsriName}_Web_Mercator_Auxiliary_Sphere" : webMercator.Title };

        var geogcs = new EsriPrjTreeNode(webMercator.Ellipsoid, webMercator.DatumName, webMercator.Ellipsoid.Srid);

        var projection = new EsriPrjTreeNode(EsriPrjFile._projection, EsriPrjFile._esriWebMercator);

        var falseEasting = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._falseEasting, webMercator.FalseEasting.AsExactString());

        var falseNorthing = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._falseNorthing, webMercator.FalseNorthing.AsExactString());

        var centralMeridian = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._centralMeridian, webMercator.CentralMeridian.AsExactString());

        var standardParallel1 = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._standardParallel1, webMercator.StandardParallel1.AsExactString());

        var auxiliarySphereType = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._auxiliarySphereType, webMercator.AuxiliarySphereType.AsExactString());

        var unit = EsriPrjTreeNode.MeterUnit;

        root.Children = new List<EsriPrjTreeNode>() { geogcs, projection, falseEasting, falseNorthing, centralMeridian, standardParallel1, auxiliarySphereType, unit };

        return new EsriPrjFile(root);
    }

    private static EsriPrjFile AsEsriPrjFile(Mercator mercator)
    {
        EsriPrjTreeNode root = new EsriPrjTreeNode();

        root.Name = EsriPrjFile._projcs;

        root.Values = new List<string>() { string.IsNullOrWhiteSpace(mercator.Title) ? "WGS_1984_World_Mercator" : mercator.Title };

        var geogcs = new EsriPrjTreeNode(mercator.Ellipsoid, mercator.DatumName, mercator.Ellipsoid.Srid);

        var projection = new EsriPrjTreeNode(EsriPrjFile._projection, EsriPrjFile._esriMercator);

        var falseEasting = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._falseEasting, mercator.FalseEasting.AsExactString());

        var falseNorthing = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._falseNorthing, mercator.FalseNorthing.AsExactString());

        var centralMeridian = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._centralMeridian, mercator.CentralMeridian.AsExactString());

        var standardParallel1 = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._standardParallel1, mercator.StandardParallel1.AsExactString());

        var unit = EsriPrjTreeNode.MeterUnit;

        root.Children = new List<EsriPrjTreeNode>() { geogcs, projection, falseEasting, falseNorthing, centralMeridian, standardParallel1, unit };

        return new EsriPrjFile(root);
    }

    private static EsriPrjFile AsEsriPrjFile(CylindricalEqualArea cylindricalEqualArea)
    {
        EsriPrjTreeNode root = new EsriPrjTreeNode();

        root.Name = EsriPrjFile._projcs;

        root.Values = new List<string>() { string.IsNullOrWhiteSpace(cylindricalEqualArea.Title) ? "World_Cylindrical_Equal_Area" : cylindricalEqualArea.Title };

        var geogcs = new EsriPrjTreeNode(cylindricalEqualArea.Ellipsoid, cylindricalEqualArea.DatumName, cylindricalEqualArea.Ellipsoid.Srid);

        var projection = new EsriPrjTreeNode(EsriPrjFile._projection, EsriPrjFile._esriCylindricalEqualArea);

        var falseEasting = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._falseEasting, cylindricalEqualArea.FalseEasting.AsExactString());

        var falseNorthing = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._falseNorthing, cylindricalEqualArea.FalseNorthing.AsExactString());

        var centralMeridian = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._centralMeridian, cylindricalEqualArea.CentralMeridian.AsExactString());

        var standardParallel1 = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._standardParallel1, cylindricalEqualArea.StandardParallel1.AsExactString());

        var unit = EsriPrjTreeNode.MeterUnit;

        root.Children = new List<EsriPrjTreeNode>() { geogcs, projection, falseEasting, falseNorthing, centralMeridian, standardParallel1, unit };

        return new EsriPrjFile(root);
    }

    private static EsriPrjFile AsEsriPrjFile(TransverseMercator transverseMercator)
    {
        EsriPrjTreeNode root = new EsriPrjTreeNode();

        root.Name = EsriPrjFile._projcs;

        root.Values = new List<string>() { string.IsNullOrWhiteSpace(transverseMercator.Title) ? "Transverse Mercator" : transverseMercator.Title };

        var geogcs = new EsriPrjTreeNode(transverseMercator.Ellipsoid, transverseMercator.DatumName, transverseMercator.Ellipsoid.Srid);

        var projection = new EsriPrjTreeNode(EsriPrjFile._projection, EsriPrjFile._esriTransverseMercator);

        var falseEasting = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._falseEasting, transverseMercator.FalseEasting.AsExactString());

        var falseNorthing = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._falseNorthing, transverseMercator.FalseNorthing.AsExactString());

        var centralMeridian = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._centralMeridian, transverseMercator.CentralMeridian.AsExactString());

        var scaleFactor = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._scaleFactor, transverseMercator.ScaleFactor.AsExactString());

        var latitudeOfOrigin = new EsriPrjTreeNode(EsriPrjFile._parameter, EsriPrjFile._latitudeOfOrigin, transverseMercator.StandardParallel2.AsExactString());

        var unit = EsriPrjTreeNode.MeterUnit;

        root.Children = new List<EsriPrjTreeNode>() { geogcs, projection, falseEasting, falseNorthing, centralMeridian, scaleFactor, latitudeOfOrigin, unit };

        return new EsriPrjFile(root);
    }

}
