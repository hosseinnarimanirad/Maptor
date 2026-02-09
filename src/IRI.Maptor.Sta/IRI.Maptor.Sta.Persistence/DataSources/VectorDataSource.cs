using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Persistence.Abstractions;

namespace IRI.Maptor.Sta.Persistence.DataSources;

public abstract class VectorDataSource : IVectorDataSource
{
    public virtual BoundingBox WebMercatorExtent { get; protected set; }

    public abstract int Srid { get; /*protected set;*/ }

    public virtual GeometryType? GeometryType { get; protected set; }

    public List<Field> Fields { get; set; } = new List<Field>();

    public VectorDataSource(List<Field> fields)
    {
        this.Fields = fields;
    }

    #region Get as FeatureSet

    public virtual FeatureSet<Point> GetAsFeatureSet() => GetAsFeatureSet(Geometry<Point>.Empty);

    public abstract FeatureSet<Point> GetAsFeatureSet(BoundingBox boundingBox);

    public abstract FeatureSet<Point> GetAsFeatureSet(Geometry<Point>? geometry);

    public virtual FeatureSet<Point> GetAsFeatureSet(double mapScale, BoundingBox boundingBox) => GetAsFeatureSet(boundingBox);


    public virtual Task<FeatureSet<Point>> GetAsFeatureSetAsync() => Task.Run(GetAsFeatureSet);

    public virtual Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundingBox) => Task.Run(() => GetAsFeatureSet(boundingBox));

    public virtual Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry) => Task.Run(() => GetAsFeatureSet(geometry));

    public virtual Task<FeatureSet<Point>> GetAsFeatureSetAsync(double mapScale, BoundingBox boundingBox) => Task.Run(() => GetAsFeatureSet(mapScale, boundingBox));

    #endregion



    public abstract FeatureSet<Point> Search(string searchText);
}
