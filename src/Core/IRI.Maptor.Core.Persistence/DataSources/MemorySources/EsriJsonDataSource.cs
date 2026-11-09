using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Core.Spatial.IO.TopoJson;
using System.IO;
using IRI.Maptor.Core.Spatial.GeoJsonFormat;
using IRI.Maptor.Core.Spatial.IO.EsriJson;
using IRI.Maptor.Core.Persistence.Abstractions;
using IRI.Maptor.Core.Persistence.Model;

namespace IRI.Maptor.Core.Persistence.DataSources;

public class EsriJsonDataSource : MemoryDataSource
{
    private readonly string _fileName;

    private readonly int _sourceSrid;

    public override int OriginalSrid => _sourceSrid;

    public override SourceLocation? Location => string.IsNullOrEmpty(_fileName) ? null : new FileLocation { Path = _fileName };

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
    /// Creates an EsriJsonDataSource from an Esri JSON file. <c>fallbackSrid</c> (the user's choice) is used only
    /// when the file names no spatial reference.
    /// </summary>
    public static async Task<EsriJsonDataSource?> CreateFromFileAsync(string fileName, int? fallbackSrid = null)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
            throw new FileNotFoundException($"Esri JSON file not found: {fileName}", fileName);

        var jsonString = await File.ReadAllTextAsync(fileName);

        return CreateFromJson(jsonString, fileName, fallbackSrid);
    }

    /// <summary>
    /// Creates an EsriJsonDataSource from pasted or in-memory JSON text. <c>fallbackSrid</c> (the user's choice) is
    /// used only when the text names no spatial reference.
    /// </summary>
    public static Task<EsriJsonDataSource?> CreateFromTextAsync(string jsonText, int? fallbackSrid = null)
    {
        if (string.IsNullOrWhiteSpace(jsonText))
            throw new ArgumentException("JSON text cannot be empty.", nameof(jsonText));

        var ds = CreateFromJson(jsonText, string.Empty, fallbackSrid);

        return Task.FromResult<EsriJsonDataSource?>(ds);
    }

    private static EsriJsonDataSource? CreateFromJson(string jsonString, string fileName, int? fallbackSrid)
    {
        var esriJsonGeometry = EsriJsonFeatureSet.Parse(jsonString);

        if (esriJsonGeometry is null)
            return null;

        var fileSrid = esriJsonGeometry.SpatialReference?.LatestWkid ?? esriJsonGeometry.SpatialReference?.Wkid ?? 0;

        if (fileSrid <= 0 && fallbackSrid > 0)
        {
            esriJsonGeometry.SpatialReference = new EsriJsonSpatialReference { Wkid = fallbackSrid.Value };

            fileSrid = fallbackSrid.Value;
        }

        var features = esriJsonGeometry.AsFeatureSet().Project(SrsBases.WebMercator);

        if (features.IsNullOrEmpty())
            throw new InvalidOperationException(string.IsNullOrEmpty(fileName)
                ? "No features found in the JSON text."
                : $"No features found in Esri JSON file: {fileName}");

        // The SRS the data came in, so a save writes it back in that SRS. Before, the SRID after projection
        // (Web Mercator) was kept, and a save rewrote the file in Web Mercator.
        var sourceSrid = fileSrid > 0 && SrsBase.Create(fileSrid) is not null ? fileSrid : features.Srid;

        return new EsriJsonDataSource(fileName ?? string.Empty, features.Features, sourceSrid);
    }
}
