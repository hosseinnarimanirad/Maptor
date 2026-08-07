using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.Persistence.Model;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;

namespace IRI.Maptor.Ket.SqlitePersistence.GeoPackage;

/// <summary>
/// Vector data source for OGC GeoPackage format
/// Provides access to feature layers stored in GeoPackage
/// </summary>
public class GeoPackageDataSource : VectorDataSource, IDisposable
{
    private readonly GpkgVectorReader _reader;
    private readonly string _filePath;
    private readonly string _tableName;
    private GpkgLayerMetadata? _layerMetadata;
    private GpkgGeometryColumn? _geometryColumn;
    private bool _disposed;

    private SrsBase? _sourceSrs;
    private bool _hasSpatialIndex;

    // Features are projected to Web Mercator before being returned, so the source reports 3857.
    public override int Srid => SridHelper.WebMercator;

    public override DataSourceKind DataSourceKind => DataSourceKind.GeoPackage;

    public override SourceLocation? Location => new FileLocation { Path = _filePath, TableName = _tableName };

    /// <summary>
    /// Gets the layer metadata
    /// </summary>
    public GpkgLayerMetadata? LayerMetadata => _layerMetadata;

    /// <summary>
    /// Gets the geometry column information
    /// </summary>
    public GpkgGeometryColumn? GeometryColumn => _geometryColumn;

    /// <summary>
    /// Creates a new GeoPackage vector data source
    /// </summary>
    /// <param name="filePath">Path to the .gpkg file</param>
    /// <param name="tableName">Name of the feature table/layer to read</param>
    /// <param name="openImmediately">If true, opens the database immediately</param>
    public GeoPackageDataSource(string filePath, string tableName, bool openImmediately = true)
        : base(new List<Field>())
    {
        _reader = new GpkgVectorReader(filePath);
        _filePath = filePath;
        _tableName = tableName;

        if (openImmediately)
        {
            _reader.Open();
            Initialize();
        }
    }

    /// <summary>
    /// Opens the GeoPackage database if not already opened
    /// </summary>
    public void Open()
    {
        _reader.Open();
        Initialize();
    }

    private void Initialize()
    {
        // Get layer metadata
        var layers = _reader.GetFeatureLayers();

        _layerMetadata = layers.FirstOrDefault(l => l.TableName.Equals(_tableName, StringComparison.OrdinalIgnoreCase));

        if (_layerMetadata == null)
            throw new InvalidOperationException($"Feature layer not found: {_tableName}");

        // Get geometry column info
        _geometryColumn = _reader.GetGeometryColumnInfo(_tableName);

        if (_geometryColumn == null)
            throw new InvalidOperationException($"No geometry column found for layer: {_tableName}");

        // Resolve the source spatial reference so geometry can be projected to Web Mercator.
        _sourceSrs = SrsBase.Create(_geometryColumn.SrsId);

        // The bbox query uses the R-tree; not every GeoPackage has one.
        _hasSpatialIndex = _reader.HasSpatialIndex(_tableName, _geometryColumn.ColumnName);

        // Set extent, projected from the file SRS to Web Mercator (matches ShapefileDataSource).
        var fileExtent = new BoundingBox(
            _layerMetadata.MinX,
            _layerMetadata.MinY,
            _layerMetadata.MaxX,
            _layerMetadata.MaxY);

        WebMercatorExtent = _sourceSrs == null
            ? fileExtent
            : fileExtent.Transform(p => p.Project(_sourceSrs, SrsBases.WebMercator));

        // Map geometry type; default mixed/unknown ("GEOMETRY") to Polygon so the layer stays
        // symbolizable (SpatialModelMode != None).
        GeometryType = MapGeometryType(_geometryColumn.GeometryTypeName) ?? Sta.Common.Enums.GeometryType.Polygon;

        // Read a sample feature to get fields
        var sampleFeatures = _reader.ReadFeatures(_tableName);
        if (sampleFeatures.Any())
        {
            var sample = sampleFeatures.First();
            if (sample.Attributes != null)
            {
                Fields = Field.FromDictionary(sample.Attributes);
            }
        }
    }

    /// <summary>
    /// Gets all features as a FeatureSet asynchronously
    /// </summary>
    public override async Task<FeatureSet<Point>> GetAsFeatureSetAsync()
    {
        var features = await _reader.ReadFeaturesAsync(_tableName);
        return ToWebMercator(FeatureSet<Point>.Create(_tableName, features));
    }

    /// <summary>
    /// Gets features intersecting the given geometry (in Web Mercator) as a FeatureSet.
    /// </summary>
    public override async Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry)
    {
        if (geometry == null || geometry.IsNullOrEmpty())
            return await GetAsFeatureSetAsync();

        // geometry is in Web Mercator; query the source-SRS rtree with its projected bbox.
        var sourceBox = ToSourceBoundingBox(geometry.GetBoundingBox());
        var features = await _reader.ReadFeaturesAsync(_tableName, sourceBox);

        var webMercator = ToWebMercator(FeatureSet<Point>.Create(_tableName, features));

        var filtered = webMercator.Features.Where(f =>
            f.TheGeometry != null && !f.TheGeometry.IsNullOrEmpty() &&
            f.TheGeometry.Intersects(geometry)).ToList();

        return FeatureSet<Point>.Create(_tableName, filtered);
    }

    /// <summary>
    /// Gets features within a Web Mercator bounding box as a FeatureSet (projected to Web Mercator).
    /// </summary>
    public override async Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox webMercatorBoundingBox)
    {
        var sourceBox = ToSourceBoundingBox(webMercatorBoundingBox);
        var features = await _reader.ReadFeaturesAsync(_tableName, sourceBox);
        return ToWebMercator(FeatureSet<Point>.Create(_tableName, features));
    }

    /// <summary>
    /// Gets features as a FeatureSet asynchronously with map scale
    /// </summary>
    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(double mapScale, BoundingBox webMercatorBoundingBox)
    {
        return GetAsFeatureSetAsync(webMercatorBoundingBox);
    }

    /// <summary>
    /// Searches features by text (searches in all string attributes)
    /// </summary>
    public override async Task<FeatureSet<Point>> SearchAsync(string searchText)
    {
        var allFeatures = await _reader.ReadFeaturesAsync(_tableName);

        var matched = string.IsNullOrWhiteSpace(searchText)
            ? allFeatures
            : allFeatures.Where(f => f.Attributes != null && f.Attributes.Values.Any(v =>
                v != null && v.ToString()?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true)).ToList();

        return ToWebMercator(FeatureSet<Point>.Create(_tableName, matched));
    }

    /// <summary>Projects a source-SRS feature set to Web Mercator (identity when already 3857).</summary>
    private FeatureSet<Point> ToWebMercator(FeatureSet<Point> featureSet)
    {
        if (featureSet == null || featureSet.HasNoGeometry())
            return featureSet ?? FeatureSet<Point>.Empty;

        return featureSet.Project(SrsBases.WebMercator);
    }

    /// <summary>Transforms a Web Mercator bounding box to the source SRS for the rtree query.</summary>
    private BoundingBox ToSourceBoundingBox(BoundingBox webMercatorBoundingBox)
    {
        return _sourceSrs == null
            ? webMercatorBoundingBox
            : webMercatorBoundingBox.Transform(p => p.Project(SrsBases.WebMercator, _sourceSrs));
    }

    /// <summary>
    /// Gets the number of features in the layer
    /// </summary>
    public long GetFeatureCount()
    {
        return _reader.GetFeatureCount(_tableName);
    }

    /// <summary>
    /// Maps GeoPackage geometry type names to Maptor GeometryType enum
    /// </summary>
    private GeometryType? MapGeometryType(string gpkgTypeName)
    {
        var typeName = gpkgTypeName.ToUpperInvariant();

        if (typeName.Contains("POINT"))
            return Sta.Common.Enums.GeometryType.Point;

        else if (typeName.Contains("LINESTRING") || typeName.Contains("LINE"))
            return Sta.Common.Enums.GeometryType.LineString;

        else if (typeName.Contains("POLYGON"))
            return Sta.Common.Enums.GeometryType.Polygon;

        else if (typeName.Contains("MULTIPOINT"))
            return Sta.Common.Enums.GeometryType.MultiPoint;

        else if (typeName.Contains("MULTILINESTRING"))
            return Sta.Common.Enums.GeometryType.MultiLineString;

        else if (typeName.Contains("MULTIPOLYGON"))
            return Sta.Common.Enums.GeometryType.MultiPolygon;

        else if (typeName.Contains("GEOMETRY"))
            return null; // Mixed geometry types

        return null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _reader?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~GeoPackageDataSource()
    {
        Dispose();
    }

    public override string ToString()
    {
        return $"GeoPackageDataSource: {_tableName} ({GetFeatureCount()} features)";
    }
}

