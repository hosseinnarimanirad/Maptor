using System;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.ShapefileFormat.Dbf;
using IRI.Maptor.Sta.ShapefileFormat.Model;
using IRI.Maptor.Sta.ShapefileFormat.EsriType;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.Persistence.Model;

namespace IRI.Maptor.Sta.Persistence.DataSources;

public class ShapefileDataSource : MemoryDataSource
{
    public override SourceLocation? Location => new FileLocation { Path = _shapefileName };

    public override DataSourceKind DataSourceKind => DataSourceKind.Shapefile;

    private string _shapefileName;

    private SrsBase _sourceSrs;

    //private SrsBase? _targetSrs;

    private List<ObjectToDbfTypeMap<Feature<Point>>> _objectToDbfTypeMap;

    private Encoding? _encoding;

    private Func<Geometry<Point>, Dictionary<string, object>, Feature<Point>> _createFeatureFunc;

    private Func<Feature<Point>, List<object>> _inverseAttributeMap;

    /// <summary>
    /// Lazy constructor: reads only shapefile header for extent/geometry type; does not load features.
    /// Call LoadAsync() to load the full data.
    /// </summary>
    internal ShapefileDataSource(string shapefileName,
                                //SrsBase? targetSrs,
                                Encoding? encoding,
                                Func<Geometry<Point>, Dictionary<string, object>, Feature<Point>> createFeatureFunc,
                                Func<Feature<Point>, List<object>> inverseAttributeMap)
    {
        _shapefileName = shapefileName;

        _sourceSrs = ShapefileFormat.Shapefile.TryGetSrs(shapefileName)
            ?? throw new NotImplementedException("Shapefile SRS could not be determined.");

        //_targetSrs = targetSrs;

        _encoding = encoding;

        _createFeatureFunc = createFeatureFunc;

        _inverseAttributeMap = inverseAttributeMap;

        var mainHeader = ShapefileFormat.Shapefile.GetFileHeader(shapefileName);

        WebMercatorExtent = mainHeader.MinimumBoundingBox.Transform(p => p.Project(_sourceSrs, SrsBases.WebMercator));

        //if (targetSrs != null)
        //{
        //    Func<Point, Point> transformFunc = p => p.Project(_sourceSrs, targetSrs);

        //    WebMercatorExtent = WebMercatorExtent.Transform(transformFunc);
        //}

        GeometryType = mainHeader.ShapeType.AsGeometryType() ?? Common.Enums.GeometryType.None;

        _webMercatorFeatureSet = FeatureSet<Point>.Empty;

        Fields = new List<Field>();

        _objectToDbfTypeMap = null;

        IsLoaded = false;
    }

    internal ShapefileDataSource(string shapefileName,
                                IEsriShapeCollection geometries,
                                EsriAttributeDictionary attributes,
                                Func<Geometry<Point>, Dictionary<string, object>, Feature<Point>> createFeatureFunc,
                                Func<Feature<Point>, List<object>> inverseAttributeMap)//,
                                //SrsBase targetSrs)
    {
        if (attributes == null)
            throw new NotImplementedException();

        _shapefileName = shapefileName;

        _sourceSrs = ShapefileFormat.Shapefile.TryGetSrs(shapefileName);

        if (_sourceSrs is null)
            throw new NotImplementedException();

        //_targetSrs = targetSrs;

        Initialize(geometries, attributes, createFeatureFunc, inverseAttributeMap);
    }

    public override async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsInitializing = true;

        try
        {
            System.Diagnostics.Debug.WriteLine($"***** LoadAsync shapefile started {DateTime.Now.ToLongTimeString()}");

            HasError = false;

            await Task.Delay(10);

            var attributes = await DbfFile.ReadAsync(ShapefileFormat.Shapefile.GetDbfFileName(_shapefileName), true, _encoding);

            System.Diagnostics.Debug.WriteLine($"***** LoadAsync shapefile - attributes read {DateTime.Now.ToLongTimeString()}");

            var geometries = await ShapefileFormat.Shapefile.ReadShapesAsync(_shapefileName);

            System.Diagnostics.Debug.WriteLine($"***** LoadAsync shapefile - geometry read {DateTime.Now.ToLongTimeString()}");

            //await Task.Delay(5000);

            Initialize(geometries, attributes, _createFeatureFunc, _inverseAttributeMap);

            System.Diagnostics.Debug.WriteLine($"***** LoadAsync shapefile - Initialize passes {DateTime.Now.ToLongTimeString()}");
        }
        catch
        {
            HasError = true;

            throw;
        }
        finally
        {
            IsInitializing = false;
        }
    }

    private void Initialize(IEsriShapeCollection geometries,
                             EsriAttributeDictionary attributes,
                             Func<Geometry<Point>, Dictionary<string, object>, Feature<Point>> map,
                             Func<Feature<Point>, List<object>> inverseAttributeMap)
    {
        System.Diagnostics.Debug.WriteLine($"***** Initialize shapefile started {DateTime.Now.ToLongTimeString()}");

        if (attributes == null)
            throw new NotImplementedException();

        //Func<Point, Point>? transformFunc = _targetSrs != null ? (p => p.Project(_sourceSrs, _targetSrs)) : null;

        //var webMercator = new WebMercator();

        //WebMercatorExtent = geometries.MainHeader.MinimumBoundingBox.Transform(p => p.Project(_sourceSrs, new WebMercator()));
        WebMercatorExtent = BoundingBox.GetMergedBoundingBox(geometries.Select(g => g.MinimumBoundingBox.Transform(p => p.Project(_sourceSrs, SrsBases.WebMercator))), true);

        GeometryType = geometries.MainHeader.ShapeType.AsGeometryType();

        _objectToDbfTypeMap = new List<ObjectToDbfTypeMap<Feature<Point>>>();

        foreach (var field in attributes.Fields)
        {
            var fieldName = field.Name;
            _objectToDbfTypeMap.Add(new ObjectToDbfTypeMap<Feature<Point>>(field, t =>
                t.Attributes.TryGetValue(fieldName, out var v) ? v : null));
        }

        this.Fields = attributes.Fields.Select(f => f.AsField()).ToList();

        if (geometries?.Count != attributes.Attributes?.Count)
            throw new NotImplementedException();

        var features = new List<Feature<Point>>();

        for (int i = 0; i < geometries.Count; i++)
        {
            //Geometry<Point>? geometry = transformFunc == null
            //    ? geometries[i].AsGeometry()
            //    : geometries[i].AsGeometry().Transform(transformFunc, _targetSrs!.Srid);
            var geometry = geometries[i].AsGeometry().Project(SrsBases.WebMercator);

            var feature = map(geometry, attributes.Attributes[i]);

            feature.Id = GetNewId();

            features.Add(feature);
        }

        _webMercatorFeatureSet = FeatureSet<Point>.Create(System.IO.Path.GetFileNameWithoutExtension(_shapefileName), features);

        IsLoaded = true;

        System.Diagnostics.Debug.WriteLine($"***** Initialize shapefile finished {DateTime.Now.ToLongTimeString()}");

    }


    public override Task SaveChangesAsync()
    {
        Func<Feature<Point>, EsriShapeBase?> geometryMap = f => f.TheGeometry.Project(_sourceSrs).AsEsriShape(_sourceSrs.Srid);

        var features = _webMercatorFeatureSet.Features;

        //save shp, shx, dbf, prj, cpg

        //if (_targetSrs != null)
        //{
        //    Func<Point, Point> inverseTransformFunc = p => p.Project(_targetSrs, _sourceSrs);

        //    geometryMap = t => t.TheGeometry.AsEsriShape(_sourceSrs.Srid, inverseTransformFunc);
        //}
        //else
        //{
        //    geometryMap = t => t.TheGeometry.AsEsriShape(_sourceSrs.Srid);
        //}

        ShapefileFormat.Shapefile.Save(_shapefileName, features, geometryMap, _objectToDbfTypeMap, _encoding ?? EncodingHelper.ArabicEncoding, _sourceSrs, true);

        _webMercatorFeatureSet.ApplyChanges();

        UpdateHasPendingChanges();

        return Task.CompletedTask;
    }
}
