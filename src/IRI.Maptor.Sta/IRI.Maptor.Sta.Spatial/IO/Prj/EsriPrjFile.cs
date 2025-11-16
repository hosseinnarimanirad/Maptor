using System.Globalization;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Metrics;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.Models;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using Ellipsoid = IRI.Maptor.Sta.SpatialReferenceSystem.Ellipsoid<IRI.Maptor.Sta.Metrics.Meter, IRI.Maptor.Sta.Metrics.Degree>;

namespace IRI.Maptor.Sta.Spatial.IO.Prj;

public class EsriPrjFile
{
    #region Constants

    public const string _esriLambertConformalConic = "Lambert_Conformal_Conic";
    public const string _esriTransverseMercator = "Transverse_Mercator";
    public const string _esriMercator = "Mercator";
    public const string _esriAzimuthalEquidistant = "Azimuthal_Equidistant";
    public const string _esriCylindricalEqualArea = "Cylindrical_Equal_Area";
    public const string _esriWebMercator = "Mercator_Auxiliary_Sphere";

    public const string _projcs = "PROJCS";
    public const string _geogcs = "GEOGCS";
    public const string _spheroid = "SPHEROID";
    public const string _datum = "DATUM";
    public const string _projection = "PROJECTION";
    public const string _unit = "UNIT";
    public const string _parameter = "PARAMETER";
    public const string _primem = "PRIMEM";
    public const string _authority = "AUTHORITY";
    public const string _toWgs84 = "TOWGS84";

    public const string _falseEasting = "False_Easting";
    public const string _falseNorthing = "False_Northing";
    public const string _centralMeridian = "Central_Meridian";
    public const string _scaleFactor = "Scale_Factor";
    public const string _latitudeOfOrigin = "Latitude_Of_Origin";
    public const string _standardParallel1 = "Standard_Parallel_1";
    public const string _standardParallel2 = "Standard_Parallel_2";
    public const string _auxiliarySphereType = "Auxiliary_Sphere_Type";
    public const string _greenwich = "Greenwich";
    public const string _degree = "Degree";
    public const string _degreeValue = "0.0174532925199433";
    public const string _epsg = "EPSG";

    #endregion

    private EsriPrjTreeNode _rootNode;


    public EsriPrjFile(EsriPrjTreeNode root)
    {
        _rootNode = root;

        //sample: AUTHORITY["EPSG", "4326"]
        //var authorityInfo = root.Children.SingleOrDefault(i => i.Name == _authority)?.Values;

        //int srid = 0;

        //if (authorityInfo != null && authorityInfo.Count == 2 && authorityInfo?[0] == _epsg)
        //{
        //    int.TryParse(authorityInfo[1], out srid);
        //}

        _srid = GetCrsSrid();
    }

    public EsriPrjFile(string prjFileName)
    {
        _rootNode = EsriPrjTreeNode.Parse(File.ReadAllText(prjFileName));

        _srid = GetCrsSrid();

    }

    public static EsriPrjFile Parse(string esriWktPrj)
    {
        return new EsriPrjFile(EsriPrjTreeNode.Parse(esriWktPrj));
    }

    //Prj file type: GEOGCS, PROJCS
    public EsriSrType Type
    {
        get
        {
            switch (_rootNode?.Name)
            {
                case _projcs:
                    return EsriSrType.Projcs;

                case _geogcs:
                    return EsriSrType.Geogcs;

                default:
                    throw new NotImplementedException();
            }
        }
    }


    // Projection Type
    private string _projectionName;
    public string ProjectionName
    {
        get
        {
            if (string.IsNullOrEmpty(_projectionName))
                _projectionName = GetProjectionName();

            return _projectionName;
        }
    }

    public SpatialReferenceType ProjectionType
    {
        get
        {
            switch (ProjectionName)
            {
                case _esriLambertConformalConic:
                    return SpatialReferenceType.LambertConformalConic;

                case _esriTransverseMercator:
                    return SpatialReferenceType.TransverseMercator;

                case _esriMercator:
                    return SpatialReferenceType.Mercator;

                case _esriAzimuthalEquidistant:
                    return SpatialReferenceType.AzimuthalEquidistant;

                case _esriCylindricalEqualArea:
                    return SpatialReferenceType.CylindricalEqualArea;

                case _esriWebMercator:
                    return SpatialReferenceType.WebMercator;

                case "None":
                    return SpatialReferenceType.None;

                default:
                    throw new NotImplementedException();
            }
        }
    }

    public string Title
    {
        get { return _rootNode?.Values?.First(); }
    }

    private EsriPrjTreeNode _geogcsNode;
    private EsriPrjTreeNode GeogcsNode
    {
        get
        {
            if (_geogcsNode is null)
                _geogcsNode = GetGeogcs();

            return _geogcsNode;
        }
    }

    private EsriPrjTreeNode _datumNode;
    private EsriPrjTreeNode DatumNode
    {
        get
        {
            if (_datumNode is null)
                _datumNode = GetDatumNode();

            return _datumNode;
        }
    }

    private int _srid;
    public int Srid
    {
        get { return _srid; }
    }

    // Ellipsoid
    private string _ellipsoidName;
    public string EllipsoidName
    {
        get
        {
            if (string.IsNullOrEmpty(_ellipsoidName))
                _ellipsoidName = GetEllipsoidName();

            return _ellipsoidName;
        }
    }

    //TODO: TEST EPSG
    public Ellipsoid Ellipsoid
    {
        get
        {
            var spheroidValues = DatumNode.Children.Single(i => i.Name.EqualsIgnoreCase(_spheroid)).Values;

            var toWgs84Values = DatumNode.Children.SingleOrDefault(i => i.Name.EqualsIgnoreCase(_toWgs84))?.Values;

            var srid = GetEllipsoidSrid();

            if (srid == 0)
            {
                if (Type == EsriSrType.Geogcs && spheroidValues.First().ToUpper() == "WGS_1984")
                {
                    srid = SridHelper.GeodeticWGS84;
                }
            }

            if (toWgs84Values == null)
            {
                return new Ellipsoid(spheroidValues.First(),
                                    new Meter(double.Parse(spheroidValues.Skip(1).First(), CultureInfo.InvariantCulture)),
                                    double.Parse(spheroidValues.Skip(2).First(), CultureInfo.InvariantCulture), srid)
                {
                    EsriName = spheroidValues.First(),
                };
            }
            else
            {
                var dx = double.Parse(toWgs84Values[0], CultureInfo.InvariantCulture);
                var dy = double.Parse(toWgs84Values[1], CultureInfo.InvariantCulture);
                var dz = double.Parse(toWgs84Values[2], CultureInfo.InvariantCulture);

                var drx = double.Parse(toWgs84Values[3], CultureInfo.InvariantCulture);
                var dry = double.Parse(toWgs84Values[4], CultureInfo.InvariantCulture);
                var drz = double.Parse(toWgs84Values[5], CultureInfo.InvariantCulture);

                return new Ellipsoid(spheroidValues.First(),
                                    new Meter(double.Parse(spheroidValues.Skip(1).First(), CultureInfo.InvariantCulture)),
                                    double.Parse(spheroidValues.Skip(2).First(), CultureInfo.InvariantCulture),
                                    new Cartesian3DPoint<Meter>(new Meter(dx), new Meter(dy), new Meter(dz)),
                                    new OrientationParameter(new Degree(drx), new Degree(dry), new Degree(drz)),
                                    srid)
                {
                    EsriName = spheroidValues.First(),
                };
            }
        }
    }


    #region Private Methods

    private string GetProjectionName()
    {
        switch (Type)
        {
            case EsriSrType.Projcs:
                return _rootNode.Children.Single(i => i.Name.EqualsIgnoreCase(_projection)).Values.First();

            case EsriSrType.Geogcs:
                return "None";

            default:
                throw new NotImplementedException();
        }
    }

    private EsriPrjTreeNode GetDatumNode()
    {
        return GeogcsNode.Children.Single(i => i.Name.EqualsIgnoreCase(_datum));
    }

    private EsriPrjTreeNode GetGeogcs()
    {
        switch (Type)
        {
            case EsriSrType.Projcs:
                return _rootNode.Children.Single(i => i.Name.EqualsIgnoreCase(_geogcs));

            case EsriSrType.Geogcs:
                return _rootNode;

            default:
                throw new NotImplementedException();
        }
    }

    private string GetEllipsoidName()
    {
        return DatumNode.Children.Single(i => i.Name.EqualsIgnoreCase(_spheroid)).Values?.First();
    }

    private int GetCrsSrid()
    {
        var crsAuthorityNode = _rootNode.Children.SingleOrDefault(i => i.Name == _authority);

        var srid = GetSridFromAuthorityNode(crsAuthorityNode);

        ////1399.06.13
        ////in the authority field was not available
        if (srid == 0)
        {
            if (Type == EsriSrType.Geogcs && Ellipsoid.Name.ToUpper() == "WGS_1984")
            {
                srid = SridHelper.GeodeticWGS84;
            }
        }

        return srid;
    }

    private int GetEllipsoidSrid()
    {
        var ellipsoidAuthorityNode = GeogcsNode.Children.SingleOrDefault(i => i.Name.EqualsIgnoreCase(_authority));

        var srid = GetSridFromAuthorityNode(ellipsoidAuthorityNode);

        return srid;
    }

    private int GetSridFromAuthorityNode(EsriPrjTreeNode authorityNode)
    {
        int srid = 0;

        if (authorityNode?.Values?.Count == 2 && authorityNode?.Values?[0] == _epsg)
        {
            int.TryParse(authorityNode.Values[1], out srid);
        }

        return srid;
    }

    public bool IsSISystem()
    {
        var isDegree = GeogcsNode.Children.Single(i => i.Name.EqualsIgnoreCase(_unit)).Values.First().ToLower() == "degree";

        switch (Type)
        {
            case EsriSrType.Projcs:
                return isDegree && _rootNode.Children.Single(i => i.Name.EqualsIgnoreCase(_unit)).Values.First().ToLower() == "meter";

            case EsriSrType.Geogcs:
                return isDegree;

            default:
                throw new NotImplementedException();
        }
    }

    private bool HasParameter(EsriPrjParameterType parameter)
    {
        switch (parameter)
        {
            case EsriPrjParameterType.FalseEasting:
                return HasParameter(_falseEasting);

            case EsriPrjParameterType.FalseNorthing:
                return HasParameter(_falseNorthing);

            case EsriPrjParameterType.CentralMeridian:
                return HasParameter(_centralMeridian);

            case EsriPrjParameterType.ScaleFactor:
                return HasParameter(_scaleFactor);

            case EsriPrjParameterType.LatitudeOfOrigin:
                return HasParameter(_latitudeOfOrigin);

            case EsriPrjParameterType.StandardParallel_1:
                return HasParameter(_standardParallel1);

            case EsriPrjParameterType.StandardParallel_2:
                return HasParameter(_standardParallel2);

            default:
                throw new NotImplementedException();
        }
    }

    private bool HasParameter(string parameterName)
    {
        var parameters = _rootNode.Children.Where(i => i.Name.EqualsIgnoreCase(_parameter)).ToList();

        return parameters.Any(i => i.Values.First().EqualsIgnoreCase(parameterName));
    }

    private double GetParameter(EsriPrjParameterType parameter, double defaultValue)
    {
        if (!HasParameter(parameter))
        {
            return defaultValue;
        }

        switch (parameter)
        {
            case EsriPrjParameterType.FalseEasting:
                return GetParameter(_falseEasting);

            case EsriPrjParameterType.FalseNorthing:
                return GetParameter(_falseNorthing);

            case EsriPrjParameterType.CentralMeridian:
                return GetParameter(_centralMeridian);

            case EsriPrjParameterType.ScaleFactor:
                return GetParameter(_scaleFactor);

            case EsriPrjParameterType.LatitudeOfOrigin:
                return GetParameter(_latitudeOfOrigin);

            case EsriPrjParameterType.StandardParallel_1:
                return GetParameter(_standardParallel1);

            case EsriPrjParameterType.StandardParallel_2:
                return GetParameter(_standardParallel2);

            default:
                throw new NotImplementedException();
        }
    }

    private double GetParameter(string parameterName)
    {
        var parameters = _rootNode.Children.Where(i => i.Name.EqualsIgnoreCase(_parameter)).ToList();

        return double.Parse(parameters.Single(i => i.Values.First().EqualsIgnoreCase(parameterName)).Values.Skip(1).First());
    }

    #endregion


    public SrsBase AsMapProjection()
    {
        SrsBase result =
         ProjectionType switch
         {
             SpatialReferenceType.None =>
                new NoProjection(Title, Ellipsoid),

             SpatialReferenceType.AlbersEqualAreaConic or
             SpatialReferenceType.AzimuthalEquidistant =>
                throw new NotImplementedException(),

             SpatialReferenceType.CylindricalEqualArea =>
                new CylindricalEqualArea(Title, Ellipsoid, Srid),

             SpatialReferenceType.LambertConformalConic =>
                new LambertConformalConic2P(
                   Ellipsoid,
                   GetParameter(EsriPrjParameterType.StandardParallel_1, double.NaN),
                   GetParameter(EsriPrjParameterType.StandardParallel_2, double.NaN),
                   GetParameter(EsriPrjParameterType.CentralMeridian, 0),
                   GetParameter(EsriPrjParameterType.LatitudeOfOrigin, 0),
                   GetParameter(EsriPrjParameterType.FalseEasting, 0),
                   GetParameter(EsriPrjParameterType.FalseNorthing, 0),
                   GetParameter(EsriPrjParameterType.ScaleFactor, 1),
                   Srid),

             SpatialReferenceType.Mercator =>
                new Mercator(Ellipsoid, Srid),

             SpatialReferenceType.TransverseMercator =>
                new TransverseMercator(
                   Ellipsoid,
                   GetParameter(EsriPrjParameterType.CentralMeridian, 0),
                   GetParameter(EsriPrjParameterType.LatitudeOfOrigin, 0),
                   GetParameter(EsriPrjParameterType.FalseEasting, 0),
                   GetParameter(EsriPrjParameterType.FalseNorthing, 0),
                   GetParameter(EsriPrjParameterType.ScaleFactor, 1),
                   Srid),

             SpatialReferenceType.UTM =>
                new UTM(Ellipsoid,
                               GetParameter(EsriPrjParameterType.CentralMeridian, 0)),

             SpatialReferenceType.WebMercator =>
                new WebMercator(),

             SpatialReferenceType.Geodetic => throw new NotImplementedException(),
             _ =>
                 throw new NotImplementedException()
         };

        result.Title = Title;
        result.DatumName = GeogcsNode.Values?.FirstOrDefault();

        return result;
    }

    public string AsEsriCrsWkt()
    {
        return _rootNode.AsEsriCrsWkt();
    }

    public void Save(string prjFileName)
    {
        if (Path.GetExtension(prjFileName).ToLower() != ".prj")
            throw new NotImplementedException();

        File.WriteAllText(prjFileName, AsEsriCrsWkt());
    }
}
