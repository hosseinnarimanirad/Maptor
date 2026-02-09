using System;
using System.Collections.Generic;
using System.Linq;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Ket.SqlitePersistence.GeoPackage;

/// <summary>
/// Vector data source for OGC GeoPackage format
/// Provides access to feature layers stored in GeoPackage
/// </summary>
public class GeoPackageDataSource : VectorDataSource, IDisposable
{
    private readonly GpkgVectorReader _reader;
    private readonly string _tableName;
    private GpkgLayerMetadata? _layerMetadata;
    private GpkgGeometryColumn? _geometryColumn;
    private bool _disposed;

    private int _srid;
    public override int Srid { get; /*protected set;*/ }

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

        // Set SRID
        this._srid = _geometryColumn.SrsId;

        // Set extent from metadata
        WebMercatorExtent = new BoundingBox(
            _layerMetadata.MinX,
            _layerMetadata.MinY,
            _layerMetadata.MaxX,
            _layerMetadata.MaxY);

        // Map geometry type
        GeometryType = MapGeometryType(_geometryColumn.GeometryTypeName);

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
    /// Gets all features as a FeatureSet
    /// </summary>
    public override FeatureSet<Point> GetAsFeatureSet(Geometry<Point>? geometry)
    {
        if (geometry == null || geometry.IsNullOrEmpty())
        {
            // Read all features
            var features = _reader.ReadFeatures(_tableName);
            return FeatureSet<Point>.Create(_tableName, features/*, Srid*/);
        }
        else
        {
            // Read features that intersect with the geometry
            var bbox = geometry.GetBoundingBox();
            var features = _reader.ReadFeatures(_tableName, bbox);

            // Further filter by actual geometry intersection
            var filtered = features.Where(f => 
                f.TheGeometry != null && !f.TheGeometry.IsNullOrEmpty() && 
                f.TheGeometry.Intersects(geometry)).ToList();

            return FeatureSet<Point>.Create(_tableName, filtered/*, Srid*/);
        }
    }

    /// <summary>
    /// Gets features within a bounding box as a FeatureSet
    /// </summary>
    public override FeatureSet<Point> GetAsFeatureSet(BoundingBox boundingBox)
    {
        var features = _reader.ReadFeatures(_tableName, boundingBox);
        return FeatureSet<Point>.Create(_tableName, features/*, Srid*/);
    }

    /// <summary>
    /// Searches features by text (searches in all string attributes)
    /// </summary>
    public override FeatureSet<Point> Search(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return GetAsFeatureSet(Geometry<Point>.Empty);

        var allFeatures = _reader.ReadFeatures(_tableName);

        var filtered = allFeatures.Where(f =>
        {
            if (f.Attributes == null)
                return false;

            return f.Attributes.Values.Any(v =>
                v != null && v.ToString()?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true);
        }).ToList();

        return FeatureSet<Point>.Create(_tableName, filtered);
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
            return Sta.Common.Primitives.GeometryType.Point;

        else if (typeName.Contains("LINESTRING") || typeName.Contains("LINE"))
            return Sta.Common.Primitives.GeometryType.LineString;
        
        else if (typeName.Contains("POLYGON"))
            return Sta.Common.Primitives.GeometryType.Polygon;
        
        else if (typeName.Contains("MULTIPOINT"))
            return Sta.Common.Primitives.GeometryType.MultiPoint;
        
        else if (typeName.Contains("MULTILINESTRING"))
            return Sta.Common.Primitives.GeometryType.MultiLineString;
        
        else if (typeName.Contains("MULTIPOLYGON"))
            return Sta.Common.Primitives.GeometryType.MultiPolygon;
        
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

