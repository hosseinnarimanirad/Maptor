using System;
using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Persistence.DataSources;

using IRI.Maptor.Core.Ogc.SLD;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Spatial.GeoJsonFormat;
using IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;
using Geometry = IRI.Maptor.Core.Spatial.Primitives.Geometry<IRI.Maptor.Core.Common.Primitives.Point>;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Models.MapText;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Wpf.Controls.MapMarkers;
using IRI.Maptor.Presentation.Wpf.ViewModels.Map;

namespace IRI.Maptor.Presentation.Wpf.Layers;

public class DrawingItemLayer : VectorLayer
{
    public Action<DrawingItemLayer>? RequestHighlightGeometry;

    public Guid HighlightGeometryKey { get; private set; }

    public Geometry? Geometry => Feature?.TheGeometry;

    public Geometry? Wgs84Geometry => Geometry?.Project(SrsBases.GeodeticWgs84);

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
        protected set => throw new NotImplementedException("DrawingItemLayer > Extent ");
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
        get => base.Opacity;
        set
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
                DataSource = new MemoryDataSource([_feature]);

                //this.Extent = _feature.TheGeometry.GetBoundingBox();
            }
        }
    }

    public SpecialPointLayer? SpecialPointLayer { get; set; }

    private DrawingItemLayer(string layerName, RasterizationMethod rasterizationMethod)
    {
        LayerName = layerName;
        _rasterizationApproach = rasterizationMethod;
        ZIndex = int.MaxValue;
        HighlightGeometryKey = Guid.NewGuid();
    }

    private DrawingItemLayer(string layerName, Feature<Point> feature, RasterizationMethod rasterizationMethod) : this(layerName, rasterizationMethod)
    {
        Feature = feature;
    }

    public bool IsSpecialLayer() => SpecialPointLayer != null;

    public bool CanShowHighlightGeometry() => IsSelectedInToc && Visibility == System.Windows.Visibility.Visible;


    public string Serialize()
    {
        if (IsTextLayer)
            return SerializeText();

        var sld = GetSld();

        var sldString = XmlHelper.Parse(sld);

        if (Feature is null)
            return string.Empty;

        var feature = Feature.AsGeoJsonFeature();

        // AsGeoJsonFeature hands over the drawing's own Attributes dictionary: add the style to a
        // copy, so saving never writes it into the live feature
        feature.Properties = Feature.Attributes is null ? [] : new Dictionary<string, object>(Feature.Attributes, Feature.Attributes.Comparer);

        feature.AddSldAttribute(sldString);

        return JsonHelper.Serialize(feature);
    }

    // A text drawing is saved like a geometry drawing: a GeoJSON point (the label's bottom-centre,
    // where the marker is anchored) with the text in the Label attribute and its style as a
    // TextSymbolizer-only SLD (see MapTextSld). An empty label is not saved.
    private string SerializeText()
    {
        var locateable = SpecialPointLayer?.Items.FirstOrDefault();

        if (locateable?.Element?.DataContext is not TextboxMarkerViewModel viewModel || string.IsNullOrWhiteSpace(viewModel.LabelValue))
            return string.Empty;

        var position = Geometry<Point>.Create(locateable.X, locateable.Y, SridHelper.WebMercator);

        var feature = GeoJsonFeature.Create(
            position.Project(SrsBases.GeodeticWgs84).AsGeoJson(),
            new Dictionary<string, object> { [MapTextSld.LabelAttribute] = viewModel.LabelValue });

        feature.AddSldAttribute(XmlHelper.Parse(MapTextSld.ToSld(viewModel.ToStyle(), LayerName)));

        return JsonHelper.Serialize(feature);
    }

    public static DrawingItemLayer Deserialize(string featureName, string jsonLayer)
    {
        var geoJsonFeature = JsonHelper.Deserialize<GeoJsonFeature>(jsonLayer)!;

        var sldString = geoJsonFeature.RetrieveSldAttribute();

        // the style now lives in the layer (symbolizers, SourceSld) and every save regenerates it,
        // so it must not stay behind as an attribute of the feature built from these properties
        geoJsonFeature.RemoveSldAttribute();

        var sld = XmlHelper.DeserializeFromXmlString<StyledLayerDescriptor>(sldString);

        // a point styled only with text is a text drawing (geometry drawings always carry a
        // point/line/polygon symbolizer), so no separate type marker is stored
        if (geoJsonFeature.Geometry?.Type == GeoJson.Point && MapTextSld.TryParse(sld) is { } textStyle)
            return DeserializeText(featureName, geoJsonFeature, sld, textStyle);

        var symbolizers = sld.ParseToSymbolizers();

        var layer = Create(featureName, geoJsonFeature.AsFeature(true, SrsBases.WebMercator), symbolizers)!;

        if (layer is not null)
            layer.SourceSld = sld;

        return layer!;
    }

    private static DrawingItemLayer DeserializeText(string featureName, GeoJsonFeature geoJsonFeature, StyledLayerDescriptor sld, MapTextStyle style)
    {
        var points = geoJsonFeature.AsFeature(true, SrsBases.WebMercator).TheGeometry?.Points;

        // callers (project restore, drawing file open) report a drawing that fails to load
        if (points is not { Count: > 0 })
            throw new System.IO.InvalidDataException("The text drawing has no position.");

        var point = points[0];

        object? text = null;
        geoJsonFeature.Properties?.TryGetValue(MapTextSld.GetLabelAttribute(sld), out text);

        var viewModel = new TextboxMarkerViewModel { LabelValue = text?.ToString() ?? string.Empty };

        viewModel.ApplyStyle(style);

        return CreateTextLayer(featureName, viewModel, point);
    }

    public static DrawingItemLayer? Create(string layerName, Feature<Point> feature, IEnumerable<ISymbolizer> symbolizers)
    {
        if (feature is null || feature.TheGeometry.IsNotValidOrEmpty())
            return null;

        DrawingItemLayer result = new DrawingItemLayer(layerName, feature, /*id, */RasterizationMethod.DrawingVisual);

        foreach (var item in symbolizers)
        {
            result.SetSymbolizer(item);
        }

        //var geometryType = feature.TheGeometry.Type;

        //result.SpatialModelMode =
        //    (geometryType == GeometryType.Point || geometryType == GeometryType.MultiPoint) ? SpatialModelMode.Point :
        //    ((geometryType == GeometryType.LineString || geometryType == GeometryType.MultiLineString) ? SpatialModelMode.Polyline :
        //    (geometryType == GeometryType.Polygon || geometryType == GeometryType.MultiPolygon) ? SpatialModelMode.Polygon : SpatialModelMode.None);

        result.SpatialModelMode = feature.GeometryType switch
        {
            GeometryType.Point => SpatialModelMode.Point,
            GeometryType.MultiPoint => SpatialModelMode.Point,
            GeometryType.LineString => SpatialModelMode.Polyline,
            GeometryType.MultiLineString => SpatialModelMode.Polyline,
            GeometryType.Polygon => SpatialModelMode.Polygon,
            GeometryType.MultiPolygon => SpatialModelMode.Polygon,
            _ => SpatialModelMode.None
        };

        result._type = LayerType.Drawing;

        result.Commands = [];

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

    /// <summary>
    /// One text-on-map label: a TextboxMarker anchored by its bottom-centre at
    /// <paramref name="webMercatorPosition"/>. Used both for a new label and a loaded one.
    /// </summary>
    public static DrawingItemLayer CreateTextLayer(string layerName, TextboxMarkerViewModel viewModel, Point webMercatorPosition, bool focusTextBoxOnLoad = false)
    {
        var locateable = new Locateable(AnchorFunctionHandlers.BottomCenter)
        {
            X = webMercatorPosition.X,
            Y = webMercatorPosition.Y,
            Element = new TextboxMarker { DataContext = viewModel, FocusTextBoxOnLoad = focusTextBoxOnLoad }
        };

        return CreateTextLayer(layerName, [locateable]);
    }

    public static DrawingItemLayer CreateTextLayer(string layerName, List<Locateable> locateables)
    {
        DrawingItemLayer result = new DrawingItemLayer(layerName, RasterizationMethod.DrawingVisual)
        {
            //_type = LayerType.MoveableItem,
            _type = LayerType.Complex,
            IsMovable = true,
            IsTextLayer = true
        };

        result.SetSymbolizer(new SimpleSymbolizer(VisualParameters.GetDefaultForDrawingItems()));

        result.OnVisibilityChanged += (sender, e) =>
        {
            result.RequestChangeVisibility?.Invoke(result);
        };

        //result.SpecialPointLayer = new SpecialPointLayer(layerName, locateables, .8, null, LayerType.MoveableItem)
        result.SpecialPointLayer = new SpecialPointLayer(layerName, locateables, .8, null, LayerType.Complex)
        {
            //ParentLayerId = result.LayerId,
            //ParentLayerName = result.LayerName,
            Parent = result,
            IsMovable = true
        };

        return result;
    }
}
