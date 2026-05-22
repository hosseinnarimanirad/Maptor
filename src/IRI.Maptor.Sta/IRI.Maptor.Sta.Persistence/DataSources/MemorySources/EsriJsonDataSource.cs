using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.Spatial.IO.TopoJson;
using System.IO;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Sta.Spatial.IO.EsriJson;

namespace IRI.Maptor.Sta.Persistence.DataSources;

public class EsriJsonDataSource : MemoryDataSource
{
    private readonly string _fileName;

    private readonly int _sourceSrid;

    public override int OriginalSrid => _sourceSrid;

    public override string SourceAddress => $"Esri Json file: {_fileName}";

    public override DataSourceKind DataSourceKind => DataSourceKind.EsriJson;

    private EsriJsonDataSource(string fileName, IEnumerable<Feature<Point>> features, int sourceSrid)
        : base(features, resetIds: true, kind: DataSourceKind.Kml)
    {
        _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));

        _sourceSrid = sourceSrid;
    }

    public override string ToString() => $"{nameof(EsriJsonDataSource)}";

    public override async Task SaveChangesAsync()
    {
        if (!string.IsNullOrEmpty(_fileName))
        {
            var features = _webMercatorFeatureSet.Project(_sourceSrid);

            await features.SaveAsEsriJson(_fileName);
        }

        _webMercatorFeatureSet.ApplyChanges();

        UpdateHasPendingChanges();
    }

    /// <summary>
    /// Creates a TopoJsonDataSource from a TopoJSON file.
    /// </summary>
    public static async Task<EsriJsonDataSource?> CreateFromFileAsync(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
            throw new FileNotFoundException($"TopoJSON file not found: {fileName}", fileName);

        var jsonString = await File.ReadAllTextAsync(fileName);

        return CreateFromJson(jsonString, fileName);
    }

    /// <summary>
    /// Creates a TopoJsonDataSource from pasted or in-memory JSON text.
    /// </summary>
    public static Task<EsriJsonDataSource?> CreateFromTextAsync(string jsonText)
    {
        if (string.IsNullOrWhiteSpace(jsonText))
            throw new ArgumentException("JSON text cannot be empty.", nameof(jsonText));

        var ds = CreateFromJson(jsonText, string.Empty);
         
        return Task.FromResult<EsriJsonDataSource?>(ds);
    }

    private static EsriJsonDataSource? CreateFromJson(string jsonString, string fileName)
    {
        var esriJsonGeometry = EsriJsonFeatureSet.Parse(jsonString);

        if (esriJsonGeometry is null)
            return null;

        var features = esriJsonGeometry.AsFeatureSet().Project(SrsBases.WebMercator);



        if (features.IsNullOrEmpty())
            throw new InvalidOperationException(string.IsNullOrEmpty(fileName)
                ? "No features found in the JSON text."
                : $"No features found in GeoJSON file: {fileName}");

        return new EsriJsonDataSource(fileName ?? string.Empty, features.Features, features.Srid);
    }
}
