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

namespace IRI.Maptor.Sta.Persistence.DataSources;

/// <summary>
/// Memory data source that loads from TopoJSON. Does not support save back to TopoJSON.
/// </summary>
public class TopoJsonDataSource : MemoryDataSource
{
    private readonly string _fileName;

    private readonly int _sourceSrid;

    // ************************************************************************
    //// The official TopoJSON specification follows the same recommendation as
    //// GeoJSON. It defines the order of elements for a position as x, y, z.
    //// For geographic coordinates (using the WGS84 datum), this translates
    //// to longitude, latitude, altitude but for the sake of flexibility we
    //// my support reodering of esting/northing in future
    // ************************************************************************
    //private readonly bool _isLongitudeFirst;

    public override int OriginalSrid => _sourceSrid;

    public override string SourceAddress => $"TopoJson file: {_fileName}";

    public override DataSourceKind DataSourceKind => DataSourceKind.TopoJson;

    private TopoJsonDataSource(string fileName, List<Feature<Point>> features, int sourceSrid)
        : base(features, resetIds: true, kind: DataSourceKind.TopoJson)
    {
        _fileName = fileName ?? string.Empty;

        _sourceSrid = sourceSrid;
    }

    public override string ToString() => $"{nameof(TopoJsonDataSource)}";

    public override async Task SaveChangesAsync()
    {
        if (!string.IsNullOrWhiteSpace(_fileName))
        {
            var featureSet = _webMercatorFeatureSet.Project(_sourceSrid);

            await featureSet.SaveAsTopoJson(_fileName);
        }

        _webMercatorFeatureSet.ApplyChanges();

        UpdateHasPendingChanges();
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
        if (sourceSrid == 0)
            throw new NotImplementedException("TopoJsonDataSource > CreateFromJson > srid cannot be 0!");

        var topology = TopoJson.Parse(jsonString);

        var geometries = TopoJson.ToGeometry(topology, sourceSrid);

        //var webMercator = new WebMercator();
        var features = new List<Feature<Point>>();

        foreach (var kvp in geometries)
        {
            if (kvp.Value == null || kvp.Value.IsNullOrEmpty())
                continue;

            var projected = kvp.Value.Project(SrsBases.WebMercator);
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

        return new TopoJsonDataSource(fileName ?? string.Empty, features, sourceSrid);
    }
}
