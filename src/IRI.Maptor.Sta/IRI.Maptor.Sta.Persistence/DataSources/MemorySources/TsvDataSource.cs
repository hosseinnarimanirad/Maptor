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
/// Memory data source that loads from and saves to TSV files (point-only).
/// </summary>
public class TsvDataSource : MemoryDataSource
{
    public override DataSourceKind DataSourceKind => DataSourceKind.Tsv;

    private readonly string _fileName;
    private readonly bool _useFirstLineAsHeader;

    private TsvDataSource(string fileName, List<Feature<Point>> features, bool useFirstLineAsHeader)
        : base(features, resetIds: true, kind: DataSourceKind.Tsv)
    {
        _fileName = fileName ?? string.Empty;
        _useFirstLineAsHeader = useFirstLineAsHeader;
    }

    public override string ToString() => $"{nameof(TsvDataSource)}";

    public override Task SaveChanges()
    {
        if (!string.IsNullOrWhiteSpace(_fileName))
        {
            _featureSet.SaveAsTsv(_fileName, _useFirstLineAsHeader);
        }
        _featureSet.ApplyChanges();
        UpdateHasPendingChanges();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a TsvDataSource from the given file path and features.
    /// Features should already be in Web Mercator.
    /// </summary>
    public static TsvDataSource Create(string fileName, List<Feature<Point>> features, bool useFirstLineAsHeader = false)
    {
        if (fileName == null)
            throw new ArgumentNullException(nameof(fileName));
        if (features == null || features.Count == 0)
            throw new ArgumentException("At least one feature is required.", nameof(features));

        return new TsvDataSource(fileName, features, useFirstLineAsHeader);
    }

    /// <summary>
    /// Creates a TsvDataSource from a TSV file.
    /// </summary>
    public static async Task<TsvDataSource> CreateFromFileAsync(string fileName, GeometryType type, bool useFirstLineAsHeader = false)
    {
        return await CreateFromFileAsync(fileName, useFirstLineAsHeader, SridHelper.GeodeticWGS84, isLongitudeFirst: true, type);
    }

    /// <summary>
    /// Creates a TsvDataSource from a TSV file with the specified spatial reference.
    /// </summary>
    public static async Task<TsvDataSource> CreateFromFileAsync(string fileName, bool useFirstLineAsHeader, int sourceSrid, bool isLongitudeFirst, GeometryType type)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
            throw new FileNotFoundException($"TSV file not found: {fileName}", fileName);

        var rawData = await IOHelper.ReadAllDelimitedFileAsync(fileName, IOHelper.TsvDelimiterChar);
        var features = ParseToWebMercatorFeatures(rawData, useFirstLineAsHeader, isLongitudeFirst, sourceSrid, type);

        if (features.Count == 0)
            throw new InvalidOperationException($"No features found in TSV file: {fileName}");

        return new TsvDataSource(fileName, features, useFirstLineAsHeader);
    }

    /// <summary>
    /// Creates a TsvDataSource from pasted or in-memory text (e.g. from clipboard).
    /// </summary>
    public static Task<TsvDataSource> CreateFromTextAsync(string text, int sourceSrid, bool isLongitudeFirst, GeometryType type, bool useFirstLineAsHeader = false)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be empty.", nameof(text));

        var rawData = IOHelper.ReadDelimitedFromText(text, IOHelper.TsvDelimiterChar);
        var features = ParseToWebMercatorFeatures(rawData, useFirstLineAsHeader, isLongitudeFirst, sourceSrid, type);

        if (features.Count == 0)
            throw new InvalidOperationException("No valid features found in the text.");

        return Task.FromResult(new TsvDataSource(string.Empty, features, useFirstLineAsHeader));
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
            return [Geometry<Point>.Create(result.Select(r => r.TheGeometry).ToList(), Common.Enums.GeometryType.Polygon, SridHelper.WebMercator).AsFeature()];
        }
        else if (type == Common.Enums.GeometryType.LineString)
        {
            return [Geometry<Point>.Create(result.Select(r => r.TheGeometry).ToList(), Common.Enums.GeometryType.LineString, SridHelper.WebMercator).AsFeature()];
        }

        return result;
    }
}
