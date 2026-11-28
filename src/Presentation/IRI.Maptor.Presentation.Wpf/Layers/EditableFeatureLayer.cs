using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Presentation.Wpf.ViewModels;
using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Presentation.Wpf.Controls.MapOptions;
using IRI.Maptor.Presentation.Wpf.Controls.MapMarkers;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;

using WpfPoint = System.Windows.Point;
using Point = IRI.Maptor.Core.Common.Primitives.Point;
using LineSegment = System.Windows.Media.LineSegment;
using Geometry = IRI.Maptor.Core.Spatial.Primitives.Geometry<IRI.Maptor.Core.Common.Primitives.Point>;
using IRI.Maptor.Presentation.Wpf.Views;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Models;
using IRI.Maptor.Presentation.Core.Data;

namespace IRI.Maptor.Presentation.Wpf.Layers;

public class EditableFeatureLayer : SymbolizableLayer, IDisposable
{
    Transform _toScreen;

    Func<double, double> _screenToMap;

    private Func<FrameworkElement> MakePrimaryVertex { get; set; } = () => new Circle(1);

    private Geometry? _webMercatorGeometry;

    private Path? _feature;

    private MouseButtonEventHandler? _featureRightButtonUpHandler;

    private PathGeometry? _pathGeometry;

    private RecursiveCollection<Locateable>? _vertices;

    /// <summary>
    /// One part of the geometry (a line or a ring) and what stands for it on the map. The vertices, the edge labels and
    /// the figure of the screen path are all in the order of the part's points, so what belongs to a vertex is found
    /// by its index, never by searching for its coordinates (two vertices can be at one place).
    /// </summary>
    private sealed class EditedPart
    {
        public Geometry Geometry = null!;

        /// <summary>The index of the part's figure in the screen path.</summary>
        public int Figure;

        /// <summary>The vertices of the part, in the order of its points (the same list as in <c>_vertices</c>).</summary>
        public List<Locateable> Vertices = new();

        /// <summary>The label of the edge that starts at each point, while edge labels are shown; else empty.</summary>
        public List<Locateable> EdgeLabels = new();
    }

    // rebuilt with the locateables (ReconstructLocateables); a vertex added to a sketch is appended
    private readonly List<EditedPart> _parts = new();

    private readonly Dictionary<Locateable, (EditedPart Part, int Index)> _addresses = new();

    // true while EditedPart.EdgeLabels and the total label are what UpdateEdgeLabels built: only then can a vertex
    // move update the labels it touches in place
    private bool _edgeLabelsInSync;

    // the label of the whole length or area, and the point of the geometry it was placed from
    private Locateable? _totalLabel;

    private Point? _totalLabelAnchor;


    private SpecialPointLayer _primaryVerticesLayer;

    // Edge Length
    private SpecialPointLayer _edgeLabelLayer;

    // Vertex Coordinates 
    private SpecialPointLayer _primaryVerticesLabelLayer;


    //public EditableFeatureLayerOptions Options { get; }
    private EditableFeatureLayerOptions _options;
    public EditableFeatureLayerOptions Options
    {
        get { return _options; }
        set
        {
            if (_options != null)
            {
                _options.RequestHandleMeasureVisibilityChanged = null;
            }

            _options = value;
            RaisePropertyChanged();
        }
    }


    public override BoundingBox Extent
    {
        get => _webMercatorGeometry.GetBoundingBox();

        protected set => throw new NotImplementedException();
    }

    public override LayerType Type => LayerType.EditableItem;

    private double _height;
    public double Height
    {
        get { return _height; }
        set
        {
            _height = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(GroundLength));
            RaisePropertyChanged(nameof(EuclideanLengthInUtm_Refined));
            RaisePropertyChanged(nameof(GroundArea));
        }
    }

    public bool IsMultiPartGeometry => _webMercatorGeometry?.IsMultiPartGeometry == true;

    public bool IsPreviousPartAvailable => FindCurrentPartIndex() > 0;

    public bool IsNextPartAvailable
    {
        get
        {
            var current = FindCurrentPartIndex();

            return current >= 0 && current < GetPartsLocateables().Count - 1;
        }
    }


    #region Actions

    public Action<FrameworkElement, MouseButtonEventArgs, ILocatable>? RequestRightClickOptions;

    public Action? RequestRemoveRightClickOptions;

    public Action<EditableFeatureLayer>? RequestRefresh;

    public Action<Geometry>? RequestConvertToDrawingItem;

    public Action<EditableFeatureLayer>? RequestShowGeometryDetails;

    // todo: check if duplicate
    //public Action<EditableFeatureLayer>? RequestShowCoordinates;

    // drawing
    public event EventHandler? OnRequestFinishDrawing;

    public Action? RequestCancelDrawing;

    // editing
    public Action<Geometry>? RequestFinishEditing;

    public Action<EditableFeatureLayer>? RequestCancelEditing;

    //public Action<EditableFeatureLayer>? RequestDeleteLayer;

    public Action<Locateable?, int>? RequestSelectedLocatableChanged;

    // zoom
    public Action<Point>? RequestZoomToPoint;

    public Action<Geometry>? RequestZoomToGeometry;

    // pan: centre the map on a point (web mercator) and keep the scale
    public Action<Point>? RequestPanToPoint;

    public event Action? LocateablesReconstructed;

    /// <summary>Raised whenever the geometry's points/parts are added or removed (not on coordinate moves).</summary>
    public event Action? OnGeometryChanged;

    /// <summary>True once a vertex was moved, added or removed in this session (Esc asks before discarding).</summary>
    public bool HasChanges { get; private set; }

    public Func<CoordinateDisplayMode>? RequestGetCoordinateDisplayMode;

    public Func<IMapSettings>? RequestGetMapSettings;

    #endregion




    /// <summary>
    /// For Polygons do not repeat first point in the last point
    /// </summary>
    /// <param name="name"></param>
    /// <param name="webMercatorPoints"></param>
    /// <param name="isClosed"></param>
    public EditableFeatureLayer(
        string name,
        List<Point> webMercatorPoints,
        Transform toScreen,
        Func<double, double> screenToMap,
        GeometryType type,
        Action<Locateable?, int> requestSelectedLocatableChanged,
        EditableFeatureLayerOptions? options = null)
        : this(name, Geometry.Create(webMercatorPoints, type, SridHelper.WebMercator), toScreen, screenToMap,
                requestSelectedLocatableChanged, options)
    {
    }

    /// <summary>
    /// For Polygons do not repeat first point in the last point
    /// </summary>
    /// <param name="name"></param>
    /// <param name="mercatorPoints"></param>
    /// <param name="isClosed"></param>
    public EditableFeatureLayer(
        string name,
        Geometry webMercatorGeometry,
        Transform toScreen,
        Func<double, double> screenToMap,
        Action<Locateable?, int> requestSelectedLocatableChanged,
        EditableFeatureLayerOptions options)
    {
        Options = options ?? EditableFeatureLayerOptions.CreateDefault();

        Options.RequestHandleMeasureVisibilityChanged = UpdateEdgeLabels;

        LayerName = name;

        _webMercatorGeometry = webMercatorGeometry;

        LayerId = Guid.NewGuid();

        _toScreen = toScreen;

        _screenToMap = screenToMap;

        VisibleRange = ScaleInterval.All;

        //this.VisualParameters = new VisualParameters(_mercatorGeometry.IsRingBase() ? _fill : null, _stroke, 3, 1);
        //this.VisualParameters = Options.Visual;
        SetSymbolizer(new SimpleSymbolizer(Options.Visual));

        _feature = GetDefaultEditingPath();

        _pathGeometry = new PathGeometry();

        MakePathGeometry();

        _feature.Data = _pathGeometry;

        //if 
        //{
        //    this._feature.MouseUp += (sender, e) => { this.RegisterMapOptionsForNewPath(e); };
        //}
        if (!Options.IsNewDrawing && !Options.IsMeasureMode)
        {
            // a right click on the line itself; one beside it (within reach of the edge handle) comes from the map
            _featureRightButtonUpHandler = (sender, e) => { ShowEditOptions(e, PathToMap(e.GetPosition(_feature))); };

            _feature.MouseRightButtonUp += _featureRightButtonUpHandler;
        }

        bool isMovable = !Options.IsNewDrawing;

        //var layerType = Options.IsNewDrawing ? LayerType.EditableItem : LayerType.MoveableItem | LayerType.EditableItem;
        var layerType = LayerType.EditableItem;

        this.RequestSelectedLocatableChanged = requestSelectedLocatableChanged;

        _primaryVerticesLayer = new SpecialPointLayer("#vert", new List<Locateable>(), 1, ScaleInterval.All, layerType) { AlwaysTop = true, IsMovable = isMovable };

        _primaryVerticesLayer.RequestSelectedLocatableChanged = (l, i) =>
        {
            RequestSelectedLocatableChanged?.Invoke(l, i);

            RaisePropertyChanged(nameof(IsPreviousPartAvailable));
            RaisePropertyChanged(nameof(IsNextPartAvailable));
        };

        // the labels can be dragged, so the user can lay them out around the line or polygon (user decision 2026-11-28)
        _edgeLabelLayer = new SpecialPointLayer("#edge length", new List<Locateable>(), .9, ScaleInterval.All, layerType) { AlwaysTop = false, IsMovable = isMovable };

        _primaryVerticesLabelLayer = new SpecialPointLayer("#vert length", new List<Locateable>(), .9, ScaleInterval.All, layerType) { AlwaysTop = false, IsMovable = isMovable };

        ReconstructLocateables();

        // the initial build is not a change
        HasChanges = false;

        SelectPoint(0);
        //_primaryVerticesLayer.SelectLocatable(0);

        if (Options.IsNewDrawing)
        {
            //add virtual vertex which show last point
            AddSemiVertex(webMercatorGeometry.Points == null ? webMercatorGeometry.Geometries.Last().Points.Last() : webMercatorGeometry.Points.Last());
        }
    }

    private WpfPoint ToScreen(WpfPoint point)
    {
        return _toScreen.Transform(point);
    }

    #region New Drawing

    // add point to the last part
    internal void StartNewPart(Point webMercatorPoint)
    {
        _webMercatorGeometry.Geometries.Last().InsertLastPoint(webMercatorPoint);

        MakePathGeometry();

        ReconstructLocateables();
    }

    // add an empty new part
    internal bool TryFinishDrawingPart()
    {
        var result = _webMercatorGeometry.TryAddNewPart();

        MakePathGeometry();

        ReconstructLocateables();

        return result;
    }

    /// <summary>
    /// Adds a new empty part to the geometry
    /// </summary>
    /// <returns>True if the part was successfully added, false otherwise</returns>
    public bool TryAddNewPart()
    {
        var result = _webMercatorGeometry.TryAddNewPart();

        if (result)
        {
            MakePathGeometry();
            ReconstructLocateables();
        }

        return result;
    }

    internal bool TryAddNewRing(int? currentPolygonIndex)
    {
        bool result = false;

        if (_webMercatorGeometry is null)
            return result;

        if (_webMercatorGeometry.Type == GeometryType.Polygon)
        {
            result = _webMercatorGeometry.TryAddNewRing();
        }
        else if (_webMercatorGeometry.Type == GeometryType.MultiPolygon)
        {
            result = _webMercatorGeometry.Geometries![currentPolygonIndex!.Value].TryAddNewRing();
        }

        if (result)
        {
            MakePathGeometry();
            ReconstructLocateables();
        }

        return result;
    }

    internal void CancelDrawing()
    {
        RequestCancelDrawing?.Invoke();
    }

    #endregion


    #region Private Methods

    private Path GetDefaultEditingPath()
    {
        var result = new Path()
        {
            Stroke = Options.Visual.Stroke,
            StrokeThickness = Options.Visual.StrokeThickness,
            StrokeDashArray = Options.Visual.DashStyle?.Dashes,
            Opacity = Options.Visual.Opacity,
            StrokeLineJoin = PenLineJoin.Round,
        };

        if (_webMercatorGeometry.IsRingBase())
        {
            result.Fill = Options.Visual.Fill;
        }

        return result;
    }

    private void ReconstructLocateables()
    {
        // Unwire the previous generation of locateables before discarding them so their
        // event subscriptions (and WPF marker elements) can be garbage collected.
        _vertices?.GetFlattenCollection().ForEach(l => l.Detach());

        _vertices = new RecursiveCollection<Locateable>();

        _parts.Clear();

        _addresses.Clear();

        InvalidateMeasures();

        MakeLocateables(_webMercatorGeometry, _vertices);

        _primaryVerticesLayer.Items.Clear();

        _primaryVerticesLabelLayer.Items.Clear();

        var primary = _vertices.GetFlattenCollection();

        foreach (var item in primary)
        {
            _primaryVerticesLayer.Items.Add(item);
        }

        UpdateEdgeLabels();

        // Notify that Locateables have been reconstructed
        LocateablesReconstructed?.Invoke();

        // Structural change (points/parts added or removed) funnels through here.
        HasChanges = true;

        OnGeometryChanged?.Invoke();

        RaisePropertyChanged(nameof(IsPreviousPartAvailable));
        RaisePropertyChanged(nameof(IsNextPartAvailable));
    }


    /// <summary>
    /// Builds the edge labels and the label of the whole length or area again, from nothing. Used when the structure
    /// or what is shown changes; a vertex move goes through <see cref="UpdateLabelsAfterMove"/> instead.
    /// </summary>
    private void UpdateEdgeLabels()
    {
        _edgeLabelLayer.Items.Clear();

        _edgeLabelsInSync = false;

        _totalLabel = null;

        _totalLabelAnchor = null;

        foreach (var part in _parts)
        {
            part.EdgeLabels.Clear();
        }

        if (!_webMercatorGeometry.IsValid())
            return;

        if (this._webMercatorGeometry.Type.GetCategory() == GeometryCategory.Point)
            return;

        if (Options.IsMeasureVisible && Options.IsEdgeLabelVisible)
        {
            foreach (var part in _parts)
            {
                var points = part.Geometry.Points;

                var edges = EdgeCount(part);

                for (int i = 0; i < edges; i++)
                {
                    var label = ToEdgeLengthLocatable(points[i], points[(i + 1) % points.Count]);

                    part.EdgeLabels.Add(label);

                    _edgeLabelLayer.Items.Add(label);
                }
            }
        }

        if (Options.IsMeasureVisible)
        {
            var point = _webMercatorGeometry?.GetMeanOrLastPoint();

            if (point == null)
                return;

            var element = new RectangleLabelMarker(MeasureLabel);

            //do not show length/area when geometry has just one/two point or new part has just one/two point
            if (double.IsNaN(MeasureValue))
                return;

            element.TooltipValue = MeasureValue.ToInvariantString();

            var offset = _screenToMap(20);

            _totalLabel = new Locateable(AnchorFunctionHandlers.BottomCenter)
            {
                Element = element,
                X = point.X + offset,
                Y = point.Y + offset
            };

            _totalLabelAnchor = new Point(point.X, point.Y);

            _edgeLabelLayer.Items.Add(_totalLabel);
        }

        _edgeLabelsInSync = true;

        RaiseMeasuresChanged();
    }

    /// <summary>The number of edges of <paramref name="part"/>: a ring has one more than a line, the closing edge.</summary>
    private int EdgeCount(EditedPart part)
    {
        var count = part.Geometry.Points?.Count ?? 0;

        if (count < 2)
            return 0;

        return _webMercatorGeometry.IsRingBase() ? count : count - 1;
    }

    /// <summary>
    /// After the vertex at <paramref name="index"/> of <paramref name="part"/> moved by <paramref name="displacement"/>:
    /// the labels of its two edges get their new length and follow the middle of their edge, and the label of the
    /// whole follows the geometry. Each label is moved by how far its place moved, so a label the user dragged
    /// somewhere keeps its distance. The other labels are not touched.
    /// </summary>
    private void UpdateLabelsAfterMove(EditedPart part, int index, Point displacement)
    {
        if (!_edgeLabelsInSync)
        {
            UpdateEdgeLabels();

            return;
        }

        var edges = part.EdgeLabels.Count;

        if (edges > 0)
        {
            // the edge that starts at the vertex, and the one that ends at it (for the first vertex of a ring, the
            // closing edge)
            var next = index < edges ? index : -1;

            var previous = index > 0 ? index - 1 : (edges == part.Geometry.Points.Count ? edges - 1 : -1);

            UpdateEdgeLabel(part, next, displacement);

            if (previous != next)
                UpdateEdgeLabel(part, previous, displacement);
        }

        if (_totalLabel is not null && _totalLabelAnchor is not null && _webMercatorGeometry.GetMeanOrLastPoint() is { } anchor)
        {
            if (_totalLabel.Element is RectangleLabelMarker marker)
            {
                marker.LabelValue = MeasureLabel;

                marker.TooltipValue = MeasureValue.ToInvariantString();
            }

            _totalLabel.MoveTo(_totalLabel.X + anchor.X - _totalLabelAnchor.X, _totalLabel.Y + anchor.Y - _totalLabelAnchor.Y);

            _totalLabelAnchor = new Point(anchor.X, anchor.Y);
        }

        RaiseMeasuresChanged();
    }

    private void UpdateEdgeLabel(EditedPart part, int edge, Point displacement)
    {
        if (edge < 0 || edge >= part.EdgeLabels.Count)
            return;

        var points = part.Geometry.Points;

        var label = part.EdgeLabels[edge];

        var text = EdgeLengthLabel(points[edge], points[(edge + 1) % points.Count]);

        if (label is EdgeLabel edgeLabel)
            edgeLabel.Text = text;

        if (label.CreatedElement is RectangleLabelMarker marker)
            marker.LabelValue = text;

        // one end of the edge moved, so its middle moved by half of that
        label.MoveTo(label.X + displacement.X / 2.0, label.Y + displacement.Y / 2.0);
    }

    private void RaiseMeasuresChanged()
    {
        RaisePropertyChanged(nameof(EllipsoidalLength));
        RaisePropertyChanged(nameof(ScaleFactor));
        RaisePropertyChanged(nameof(EuclideanLengthInUtm));
        RaisePropertyChanged(nameof(EuclideanLengthInUtm_Refined));
        RaisePropertyChanged(nameof(SphericalLength));
        RaisePropertyChanged(nameof(GroundLength));

        RaisePropertyChanged(nameof(EuclideanArea));
        RaisePropertyChanged(nameof(EllipsoidalArea));
        RaisePropertyChanged(nameof(AuthalicSphereArea));
        RaisePropertyChanged(nameof(KarneyArea));
        RaisePropertyChanged(nameof(GroundArea));
    }

    private void UpdateCoordinate(Locateable locatable)
    {
        var locatables = _primaryVerticesLabelLayer.Get(locatable.Id);

        foreach (var item in locatables)
        {
            //(item.Element as CoordinateMarker).WebMercatorLocation = new Point(locatable.X, locatable.Y);
            (item.Element as CoordinateMarker)!.WebMercatorLocation = locatable;

            item.MoveTo(locatable.X, locatable.Y);
        }
    }

    private void MakeLocateables(Geometry geometry, RecursiveCollection<Locateable> primaryCollection)
    {
        if (geometry.Points != null)
        {
            primaryCollection.Values = new List<Locateable>();

            // the parts are met in the order MakePathGeometry adds their figures
            var part = new EditedPart { Geometry = geometry, Figure = _parts.Count, Vertices = primaryCollection.Values };

            _parts.Add(part);

            for (int i = 0; i < geometry.Points.Count; i++)
            {
                var locateable = ToPrimaryLocateable(geometry.Points[i]);

                primaryCollection.Values.Add(locateable);

                _addresses[locateable] = (part, i);
            }
        }
        else
        {
            primaryCollection.Collections = new List<RecursiveCollection<Locateable>>();

            foreach (var item in geometry.Geometries)
            {
                RecursiveCollection<Locateable> subPrimaryCollection = new RecursiveCollection<Locateable>();

                MakeLocateables(item, subPrimaryCollection);

                primaryCollection.Collections.Add(subPrimaryCollection);
            }
        }
    }

    internal void ChangeCurrentEditingPoint(Point point)
    {
        //find selected locatable

        var currentEditingPoint = _primaryVerticesLayer.FindSelectedLocatable();

        if (currentEditingPoint == null)
            return;

        currentEditingPoint.MoveTo(point.X, point.Y);
    }

    private void MakePathGeometry()
    {
        _pathGeometry.Figures.Clear();

        MakePathGeometry(_webMercatorGeometry);
    }

    private void MakePathGeometry(Geometry geometry)
    {
        if (geometry.Points != null)
        {
            PathFigure pathFigure = new PathFigure() { IsClosed = _webMercatorGeometry.IsRingBase() };

            if (geometry.Points.Count > 0)
            {
                pathFigure.StartPoint = ToScreen(geometry.Points.First().AsWpfPoint());
            }

            for (int i = 1; i < geometry.Points.Count; i++)
            {
                var segment = new LineSegment(ToScreen(geometry.Points[i].AsWpfPoint()), true);

                pathFigure.Segments.Add(segment);
            }

            _pathGeometry.Figures.Add(pathFigure);
        }
        else if (geometry.Geometries != null)
        {
            foreach (var g in geometry.Geometries)
            {
                MakePathGeometry(g);
            }
        }
        //else
        //{
        //    return;
        //}
        RaisePropertyChanged(nameof(IsMultiPartGeometry));
    }



    private Locateable ToPrimaryLocateable(IPoint point)
    {
        var webMercatorPoint = point;

        var locateable = new Locateable(AnchorFunctionHandlers.CenterCenter)
        {
            X = webMercatorPoint.X,
            Y = webMercatorPoint.Y,
            Id = Guid.NewGuid(),
            CanBeUsedAsEditingPoint = true
        };

        // The control is made when the vertex goes on the map, so only for the vertices in view: a geometry of
        // thousands of vertices no longer makes thousands of controls (step 5 of the drawing and editing plan).
        locateable.CreateElement = () => MakeVertexElement(locateable, webMercatorPoint);

        locateable.RequestChangeIsSelected = (isSelected) =>
        {
            if (locateable.CreatedElement is IMapMarker marker)
                marker.IsSelected = isSelected;
        };

        locateable.OnPositionChanged += (sender, e) =>
        {
            var displacement = new Point(locateable.X - webMercatorPoint.X, locateable.Y - webMercatorPoint.Y);

            webMercatorPoint.X = locateable.X;
            webMercatorPoint.Y = locateable.Y;

            HasChanges = true;

            InvalidateMeasures();

            // only what the vertex touches is updated: its point of the screen path, the labels of its two edges and
            // the label of the whole
            if (_addresses.TryGetValue(locateable, out var address))
            {
                UpdatePathPoint(address.Part, address.Index, locateable);

                UpdateLabelsAfterMove(address.Part, address.Index, displacement);
            }
            else
            {
                UpdateEdgeLabels();
            }

            UpdateCoordinate(locateable);
        };

        return locateable;
    }

    /// <summary>The control of a vertex, with its mouse handlers; undone by the vertex's <see cref="Locateable.Detach"/>.</summary>
    private FrameworkElement MakeVertexElement(Locateable locateable, IPoint webMercatorPoint)
    {
        var element = /*Options*/this.MakePrimaryVertex();

        if (Options.IsNewDrawing)
        {
            element.MouseDown -= Element_MouseDown;
            element.MouseDown += Element_MouseDown;

            locateable.OnDetach += () => element.MouseDown -= Element_MouseDown;

            //Finish Drawing if click on any point
            //locateable.Element.MouseDown += (sender, e) =>
            //{
            //    if (e.LeftButton == MouseButtonState.Pressed)
            //    {
            //        OnRequestFinishDrawing?.Invoke(this, EventArgs.Empty);

            //        e.Handled = true;
            //    }
            //};
        }
        else
        {
            MouseButtonEventHandler rightDown = (sender, e) =>
            {
                _primaryVerticesLayer.SelectLocatable(locateable);
            };

            MouseButtonEventHandler rightUp = (sender, e) =>
            {
                e.Handled = true;

                RegisterMapOptionsForVertices(e, webMercatorPoint, locateable);
            };

            element.MouseRightButtonDown += rightDown;

            element.MouseRightButtonUp += rightUp;

            locateable.OnDetach += () =>
            {
                element.MouseRightButtonDown -= rightDown;
                element.MouseRightButtonUp -= rightUp;
            };
        }

        return element;
    }


    // Finish Drawing if click on any point
    private void Element_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            OnRequestFinishDrawing?.Invoke(this, EventArgs.Empty);

            e.Handled = true;
        }
    }

    private void RegisterMapOptionsForVertices(MouseButtonEventArgs e, IPoint point, Locateable locateable)
    {
        var presenter = new MapOptionsViewModel(
                rightToolTip: IRI.Maptor.Presentation.Core.Properties.Resources.mapPanel_currentPoint_copyCoordinate,
                leftToolTip: IRI.Maptor.Presentation.Core.Properties.Resources.mapPanel_currentPoint_displayCoordinate,
                middleToolTip: IRI.Maptor.Presentation.Core.Properties.Resources.mapPanel_currentPoint_delete,

                rightSymbol: MapOptionsIcon.FromMaterial(MahApps.Metro.IconPacks.PackIconMaterialKind.ContentCopy),
                leftSymbol: MapOptionsIcon.FromMaterial(MahApps.Metro.IconPacks.PackIconMaterialKind.AxisArrowInfo),
                middleSymbol: MapOptionsIcon.FromMaterial(MahApps.Metro.IconPacks.PackIconMaterialKind.TrashCanOutline));

        presenter.RightCommandAction = i =>
        {
            var mode = RequestGetCoordinateDisplayMode?.Invoke() ?? CoordinateDisplayMode.GeodeticDecimal;

            var mapSettings = RequestGetMapSettings?.Invoke() ?? MapSettings.Default;

            var options = CopyCoordinateOptions.Create(mapSettings.Clipboard_LatLongPrecision, mapSettings.Clipboard_XyPrecision);

            ClipboardHelper.CopyToClipboard(new Point(point.X, point.Y), mode, options, mapSettings.Clipboard_IsLatitudeFirst /*null, null, null, null*/);

            RemoveMapOptions();
        };

        presenter.LeftCommandAction = i =>
        {
            if (_primaryVerticesLabelLayer.Items.Any(l => l.Id == locateable.Id))
            {
                _primaryVerticesLabelLayer.Remove(locateable.Id);
            }
            else
            {
                var displayMode = RequestGetCoordinateDisplayMode?.Invoke();

                var element = new CoordinateMarker(locateable, displayMode);

                var auxLocateable = new Locateable(AnchorFunctionHandlers.CenterLeft) { Element = element, X = point.X, Y = point.Y, Id = locateable.Id };

                _primaryVerticesLabelLayer.Items.Add(auxLocateable);
            }

            RemoveMapOptions();
        };

        presenter.MiddleCommandAction = i =>
        {
            TryDeleteVertex(locateable);

            RemoveMapOptions();
        };

        RequestRightClickOptions?.Invoke(new MapThreeOptions(false), e, presenter);

    }

    /// <summary>
    /// The right-click options of the edited geometry at <paramref name="mapPoint"/> (Web Mercator): finish and cancel,
    /// and in the middle, when the point is on an edge (within reach of the edge handle), add a vertex there.
    /// </summary>
    public void ShowEditOptions(MouseButtonEventArgs e, Point mapPoint)
    {
        var hit = CanInsertVertices
            ? FindEdge(mapPoint, _screenToMap(EdgeHandleReach), _screenToMap(VertexReach))
            : null;

        var presenter = new MapOptionsViewModel(
            leftToolTip: IRI.Maptor.Presentation.Core.Properties.Resources.mapPanel_edit_cancel,
            rightToolTip: IRI.Maptor.Presentation.Core.Properties.Resources.mapPanel_edit_finish,
            middleToolTip: hit is null ? string.Empty : IRI.Maptor.Presentation.Core.Properties.Resources.map_draw_addPoint,

            leftSymbol: MapOptionsIcon.FromMaterial(MahApps.Metro.IconPacks.PackIconMaterialKind.CloseThick),
            rightSymbol: MapOptionsIcon.FromMaterial(MahApps.Metro.IconPacks.PackIconMaterialKind.CheckBold),
            middleSymbol: hit is null ? string.Empty : MapOptionsIcon.FromMaterial(MahApps.Metro.IconPacks.PackIconMaterialKind.VectorPolylinePlus)
            );

        presenter.RightCommandAction = i =>
        {
            FinishEditing();

            RemoveMapOptions();
        };

        presenter.LeftCommandAction = i =>
        {
            CancelEditing();
        };

        if (hit is { } edge)
        {
            // on the edge at the place the menu was opened for (the menu covers the pointer once it is open)
            presenter.MiddleCommandAction = i =>
            {
                RemoveMapOptions();

                InsertVertex(edge, edge.Point);
            };
        }

        RequestRightClickOptions?.Invoke(hit is null ? new MapTwoOptions(false) : new MapThreeOptions(false), e, presenter);
    }

    private void RemoveMapOptions()
    {
        RequestRemoveRightClickOptions?.Invoke();
    }

    // ---------------------------------------------------------------- inserting a vertex on an edge

    /// <summary>
    /// Above this many vertices in view no handles are drawn and the map asks to zoom in (decision D5): a handle is
    /// a control, and thousands of them make the map slow while giving nothing that can be aimed at.
    /// </summary>
    public const int MaxHandlesInView = 2_000;

    /// <summary>How far from an edge, in pixels, the edge handle shows and a press inserts a vertex (decision D6).</summary>
    public const double EdgeHandleReach = 6;

    /// <summary>Within this many pixels of a vertex the vertex is meant, not its edges: no edge handle there.</summary>
    public const double VertexReach = 8;

    /// <summary>An edge of the edited geometry and the point on it nearest to where it was looked for (Web Mercator).</summary>
    /// <param name="Part">The part, in the order of the geometry's lines and rings.</param>
    /// <param name="Edge">The edge's index in its part: from point <c>Edge</c> to the next one (for a ring's last edge, to the first).</param>
    public readonly record struct EdgeHit(int Part, int Edge, Point Start, Point End, Point Point, double Distance)
    {
        public Point Middle => new((Start.X + End.X) / 2.0, (Start.Y + End.Y) / 2.0);
    }

    /// <summary>True when a vertex can be added on an edge: an edit of a line or a polygon, not a sketch being drawn.</summary>
    public bool CanInsertVertices
        => !Options.IsNewDrawing && _webMercatorGeometry?.Type.GetCategory() is GeometryCategory.Polyline or GeometryCategory.Polygon;

    /// <summary>
    /// The edge nearest to <paramref name="mapPoint"/> within <paramref name="reach"/>, or null. Null too when a vertex
    /// is within <paramref name="vertexReach"/>: there the vertex is meant. Distances are in map units.
    /// </summary>
    public EdgeHit? FindEdge(Point mapPoint, double reach, double vertexReach)
    {
        if (!CanInsertVertices || mapPoint.IsNaN())
            return null;

        EdgeHit? best = null;

        for (int p = 0; p < _parts.Count; p++)
        {
            var points = _parts[p].Geometry.Points;

            if (points is null)
                continue;

            foreach (var point in points)
            {
                if (Distance(point, mapPoint) <= vertexReach)
                    return null;
            }

            var edges = EdgeCount(_parts[p]);

            for (int i = 0; i < edges; i++)
            {
                var start = points[i];
                var end = points[(i + 1) % points.Count];

                var nearest = NearestOnSegment(start, end, mapPoint);

                var distance = Distance(nearest, mapPoint);

                if (distance <= reach && (best is null || distance < best.Value.Distance))
                    best = new EdgeHit(p, i, new Point(start.X, start.Y), new Point(end.X, end.Y), nearest, distance);
            }
        }

        return best;
    }

    /// <summary>
    /// Adds a vertex at <paramref name="at"/> (Web Mercator) after the start of the edge <paramref name="hit"/>, selects
    /// it and returns it, or null when the edge is not there any more.
    /// </summary>
    public Locateable? InsertVertex(EdgeHit hit, Point at)
    {
        if (hit.Part < 0 || hit.Part >= _parts.Count || _parts[hit.Part].Geometry.Points is not { } points || hit.Edge >= points.Count)
            return null;

        var part = _parts[hit.Part];

        var index = hit.Edge + 1;

        var point = new Point(at.X, at.Y);

        // in place: one point, one vertex, one segment of the screen path; the map puts the vertex on itself
        // through the collection change (nothing is rebuilt, step 5 of the drawing and editing plan)
        points.Insert(index, point);

        var vertex = ToPrimaryLocateable(point);

        part.Vertices.Insert(index, vertex);

        Readdress(part, index);

        _primaryVerticesLayer.Items.Insert(FlatIndex(part, index), vertex);

        if (part.Figure < _pathGeometry.Figures.Count)
            _pathGeometry.Figures[part.Figure].Segments.Insert(index - 1, new LineSegment(ToScreen(point.AsWpfPoint()), true));

        AfterStructureChangeInPlace();

        _primaryVerticesLayer.SelectLocatable(vertex);

        return vertex;
    }

    /// <summary>The addresses of the vertices of <paramref name="part"/> from <paramref name="from"/> on, after one was added or removed there.</summary>
    private void Readdress(EditedPart part, int from)
    {
        for (int i = Math.Max(0, from); i < part.Vertices.Count; i++)
        {
            _addresses[part.Vertices[i]] = (part, i);
        }
    }

    /// <summary>The place of the vertex at <paramref name="index"/> of <paramref name="part"/> among all vertices of the geometry.</summary>
    private int FlatIndex(EditedPart part, int index)
    {
        var before = 0;

        foreach (var other in _parts)
        {
            if (ReferenceEquals(other, part))
                break;

            before += other.Vertices.Count;
        }

        return before + index;
    }

    /// <summary>What follows a vertex added or removed in place: the labels, the measures and the notifications.</summary>
    private void AfterStructureChangeInPlace()
    {
        InvalidateMeasures();

        UpdateEdgeLabels();

        HasChanges = true;

        LocateablesReconstructed?.Invoke();

        OnGeometryChanged?.Invoke();
    }

    /// <summary>A point of the screen path (in the path's own units) as a map point (Web Mercator).</summary>
    private Point PathToMap(WpfPoint pathPoint)
    {
        var map = _toScreen.Inverse?.Transform(pathPoint) ?? new WpfPoint(double.NaN, double.NaN);

        return new Point(map.X, map.Y);
    }

    private static double Distance(IPoint first, IPoint second)
        => Math.Sqrt((first.X - second.X) * (first.X - second.X) + (first.Y - second.Y) * (first.Y - second.Y));

    private static Point NearestOnSegment(IPoint start, IPoint end, IPoint point)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;

        var lengthSquared = dx * dx + dy * dy;

        var t = lengthSquared == 0 ? 0 : ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / lengthSquared;

        t = Math.Max(0, Math.Min(1, t));

        return new Point(start.X + t * dx, start.Y + t * dy);
    }

    /// <summary>
    /// Deletes <paramref name="vertex"/> from its part. False when it is not a vertex of the geometry or the part
    /// cannot lose a point (a line of two points, a ring of three).
    /// </summary>
    private bool TryDeleteVertex(Locateable vertex)
    {
        if (!_addresses.TryGetValue(vertex, out var address))
            return false;

        var points = address.Part.Geometry.Points;

        var minimumPoints = _webMercatorGeometry.IsRingBase() ? 3 : 2;

        if (points is null || points.Count <= minimumPoints || address.Index >= points.Count)
            return false;

        var part = address.Part;

        var index = address.Index;

        // by its place in the part: another vertex at the same coordinates is another point; in place, as an insert
        points.RemoveAt(index);

        part.Vertices.RemoveAt(index);

        _addresses.Remove(vertex);

        Readdress(part, index);

        _primaryVerticesLayer.Items.Remove(vertex);

        _primaryVerticesLabelLayer.Remove(vertex.Id);

        vertex.Detach();

        if (part.Figure < _pathGeometry.Figures.Count)
        {
            var figure = _pathGeometry.Figures[part.Figure];

            // the first point is the start of the figure: the next point takes its place
            if (index == 0)
            {
                if (figure.Segments.Count > 0 && figure.Segments[0] is LineSegment first)
                {
                    figure.StartPoint = first.Point;

                    figure.Segments.RemoveAt(0);
                }
            }
            else if (index - 1 < figure.Segments.Count)
            {
                figure.Segments.RemoveAt(index - 1);
            }
        }

        AfterStructureChangeInPlace();

        return true;
    }

    /// <summary>Puts the point of the screen path that stands for the vertex at <paramref name="index"/> where the vertex is now.</summary>
    private void UpdatePathPoint(EditedPart part, int index, Locateable vertex)
    {
        if (part.Figure >= _pathGeometry.Figures.Count)
            return;

        var figure = _pathGeometry.Figures[part.Figure];

        var screenPoint = ToScreen(new WpfPoint(vertex.X, vertex.Y));

        // the first point of a part is the start of its figure, every other one the end of a segment
        if (index == 0)
        {
            figure.StartPoint = screenPoint;
        }
        else if (index - 1 < figure.Segments.Count && figure.Segments[index - 1] is LineSegment segment)
        {
            segment.Point = screenPoint;
        }
    }

    //probably this method can be better
    private Locateable? AddVertex(Point webMercatorPoint, Geometry geometry, RecursiveCollection<Locateable> primaryCollection)
    {
        if (geometry.Points != null)
        {
            //var point = this.ToScreen(webMercatorPoint.AsWpfPoint());

            var locateable = ToPrimaryLocateable(webMercatorPoint);

            //geometry.Points.Count > 0, is to see if it is not going to add first point of a new part
            if (geometry.Points.Count > 0 && geometry.Points.Last().AreExactlyTheSame(webMercatorPoint) == true)
                return null;

            geometry.InsertLastPoint(webMercatorPoint);

            primaryCollection.Values.Add(locateable);

            if (_parts.FirstOrDefault(p => ReferenceEquals(p.Geometry, geometry)) is { } part)
                _addresses[locateable] = (part, geometry.Points.Count - 1);

            InvalidateMeasures();

            //if (!MassEdit)
            //{
            _primaryVerticesLayer.Items.Add(locateable);
            //}

            // Update path geometry to reflect the new point immediately
            MakePathGeometry();

            if (geometry.Points.Count > 1 && Options.IsEdgeLabelVisible)
            {
                _edgeLabelLayer.Items.Add(ToEdgeLengthLocatable(geometry.Points[geometry.Points.Count - 2], webMercatorPoint));

                // added outside UpdateEdgeLabels: the next vertex move builds the labels again
                _edgeLabelsInSync = false;
            }

            // Incremental add bypasses ReconstructLocateables, so notify here.
            OnGeometryChanged?.Invoke();

            return locateable;
        }
        else
        {
            return AddVertex(webMercatorPoint, geometry.Geometries.Last(), primaryCollection.Collections.Last());
        }
    }

    private Locateable ToEdgeLengthLocatable(Point first, Point second)
    {
        var edge = new LineSegment<Point>(first, second);

        var label = new EdgeLabel { X = edge.Middle.X, Y = edge.Middle.Y, Text = EdgeLengthLabel(first, second) };

        // made when the label goes on the map, with the text it has then
        label.CreateElement = () => new RectangleLabelMarker(label.Text);

        return label;
    }

    /// <summary>The label of an edge: its text is kept here, so it can change before the label's control is made.</summary>
    private sealed class EdgeLabel : Locateable
    {
        public EdgeLabel() : base(AnchorFunctionHandlers.BottomCenter)
        {
        }

        public string Text = string.Empty;
    }

    private static string EdgeLengthLabel(Point first, Point second)
        => SpatialUtility.GetLengthLabel(new LineSegment<Point>(first, second), p => MapProjects.WebMercatorToGeodeticWgs84(p));

    #endregion


    #region Measures

    // The measure properties below are read together after every change (the measure panel binds to them). The
    // geometry is transformed once per change for all of them, not once per property.
    private Geometry? _wgs84ForMeasures;

    private Geometry? _utmForMeasures;

    private void InvalidateMeasures()
    {
        _wgs84ForMeasures = null;

        _utmForMeasures = null;
    }

    private Geometry Wgs84ForMeasures() => _wgs84ForMeasures ??= GetGeodeticWgs84Geometry();

    private Geometry UtmForMeasures() => _utmForMeasures ??= ToUtm(Wgs84ForMeasures());


    public double MeasureValue => SpatialUtility.GetEllipsoidMeasure(_webMercatorGeometry, MapProjects.WebMercatorToGeodeticWgs84);

    public string MeasureLabel => SpatialUtility.GetEllipsoidMeasureLabel(_webMercatorGeometry, MapProjects.WebMercatorToGeodeticWgs84);

    public string AreaLabel => UnitHelper.GetAreaLabel(SpatialUtility.GetEllipsoidalArea(_webMercatorGeometry, MapProjects.WebMercatorToGeodeticWgs84));

    public string LengthLabel => UnitHelper.GetLengthLabel(Wgs84ForMeasures().GetEllipsoidalLength(/*MapProjects.WebMercatorToGeodeticWgs84*/));


    public double ScaleFactor => MapProjects.CalculateUTMScaleFactor(Wgs84ForMeasures().GetCentroidPlus().AsPoint());


    public double EuclideanLengthInUtm => UtmForMeasures().GetEuclideanLength();

    public double EuclideanLengthInUtm_Refined => EuclideanLengthInUtm * (1.0 / ScaleFactor) * (1.0 + Height / WebMercatorUtility.EarthRadius);

    public double SphericalLength => Wgs84ForMeasures().GetSphericalLength();

    public double EllipsoidalLength => Wgs84ForMeasures().GetEllipsoidalLength();

    public double GroundLength => EllipsoidalLength * (1.0 + Height / WebMercatorUtility.EarthRadius);


    public double EuclideanArea => UtmForMeasures().EuclideanArea;

    public double EllipsoidalArea => SpatialUtility.GetEllipsoidalArea(Wgs84ForMeasures());

    public double AuthalicSphereArea => SpatialUtility.GetAreaOnAuthalicSphere(Wgs84ForMeasures());

    public double KarneyArea => SpatialUtility.GetKarneyArea(Wgs84ForMeasures());

    //public double GroundArea => EllipsoidalArea * (1.0 + 2 * Height / WebMercatorUtility.EarthRadius);
    public double GroundArea => SpatialUtility.GetGroundArea(Wgs84ForMeasures(), Height);

    #endregion


    #region Public Methods

    public Geometry GetGeodeticWgs84Geometry() => _webMercatorGeometry.Transform(MapProjects.WebMercatorToGeodeticWgs84, SridHelper.GeodeticWGS84);

    public Geometry GetUtmGeometry() => ToUtm(GetGeodeticWgs84Geometry());

    private static Geometry ToUtm(Geometry geography)
    {
        var boundary = geography.GetBoundingBox();

        var zone = MapProjects.FindUtmZone(boundary.MiddleBottom.X);

        return geography.Transform(p => MapProjects.GeodeticToUTM(p, Ellipsoids.WGS84, zone, boundary.YMin > 0), SridHelper.GetUtmSrid(zone));
    }

    public bool HasAnyPoint() => _webMercatorGeometry != null && _webMercatorGeometry.HasAnyPoint();

    public Path GetPath(Transform transform)
    {
        _toScreen = transform;

        MakePathGeometry();

        return _feature;
    }

    public SpecialPointLayer GetVertices() => _primaryVerticesLayer;

    /// <summary>
    /// The edge labels and the label of the whole. They are kept between calls (the map asks for them on every pan
    /// and zoom), so a label the user dragged stays where it was put; they are built only when they are not up to date.
    /// </summary>
    public SpecialPointLayer GetEdgeLengths()
    {
        if (!_edgeLabelsInSync)
            UpdateEdgeLabels();

        return _edgeLabelLayer;
    }

    public SpecialPointLayer GetPrimaryVerticesLabels() => _primaryVerticesLabelLayer;

    /// <summary>
    /// The placed vertices of the part being drawn, in order (Web Mercator). The vertex that follows the mouse is not
    /// among them: it exists only in the screen path until it is clicked.
    /// </summary>
    public IReadOnlyList<Point> GetCurrentPartPoints()
        => _webMercatorGeometry?.GetLastPart() is { } points ? points : Array.Empty<Point>();

    /// <summary>
    /// The vertices around <paramref name="vertex"/> in its part, as points (Web Mercator): the one before it and
    /// the one before that, the one after it and the one after that. A ring goes round; a line stops at its ends.
    /// Null where there is none, and never the vertex itself.
    /// </summary>
    public (Point? BeforePrevious, Point? Previous, Point? Next, Point? AfterNext) GetVertexNeighbours(Locateable vertex)
    {
        var isRing = _webMercatorGeometry?.IsRingBase() == true;

        foreach (var part in GetPartsLocateables())
        {
            var index = part.IndexOf(vertex);

            if (index >= 0)
                return NeighboursOf(part.Select(l => new Point(l.X, l.Y)).ToList(), index, isRing);
        }

        return (null, null, null, null);
    }

    /// <summary>
    /// The points around the one at <paramref name="index"/> in <paramref name="points"/>: the one before it and the
    /// one before that, the one after it and the one after that. A ring goes round; a line stops at its ends. Null
    /// where there is none, and never the point itself.
    /// </summary>
    public static (Point? BeforePrevious, Point? Previous, Point? Next, Point? AfterNext) NeighboursOf(IReadOnlyList<Point> points, int index, bool isRing)
    {
        if (points is null || index < 0 || index >= points.Count)
            return (null, null, null, null);

        Point? At(int offset)
        {
            var i = index + offset;

            if (isRing)
                i = ((i % points.Count) + points.Count) % points.Count;

            // going round a small ring comes back to the point itself: not a neighbour
            if (i < 0 || i >= points.Count || i == index)
                return null;

            return new Point(points[i].X, points[i].Y);
        }

        return (At(-2), At(-1), At(1), At(2));
    }

    public Geometry GetLastGeometry()
    {
        if (_webMercatorGeometry.Type == GeometryType.MultiPolygon)
        {
            var rings = _webMercatorGeometry.Geometries?.SelectMany(g => g.Geometries).ToList();

            // should not fixOrientation is false to prevent early ring orientation change base on e.g. 3 points
            return Geometry<Point>.CreatePolygonOrMultiPolygon(rings, _webMercatorGeometry.Srid, fixOrientation: false);
        }

        return _webMercatorGeometry;
    }

    public Geometry GetFinalFixedGeometry()
    {
        var result = GetLastGeometry();

        if (result.IsRingBase())
        {
            result.FixPolygonRingOrientations();
        }

        return result;
    }

    public Locateable? AddVertex(Point webMercatorPoint)
    {
        return AddVertex(webMercatorPoint, _webMercatorGeometry, _vertices);
    }

    /// <summary>
    /// Adds a vertex to a specific part by part index
    /// </summary>
    /// <param name="webMercatorPoint">The point to add (in Web Mercator coordinates)</param>
    /// <param name="partIndex">The index of the part to add the vertex to</param>
    /// <returns>The newly created Locateable instance, or null if addition failed</returns>
    public Locateable? AddVertexToPart(Point webMercatorPoint, int? polygonIndex, int partIndex)
    {
        if (_webMercatorGeometry.Points != null)
        {
            return AddVertex(webMercatorPoint, _webMercatorGeometry, _vertices);
        }
        else if (polygonIndex >= 0)
        {
            return AddVertex(webMercatorPoint,
                            _webMercatorGeometry.Geometries[polygonIndex.Value].Geometries[partIndex],
                            _vertices.Collections[polygonIndex.Value].Collections[partIndex]);
        }
        else
        {
            return AddVertex(webMercatorPoint, _webMercatorGeometry.Geometries[partIndex], _vertices.Collections[partIndex]);
        }


        //if (_webMercatorGeometry == null)
        //    return null;

        //// Single-part geometry
        //if (_webMercatorGeometry.Points != null)
        //{
        //    if (partIndex != 0)
        //        return null;

        //    // Check if point already exists (avoid duplicates)
        //    if (_webMercatorGeometry.Points.Count > 0)
        //    {
        //        if (_webMercatorGeometry.Points.Last().AreExactlyTheSame(webMercatorPoint))
        //            return null;
        //    }

        //    // Directly add the point to the geometry
        //    _webMercatorGeometry.InsertLastPoint(webMercatorPoint);

        //}

        //// Multi-part geometry
        //if (_webMercatorGeometry.Geometries != null)
        //{
        //    if (partIndex < 0 || partIndex >= _webMercatorGeometry.Geometries.Count)
        //        return null;

        //    // Get the target part geometry
        //    var targetPart = _webMercatorGeometry.Type == GeometryType.MultiPolygon ?
        //                        _webMercatorGeometry?.Geometries[polygonIndex ?? 0]?.Geometries?[partIndex] :
        //                        _webMercatorGeometry.Geometries[partIndex];

        //    if (targetPart == null)
        //        return null;

        //    // Check if point already exists (avoid duplicates)
        //    if (targetPart.Points != null && targetPart.Points.Count > 0)
        //    {
        //        if (targetPart.Points.Last().AreExactlyTheSame(webMercatorPoint))
        //            return null;
        //    }

        //    // Directly add the point to the target part's geometry
        //    targetPart.InsertLastPoint(webMercatorPoint);

        //}

        //// Update path geometry
        //MakePathGeometry();

        //// Reconstruct locateables to sync with the updated geometry
        //// This ensures LocateablesReconstructed event fires and ViewModel gets updated
        //ReconstructLocateables();

        //// Find and return the newly created Locateable
        //var locateables = GetLocateablesForPart(polygonIndex, partIndex);

        //if (locateables.Count > 0)
        //{
        //    // Return the last point (the one we just added)
        //    return locateables[locateables.Count - 1];
        //}

        //return null;
    }

    public void AddSemiVertex(Point webMercatorPoint)
    {
        var point = ToScreen(webMercatorPoint.AsWpfPoint());

        _pathGeometry.Figures.Last().Segments.Add(new LineSegment(new WpfPoint(point.X, point.Y), true));
    }

    public void UpdateLastSemiVertexLocation(Point newMercatorPoint)
    {
        if (_pathGeometry.Figures?.Last()?.Segments?.Count() < 1)
            AddSemiVertex(newMercatorPoint);

        if (_pathGeometry.Figures.Last().Segments.Count < _webMercatorGeometry.GetLastPart().Count)
        {
            AddSemiVertex(newMercatorPoint);
        }

        var newPoint = ToScreen(newMercatorPoint.AsWpfPoint());

        //var lastSegment = ((LineSegment)_pathGeometry.Figures.Last().Segments.Last()).Point = new WpfPoint(newPoint.X, newPoint.Y);
        ((LineSegment)_pathGeometry.Figures.Last().Segments.Last()).Point = new WpfPoint(newPoint.X, newPoint.Y);
    }

    public void FinishEditing()
    {
        RemoveMapOptions();

        _webMercatorGeometry.ClearEmptyGeometries();

        RequestFinishEditing?.Invoke(_webMercatorGeometry);
    }

    public void CancelEditing()
    {
        RemoveMapOptions();

        _edgeLabelLayer.Items.Clear();

        RequestCancelEditing?.Invoke(this);
    }

    private void GoToPreviousPoint() => _primaryVerticesLayer.SelectPreviousLocatable();

    private void GoToNextPoint() => _primaryVerticesLayer.SelectNextLocatable();

    public void TryDeleteCurrentPoint()
    {
        var locateable = _primaryVerticesLayer.FindSelectedLocatable();

        if (locateable == null)
            return;

        var index = _primaryVerticesLayer.FindSelectedLocatableIndex();

        // the selected vertex itself, in whatever part it is; when nothing is deleted (a line of two points, a
        // ring of three) the selection stays where it is
        if (!TryDeleteVertex(locateable))
            return;

        SelectPoint(Math.Max(0, index - 1));
    }

    private void ZoomToCurrentPoint()
    {
        var currentPoint = _primaryVerticesLayer.FindSelectedLocatable();

        if (currentPoint == null)
            return;

        RequestZoomToPoint?.Invoke(new Point(currentPoint.X, currentPoint.Y));
    }

    private void PanToCurrentPoint()
    {
        var currentPoint = _primaryVerticesLayer.FindSelectedLocatable();

        if (currentPoint == null)
            return;

        RequestPanToPoint?.Invoke(new Point(currentPoint.X, currentPoint.Y));
    }

    private void CopyCurrentPointCoordinateToClipboard(CoordinateDisplayMode mode)
    {
        var currentPoint = _primaryVerticesLayer.FindSelectedLocatable();

        if (currentPoint == null)
            return;

        Point point = new(currentPoint.X, currentPoint.Y);

        var mapSettings = RequestGetMapSettings?.Invoke() ?? MapSettings.Default;

        var options = CopyCoordinateOptions.Create(mapSettings.Clipboard_LatLongPrecision, mapSettings.Clipboard_XyPrecision);

        ClipboardHelper.CopyToClipboard(point, mode, options, mapSettings.Clipboard_IsLatitudeFirst/*null, null, null, null*/);

        //var format = CoordinateHelper.Format(point, mode, thousandSeparator: false, null, null, null, null);

        //if (mode == CoordinateDisplayMode.GeodeticDms || mode == CoordinateDisplayMode.GeodeticDecimal)
        //{
        //    Clipboard.SetDataObject($"{format.y};{format.x}");
        //}
        //else
        //{
        //    Clipboard.SetDataObject($"{format.x};{format.y}");
        //}

        //switch (spatialReferenceType)
        //{
        //    case CoordinateDisplayMode.UTM:
        //        var geodetic = MapProjects.WebMercatorToGeodeticWgs84(point);
        //        point = MapProjects.GeodeticToUTM(geodetic, geodetic.Y > 0);
        //        Clipboard.SetDataObject($"{point.X:#.##};{point.Y:#.##}");
        //        break;

        //    case CoordinateDisplayMode.WebMercator:
        //        Clipboard.SetDataObject($"{point.X:#.##};{point.Y:#.##}");
        //        break;

        //    case CoordinateDisplayMode.Geodetic:
        //    case CoordinateDisplayMode.None:
        //        point = MapProjects.WebMercatorToGeodeticWgs84(point);
        //        Clipboard.SetDataObject($"{point.Y:#.#####};{point.X:#.#####}");
        //        break;
        //    case CoordinateDisplayMode.AlbersEqualAreaConic:
        //    case CoordinateDisplayMode.CylindricalEqualArea:
        //    case CoordinateDisplayMode.LambertConformalConic:
        //    case CoordinateDisplayMode.Mercator:
        //    case CoordinateDisplayMode.TransverseMercator:
        //    default:
        //        throw new NotImplementedException("EditableFeatureLayer > CopyCurrentPointCoordinateToClipboard > unknown srs");
        //}

        //var geodetic = MapProjects.WebMercatorToGeodeticWgs84(new Point(currentPoint.X, currentPoint.Y));

        //Clipboard.SetDataObject($"{geodetic.X.ToString("n4")},{geodetic.Y.ToString("n4")}");

        //Clipboard.SetDataObject($"{point.X:n4};{point.Y:n4}");
    }

    private void DeleteCurrentPart()
    {
        var currentPoint = _primaryVerticesLayer.FindSelectedLocatable();

        if (currentPoint == null)
            return;

        _webMercatorGeometry.TryRemoveEntireRingOrLineString(currentPoint.X, currentPoint.Y);

        ReconstructLocateables();

        RequestRefresh?.Invoke(this);
    }

    private void ZoomToCurrentPart()
    {
        var currentPoint = _primaryVerticesLayer.FindSelectedLocatable();

        if (currentPoint == null)
            return;

        var part = _webMercatorGeometry.GetRingOrLineStringPassingPoint(currentPoint.X, currentPoint.Y);

        RequestZoomToGeometry?.Invoke(part);
    }

    private void PanToCurrentPart()
    {
        var currentPoint = _primaryVerticesLayer.FindSelectedLocatable();

        if (currentPoint == null)
            return;

        var part = _webMercatorGeometry.GetRingOrLineStringPassingPoint(currentPoint.X, currentPoint.Y);

        if (part == null)
            return;

        RequestPanToPoint?.Invoke(part.GetBoundingBox().Center);
    }

    // Flattens _vertices into the ordered list of parts (each part = its primary vertices).
    private List<List<Locateable>> GetPartsLocateables()
    {
        var result = new List<List<Locateable>>();

        if (_vertices != null)
            CollectParts(_vertices, result);

        return result;
    }

    private static void CollectParts(RecursiveCollection<Locateable> node, List<List<Locateable>> result)
    {
        if (node.Collections == null)
        {
            if (node.Values != null)
                result.Add(node.Values);
        }
        else
        {
            foreach (var child in node.Collections)
                CollectParts(child, result);
        }
    }

    private int FindCurrentPartIndex()
    {
        var selected = _primaryVerticesLayer.FindSelectedLocatable();

        if (selected == null)
            return -1;

        var parts = GetPartsLocateables();

        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i].Contains(selected))
                return i;
        }

        return -1;
    }

    private void GoToPart(int partIndex)
    {
        var parts = GetPartsLocateables();

        if (partIndex < 0 || partIndex >= parts.Count)
            return;

        var part = parts[partIndex];

        if (part.Count == 0)
            return;

        // Selecting the first vertex of the part makes it the "current part" for delete/zoom/measure.
        _primaryVerticesLayer.SelectLocatable(part[0]);
    }

    private void GoToPreviousPart()
    {
        var count = GetPartsLocateables().Count;

        if (count == 0)
            return;

        var current = FindCurrentPartIndex();

        if (current < 0)
            current = 0;

        GoToPart(current - 1 >= 0 ? current - 1 : count - 1);
    }

    private void GoToNextPart()
    {
        var count = GetPartsLocateables().Count;

        if (count == 0)
            return;

        var current = FindCurrentPartIndex();

        if (current < 0)
            current = 0;

        GoToPart(current + 1 < count ? current + 1 : 0);
    }

    public void FindNearestPoint(Point point)
    {
        var nearestPoint = _webMercatorGeometry.GetNearestPoint(point);

        _primaryVerticesLabelLayer.Items.Clear();

        _primaryVerticesLabelLayer.Items.Add(ToPrimaryLocateable(nearestPoint));
    }

    public void SelectPoint(int index)
    {
        _primaryVerticesLayer.SelectLocatable(index);
    }

    /// <summary>
    /// Gets the Locateable objects for a specific part index
    /// </summary>
    public List<Locateable> GetLocateablesForPart(int? polygonIndex, int partIndex)
    {
        if (_vertices == null)
            return new List<Locateable>();

        // Single-part geometry
        if (_vertices.Values != null)
            return partIndex == 0 ? _vertices.Values.ToList() : new List<Locateable>();

        // Multi-polygon geometry
        if (polygonIndex != null && _vertices.Collections != null && polygonIndex >= 0 && polygonIndex < _vertices.Collections.Count)
        {
            var polygon = _vertices.Collections[polygonIndex.Value];

            if (polygon != null && partIndex >= 0 && partIndex < polygon.Collections?.Count)
            {
                return polygon.Collections[partIndex].GetFlattenCollection();
            }
        }

        // Multi-part geometry
        if (_vertices.Collections != null && partIndex >= 0 && partIndex < _vertices.Collections.Count)
        {
            return _vertices.Collections[partIndex].GetFlattenCollection();
        }

        return new List<Locateable>();
    }



    ///// <summary>
    ///// Inserts a vertex at the specified global index
    ///// </summary>
    ///// <param name="webMercatorPoint">The point to insert (in Web Mercator coordinates)</param>
    ///// <param name="globalIndex">The global index across all parts where the point should be inserted</param>
    ///// <returns>The newly created Locateable instance, or null if insertion failed</returns>
    //public Locateable? InsertVertexAt(Point webMercatorPoint, int globalIndex)
    //{
    //    if (_webMercatorGeometry == null)
    //        return null;

    //    // Single-part geometry
    //    if (_webMercatorGeometry.Points != null)
    //    {
    //        if (globalIndex < 0 || globalIndex > _webMercatorGeometry.Points.Count)
    //            return null;

    //        _webMercatorGeometry.InsertPoint(webMercatorPoint, globalIndex);
    //        ReconstructLocateables();

    //        // Find the newly inserted Locateable after reconstruction
    //        var locateables = GetLocateablesForPart(0);
    //        if (globalIndex >= 0 && globalIndex < locateables.Count)
    //        {
    //            return locateables[globalIndex];
    //        }
    //        return null;
    //    }

    //    // Multi-part geometry
    //    if (_webMercatorGeometry.Geometries != null)
    //    {
    //        // Find which part contains the global index
    //        int currentGlobalIndex = 0;
    //        for (int partIndex = 0; partIndex < _webMercatorGeometry.Geometries.Count; partIndex++)
    //        {
    //            var part = _webMercatorGeometry.Geometries[partIndex];
    //            int partPointCount = part.Points?.Count ?? 0;

    //            // Check if globalIndex falls within this part's range
    //            // Valid range: [currentGlobalIndex, currentGlobalIndex + partPointCount)
    //            // We use < instead of <= for the upper bound to ensure we don't match the start of the next part
    //            // Exception: if this is the last part, we can insert at the end (currentGlobalIndex + partPointCount)
    //            bool isLastPart = partIndex == _webMercatorGeometry.Geometries.Count - 1;
    //            bool isInRange = globalIndex >= currentGlobalIndex &&
    //                             (isLastPart ? globalIndex <= currentGlobalIndex + partPointCount
    //                                        : globalIndex < currentGlobalIndex + partPointCount);

    //            if (isInRange)
    //            {
    //                // Found the part - calculate local index
    //                int localIndex = globalIndex - currentGlobalIndex;

    //                // Validate local index: can insert at positions [0, partPointCount] (inclusive)
    //                // 0 = before first point, partPointCount = after last point
    //                if (part.Points != null && localIndex >= 0 && localIndex <= partPointCount)
    //                {
    //                    part.InsertPoint(webMercatorPoint, localIndex);
    //                    ReconstructLocateables();

    //                    // Find the newly inserted Locateable after reconstruction
    //                    var locateables = GetLocateablesForPart(partIndex);
    //                    if (localIndex >= 0 && localIndex < locateables.Count)
    //                    {
    //                        return locateables[localIndex];
    //                    }
    //                    return null;
    //                }
    //            }

    //            currentGlobalIndex += partPointCount;
    //        }
    //    }

    //    return null;
    //}

    /// <summary>
    /// Deletes a part (ring or line string) by its index
    /// </summary>
    /// <param name="partIndex">The index of the part to delete</param>
    /// <returns>True if the part was successfully deleted, false otherwise</returns>
    public bool TryDeletePartByIndex(int partIndex)
    {
        if (_webMercatorGeometry == null)
            return false;

        // Single-part geometry - cannot delete the only part
        if (_webMercatorGeometry.Points != null)
            return false;

        // Multi-part geometry
        if (_webMercatorGeometry.Geometries != null)
        {
            if (partIndex < 0 || partIndex >= _webMercatorGeometry.Geometries.Count)
                return false;

            var partToDelete = _webMercatorGeometry.Geometries[partIndex];

            if (_webMercatorGeometry.TryRemovePart(partToDelete))
            {
                MakePathGeometry();
                ReconstructLocateables();
                RequestRefresh?.Invoke(this);
                return true;
            }
        }

        return false;
    }

    #endregion


    #region Overrides

    public override Task<FeatureSet<Point>> GetFeatureSet(BoundingBox mapExtent, double mapScale)
    {
        var geometry = GetFinalFixedGeometry();
        if (geometry == null || geometry.IsNullOrEmpty())
            return Task.FromResult(FeatureSet<Point>.Empty);

        // Check if geometry intersects bounding box
        var extentGeometry = mapExtent.AsGeometry<Point>(SridHelper.WebMercator);

        if (!geometry.Intersects(extentGeometry))
            return Task.FromResult(FeatureSet<Point>.Empty);

        return Task.FromResult(FeatureSet<Point>.Create($"{nameof(EditableFeatureLayer)}-{LayerId}", [geometry.AsFeature()]));
        //return new List<Feature<Point>> { new Feature<Point>(geometry) };
    }

    #endregion


    #region Commands

    private RelayCommand? _finishEditingCommand;
    public RelayCommand FinishEditingCommand
    {
        get
        {
            if (_finishEditingCommand == null)
                _finishEditingCommand = new RelayCommand(param => FinishEditing());

            return _finishEditingCommand;
        }
    }


    private RelayCommand? _cancelEditingCommand;
    public RelayCommand CancelEditingCommand
    {
        get
        {
            if (_cancelEditingCommand == null)
                _cancelEditingCommand = new RelayCommand(param => RequestCancelEditing?.Invoke(this));

            return _cancelEditingCommand;
        }
    }


    private RelayCommand? _deleteCommand;
    public RelayCommand DeleteCommand
    {
        get
        {
            if (_deleteCommand == null)
                _deleteCommand = new RelayCommand(param =>
                {
                    CancelEditing();
                });

            return _deleteCommand;
        }
    }


    private RelayCommand? _cancelDrawingCommand;
    public RelayCommand CancelDrawingCommand
    {
        get
        {
            if (_cancelDrawingCommand == null)
                _cancelDrawingCommand = new RelayCommand(param => CancelDrawing());

            return _cancelDrawingCommand;
        }
    }


    private RelayCommand? _goToPreviousPointCommand;
    public RelayCommand GoToPreviousPointCommand
    {
        get
        {
            if (_goToPreviousPointCommand == null)
                _goToPreviousPointCommand = new RelayCommand(param => GoToPreviousPoint());

            return _goToPreviousPointCommand;
        }
    }


    private RelayCommand? _goToNextPointCommand;
    public RelayCommand GoToNextPointCommand
    {
        get
        {
            if (_goToNextPointCommand == null)
                _goToNextPointCommand = new RelayCommand(param => GoToNextPoint());

            return _goToNextPointCommand;
        }
    }


    private RelayCommand? _deleteCurrentPointCommand;
    public RelayCommand DeleteCurrentPointCommand
    {
        get
        {
            if (_deleteCurrentPointCommand == null)
                _deleteCurrentPointCommand = new RelayCommand(param => TryDeleteCurrentPoint());

            return _deleteCurrentPointCommand;
        }
    }


    private RelayCommand? _zoomToCurrentPointCommand;
    public RelayCommand ZoomToCurrentPointCommand
    {
        get
        {
            if (_zoomToCurrentPointCommand == null)
                _zoomToCurrentPointCommand = new RelayCommand(param => ZoomToCurrentPoint());

            return _zoomToCurrentPointCommand;
        }
    }


    private RelayCommand? _panToCurrentPointCommand;
    public RelayCommand PanToCurrentPointCommand
    {
        get
        {
            if (_panToCurrentPointCommand == null)
                _panToCurrentPointCommand = new RelayCommand(param => PanToCurrentPoint());

            return _panToCurrentPointCommand;
        }
    }


    private RelayCommand? _copyCurrentPointCommand;
    public RelayCommand CopyCurrentPointCommand
    {
        get
        {
            if (_copyCurrentPointCommand == null)
                _copyCurrentPointCommand = new RelayCommand(param => CopyCurrentPointCoordinateToClipboard((CoordinateDisplayMode)param));

            return _copyCurrentPointCommand;
        }
    }


    private RelayCommand? _deleteCurrentPartCommand;
    public RelayCommand DeleteCurrentPartCommand
    {
        get
        {
            if (_deleteCurrentPartCommand == null)
                _deleteCurrentPartCommand = new RelayCommand(param => DeleteCurrentPart(), _ => IsMultiPartGeometry);

            return _deleteCurrentPartCommand;
        }
    }


    private RelayCommand? _zoomToCurrentPartCommand;
    public RelayCommand ZoomToCurrentPartCommand
    {
        get
        {
            if (_zoomToCurrentPartCommand == null)
                _zoomToCurrentPartCommand = new RelayCommand(param => ZoomToCurrentPart());

            return _zoomToCurrentPartCommand;
        }
    }


    private RelayCommand? _panToCurrentPartCommand;
    public RelayCommand PanToCurrentPartCommand
    {
        get
        {
            if (_panToCurrentPartCommand == null)
                _panToCurrentPartCommand = new RelayCommand(param => PanToCurrentPart());

            return _panToCurrentPartCommand;
        }
    }


    private RelayCommand? _goToPreviousPartCommand;
    public RelayCommand GoToPreviousPartCommand
    {
        get
        {
            if (_goToPreviousPartCommand == null)
                _goToPreviousPartCommand = new RelayCommand(param => GoToPreviousPart(), _ => IsPreviousPartAvailable);

            return _goToPreviousPartCommand;
        }
    }


    private RelayCommand? _goToNextPartCommand;
    public RelayCommand GoToNextPartCommand
    {
        get
        {
            if (_goToNextPartCommand == null)
                _goToNextPartCommand = new RelayCommand(param => GoToNextPart(), _ => IsNextPartAvailable);

            return _goToNextPartCommand;
        }
    }



    private RelayCommand? _convertToDrawingItemCommand;
    public RelayCommand ConvertToDrawingItemCommand
    {
        get
        {
            if (_convertToDrawingItemCommand == null)
                _convertToDrawingItemCommand = new RelayCommand(param => RequestConvertToDrawingItem?.Invoke(_webMercatorGeometry));

            return _convertToDrawingItemCommand;
        }
    }




    private RelayCommand _showGeometryDetailsCommand;
    public RelayCommand ShowGeometryDetailsCommand
    {
        get
        {
            if (_showGeometryDetailsCommand == null)
            {
                _showGeometryDetailsCommand = new RelayCommand(param => RequestShowGeometryDetails?.Invoke(this));
            }
            return _showGeometryDetailsCommand;
        }
    }



    #endregion



    #region IDispose

    private bool _disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                this.RequestCancelDrawing = null;
                this.RequestCancelEditing = null;
                this.RequestChangeSymbology = null;
                this.RequestChangeVisibility = null;
                this.RequestClearSelectedLayer = null;
                this.RequestConvertToDrawingItem = null;
                this.RequestFinishEditing = null;
                this.RequestGetCoordinateDisplayMode = null;
                this.RequestGetMapSettings = null;
                this.RequestMoveLayerDown = null;
                this.RequestMoveLayerUp = null;
                this.RequestRefresh = null;
                this.RequestRemoveRightClickOptions = null;
                this.RequestRightClickOptions = null;
                this.RequestSaveChanges = null;
                this.RequestSelectedLocatableChanged = null;
                this.RequestShowGeometryDetails = null;
                this.RequestZoomToGeometry = null;
                this.RequestZoomToPoint = null;
                this.RequestPanToPoint = null;

                // Detach and release every locateable still held by the point layers so their
                // per-vertex handlers (wired in ToPrimaryLocateable) don't
                // outlive the layer. Detach must run *before* the items are cleared.
                foreach (var layer in new[] { _primaryVerticesLayer, _edgeLabelLayer, _primaryVerticesLabelLayer })
                {
                    if (layer?.Items is null)
                        continue;

                    foreach (var locateable in layer.Items)
                    {
                        locateable.Detach();
                    }

                    layer.Items.Clear();

                    // Release the closures captured by the renderer (MapViewer) and by this layer.
                    layer.HandleCollectionChanged = null;
                    layer.RequestSelectedLocatableChanged = null;
                }

                // Unsubscribe the path's right-click handler and drop the geometry it holds.
                if (_feature != null && _featureRightButtonUpHandler != null)
                {
                    _feature.MouseRightButtonUp -= _featureRightButtonUpHandler;
                }

                _featureRightButtonUpHandler = null;

                if (_feature != null)
                {
                    _feature.Data = null;
                }

                _pathGeometry?.Figures.Clear();

                // The shared/long-lived Options object would otherwise keep this layer alive.
                if (_options != null)
                {
                    _options.RequestHandleMeasureVisibilityChanged = null;
                }

                _vertices = null;

                _parts.Clear();

                _addresses.Clear();

                _totalLabel = null;

                this.LocateablesReconstructed = null;

                this.OnGeometryChanged = null;
            }

            // Dispose unmanaged resources here if any
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
