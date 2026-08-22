using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Persistence.Abstractions;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Core.Persistence.DataSources;

public abstract class VectorDataSource : BaseDataSource, IVectorDataSource
{
    public virtual GeometryType? GeometryType { get; protected set; }
     
    public List<Field> Fields { get; set; } = new List<Field>();

    public VectorDataSource(List<Field> fields)
    {
        this.Fields = fields;
    }
     

    #region Get as FeatureSet

    public virtual Task<FeatureSet<Point>> GetAsFeatureSetAsync() => GetAsFeatureSetAsync(Geometry<Point>.Empty);

    public abstract Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundingBox);

    public abstract Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry);

    public virtual Task<FeatureSet<Point>> GetAsFeatureSetAsync(double mapScale, BoundingBox boundingBox) => GetAsFeatureSetAsync(boundingBox);

    #endregion



    public abstract Task<FeatureSet<Point>> SearchAsync(string searchText);
}
