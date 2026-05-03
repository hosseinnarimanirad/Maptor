using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;

namespace IRI.Maptor.Sta.Persistence.DataSources;

/// <summary>
/// Memory data source that loads from and saves to GeoJSON files.
/// </summary>
public class GeoJsonDataSource : MemoryDataSource
{
    public override DataSourceKind DataSourceKind => DataSourceKind.GeoJson;

    private readonly string _fileName;
    private readonly bool _isLongitudeFirst;

    private GeoJsonDataSource(string fileName, List<Feature<Point>> features, bool isLongitudeFirst = true)
        : base(features, resetIds: true, kind: DataSourceKind.GeoJson)
    {
        _fileName = fileName ?? string.Empty;
        _isLongitudeFirst = isLongitudeFirst;
    }

    public override string ToString() => $"{nameof(GeoJsonDataSource)}";

    public override Task SaveChangesAsync()
    {
        if (!string.IsNullOrWhiteSpace(_fileName))
        {
            _featureSet.SaveAsGeoJson(_fileName, _isLongitudeFirst);
        }
        _featureSet.ApplyChanges();
        UpdateHasPendingChanges();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a GeoJsonDataSource from a GeoJSON file.
    /// </summary>
    public static GeoJsonDataSource CreateFromFile(string fileName, bool isLongitudeFirst = true)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
            throw new FileNotFoundException($"GeoJSON file not found: {fileName}", fileName);

        var featureSet = GeoJsonFeatureSet.Parse(File.ReadAllText(fileName));
        var features = (featureSet.Features ?? [])
            .Select(f => f.AsFeature(isLongitudeFirst, SrsBases.WebMercator))
            .ToList();

        if (features.Count == 0)
            throw new InvalidOperationException($"No features found in GeoJSON file: {fileName}");

        return new GeoJsonDataSource(fileName, features, isLongitudeFirst);
    }

    /// <summary>
    /// Creates a GeoJsonDataSource from a GeoJSON file asynchronously.
    /// </summary>
    public static async Task<GeoJsonDataSource> CreateFromFileAsync(string fileName, bool isLongitudeFirst = true)
    {
        return await CreateFromFileAsync(fileName, isLongitudeFirst, SridHelper.GeodeticWGS84);
    }

    /// <summary>
    /// Creates a GeoJsonDataSource from a GeoJSON file with the specified spatial reference.
    /// </summary>
    public static async Task<GeoJsonDataSource> CreateFromFileAsync(string fileName, bool isLongitudeFirst, int sourceSrid)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
            throw new FileNotFoundException($"GeoJSON file not found: {fileName}", fileName);

        var jsonString = await File.ReadAllTextAsync(fileName);
        return CreateFromJson(jsonString, fileName, isLongitudeFirst, sourceSrid);
    }

    /// <summary>
    /// Creates a GeoJsonDataSource from pasted or in-memory JSON text.
    /// </summary>
    public static Task<GeoJsonDataSource> CreateFromTextAsync(string jsonText, int sourceSrid, bool isLongitudeFirst, string fileName = "")
    {
        if (string.IsNullOrWhiteSpace(jsonText))
            throw new ArgumentException("JSON text cannot be empty.", nameof(jsonText));

        var ds = CreateFromJson(jsonText, fileName ?? string.Empty, isLongitudeFirst, sourceSrid);
        return Task.FromResult(ds);
    }

    private static GeoJsonDataSource CreateFromJson(string jsonString, string fileName, bool isLongitudeFirst, int sourceSrid)
    {
        var featureSet = GeoJsonFeatureSet.Parse(jsonString);
        var features = (featureSet.Features ?? [])
            .Select(f => f.AsFeature(isLongitudeFirst, SrsBases.WebMercator, sourceSrid))
            .ToList();

        if (features.Count == 0)
            throw new InvalidOperationException(string.IsNullOrEmpty(fileName)
                ? "No features found in the JSON text."
                : $"No features found in GeoJSON file: {fileName}");

        return new GeoJsonDataSource(fileName ?? string.Empty, features, isLongitudeFirst);
    }
}
