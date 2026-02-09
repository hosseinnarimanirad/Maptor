using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Persistence.Abstractions;

public interface IVectorDataSource : IDataSource
{
    GeometryType? GeometryType { get; }

    List<Field> Fields { get; set; }
     
    FeatureSet<Point> GetAsFeatureSet();
    FeatureSet<Point> GetAsFeatureSet(BoundingBox boundary);
    FeatureSet<Point> GetAsFeatureSet(Geometry<Point>? geometry);
    FeatureSet<Point> GetAsFeatureSet(double mapScale, BoundingBox boundingBox);

    Task<FeatureSet<Point>> GetAsFeatureSetAsync();
    Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundary);
    Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry);
    Task<FeatureSet<Point>> GetAsFeatureSetAsync(double mapScale, BoundingBox boundingBox);




    // Other ******************************************************************
    FeatureSet<Point> Search(string searchText);

}
