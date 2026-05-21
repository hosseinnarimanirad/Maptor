using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.ShapefileFormat;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;

namespace IRI.Maptor.Sta.Persistence.DataSources;

public class MemoryDataSource : VectorDataSource, IEditableVectorDataSource
{
    /// <summary>
    /// feature set always in webmercator
    /// </summary>
    protected FeatureSet<Point> _webMercatorFeatureSet;

    private int _uniqueId = 0;

    public override string SourceAddress => $"Memory Data Source";

    protected readonly DataSourceKind _dataSourceKind;
    public override DataSourceKind DataSourceKind => _dataSourceKind;

    public override int Srid => _webMercatorFeatureSet.Srid;

    private Geometry<Point>? _filterGeometry;
    /// <summary>
    /// Optional geometry used to filter features client-side when reading. When set, IsClientFiltered becomes true.
    /// </summary>
    public Geometry<Point>? FilterGeometry
    {
        get => _filterGeometry;
        set
        {
            _filterGeometry = value;

            HasClientFilter = _filterGeometry != null && !_filterGeometry.IsNullOrEmpty();
        }
    }



    //GML = 15,
    //EsriJson = 16,
    //Other = 100,

    public MemoryDataSource() : base(new List<Field>()) { _dataSourceKind = DataSourceKind.Other; }

    public MemoryDataSource(List<Geometry<Point>> geometries, bool resetIds = true, DataSourceKind kind = DataSourceKind.Other) : this()
    { 
        _dataSourceKind = kind;

        var features = geometries.Select(g => g.AsFeature()).ToList();

        Initialize(features, resetIds);
    }

    public MemoryDataSource(List<Feature<Point>> webMercatorFeatures, bool resetIds = true, DataSourceKind kind = DataSourceKind.Other) : this()
    { 
        _dataSourceKind = kind;

        Initialize(webMercatorFeatures, resetIds);
    }

    private void Initialize(List<Feature<Point>> webMercatorFeatures, bool resetIds)
    {
        if (resetIds)
        {
            foreach (var item in webMercatorFeatures)
                item.Id = GetNewId();
        }

        _webMercatorFeatureSet = FeatureSet<Point>.Create(string.Empty, webMercatorFeatures);

        GeometryType = webMercatorFeatures.FirstOrDefault()?.GeometryType;

        this.Fields = _webMercatorFeatureSet.Fields/*Field.FromDictionary(features?.FirstOrDefault().Attributes)*/;

        UpdateExtent();

        IsLoaded = true;
    }


    public override string ToString() => $"{nameof(MemoryDataSource)}";

    public int GetNewId() => _uniqueId++;

    protected void UpdateExtent() => WebMercatorExtent = _webMercatorFeatureSet.Extent;



    // Get as FeatureSet of Point
    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry)
    {
        if (geometry.IsNullOrEmpty())
        {
            return Task.FromResult(_webMercatorFeatureSet);
        }

        return Task.FromResult(_webMercatorFeatureSet.FilterByGeometry(f => f.Intersects(geometry)));
    }

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundingBox) => Task.FromResult(_webMercatorFeatureSet.FilterByGeometry(f => f.Intersects(boundingBox)));


    #region CRUD & Tack Changes

    public int NumberOfAddedFeatures => _webMercatorFeatureSet?.GetPendingChangesCounts(FeatureStatus.New) ?? 0;

    public int NumberOfDeletedFeatures => _webMercatorFeatureSet?.GetPendingChangesCounts(FeatureStatus.Removed) ?? 0;

    public int NumberOfUpdatedFeatures => _webMercatorFeatureSet?.GetPendingChangesCounts(FeatureStatus.Updated) ?? 0;

    public List<Feature<Point>> GetCurrentChanges() => _webMercatorFeatureSet?.GetCurrentChanges()?.ToList() ?? [];

    protected void UpdateHasPendingChanges()
    {
        HasPendingChanges = _webMercatorFeatureSet?.HasPendingChanges() ?? false;
        //RaiseHasPendingChangesChanged();
    }

    public virtual void Add(Feature<Point> feature)
    {
        _webMercatorFeatureSet.Add(feature);

        UpdateExtent();

        UpdateHasPendingChanges();
    }

    public virtual void Remove(Feature<Point> feature)
    {
        this._webMercatorFeatureSet.Remove(feature);

        UpdateExtent();

        UpdateHasPendingChanges();
    }

    public virtual bool UpdateGeometry(Feature<Point> feature, Geometry<Point> newGeometry)
    {
        if (!_webMercatorFeatureSet.UpdateGeometry(feature, newGeometry))
            return false;

        UpdateExtent();

        UpdateHasPendingChanges();
        return true;
    }

    public virtual bool UpdateAttributes(Feature<Point> feature, Dictionary<string, object> oldAttributes)
    {
        _webMercatorFeatureSet.UpdateOldAttributes(feature, oldAttributes);

        UpdateHasPendingChanges();
        return true;
    }

    public void UndoChanges(Feature<Point> feature)
    {
        _webMercatorFeatureSet.UndoSingleFeatureChanges(feature);

        UpdateHasPendingChanges();
    }

    public void UndoAllChanges()
    {
        _webMercatorFeatureSet.UndoAllChanges();

        UpdateHasPendingChanges();
    }

    public virtual Task SaveChangesAsync()
    {
        _webMercatorFeatureSet.ApplyChanges();
        UpdateHasPendingChanges();

        return Task.CompletedTask;
    }

    #endregion

    public override Task<FeatureSet<Point>> SearchAsync(string searchText) => throw new NotImplementedException();


    //#region Static Methods

    //public static MemoryDataSource CreateFromShapefile(
    //    string shpFileName,
    //    string label,
    //    SrsBase targetSrs = null,
    //    bool correctFarsiCharacters = true,
    //    Encoding dataEncoding = null,
    //    Encoding headerEncoding = null)
    //{
    //    var features = Shapefile.ReadAsFeature(shpFileName, dataEncoding, targetSrs, headerEncoding, correctFarsiCharacters, label);

    //    return new MemoryDataSource(features);
    //}

    //public static async Task<MemoryDataSource> CreateFromShapefileAsync(
    //    string shpFileName,
    //    string label,
    //    SrsBase targetSrs = null,
    //    Encoding dataEncoding = null,
    //    Encoding headerEncoding = null,
    //    bool correctFarsiCharacters = true)
    //{
    //    var features = await Shapefile.ReadAsFeatureAsync(shpFileName, dataEncoding, targetSrs, headerEncoding, correctFarsiCharacters, label);

    //    return new MemoryDataSource(features);
    //}

    //#endregion

}