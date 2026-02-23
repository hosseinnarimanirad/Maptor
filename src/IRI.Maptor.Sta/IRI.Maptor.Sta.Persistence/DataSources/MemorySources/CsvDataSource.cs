using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.Persistence.Abstractions;

namespace IRI.Maptor.Sta.Persistence.DataSources;

/// <summary>
/// Memory data source that loads from and saves to CSV files (point-only).
/// </summary>
public class CsvDataSource : MemoryDataSource
{
    public override DataSourceKind DataSourceKind => DataSourceKind.Csv;

    private readonly string _fileName;
    private readonly bool _useFirstLineAsHeader;

    private CsvDataSource(string fileName, List<Feature<Point>> features, bool useFirstLineAsHeader)
        : base(features, resetIds: true, kind: DataSourceKind.Csv)
    {
        _fileName = fileName ?? string.Empty;
        _useFirstLineAsHeader = useFirstLineAsHeader;
    }

    public override string ToString() => $"{nameof(CsvDataSource)}";

    public override Task SaveChanges()
    {
        if (!string.IsNullOrWhiteSpace(_fileName))
        {
            _featureSet.SaveAsCsv(_fileName, _useFirstLineAsHeader);
        }
        _featureSet.ApplyChanges();
        UpdateHasPendingChanges();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a CsvDataSource from the given file path and features.
    /// Features should already be in Web Mercator.
    /// </summary>
    public static CsvDataSource Create(string fileName, List<Feature<Point>> features, bool useFirstLineAsHeader = false)
    {
        if (fileName == null)
            throw new ArgumentNullException(nameof(fileName));
        if (features == null || features.Count == 0)
            throw new ArgumentException("At least one feature is required.", nameof(features));

        return new CsvDataSource(fileName, features, useFirstLineAsHeader);
    }

    /// <summary>
    /// Creates a CsvDataSource from a CSV file.
    /// </summary>
    public static async Task<CsvDataSource> CreateFromFileAsync(string fileName, bool useFirstLineAsHeader = false)
    {
        return await CreateFromFileAsync(fileName, useFirstLineAsHeader, SridHelper.GeodeticWGS84, isLongitudeFirst: true);
    }

    /// <summary>
    /// Creates a CsvDataSource from a CSV file with the specified spatial reference.
    /// </summary>
    public static async Task<CsvDataSource> CreateFromFileAsync(string fileName, bool useFirstLineAsHeader, int sourceSrid, bool isLongitudeFirst)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
            throw new FileNotFoundException($"CSV file not found: {fileName}", fileName);

        var rawData = await IOHelper.ReadAllDelimitedFileAsync(fileName, IOHelper.CsvDelimiterChar);
        var features = ParseToWebMercatorFeatures(rawData, useFirstLineAsHeader, isLongitudeFirst, sourceSrid);

        if (features.Count == 0)
            throw new InvalidOperationException($"No features found in CSV file: {fileName}");

        return new CsvDataSource(fileName, features, useFirstLineAsHeader);
    }

    /// <summary>
    /// Creates a CsvDataSource from pasted or in-memory text (e.g. from clipboard).
    /// </summary>
    public static Task<CsvDataSource> CreateFromTextAsync(string text, int sourceSrid, bool isLongitudeFirst, bool useFirstLineAsHeader = false)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be empty.", nameof(text));

        var rawData = IOHelper.ReadDelimitedFromText(text, IOHelper.CsvDelimiterChar);
        var features = ParseToWebMercatorFeatures(rawData, useFirstLineAsHeader, isLongitudeFirst, sourceSrid);

        if (features.Count == 0)
            throw new InvalidOperationException("No valid features found in the text.");

        return Task.FromResult(new CsvDataSource(string.Empty, features, useFirstLineAsHeader));
    }

    private static List<Feature<Point>> ParseToWebMercatorFeatures(List<string[]> rawData, bool useFirstLineAsHeader, bool isLongitudeFirst, int sourceSrid)
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

        return result;
    }
}
