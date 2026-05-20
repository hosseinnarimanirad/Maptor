using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.ShapefileFormat.Dbf;

namespace IRI.Maptor.Sta.Persistence.DataSources;

public static class ShapefileDataSourceFactory
{
    static Func<Geometry<Point>, Dictionary<string, object>, Feature<Point>> mapShapeToFeature = (geometry, attributes) => new Feature<Point>(geometry, attributes);

    static Func<Feature<Point>, List<object>> inverseAttributeMap = feature => feature.Attributes.Select(kvp => kvp.Value).ToList();
     

    public static ShapefileDataSource Create(
        string shapefileName,
        //SrsBase targetCrs,
        Encoding encoding = null)
        => Create(shapefileName, mapShapeToFeature, inverseAttributeMap, /*targetCrs, */encoding);
   

    public static ShapefileDataSource Create(
        string shapefileName,
        Func<Geometry<Point>, Dictionary<string, object>, Feature<Point>> createFeatureFunc,
        Func<Feature<Point>, List<object>> inverseAttributeMap,
        //SrsBase? targetSrs = null,
        Encoding? encoding = null)
    {
        var attributes = DbfFile.Read(ShapefileFormat.Shapefile.GetDbfFileName(shapefileName), true, encoding);

        var geometries = ShapefileFormat.Shapefile.ReadShapes(shapefileName);

        return new ShapefileDataSource(shapefileName, geometries, attributes, createFeatureFunc, inverseAttributeMap/*, targetSrs*/);
    }


    public static async Task<ShapefileDataSource> CreateAsync(string shapefileName, /*SrsBase targetCrs, */Encoding encoding = null)
        => await CreateAsync(shapefileName, mapShapeToFeature, inverseAttributeMap, /*targetCrs, */encoding);
     

    public static async Task<ShapefileDataSource> CreateAsync(
        string shapefileName,
        Func<Geometry<Point>, Dictionary<string, object>, Feature<Point>> createFeatureFunc,
        Func<Feature<Point>, List<object>> inverseAttributeMap,
        //SrsBase? targetSrs = null,
        Encoding? encoding = null)
    {
        var attributes = DbfFile.Read(ShapefileFormat.Shapefile.GetDbfFileName(shapefileName), true, encoding);

        var geometries = await ShapefileFormat.Shapefile.ReadShapesAsync(shapefileName);

        return new ShapefileDataSource(shapefileName, geometries, attributes, createFeatureFunc, inverseAttributeMap/*, targetSrs*/);
    }

    /// <summary>
    /// Creates a shapefile data source with header-only (lazy) load. Layer can be added immediately;
    /// call LoadAsync() (e.g. via LayerManager) to load full data in the background.
    /// </summary>
    public static ShapefileDataSource CreateLazy(string shapefileName, /*SrsBase? targetSrs = null,*/ Encoding? encoding = null)
        => new ShapefileDataSource(shapefileName, /*targetSrs, */encoding, mapShapeToFeature, inverseAttributeMap);
}
