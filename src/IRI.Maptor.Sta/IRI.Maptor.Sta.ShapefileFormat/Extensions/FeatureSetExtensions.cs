using System.Text;
using System.Globalization;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.KmlFormat;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Spatial.IO.Dxf;
using IRI.Maptor.Sta.ShapefileFormat;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.IO.EsriJson;
using IRI.Maptor.Sta.ShapefileFormat.Dbf;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.Spatial.IO.TopoJson;

namespace IRI.Maptor.Extensions;

public static class FeatureSetExtensions
{
    public static async Task Export(this FeatureSet<Point> featureSet, string filePath, DataSourceKind exportFormat, SrsBase targetSrs, bool? isLongitudeFirst = null)
    {
        if (featureSet is null)
            return;

        var targetFeatureSet = featureSet.Project(targetSrs);

        switch (exportFormat)
        {
            case DataSourceKind.Shapefile:
                SaveAsShapefile(targetFeatureSet, filePath, System.Text.Encoding.UTF8, targetSrs, overwrite: true);
                break;

            case DataSourceKind.Kml:
                await SaveAsKmlAsync(targetFeatureSet, filePath);
                break;

            case DataSourceKind.Kmz:
                await SaveAsKmzAsync(targetFeatureSet, filePath);
                break;

            case DataSourceKind.Dxf:
                await SaveAsDxf(targetFeatureSet, filePath);
                break;

            case DataSourceKind.GeoJson:
                await targetFeatureSet.SaveAsGeoJson(filePath, isLongitudeFirst ?? true);
                break;

            case DataSourceKind.TopoJson:
                throw new NotImplementedException("FeatureSetExtensions > Export!");

            case DataSourceKind.EsriJson:
                await SaveAsEsriJson(targetFeatureSet, filePath);
                break;

            case DataSourceKind.GML:
            case DataSourceKind.Gpx:
            case DataSourceKind.Csv:
            case DataSourceKind.Tsv:
            default:
                throw new NotImplementedException("FeatureSetExtensions > Export!");

            case DataSourceKind.WebApi:
            case DataSourceKind.GRPC:
            case DataSourceKind.Other:
            case DataSourceKind.Worldfile:
            case DataSourceKind.GeoTiff:
            case DataSourceKind.ZippedImagePyramid:
                throw new ArgumentException($"FeatureSetExtensions > Export. {exportFormat} not supported");

        }
    }

    public static void SaveAsShapefile(this FeatureSet<Point> featureSet, string shpFileName, Encoding encoding, SrsBase srs, bool overwrite = false)
    {
        if (featureSet is null)
            return;

        Shapefile.SaveAsShapefile(shpFileName, featureSet.Features, f => f.TheGeometry.AsEsriShape(f.TheGeometry.Srid), false, srs, overwrite);

        DbfFile.Write(Shapefile.GetDbfFileName(shpFileName), featureSet.Features.Select(f => f.Attributes).ToList(), encoding, overwrite);
    }

    public static async Task SaveAsDxf(this FeatureSet<Point> featureSet, string dxfFileName)
    {
        if (featureSet is null)
            return;

        var srsBase = SridHelper.AsSrsBase(featureSet.Srid);

        var geometries = featureSet.Features.Select(f => f.TheGeometry).ToList();

        if (geometries is null)
            return;

        await DxfWriter.WriteToFileAsync(geometries, dxfFileName);
    }

    public static async Task SaveAsKmlAsync(this FeatureSet<Point> featureSet, string kmlFileName)
    {
        if (featureSet is null)
            return;

        var srsBase = SridHelper.AsSrsBase(featureSet.Srid);

        List<KmlFeature>? kmlFeatures = featureSet.Features.Select(f => f.ToKmlFeature()).ToList();

        if (kmlFeatures is null)
            return;

        await KmlWriter.WriteToFileAsync(kmlFeatures, kmlFileName, featureSet.Title);
    }

    public static async Task SaveAsKmzAsync(this FeatureSet<Point> featureSet, string kmzFileName)
    {
        if (featureSet is null)
            return;

        var srsBase = SridHelper.AsSrsBase(featureSet.Srid);

        List<KmlFeature>? kmlFeatures = featureSet.Features.Select(f => f.ToKmlFeature()).ToList();

        if (kmlFeatures is null)
            return;

        await KmzWriter.WriteToFileAsync(kmlFeatures, kmzFileName, featureSet.Title);
    }

    public static async Task SaveAsGeoJson(this FeatureSet<Point> featureSet, string geoJsonFileName, bool isLongitudeFirst)
    {
        if (featureSet is null)
            return;

        var srsBase = SridHelper.AsSrsBase(featureSet.Srid);

        var features = featureSet.Features.Select(f => f.AsGeoJsonFeature(srsBase.ToWgs84Geodetic, isLongitudeFirst)).ToList();

        GeoJsonFeatureSet jsonFeatureSet = new GeoJsonFeatureSet()
        {
            Features = features,
            TotalFeatures = features.Count,
        };

        await jsonFeatureSet.SaveAsync(geoJsonFileName, false, true);
    }

    public static async Task SaveAsTopoJson(this FeatureSet<Point> featureSet, string topoJsonFileName)
    {
        if (featureSet is null)
            return;

        await TopoJson.WriteToFileAsync(featureSet.Features, topoJsonFileName);
    }

    public static async Task SaveAsEsriJson(this FeatureSet<Point> featureSet, string esriJsonFileName)
    {
        if (featureSet is null)
            return;

        var esriFeatureSet = EsriJsonFeatureSet.Parse(featureSet);

        await esriFeatureSet.Save(esriJsonFileName, false, true);
    }


}
