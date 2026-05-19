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
    protected FeatureSet<Point> _featureSet;

    private int _uniqueId = 0;

    private readonly DataSourceKind _dataSourceKind;
    public override DataSourceKind DataSourceKind => _dataSourceKind;

    public override int Srid { get => /*GetSrid()*/ _featureSet.Srid; /*protected set => _ = value;*/ }



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

    //protected readonly List<Feature<Point>> _addedFeatures = new List<Feature<Point>>();
    //protected readonly List<Feature<Point>> _updatedFeatures = new List<Feature<Point>>();
    //protected readonly List<int> _deletedIds = new List<int>();



    //// todo: remove this method
    //public int GetSrid()
    //{
    //    //return _features?.SkipWhile(g => g is null || g.TheGeometry.IsNotValidOrEmpty())?.FirstOrDefault()?.TheGeometry.Srid ?? 0;
    //    return _features.Srid;
    //}

    public MemoryDataSource() : base(new List<Field>()) { _dataSourceKind = DataSourceKind.Other; }

    public MemoryDataSource(List<Geometry<Point>> geometries, bool resetIds = true, DataSourceKind kind = DataSourceKind.Other) : base(new List<Field>())
    {
        _dataSourceKind = kind;
        var features = geometries.Select(g => new Feature<Point>(g) { Id = GetNewId() }).ToList();

        Initialize(features, resetIds);
    }

    public MemoryDataSource(List<Feature<Point>> features, bool resetIds = true, DataSourceKind kind = DataSourceKind.Other) : base(new List<Field>())
    {
        _dataSourceKind = kind;
        Initialize(features, resetIds);
    }

    private void Initialize(List<Feature<Point>> features, bool resetIds)
    {
        if (resetIds)
        {
            foreach (var item in features)
                item.Id = GetNewId();
        }

        _featureSet = FeatureSet<Point>.Create(string.Empty, features);

        //GeometryType = features.First().TheGeometry.Type;
        GeometryType = features.FirstOrDefault()?.GeometryType;

        this.Fields = _featureSet.Fields/*Field.FromDictionary(features?.FirstOrDefault().Attributes)*/;

        UpdateExtent();

        // Memory data source is fully initialized with features at this point.
        IsLoaded = true;
    }


    public override string ToString() => $"{nameof(MemoryDataSource)}";

    public int GetNewId() => _uniqueId++;

    protected void UpdateExtent()
    {
        WebMercatorExtent = _featureSet.Extent;
    }



    // Get as FeatureSet of Point
    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry)
    {
        if (geometry.IsNullOrEmpty())
        {
            return Task.FromResult(_featureSet);
        }
        return Task.FromResult(_featureSet.FilterByGeometry(f => f.Intersects(geometry)));
    }

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundingBox) => Task.FromResult(_featureSet.FilterByGeometry(f => f.Intersects(boundingBox)));


    #region CRUD & Tack Changes

    public int NumberOfAddedFeatures => _featureSet?.GetPendingChangesCounts(FeatureStatus.New) ?? 0;

    public int NumberOfDeletedFeatures => _featureSet?.GetPendingChangesCounts(FeatureStatus.Removed) ?? 0;

    public int NumberOfUpdatedFeatures => _featureSet?.GetPendingChangesCounts(FeatureStatus.Updated) ?? 0;

    protected void UpdateHasPendingChanges()
    { 
        HasPendingChanges = _featureSet?.HasPendingChanges() ?? false;

        RaiseHasPendingChangesChanged();
    }

    public virtual void Add(Feature<Point> feature)
    {
        _featureSet.Add(feature);

        UpdateExtent();

        UpdateHasPendingChanges();
    }

    public virtual void Remove(Feature<Point> feature)
    {
        this._featureSet.Remove(feature);

        UpdateExtent();

        UpdateHasPendingChanges();
    }

    //public virtual bool Update(Feature<Point> oldValue, Feature<Point> newValue)
    //{
    //    if (!_features.Update(oldValue, newValue))
    //        return false;

    //    UpdateExtent();

    //    UpdateHasPendingChanges();
    //    RaisePendingChangesCountsChanged();
    //    return true;
    //}

    public virtual bool UpdateGeometry(Feature<Point> feature, Geometry<Point> newGeometry)
    {
        if (!_featureSet.UpdateGeometry(feature, newGeometry))
            return false;

        UpdateExtent();

        UpdateHasPendingChanges();
        return true;
    }

    public virtual bool UpdateAttributes(Feature<Point> feature, Dictionary<string, object> oldAttributes)
    {
        _featureSet.UpdateOldAttributes(feature, oldAttributes);

        UpdateHasPendingChanges();
        return true;
    }

    public List<Feature<Point>> GetCurrentChanges() => _featureSet?.GetCurrentChanges()?.ToList() ?? [];

    public void UndoChanges(Feature<Point> feature)
    {
        _featureSet.UndoSingleFeatureChanges(feature);

        UpdateHasPendingChanges();
    }

    public void UndoAllChanges()
    {
        _featureSet.UndoAllChanges();

        UpdateHasPendingChanges();
    }

    public virtual Task SaveChangesAsync()
    {
        _featureSet.ApplyChanges();
        UpdateHasPendingChanges();

        return Task.CompletedTask;
    }

    //private void RaisePendingChangesCountsChanged()
    //{
    //    RaiseHasPendingChangesChanged();
    //}

    #endregion


    #region Static Methods

    public static MemoryDataSource CreateFromShapefile(string shpFileName, string label, SrsBase targetSrs = null, bool correctFarsiCharacters = true, Encoding dataEncoding = null, Encoding headerEncoding = null)
    {
        var features = Shapefile.ReadAsFeature(shpFileName, dataEncoding, targetSrs, headerEncoding, correctFarsiCharacters, label);

        return new MemoryDataSource(features);
    }

    public static async Task<MemoryDataSource> CreateFromShapefileAsync(string shpFileName, string label, Encoding dataEncoding = null, SrsBase targetSrs = null, Encoding headerEncoding = null, bool correctFarsiCharacters = true)
    {
        var features = await Shapefile.ReadAsFeatureAsync(shpFileName, dataEncoding, targetSrs, headerEncoding, correctFarsiCharacters, label);

        return new MemoryDataSource(features);
    }

    #endregion

    public override Task<FeatureSet<Point>> SearchAsync(string searchText)
    {
        throw new NotImplementedException();
    }

}