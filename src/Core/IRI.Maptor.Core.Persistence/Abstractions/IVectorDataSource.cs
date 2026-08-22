using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Core.Persistence.Abstractions;

public interface IVectorDataSource : IDataSource
{
    GeometryType? GeometryType { get; }

    List<Field> Fields { get; set; }
     
    Task<FeatureSet<Point>> GetAsFeatureSetAsync();
    Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundary);
    Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry);
    Task<FeatureSet<Point>> GetAsFeatureSetAsync(double mapScale, BoundingBox boundingBox);




    // Other ******************************************************************
    Task<FeatureSet<Point>> SearchAsync(string searchText);

     
}
