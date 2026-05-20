using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Enums;

namespace IRI.Maptor.Sta.Persistence.Abstractions;

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
