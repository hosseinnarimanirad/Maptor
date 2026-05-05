using IRI.Maptor.Sta.ShapefileFormat;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.ShapefileFormat.Dbf;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.KmlFormat;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Spatial.IO.Dxf;
using IRI.Maptor.Sta.Spatial.Primitives.Esri;

namespace IRI.Maptor.Extensions;

public static class FeatureSetExtensions
{
    public static void Export(this FeatureSet<Point> featureSet, string filePath, DataSourceKind exportFormat, SrsBase targetSrs, bool? isLongitudeFirst = null)
    {
        var targetFeatureSet = featureSet.Project(targetSrs);

        switch (exportFormat)
        {
            case DataSourceKind.Shapefile:
                SaveAsShapefile(targetFeatureSet, filePath, System.Text.Encoding.UTF8, targetSrs, overwrite: true);
                break;

            case DataSourceKind.Kml:
                SaveAsKml(targetFeatureSet, filePath);
                break;

            case DataSourceKind.Kmz:
                SaveAsKmz(targetFeatureSet, filePath);
                break;

            case DataSourceKind.Dxf:
                SaveAsDxf(targetFeatureSet, filePath);
                break;

            case DataSourceKind.GeoJson:
                targetFeatureSet.SaveAsGeoJson(filePath, isLongitudeFirst ?? true);
                break;

            case DataSourceKind.TopoJson:
                throw new NotImplementedException("FeatureSetExtensions > Export!");

            case DataSourceKind.EsriJson:
                SaveAsEsriJson(targetFeatureSet, filePath);
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

    public static void SaveAsDxf(this FeatureSet<Point> featureSet, string dxfFileName)
    {
        var srsBase = SridHelper.AsSrsBase(featureSet.Srid);

        var geometries = featureSet.Features.Select(f => f.TheGeometry).ToList();

        if (geometries is null)
            return;

        DxfWriter.WriteToFile(geometries, dxfFileName);
    }

    public static void SaveAsKml(this FeatureSet<Point> featureSet, string kmlFileName)
    {
        var srsBase = SridHelper.AsSrsBase(featureSet.Srid);

        List<KmlFeature>? kmlFeatures = featureSet.Features.Select(f => f.ToKmlFeature()).ToList();

        if (kmlFeatures is null)
            return;

        KmlWriter.WriteToFile(kmlFeatures, kmlFileName, featureSet.Title);
    }

    public static void SaveAsKmz(this FeatureSet<Point> featureSet, string kmzFileName)
    {
        var srsBase = SridHelper.AsSrsBase(featureSet.Srid);

        List<KmlFeature>? kmlFeatures = featureSet.Features.Select(f => f.ToKmlFeature()).ToList();

        if (kmlFeatures is null)
            return;

        KmzWriter.WriteToFile(kmlFeatures, kmzFileName, featureSet.Title);
    }

    public static void SaveAsShapefile(this FeatureSet<Point> featureSet, string shpFileName, Encoding encoding, SrsBase srs, bool overwrite = false)
    {
        Shapefile.SaveAsShapefile(shpFileName, featureSet.Features, f => f.TheGeometry.AsEsriShape(f.TheGeometry.Srid), false, srs, overwrite);

        DbfFile.Write(Shapefile.GetDbfFileName(shpFileName), featureSet.Features.Select(f => f.Attributes).ToList(), encoding, overwrite);
    }

    public static void SaveAsGeoJson(this FeatureSet<Point> featureSet, string geoJsonFileName, bool isLongitudeFirst)
    {
        var srsBase = SridHelper.AsSrsBase(featureSet.Srid);

        var features = featureSet.Features.Select(f => f.AsGeoJsonFeature(p => srsBase.ToWgs84Geodetic(p), isLongitudeFirst)).ToList();

        GeoJsonFeatureSet jsonFeatureSet = new GeoJsonFeatureSet()
        {
            Features = features,
            TotalFeatures = features.Count,
        };

        jsonFeatureSet.Save(geoJsonFileName, false, true);
    }

    public static void SaveAsEsriJson(this FeatureSet<Point> featureSet, string esriJsonFileName)
    {
        var esriFeatures = featureSet.Features.Select(f => f.AsEsriJsonFeature()).ToList();

        var esriFeatureSet = new EsriJsonFeatureSet()
        {
            Features = esriFeatures,
            FieldAliases = featureSet.Fields?.ToDictionary(f => f.Name, f => f.Alias) ?? new Dictionary<string, string?>(),
            Fields = featureSet.Fields,
            SpatialReference = new EsriJsonSpatialReference() { LatestWkid = featureSet.Srid }
        };

        esriFeatureSet.Save(esriJsonFileName, false, true);
    }

    /// <summary>
    /// Saves point features to a CSV file. First two columns are longitude, latitude (WGS84); remaining columns are attributes.
    /// </summary>
    public static void SaveAsCsv(this FeatureSet<Point> featureSet, string csvFileName, bool includeHeader = true)
    {
        SaveAsDelimited(featureSet, csvFileName, IOHelper.CsvDelimiterChar, includeHeader, null);
    }

    /// <summary>
    /// Saves point features to a CSV file in the specified target SRID. First two columns are X, Y (or longitude, latitude for WGS84); remaining columns are attributes.
    /// </summary>
    public static void SaveAsCsv(this FeatureSet<Point> featureSet, string csvFileName, bool includeHeader, int targetSrid)
    {
        SaveAsDelimited(featureSet, csvFileName, IOHelper.CsvDelimiterChar, includeHeader, targetSrid);
    }

    /// <summary>
    /// Saves point features to a TSV file. First two columns are longitude, latitude (WGS84); remaining columns are attributes.
    /// </summary>
    public static void SaveAsTsv(this FeatureSet<Point> featureSet, string tsvFileName, bool includeHeader = true)
    {
        SaveAsDelimited(featureSet, tsvFileName, IOHelper.TsvDelimiterChar, includeHeader, null);
    }

    /// <summary>
    /// Saves point features to a TSV file in the specified target SRID. First two columns are X, Y (or longitude, latitude for WGS84); remaining columns are attributes.
    /// </summary>
    public static void SaveAsTsv(this FeatureSet<Point> featureSet, string tsvFileName, bool includeHeader, int targetSrid)
    {
        SaveAsDelimited(featureSet, tsvFileName, IOHelper.TsvDelimiterChar, includeHeader, targetSrid);
    }

    private static void SaveAsDelimited(FeatureSet<Point> featureSet, string fileName, char delimiter, bool includeHeader, int? targetSrid)
    {
        var features = featureSet.Features.ToList();
        if (features.Count == 0)
            return;

        var sourceSrs = SridHelper.AsSrsBase(featureSet.Srid);
        var effectiveTargetSrid = targetSrid ?? SridHelper.GeodeticWGS84;
        var targetSrs = SridHelper.AsSrsBase(effectiveTargetSrid);
        var attributeKeys = featureSet.Fields?.Select(f => f.Name).ToList()
            ?? features.First().Attributes.Keys.OrderBy(k => k).ToList();

        var coordHeader = effectiveTargetSrid == SridHelper.GeodeticWGS84 ? new[] { "longitude", "latitude" } : new[] { "X", "Y" };

        var lines = new List<string>();

        if (includeHeader)
        {
            var header = new List<string>(coordHeader);
            header.AddRange(attributeKeys);
            lines.Add(string.Join(delimiter.ToString(), header.Select(v => EscapeDelimitedValue(v, delimiter))));
        }

        foreach (var feature in features)
        {
            var point = feature.TheGeometry.GetAllPoints().FirstOrDefault();
            if (point == null)
                continue;

            var projectedPoint = targetSrs != null ? point.Project(sourceSrs, targetSrs) : sourceSrs.ToWgs84Geodetic(point);
            var values = new List<string>
            {
                projectedPoint.X.ToString(CultureInfo.InvariantCulture),
                projectedPoint.Y.ToString(CultureInfo.InvariantCulture)
            };

            foreach (var key in attributeKeys)
            {
                var val = feature.Attributes.TryGetValue(key, out var v) ? v : null;
                values.Add(val?.ToString() ?? string.Empty);
            }

            lines.Add(string.Join(delimiter.ToString(), values.Select(v => EscapeDelimitedValue(v, delimiter))));
        }

        File.WriteAllLines(fileName, lines);
    }

    private static string EscapeDelimitedValue(string value, char delimiter)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        if (value.Contains(delimiter) || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

}
