using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Sta.Spatial.Primitives;
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
        _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        _useFirstLineAsHeader = useFirstLineAsHeader;
    }

    public override string ToString() => $"{nameof(CsvDataSource)}";

    public override Task SaveChanges()
    {
        _featureSet.SaveAsCsv(_fileName, _useFirstLineAsHeader);
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
        if (string.IsNullOrWhiteSpace(fileName))
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
        if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
            throw new FileNotFoundException($"CSV file not found: {fileName}", fileName);

        var featureSet = await GeoJsonFeatureSet.CsvToPointGeoJsonAsync(fileName, useFirstLineAsHeader);
        var features = (featureSet.Features ?? [])
            .Select(f => f.AsFeature(true, SrsBases.WebMercator))
            .ToList();

        if (features.Count == 0)
            throw new InvalidOperationException($"No features found in CSV file: {fileName}");

        return new CsvDataSource(fileName, features, useFirstLineAsHeader);
    }
}
