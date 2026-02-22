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
        _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        _useFirstLineAsHeader = useFirstLineAsHeader;
    }

    public override string ToString() => $"{nameof(TsvDataSource)}";

    public override Task SaveChanges()
    {
        _featureSet.SaveAsTsv(_fileName, _useFirstLineAsHeader);
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
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentNullException(nameof(fileName));
        if (features == null || features.Count == 0)
            throw new ArgumentException("At least one feature is required.", nameof(features));

        return new TsvDataSource(fileName, features, useFirstLineAsHeader);
    }

    /// <summary>
    /// Creates a TsvDataSource from a TSV file.
    /// </summary>
    public static async Task<TsvDataSource> CreateFromFileAsync(string fileName, bool useFirstLineAsHeader = false)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
            throw new FileNotFoundException($"TSV file not found: {fileName}", fileName);

        var featureSet = await GeoJsonFeatureSet.TsvToPointGeoJsonAsync(fileName, useFirstLineAsHeader);
        var features = (featureSet.Features ?? [])
            .Select(f => f.AsFeature(true, SrsBases.WebMercator))
            .ToList();

        if (features.Count == 0)
            throw new InvalidOperationException($"No features found in TSV file: {fileName}");

        return new TsvDataSource(fileName, features, useFirstLineAsHeader);
    }
}
