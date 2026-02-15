using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.ShapefileFormat;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using System.Text;

namespace IRI.Maptor.Sta.Persistence.DataSources;

public class MemoryDataSource : VectorDataSource, IEditableVectorDataSource
{
    protected FeatureSet<Point> _features;

    private int _uniqueId = 0;

    private readonly DataSourceKind _dataSourceKind;

    public override DataSourceKind DataSourceKind => _dataSourceKind;

    public override int Srid { get => /*GetSrid()*/ _features.Srid; /*protected set => _ = value;*/ }

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


    #region CRUD
     

    public virtual void Add(Feature<Point> newGeometry)
    {
        _features.Add(newGeometry);

        UpdateExtent();
    }

    public virtual void Remove(Feature<Point> geometry)
    {
        _features.Remove(geometry);

        UpdateExtent();
    }

    public virtual void Update(Feature<Point> newGeometry)
    {
        //if (_idFunc == null)
        //    return;

        //var geometry = _idFunc(newGeometry.Id);

        //var index = _features.IndexOf(geometry);

        ////var index = newGeometry.Id;

        ////if (index < 0)
        ////{
        ////    return;
        ////}

        //_features[index] = newGeometry;

        _features.Update(newGeometry);

        UpdateExtent();
    }

    public virtual void SaveChanges()
    {
        return;
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