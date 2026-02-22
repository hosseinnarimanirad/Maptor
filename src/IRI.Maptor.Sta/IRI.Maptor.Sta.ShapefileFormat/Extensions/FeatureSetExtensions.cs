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

namespace IRI.Maptor.Extensions;

public static class FeatureSetExtensions
{
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

    /// <summary>
    /// Saves point features to a CSV file. First two columns are longitude, latitude (WGS84); remaining columns are attributes.
    /// </summary>
    public static void SaveAsCsv(this FeatureSet<Point> featureSet, string csvFileName, bool includeHeader = true)
    {
        SaveAsDelimited(featureSet, csvFileName, IOHelper.CsvDelimiterChar, includeHeader);
    }

    /// <summary>
    /// Saves point features to a TSV file. First two columns are longitude, latitude (WGS84); remaining columns are attributes.
    /// </summary>
    public static void SaveAsTsv(this FeatureSet<Point> featureSet, string tsvFileName, bool includeHeader = true)
    {
        SaveAsDelimited(featureSet, tsvFileName, IOHelper.TsvDelimiterChar, includeHeader);
    }

    private static void SaveAsDelimited(FeatureSet<Point> featureSet, string fileName, char delimiter, bool includeHeader)
    {
        var features = featureSet.Features.ToList();
        if (features.Count == 0)
            return;

        var srsBase = SridHelper.AsSrsBase(featureSet.Srid);
        var attributeKeys = featureSet.Fields?.Select(f => f.Name).ToList()
            ?? features.First().Attributes.Keys.OrderBy(k => k).ToList();

        var lines = new List<string>();

        if (includeHeader)
        {
            var header = new List<string> { "longitude", "latitude" };
            header.AddRange(attributeKeys);
            lines.Add(string.Join(delimiter.ToString(), header.Select(v => EscapeDelimitedValue(v, delimiter))));
        }

        foreach (var feature in features)
        {
            var point = feature.TheGeometry.GetAllPoints().FirstOrDefault();
            if (point == null)
                continue;

            var wgs84 = srsBase.ToWgs84Geodetic(point);
            var values = new List<string>
            {
                wgs84.X.ToString(CultureInfo.InvariantCulture),
                wgs84.Y.ToString(CultureInfo.InvariantCulture)
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
