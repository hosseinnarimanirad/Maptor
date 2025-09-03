using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Persistence.Abstractions;

public interface IVectorDataSource : IDataSource
{
    GeometryType? GeometryType { get; }

    List<Field> Fields { get; set; }


    // Get Geometries *********************************************************

    //List<Geometry<Point>> GetGeometries();
    //List<Geometry<Point>> GetGeometries(BoundingBox boundingBox);
    //List<Geometry<Point>> GetGeometries(Geometry<Point>? geometry);

    //Task<List<Geometry<Point>>> GetGeometriesAsync();
    //Task<List<Geometry<Point>>> GetGeometriesAsync(BoundingBox boundingBox);
    //Task<List<Geometry<Point>>> GetGeometriesAsync(Geometry<Point>? geometry);


    // Get Named-Geometries ***************************************************

    //List<NamedGeometry> GetNamedGeometries();
    //List<NamedGeometry> GetNamedGeometries(BoundingBox boundary);
    //List<NamedGeometry> GetNamedGeometries(Geometry<Point>? geometry);
    //List<NamedGeometry> GetNamedGeometries(double mapScale, BoundingBox boundingBox);

    //Task<List<NamedGeometry>> GetNamedGeometriesAsync();
    //Task<List<NamedGeometry>> GetNamedGeometriesAsync(BoundingBox boundary);
    //Task<List<NamedGeometry>> GetNamedGeometriesAsync(Geometry<Point>? geometry);
    //Task<List<NamedGeometry>> GetNamedGeometriesAsync(double mapScale, BoundingBox boundingBox);



    // Get as FeatureSet of Point *********************************************

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
