using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;

namespace IRI.Maptor.Sta.Persistence.DataSources;

/// <summary>
/// Memory data source that loads from and saves to CSV/TSV files (point-only).
/// </summary>
public class TextDataSource : MemoryDataSource
{
    private readonly string _fileName;

    private readonly int _sourceSrid;

    private readonly bool _useFirstLineAsHeader;

    public override DataSourceKind DataSourceKind => _dataSourceKind/*DataSourceKind.Csv*/;

    private TextDataSource(string fileName,
                            List<Feature<Point>> features,
                            int sourceSrid,
                            bool useFirstLineAsHeader,
                            DataSourceKind dataSourceKind)
        : base(features, resetIds: true, kind: dataSourceKind)
    {
        if (dataSourceKind != DataSourceKind.Csv && dataSourceKind != DataSourceKind.Tsv)
            throw new ArgumentException();

        //_dataSourceKind = dataSourceKind;

        _fileName = fileName ?? string.Empty;

        _useFirstLineAsHeader = useFirstLineAsHeader;

        _sourceSrid = sourceSrid;
    }

    public override string ToString() => $"{nameof(TextDataSource)}";

    public override async Task SaveChangesAsync()
    {
        if (!string.IsNullOrWhiteSpace(_fileName))
        {
            if (DataSourceKind == DataSourceKind.Csv)
                await _webMercatorFeatureSet.SaveAsCsv(_fileName, _useFirstLineAsHeader, _sourceSrid);

            else if (DataSourceKind == DataSourceKind.Tsv)
                await _webMercatorFeatureSet.SaveAsTsv(_fileName, _useFirstLineAsHeader, _sourceSrid);

            else
                throw new ArgumentException();
        }

        _webMercatorFeatureSet.ApplyChanges();

        UpdateHasPendingChanges();        
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
    public static async Task<TextDataSource> CreateFromFileAsync(
        string fileName,
        GeometryType type,
        DataSourceKind dataSourceKind,
        int sourceSrid,
        bool isLongitudeFirst,
        bool useFirstLineAsHeader)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
            throw new FileNotFoundException($"CSV/TSV file not found: {fileName}", fileName);

        if (dataSourceKind != DataSourceKind.Csv && dataSourceKind != DataSourceKind.Tsv)
            throw new ArgumentException();

        var delimiter = dataSourceKind == DataSourceKind.Csv ? IOHelper.CsvDelimiterChar : IOHelper.TsvDelimiterChar;

        var rawData = await IOHelper.ReadAllDelimitedFileAsync(fileName, ignoreEmptyLines: true, delimiter);

        var features = ParseToWebMercatorFeatures(rawData, useFirstLineAsHeader, isLongitudeFirst, sourceSrid, type);

        if (features.Count == 0)
            throw new InvalidOperationException($"No features found in CSV/TSV file: {fileName}");

        return new TextDataSource(fileName, features, sourceSrid, useFirstLineAsHeader, dataSourceKind);
    }

    /// <summary>
    /// Creates a CsvDataSource from pasted or in-memory text (e.g. from clipboard).
    /// </summary>
    public static Task<TextDataSource> CreateFromTextAsync(
        string text,
        GeometryType type,
        DataSourceKind dataSourceKind,
        int sourceSrid,
        bool isLongitudeFirst,
        bool useFirstLineAsHeader)
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

        return Task.FromResult(new TextDataSource(string.Empty, features, sourceSrid, useFirstLineAsHeader, dataSourceKind));
    }

    //public static async Task<TextDataSource> Create(CsvTsvOpenDialogResult)

    private static (int xIndex, int yIndex) FindXyColumnIndices(string[] headerRow, bool isLongitudeFirst)
    {
        var xAliases = new[] { "x", "lon", "lng", "longitude", "long" };
        var yAliases = new[] { "y", "lat", "latitude" };

        int xIndex = -1;
        int yIndex = -1;

        for (int i = 0; i < headerRow.Length; i++)
        {
            var cell = headerRow[i]?.Trim() ?? string.Empty;
            if (xIndex < 0 && xAliases.Contains(cell, StringComparer.OrdinalIgnoreCase))
                xIndex = i;
            if (yIndex < 0 && yAliases.Contains(cell, StringComparer.OrdinalIgnoreCase))
                yIndex = i;
            if (xIndex >= 0 && yIndex >= 0)
                break;
        }

        return (xIndex >= 0 && yIndex >= 0 && xIndex != yIndex) ? (xIndex, yIndex) : (isLongitudeFirst ? (0, 1) : (1, 0));
    }

    private static List<Feature<Point>> ParseToWebMercatorFeatures(List<string[]> rawData, bool useFirstLineAsHeader, bool isLongitudeFirst, int sourceSrid, GeometryType type)
    {
        if (rawData.IsNullOrEmpty())
            return [];

        var sourceSrs = SridHelper.AsSrsBase(sourceSrid);

        if (sourceSrs is null)
            throw new ArgumentException($"Unsupported SRID: {sourceSrid}", nameof(sourceSrid));

        string[] headerRow;

        int startIndex = useFirstLineAsHeader ? 1 : 0;

        int xIndex = isLongitudeFirst ? 0 : 1;

        int yIndex = isLongitudeFirst ? 1 : 0;

        List<string> attributeHeaders;

        if (useFirstLineAsHeader)
        {
            headerRow = rawData[0];

            if (type == IRI.Maptor.Sta.Common.Enums.GeometryType.Point)
            {
                (xIndex, yIndex) = FindXyColumnIndices(headerRow, isLongitudeFirst);
            }

            attributeHeaders = new List<string>();

            for (int p = 0; p < headerRow.Length; p++)
            {
                if (p != xIndex && p != yIndex)
                    attributeHeaders.Add(!string.IsNullOrWhiteSpace(headerRow[p]) ? headerRow[p].Trim() : $"header {attributeHeaders.Count + 1}");
            }
        }
        else
        {
            int colCount = rawData.Count > 0 ? rawData[0].Length : 0;

            headerRow = Array.Empty<string>();

            attributeHeaders = colCount > 2 ? Enumerable.Range(1, colCount - 2).Select(i => $"header {i}").ToList() : new List<string>();
        }

        //var webMercator = new WebMercator();

        var result = new List<Feature<Point>>();

        for (int i = startIndex; i < rawData.Count; i++)
        {
            if (rawData[i].Length < Math.Max(xIndex, yIndex) + 1)
                continue;

            double x = double.Parse(rawData[i][xIndex], CultureInfo.InvariantCulture);
            double y = double.Parse(rawData[i][yIndex], CultureInfo.InvariantCulture);

            var point = new Point(x, y);

            var geom = Geometry<Point>.Create([point], IRI.Maptor.Sta.Common.Enums.GeometryType.Point, sourceSrid);

            var projected = geom.Project(sourceSrs, SrsBases.WebMercator);

            var attributes = new Dictionary<string, object>();

            int attributeIdx = 0;

            for (int p = 0; p < rawData[i].Length; p++)
            {
                if (p != xIndex && p != yIndex)
                {
                    var attrName = attributeIdx < attributeHeaders.Count ? attributeHeaders[attributeIdx] : $"header {attributeIdx + 1}";

                    attributes[attrName] = rawData[i][p];

                    attributeIdx++;
                }
            }

            result.Add(new Feature<Point> { TheGeometry = projected, Attributes = attributes });
        }

        if (type == IRI.Maptor.Sta.Common.Enums.GeometryType.Polygon)
        {
            return [Geometry<Point>.CreatePolygon(result.Select(r => r.TheGeometry.AsPoint()).ToList(), SridHelper.WebMercator).AsFeature()];
        }
        if (type == IRI.Maptor.Sta.Common.Enums.GeometryType.LineString)
        {
            return [Geometry<Point>.CreateLineStringFromPoints(result.Select(r => r.TheGeometry).ToList()).AsFeature()];
        }

        return result;
    }
}
