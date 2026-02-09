using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.ShapefileFormat;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Sta.Persistence.DataSources;

public class MemoryDataSource : VectorDataSource, IEditableVectorDataSource
{
    protected FeatureSet<Point> _features;

    private int _uniqueId = 0;

    public override int Srid { get => /*GetSrid()*/ _features.Srid; /*protected set => _ = value;*/ }

    //// todo: remove this method
    //public int GetSrid()
    //{
    //    //return _features?.SkipWhile(g => g is null || g.TheGeometry.IsNotValidOrEmpty())?.FirstOrDefault()?.TheGeometry.Srid ?? 0;
    //    return _features.Srid;
    //}

    public MemoryDataSource() : base(new List<Field>()) { }

    public MemoryDataSource(List<Geometry<Point>> geometries, bool resetIds = true) : base(new List<Field>())
    {
        var features = geometries.Select(g => new Feature<Point>(g) { Id = GetNewId() }).ToList();

        Initialize(features, resetIds);
    }

    public MemoryDataSource(List<Feature<Point>> features, bool resetIds = true) : base(new List<Field>())
    {
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
    }
     

    public override string ToString() => $"MemoryDataSource";

    protected int GetNewId() => _uniqueId++;

    protected void UpdateExtent()
    {
        WebMercatorExtent = _features.Extent;
    }

    // Get as FeatureSet of Point
    public override FeatureSet<Point> GetAsFeatureSet(Geometry<Point>? geometry)
    {
        if (geometry.IsNullOrEmpty())
        {
            return _features;
        }
        else
        {
            return _features.FilterByGeometry(f => f.Intersects(geometry)); 
        }
    }

    public override FeatureSet<Point> GetAsFeatureSet(BoundingBox boundingBox) => _features.FilterByGeometry(f => f.Intersects(boundingBox));


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

    public override FeatureSet<Point> Search(string searchText)
    {
        throw new NotImplementedException();
    }
}