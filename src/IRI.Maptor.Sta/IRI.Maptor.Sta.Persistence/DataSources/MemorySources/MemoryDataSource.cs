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
    protected FeatureSet<Point> _features;

    private int _uniqueId = 0;

    private readonly DataSourceKind _dataSourceKind;
    public override DataSourceKind DataSourceKind => _dataSourceKind;

    public override int Srid { get => /*GetSrid()*/ _features.Srid; /*protected set => _ = value;*/ }



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

        _features = FeatureSet<Point>.Create(string.Empty, features);

        GeometryType = features.First().TheGeometry.Type;

        this.Fields = Field.FromDictionary(features?.FirstOrDefault().Attributes);

        UpdateExtent();

        // Memory data source is fully initialized with features at this point.
        IsLoaded = true;
    }


    public override string ToString() => $"MemoryDataSource";

    protected int GetNewId() => _uniqueId++;

    protected void UpdateExtent()
    {
        WebMercatorExtent = _features.Extent;
    }



    // Get as FeatureSet of Point
    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry)
    {
        if (geometry.IsNullOrEmpty())
        {
            return Task.FromResult(_features);
        }
        return Task.FromResult(_features.FilterByGeometry(f => f.Intersects(geometry)));
    }

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundingBox) => Task.FromResult(_features.FilterByGeometry(f => f.Intersects(boundingBox)));


    #region CRUD & Tack Changes

    public int NumberOfAddedFeatures => _features?.GetPendingChangeCounts(FeatureStatus.New) ?? 0;

    public int NumberOfDeletedFeatures => _features?.GetPendingChangeCounts(FeatureStatus.Removed) ?? 0;

    public int NumberOfUpdatedFeatures => _features?.GetPendingChangeCounts(FeatureStatus.Updated) ?? 0;

    protected void UpdateHasPendingChanges()
    {
        //HasPendingChanges = _addedFeatures.Count > 0 || _updatedFeatures.Count > 0 || _deletedIds.Count > 0;
        HasPendingChanges = _features?.UpdateHasPendingChanges() ?? false;// _features?.Features != null && _features.Features.Any(f => f.Status != Common.Enums.FeatureStatus.Unchanged);
    }

    public virtual void Add(Feature<Point> feature)
    {
        _features.Add(feature);

        UpdateExtent();

        UpdateHasPendingChanges();
        RaisePendingChangesCountsChanged();
    }

    public virtual void Remove(Feature<Point> feature)
    {
        this._features.Remove(feature);

        UpdateExtent();

        UpdateHasPendingChanges();
        RaisePendingChangesCountsChanged();
    }

    public virtual bool Update(Feature<Point> oldValue, Feature<Point> newValue)
    {
        if (!_features.Update(oldValue, newValue))
            return false;

        UpdateExtent();

        UpdateHasPendingChanges();
        RaisePendingChangesCountsChanged();
        return true;
    }

    private void RaisePendingChangesCountsChanged()
    {
        RaiseHasPendingChangesChanged();
    }

    public virtual void SaveChanges()
    {
        _features.ApplyChanges();
        UpdateHasPendingChanges();
    }


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