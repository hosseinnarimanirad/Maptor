using System;
using System.Windows.Media;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;
using System.Linq;
using System.Threading.Tasks;
using IRI.Maptor.Sta.Ogc.WMS;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Common.Enums;

namespace IRI.Maptor.Jab.Common.Layers;

public class DrawingLayer : SymbolizableLayer
{
    DrawMode _mode;

    EditableFeatureLayer _editableFeatureLayer;

    public override LayerType Type => LayerType.EditableItem;

    public override BoundingBox Extent
    {
        get => _editableFeatureLayer.Extent;

        protected set => throw new NotImplementedException();
    }

    //public override RenderingApproach Rendering
    //{
    //    get => RenderingApproach.Default;

    //    protected set => throw new NotImplementedException();
    //}

    public DrawingLayer(DrawMode mode, Transform toScreen, Func<double, double> screenToMap, Point startMercatorPoint, EditableFeatureLayerOptions options)
    {
        _mode = mode;

        GeometryType type;

        switch (mode)
        {
            case DrawMode.Point:
                type = GeometryType.Point;
                break;
            case DrawMode.Polyline:
                type = GeometryType.LineString;
                break;
            case DrawMode.Polygon:
            case DrawMode.Rectangle:
                type = GeometryType.Polygon;
                break;
            case DrawMode.Freehand:
            default:
                throw new NotImplementedException();
        }

        //var options = new EditableFeatureLayerOptions() { IsVerticesLabelVisible = isEdgeLengthVisible };

        _editableFeatureLayer = new EditableFeatureLayer("edit", new List<Point>() { startMercatorPoint }, toScreen, screenToMap, type, options) { ZIndex = int.MaxValue };

        // todo: consider this line why not just assigning OnRequestFinishDrawing 
        _editableFeatureLayer.OnRequestFinishDrawing += (sender, e) => OnRequestFinishDrawing?.Invoke(this, EventArgs.Empty);

        _editableFeatureLayer.RequestFinishEditing = g =>
        {
            RequestFinishEditing?.Invoke(g);
        };

        _editableFeatureLayer.RequestCancelDrawing = () => { RequestCancelDrawing?.Invoke(); };

        VisibleRange = ScaleInterval.All;

        //this.VisualParameters = new VisualParameters(mode == DrawMode.Polygon ? new SolidColorBrush(Colors.YellowGreen) : null, new SolidColorBrush(Colors.Blue), 3, 1);
        var param = new VisualParameters(mode == DrawMode.Polygon ? new SolidColorBrush(Colors.YellowGreen) : null, new SolidColorBrush(Colors.Blue), 3, 1);

        SetSymbolizer(new SimpleSymbolizer(param));
    }

    public Action<Geometry<Point>> RequestFinishEditing;

    public Action RequestCancelDrawing { get; set; }

    public event EventHandler OnRequestFinishDrawing;

    public EditableFeatureLayer GetLayer()
    {
        return _editableFeatureLayer;
    }

    public void AddVertex(Point webMercatorPoint)
    {
        _editableFeatureLayer.AddVertex(webMercatorPoint);
    }

    public void UpdateLastVertexLocation(Point point)
    {
        _editableFeatureLayer.UpdateLastSemiVertexLocation(point);
    }

    public void AddSemiVertex(Point webMercatorPoint)
    {
        _editableFeatureLayer.AddSemiVertex(webMercatorPoint);
    }

    public Geometry<Point> GetFinalGeometry()
    {
        var geometry = _editableFeatureLayer.GetFinalGeometry();

        return geometry;
    }

    public bool HasAnyPoint()
    {
        return _editableFeatureLayer == null ? false : _editableFeatureLayer.HasAnyPoint();
    }

    public bool TryFinishDrawingPart()
    {
        return _editableFeatureLayer.TryFinishDrawingPart();
    }

    public void StartNewPart(Point webMercatorPoint)
    {
        _editableFeatureLayer.StartNewPart(webMercatorPoint);
    }

    public DrawingVisual? AsDrawingVisual(BoundingBox mapExtent, int imageWidth, int imageHeight, double mapScale)
    {
        var geometry = _editableFeatureLayer.GetFinalGeometry();

        if (geometry == null)
            return null;

        return geometry.AsDrawingVisual(GetMainOrDefaultSymbology()/*this.VisualParameters*/, imageWidth, imageHeight, mapExtent);

        //double xScale = imageWidth / mapExtent.Width;
        //double yScale = imageHeight / mapExtent.Height;
        //double scale = xScale > yScale ? yScale : xScale;

        //Func<System.Windows.Point, System.Windows.Point> mapToScreen =
        //    new Func<System.Windows.Point, System.Windows.Point>(p => new System.Windows.Point((p.X - mapExtent.XMin) * scale, -(p.Y - mapExtent.YMax) * scale));

        //var pen = this.VisualParameters.GetWpfPen();
        //pen.LineJoin = PenLineJoin.Round;
        //pen.EndLineCap = PenLineCap.Round;
        //pen.StartLineCap = PenLineCap.Round;

        //Brush brush = this.VisualParameters.Fill;

        //DrawingVisual drawingVisual = new SqlSpatialToDrawingVisual().ParseSqlGeometry(new List<Geometry<Point>>() { geometry }, i => mapToScreen(i), pen, brush, this.VisualParameters.PointSymbol);

        //drawingVisual.Opacity = this.VisualParameters.Opacity;

        //return drawingVisual;
    }

    public override Task<FeatureSet<Point>> GetFeatureSet(BoundingBox mapExtent, double mapScale)
    {
        return _editableFeatureLayer.GetFeatureSet(mapExtent, mapScale) ?? Task.FromResult(FeatureSet<Point>.Empty);
    }
}
