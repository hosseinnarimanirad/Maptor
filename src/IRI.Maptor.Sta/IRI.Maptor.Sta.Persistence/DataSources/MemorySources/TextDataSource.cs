using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;

namespace IRI.Maptor.Sta.Persistence.DataSources;

/// <summary>
/// Memory data source that loads from and saves to CSV/TSV files (point-only).
/// </summary>
public class TextDataSource : MemoryDataSource
{
    private DataSourceKind _dataSourceKind;
    public override DataSourceKind DataSourceKind => _dataSourceKind/*DataSourceKind.Csv*/;

    private readonly string _fileName;
    private readonly bool _useFirstLineAsHeader;

    private TextDataSource(string fileName,
                            List<Feature<Point>> features,
                            bool useFirstLineAsHeader,
                            DataSourceKind dataSourceKind)
        : base(features, resetIds: true, kind: dataSourceKind)
    {
        if (dataSourceKind != DataSourceKind.Csv && dataSourceKind != DataSourceKind.Tsv)
            throw new ArgumentException();

        _fileName = fileName ?? string.Empty;

        _useFirstLineAsHeader = useFirstLineAsHeader;
    }

    public override string ToString() => $"{nameof(TextDataSource)}";

    public override Task SaveChanges()
    {
        if (!string.IsNullOrWhiteSpace(_fileName))
        {
            if (DataSourceKind == DataSourceKind.Csv)
                _featureSet.SaveAsCsv(_fileName, _useFirstLineAsHeader);

            else if (DataSourceKind == DataSourceKind.Tsv)
                _featureSet.SaveAsTsv(_fileName, _useFirstLineAsHeader);

            else
                throw new ArgumentException();
        }

        _featureSet.ApplyChanges();

        UpdateHasPendingChanges();

        return Task.CompletedTask;
    }

    ///// <summary>
    ///// Creates a CsvDataSource from the given file path and features.
    ///// Features should already be in Web Mercator.
    ///// </summary>
    //public static CsvDataSource Create(string fileName,
    //                                    List<Feature<Point>> features,
    //                                    DataSourceKind dataSourceKind,
    //                                    bool useFirstLineAsHeader = false)
    //{
    //    if (fileName == null)
    //        throw new ArgumentNullException(nameof(fileName));

    //    if (features.IsNullOrEmpty())
    //        throw new ArgumentException("At least one feature is required.", nameof(features));

    //    if (dataSourceKind != DataSourceKind.Csv && dataSourceKind != DataSourceKind.Tsv)
    //        throw new ArgumentException();

    //    return new CsvDataSource(fileName, features, useFirstLineAsHeader, dataSourceKind);
    //}

    ///// <summary>
    ///// Creates a CsvDataSource from a CSV file.
    ///// </summary>
    //public static async Task<CsvDataSource> CreateFromFileAsync(string fileName, GeometryType type, bool useFirstLineAsHeader = false)
    //{
    //    return await CreateFromFileAsync(fileName, useFirstLineAsHeader, SridHelper.GeodeticWGS84, isLongitudeFirst: true, type);
    //}

    /// <summary>
    /// Creates a CsvDataSource from a CSV file with the specified spatial reference.
    /// </summary>
    public static async Task<TextDataSource> CreateFromFileAsync(string fileName, bool useFirstLineAsHeader, int sourceSrid, bool isLongitudeFirst, GeometryType type, DataSourceKind dataSourceKind)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
            throw new FileNotFoundException($"CSV/TSV file not found: {fileName}", fileName);

        if (dataSourceKind != DataSourceKind.Csv && dataSourceKind != DataSourceKind.Tsv)
            throw new ArgumentException();

        var delimiter = dataSourceKind == DataSourceKind.Csv ? IOHelper.CsvDelimiterChar : IOHelper.TsvDelimiterChar;

        var rawData = await IOHelper.ReadAllDelimitedFileAsync(fileName, delimiter);

        var features = ParseToWebMercatorFeatures(rawData, useFirstLineAsHeader, isLongitudeFirst, sourceSrid, type);

        if (features.Count == 0)
            throw new InvalidOperationException($"No features found in CSV/TSV file: {fileName}");

        return new TextDataSource(fileName, features, useFirstLineAsHeader, dataSourceKind);
    }

    /// <summary>
    /// Creates a CsvDataSource from pasted or in-memory text (e.g. from clipboard).
    /// </summary>
    public static Task<TextDataSource> CreateFromTextAsync(string text, int sourceSrid, bool isLongitudeFirst, GeometryType type, DataSourceKind dataSourceKind, bool useFirstLineAsHeader = false)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be empty.", nameof(text));

        if (dataSourceKind != DataSourceKind.Csv && dataSourceKind != DataSourceKind.Tsv)
            throw new ArgumentException();

        var delimiter = dataSourceKind == DataSourceKind.Csv ? IOHelper.CsvDelimiterChar : IOHelper.TsvDelimiterChar;

        var rawData = IOHelper.ReadDelimitedFromText(text, delimiter);

        var features = ParseToWebMercatorFeatures(rawData, useFirstLineAsHeader, isLongitudeFirst, sourceSrid, type);

        if (features.Count == 0)
            throw new InvalidOperationException("No valid features found in the text.");

        return Task.FromResult(new TextDataSource(string.Empty, features, useFirstLineAsHeader, dataSourceKind));
    }

    private static List<Feature<Point>> ParseToWebMercatorFeatures(List<string[]> rawData, bool useFirstLineAsHeader, bool isLongitudeFirst, int sourceSrid, GeometryType type)
    {
        if (rawData == null || rawData.Count == 0)
            return new List<Feature<Point>>();

        var sourceSrs = SridHelper.AsSrsBase(sourceSrid);
        if (sourceSrs == null)
            throw new ArgumentException($"Unsupported SRID: {sourceSrid}", nameof(sourceSrid));

        int startIndex = 0;
        List<string> header;
        if (useFirstLineAsHeader && rawData[0].Length >= 2)
        {
            startIndex = 1;
            header = rawData[0].Length > 2 ? rawData[0].Skip(2).ToList() : new List<string>();
        }
        else
        {
            int colCount = rawData[0].Length;
            header = colCount > 2 ? Enumerable.Range(1, colCount - 2).Select(i => $"header {i}").ToList() : new List<string>();
        }

        var webMercator = new WebMercator();
        var result = new List<Feature<Point>>();

        for (int i = startIndex; i < rawData.Count; i++)
        {
            if (rawData[i].Length < 2)
                continue;

            double v0 = double.Parse(rawData[i][0]);
            double v1 = double.Parse(rawData[i][1]);
            double x = isLongitudeFirst ? v0 : v1;
            double y = isLongitudeFirst ? v1 : v0;

            var point = new Point(x, y);
            var geom = Geometry<Point>.Create(new List<Point> { point }, IRI.Maptor.Sta.Common.Enums.GeometryType.Point, sourceSrid);
            var projected = geom.Project(sourceSrs, webMercator);

            var attrs = new Dictionary<string, object>();
            for (int p = 2; p < rawData[i].Length && p - 2 < header.Count; p++)
            {
                attrs[header[p - 2]] = rawData[i][p];
            }

            result.Add(new Feature<Point> { TheGeometry = projected, Attributes = attrs });
        }

        if (type == Common.Enums.GeometryType.Polygon)
        {
            return [Geometry<Point>.CreatePolygon(result.Select(r => r.TheGeometry.AsPoint()).ToList(), SridHelper.WebMercator).AsFeature()];
        }
        else if (type == Common.Enums.GeometryType.LineString)
        {
            return [Geometry<Point>.CreateLineStringFromPoints(result.Select(r => r.TheGeometry).ToList()).AsFeature()];
        }

        return result;
    }
}
