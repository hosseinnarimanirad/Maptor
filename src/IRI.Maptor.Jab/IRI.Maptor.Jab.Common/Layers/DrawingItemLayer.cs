using System;
using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Jab.Common.Models.Legend;
using IRI.Maptor.Sta.Persistence.DataSources;

using Geometry = IRI.Maptor.Sta.Spatial.Primitives.Geometry<IRI.Maptor.Sta.Common.Primitives.Point>;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;

namespace IRI.Maptor.Jab.Common;

public class DrawingItemLayer : VectorLayer//, IIdentifiable
{
    public Action<DrawingItemLayer>? RequestHighlightGeometry;


    //public int Id { get; set; }

    public Guid HighlightGeometryKey { get; private set; }

    public Geometry? Geometry
    {
        get => Feature?.TheGeometry;
    }

    public override BoundingBox Extent
    {
        get
        {
            if (Feature is not null)
            {
                return Feature.TheGeometry?.GetBoundingBox() ?? BoundingBox.NaN;
            }
            else if (SpecialPointLayer is not null)
            {
                return SpecialPointLayer.Extent;
            }

            return BoundingBox.NaN;
        }
        protected set
        {
            throw new NotImplementedException("DrawingItemLayer > Extent ");
        }
    }

    public override System.Windows.Visibility Visibility
    {
        get => base.Visibility;
        set
        {
            base.Visibility = value;

            if (SpecialPointLayer != null)
            {
                SpecialPointLayer.Visibility = value;
            }
        }
    }

    public override double Opacity
    {
        get => base.Opacity; set
        {
            base.Opacity = value;
            if (SpecialPointLayer != null)
            {
                SpecialPointLayer.Opacity = value;
            }
        }
    }

    private Feature<Point>? _feature;
    public Feature<Point>? Feature
    {
        get { return _feature; }
        set
        {
            _feature = value;
            RaisePropertyChanged();

            if (_feature is null)
            {
                //this.Extent = BoundingBox.NaN;
            }
            else
            {
                this.DataSource = new MemoryDataSource([_feature]);

                //this.Extent = _feature.TheGeometry.GetBoundingBox();
            }
        }
    }

    public SpecialPointLayer? SpecialPointLayer { get; set; }

    private DrawingItemLayer(string layerName, RasterizationMethod rasterizationMethod)
    {
        this.LayerName = layerName;
        this._rasterizationApproach = rasterizationMethod;
        this.ZIndex = int.MaxValue;
        this.HighlightGeometryKey = Guid.NewGuid();
    }

    private DrawingItemLayer(string layerName, Feature<Point> feature, RasterizationMethod rasterizationMethod) : this(layerName, rasterizationMethod)
    {
        this.Feature = feature;
    }

    public bool IsSpecialLayer() => SpecialPointLayer != null;

    public bool CanShowHighlightGeometry() => this.IsSelectedInToc && this.Visibility == System.Windows.Visibility.Visible;

    public static DrawingItemLayer? Create(string layerName, Feature<Point> feature, IEnumerable<ISymbolizer> symbolizers)
    {
        if (feature is null || feature.TheGeometry.IsNotValidOrEmpty())
            return null;

        DrawingItemLayer result = new DrawingItemLayer(layerName, feature, /*id, */RasterizationMethod.DrawingVisual);

        foreach (var item in symbolizers)
        {
            result.SetSymbolizer(item);
        }

        var geometryType = feature.TheGeometry.Type;

        result.SpatialModelMode =
            (geometryType == GeometryType.Point || geometryType == GeometryType.MultiPoint) ? SpatialModelMode.Point :
            ((geometryType == GeometryType.LineString || geometryType == GeometryType.MultiLineString) ? SpatialModelMode.Polyline :
            (geometryType == GeometryType.Polygon || geometryType == GeometryType.MultiPolygon) ? SpatialModelMode.Polygon : SpatialModelMode.None);

        result._type = LayerType.Drawing;

        result.Commands = new List<ILegendCommand>();

        result.OnIsSelectedInTocChanged += (sender, e) =>
        {
            result.RequestHighlightGeometry?.Invoke(result);
        };

        result.OnVisibilityChanged += (sender, e) =>
        {
            result.RequestChangeVisibility?.Invoke(result);
        };

        result.OnLayerNameChanged += (sender, e) =>
        {
            feature.Attributes[feature.LabelAttribute] = e.Arg;
        };

        return result;
    }

    public static DrawingItemLayer CreateSpecialLayer(string layerName, List<Locateable> locateables)
    {
        DrawingItemLayer result = new DrawingItemLayer(layerName, /*id,*/ RasterizationMethod.DrawingVisual)
        {
            //Extent = BoundingBox.CalculateBoundingBox(locateables.Select(l => new Point() { X = l.Location.X, Y = l.Location.Y })),
            _type = LayerType.MoveableItem,
        };

        result.SetSymbolizer(new SimpleSymbolizer(VisualParameters.GetDefaultForDrawingItems()));

        result.OnVisibilityChanged += (sender, e) =>
        {
            result.RequestChangeVisibility?.Invoke(result);
        };

        //result.AddVisualParameters(VisualParameters.GetDefaultForDrawingItems());

        result.SpecialPointLayer = new SpecialPointLayer(layerName, locateables, .8, null, LayerType.MoveableItem)
        {
            ParentLayerId = result.LayerId,
            ParentLayerName = result.LayerName
        };

        return result;
    }
}
