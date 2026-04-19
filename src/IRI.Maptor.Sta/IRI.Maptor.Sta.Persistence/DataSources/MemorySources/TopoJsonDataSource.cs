using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.IO.TopoJson;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.Persistence.Abstractions;

namespace IRI.Maptor.Sta.Persistence.DataSources;

/// <summary>
/// Memory data source that loads from TopoJSON. Does not support save back to TopoJSON.
/// </summary>
public class TopoJsonDataSource : MemoryDataSource
{
    public override DataSourceKind DataSourceKind => DataSourceKind.TopoJson;

    private readonly string _fileName;

    private TopoJsonDataSource(string fileName, List<Feature<Point>> features)
        : base(features, resetIds: true, kind: DataSourceKind.TopoJson)
    {
        _fileName = fileName ?? string.Empty;
    }

    public override string ToString() => $"{nameof(TopoJsonDataSource)}";

    public override Task SaveChangesAsync()
    {
        // TopoJSON save not supported; pasted or loaded data is read-only for persistence
        _featureSet.ApplyChanges();
        UpdateHasPendingChanges();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a TopoJsonDataSource from a TopoJSON file.
    /// </summary>
    public static async Task<TopoJsonDataSource> CreateFromFileAsync(string fileName, int sourceSrid = 4326)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
            throw new FileNotFoundException($"TopoJSON file not found: {fileName}", fileName);

        var jsonString = await File.ReadAllTextAsync(fileName);
        return CreateFromJson(jsonString, fileName, sourceSrid);
    }

    /// <summary>
    /// Creates a TopoJsonDataSource from pasted or in-memory JSON text.
    /// </summary>
    public static Task<TopoJsonDataSource> CreateFromTextAsync(string jsonText, int sourceSrid = 4326, string fileName = "")
    {
        if (string.IsNullOrWhiteSpace(jsonText))
            throw new ArgumentException("JSON text cannot be empty.", nameof(jsonText));

        var ds = CreateFromJson(jsonText, fileName ?? string.Empty, sourceSrid);
        return Task.FromResult(ds);
    }

    private static TopoJsonDataSource CreateFromJson(string jsonString, string fileName, int sourceSrid)
    {
        var topology = TopoJson.Parse(jsonString);
        var geometries = TopoJson.ToGeometry(topology, sourceSrid);

        var webMercator = new WebMercator();
        var features = new List<Feature<Point>>();

        foreach (var kvp in geometries)
        {
            if (kvp.Value == null || kvp.Value.IsNullOrEmpty())
                continue;

            var projected = kvp.Value.Project(webMercator);
            features.Add(new Feature<Point>
            {
                TheGeometry = projected,
                Attributes = new Dictionary<string, object> { { "object", kvp.Key } }
            });
        }

        if (features.Count == 0)
            throw new InvalidOperationException(string.IsNullOrEmpty(fileName)
                ? "No features found in the TopoJSON text."
                : $"No features found in TopoJSON file: {fileName}");

        return new TopoJsonDataSource(fileName ?? string.Empty, features);
    }
}
