using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Jab.Common.ViewModels;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Models.DataStructure;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;

using WpfPoint = System.Windows.Point;
using Point = IRI.Maptor.Sta.Common.Primitives.Point;
using LineSegment = System.Windows.Media.LineSegment;
using Geometry = IRI.Maptor.Sta.Spatial.Primitives.Geometry<IRI.Maptor.Sta.Common.Primitives.Point>;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Sta.Spatial.Primitives;
using System.Threading.Tasks;
using IRI.Maptor.Sta.Common.Enums;

namespace IRI.Maptor.Jab.Common;

public class EditableFeatureLayer : SymbolizableLayer
{
    Transform _toScreen;

    Func<double, double> _screenToMap;

    private Geometry _webMercatorGeometry;

    private Path _feature;

    private PathGeometry _pathGeometry;

    private RecursiveCollection<Locateable> _vertices;

    private RecursiveCollection<Locateable> _midVertices;


    private SpecialPointLayer _primaryVerticesLayer;

    private SpecialPointLayer _midVerticesLayer;

    // Edge Length
    private SpecialPointLayer _edgeLabelLayer;

    // Vertext Coordinates 
    private SpecialPointLayer _primaryVerticesLabelLayer;


    public EditableFeatureLayerOptions Options { get; }

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


    #region Actions

    public Action<FrameworkElement, MouseButtonEventArgs, ILocateable>? RequestRightClickOptions;

    public Action? RequestRemoveRightClickOptions;

    public Action<EditableFeatureLayer>? RequestRefresh;

    public Action<Geometry>? RequestConvertToDrawingItem;

    public Action<EditableFeatureLayer>? RequestShowGeometryDetails;

    // todo: check if duplicate
    //public Action<EditableFeatureLayer>? RquestShowCoordinates;

    // drawing
    public event EventHandler? OnRequestFinishDrawing;

    public Action? RequestCancelDrawing;

    // editing
    public Action<Geometry>? RequestFinishEditing;

    public Action<EditableFeatureLayer>? RequestCancelEditing;

    public event EventHandler? OnRequestDeleteGeometry;

    public Action<Locateable?, int>? RequestSelectedLocatableChanged;

    // zoom
    public Action<Point>? RequestZoomToPoint;

    public Action<Geometry>? RequestZoomToGeometry;

    public event Action? LocateablesReconstructed;

    public Func<CoordinateDisplayMode> RequestGetCoordinateDisplayMode;

    #endregion


    /// <summary>
    /// For Polygons do not repeat first point in the last point
    /// </summary>
    /// <param name="name"></param>
    /// <param name="mercatorPoints"></param>
    /// <param name="isClosed"></param>
    public EditableFeatureLayer(
        string name,
        List<Point> mercatorPoints,
        Transform toScreen,
        Func<double, double> screenToMap,
        GeometryType type,
        EditableFeatureLayerOptions? options = null)
        : this(name, Geometry.Create(mercatorPoints, type, SridHelper.WebMercator), toScreen, screenToMap, options)
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
        EditableFeatureLayerOptions? options = null)
    {
        this.Options = options ?? EditableFeatureLayerOptions.CreateDefault();

        this.Options.RequestHandleIsEdgeLabelVisibleChanged = UpdateEdgeLables;

        this.LayerName = name;

        this._webMercatorGeometry = webMercatorGeometry;

        this.LayerId = Guid.NewGuid();

        this._toScreen = toScreen;

        this._screenToMap = screenToMap;

        VisibleRange = ScaleInterval.All;

        //this.VisualParameters = new VisualParameters(_mercatorGeometry.IsRingBase() ? _fill : null, _stroke, 3, 1);
        //this.VisualParameters = Options.Visual;
        this.SetSymbolizer(new SimpleSymbolizer(Options.Visual));

        this._feature = GetDefaultEditingPath();

        this._pathGeometry = new PathGeometry();

        MakePathGeometry();

        this._feature.Data = this._pathGeometry;

        //if 
        //{
        //    this._feature.MouseUp += (sender, e) => { this.RegisterMapOptionsForNewPath(e); };
        //}
        if (!Options.IsNewDrawing)
        {
            this._feature.MouseRightButtonDown += (sender, e) => { this.RegisterMapOptionsForEditPath(e); };
        }

        bool isMovable = !Options.IsNewDrawing;

        //var layerType = Options.IsNewDrawing ? LayerType.EditableItem : LayerType.MoveableItem | LayerType.EditableItem;
        var layerType = LayerType.EditableItem;

        this._primaryVerticesLayer = new SpecialPointLayer("#vert", new List<Locateable>(), 1, ScaleInterval.All, layerType) { AlwaysTop = true, IsMovable = isMovable };

        this._primaryVerticesLayer.RequestSelectedLocatableChanged = (l, i) => this.RequestSelectedLocatableChanged?.Invoke(l, i);

        this._midVerticesLayer = new SpecialPointLayer("#int. vert", new List<Locateable>(), .7, ScaleInterval.All, layerType) { AlwaysTop = true, IsMovable = isMovable };

        this._edgeLabelLayer = new SpecialPointLayer("#edge length", new List<Locateable>(), .9, ScaleInterval.All, layerType) { AlwaysTop = false, IsMovable = isMovable };

        this._primaryVerticesLabelLayer = new SpecialPointLayer("#vert length", new List<Locateable>(), .9, ScaleInterval.All, layerType) { AlwaysTop = false, IsMovable = isMovable };

        ReconstructLocateables();

        this._primaryVerticesLayer.SelectLocatable(0);

        if (Options.IsNewDrawing)
        {
            //add virtual vertex which show last point
            this.AddSemiVertex((Point)(webMercatorGeometry.Points == null ? webMercatorGeometry.Geometries.Last().Points.Last() : webMercatorGeometry.Points.Last()));
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
        this._webMercatorGeometry.Geometries.Last().InsertLastPoint(webMercatorPoint);

        MakePathGeometry();

        ReconstructLocateables();
    }

    // add an empty new part
    internal bool TryFinishDrawingPart()
    {
        var result = this._webMercatorGeometry.TryAddNewPart();

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
        var result = this._webMercatorGeometry.TryAddNewPart();

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

        if (this._webMercatorGeometry.Type == GeometryType.Polygon)
        {
            result = this._webMercatorGeometry.TryAddNewRing();
        }
        else if (this._webMercatorGeometry.Type == GeometryType.MultiPolygon)
        {
            result = this._webMercatorGeometry.Geometries![currentPolygonIndex!.Value].TryAddNewRing();
        }

        if (result)
        {
            MakePathGeometry();
            ReconstructLocateables();
        }

        return true;
    }

    internal void CancelDrawing()
    {
        this.RequestCancelDrawing?.Invoke();
    }

    #endregion


    #region Private Methods

    private Path GetDefaultEditingPath()
    {
        var result = new Path()
        {
            Stroke = this.Options.Visual.Stroke,
            StrokeThickness = this.Options.Visual.StrokeThickness,
            StrokeDashArray = Options.Visual.DashStyle?.Dashes,
            Opacity = this.Options.Visual.Opacity,
            StrokeLineJoin = PenLineJoin.Round,
        };

        if (_webMercatorGeometry.IsRingBase())
        {
            result.Fill = this.Options.Visual.Fill;
        }

        return result;
    }

    private void ReconstructLocateables()
    {
        this._vertices = new RecursiveCollection<Locateable>();

        this._midVertices = new RecursiveCollection<Locateable>();

        MakeLocateables(this._webMercatorGeometry, _vertices, _midVertices);

        this._primaryVerticesLayer.Items.Clear();

        this._midVerticesLayer.Items.Clear();

        this._primaryVerticesLabelLayer.Items.Clear();

        var primary = _vertices.GetFlattenCollection();

        var mid = _midVertices.GetFlattenCollection();

        foreach (var item in mid)
        {
            _midVerticesLayer.Items.Add(item);
        }

        foreach (var item in primary)
        {
            _primaryVerticesLayer.Items.Add(item);
        }

        UpdateEdgeLables();

        // Notify that Locateables have been reconstructed
        LocateablesReconstructed?.Invoke();
    }


    //1397.08.26
    //why this methods calls multiple time. enable break point to see why
    private void UpdateEdgeLables()
    {
        this._edgeLabelLayer.Items.Clear();

        if (!_webMercatorGeometry.IsValid())
            return;

        if (Options.IsEdgeLabelVisible)
        {
            var edges = _webMercatorGeometry.GetLineSegments().Select(i => ToEdgeLengthLocatable(i.Start, i.End));

            foreach (var item in edges)
            {
                _edgeLabelLayer.Items.Add(item);
            }
        }

        if (Options.IsMeasureVisible)
        {
            var point = this._webMercatorGeometry?.GetMeanOrLastPoint();

            if (point == null)
                return;

            var element = new Views.MapMarkers.RectangleLabelMarker(MeasureLabel);

            //do not show length/area when geometry has just one/two point or new part has just one/two point
            if (double.IsNaN(MeasureValue))
                return;

            element.TooltipValue = MeasureValue.ToInvariantString();

            var offset = _screenToMap(20);

            _edgeLabelLayer.Items.Add(new Locateable(AncherFunctionHandlers.BottomCenter)
            {
                Element = element,
                X = point.X + offset,
                Y = point.Y + offset
            });
        }

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
        var locatables = this._primaryVerticesLabelLayer.Get(locatable.Id);

        foreach (var item in locatables)
        {
            //(item.Element as Views.MapMarkers.CoordinateMarker).WebMercatorLocation = new Point(locatable.X, locatable.Y);
            (item.Element as Views.MapMarkers.CoordinateMarker)!.WebMercatorLocation = locatable;

            item.X = locatable.X;
            item.Y = locatable.Y;
        }
    }

    private void MakeLocateables(Geometry geometry, RecursiveCollection<Locateable> primaryCollection, RecursiveCollection<Locateable> midCollection)
    {
        if (geometry.Points != null)
        {
            primaryCollection.Values = new List<Locateable>();

            midCollection.Values = new List<Locateable>();

            for (int i = 0; i < geometry.Points.Count; i++)
            {
                var locateable = ToPrimaryLocateable(geometry.Points[i]);

                primaryCollection.Values.Add(locateable);

                //do not make mid points in drawing mode
                if (Options.IsNewDrawing)
                    continue;

                if (geometry.Type == GeometryType.Point || geometry.Type == GeometryType.MultiPoint)
                    continue;

                if (i == geometry.Points.Count - 1)
                {
                    if (_webMercatorGeometry.IsRingBase())
                    {
                        midCollection.Values.Add(ToSecondaryLocateable(geometry.Points[i], geometry.Points[0]));
                    }
                }
                else
                {
                    midCollection.Values.Add(ToSecondaryLocateable(geometry.Points[i], geometry.Points[i + 1]));
                }
            }
        }
        else
        {
            primaryCollection.Collections = new List<RecursiveCollection<Locateable>>();

            midCollection.Collections = new List<RecursiveCollection<Locateable>>();

            foreach (var item in geometry.Geometries)
            {
                RecursiveCollection<Locateable> subPrimaryCollection = new RecursiveCollection<Locateable>();

                RecursiveCollection<Locateable> subMidCollection = new RecursiveCollection<Locateable>();

                MakeLocateables(item, subPrimaryCollection, subMidCollection);

                primaryCollection.Collections.Add(subPrimaryCollection);

                midCollection.Collections.Add(subMidCollection);
            }
        }
    }

    internal void ChangeCurrentEditingPoint(Point point)
    {
        //find selected locatable

        var currentEditingPoint = this._primaryVerticesLayer.FindSelectedLocatable();

        if (currentEditingPoint == null)
            return;

        currentEditingPoint.X = point.X;

        currentEditingPoint.Y = point.Y;
    }

    private void MakePathGeometry()
    {
        _pathGeometry.Figures.Clear();

        MakePathGeometry(this._webMercatorGeometry);
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

            this._pathGeometry.Figures.Add(pathFigure);
        }
        else if (geometry.Geometries != null)
        {
            foreach (var g in geometry.Geometries)
            {
                MakePathGeometry(g);
            }
        }
        else
        {
            return;
        }
    }

    private Locateable ToPrimaryLocateable(IPoint point)
    {
        var webMercatorPoint = point;

        var element = Options.MakePrimaryVertex();

        var locateable = new Locateable(AncherFunctionHandlers.CenterCenter)
        {
            Element = element,
            X = webMercatorPoint.X,
            Y = webMercatorPoint.Y,
            Id = Guid.NewGuid(),
            CanBeUsedAsEditingPoint = true
        };

        locateable.RequestChangeIsSelected = (isSelected) =>
        {
            ((IMapMarker)locateable.Element).IsSelected = isSelected;
        };

        if (Options.IsNewDrawing)
        {
            //Finish Drawing if click on any point
            locateable.Element.MouseDown += (sender, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    this.OnRequestFinishDrawing?.Invoke(this, EventArgs.Empty);

                    e.Handled = true;
                }
            };
        }
        else
        {
            element.MouseRightButtonDown += (sender, e) =>
            {
                //locateable.IsSelected = true;

                this._primaryVerticesLayer.SelectLocatable(locateable.Element);

                RegisterMapOptionsForVertices(e, webMercatorPoint, locateable);
            };
        }

        locateable.OnPositionChanged += (sender, e) =>
        {
            UpdateLineSegment(webMercatorPoint as Point, new Point(locateable.X, locateable.Y));

            webMercatorPoint.X = locateable.X;
            webMercatorPoint.Y = locateable.Y;

            UpdateEdgeLables();

            UpdateCoordinate(locateable);
        };

        return locateable;
    }

    private Locateable ToSecondaryLocateable(IPoint first, IPoint second)
    {
        var webMercatorPoint = new Point((first.X + second.X) / 2.0, (first.Y + second.Y) / 2.0);

        //var element = new View.MapMarkers.Circle(.6);
        var element = Options.MakeSecondaryVertex();

        var locateable = new Locateable(AncherFunctionHandlers.CenterCenter) { Element = element, X = webMercatorPoint.X, Y = webMercatorPoint.Y };

        element.MouseLeftButtonDown += (sender, e) =>
        {
            webMercatorPoint.X = locateable.X;

            webMercatorPoint.Y = locateable.Y;

            if (!TryInsertPoint(webMercatorPoint, first, second, this._webMercatorGeometry))
                throw new NotImplementedException();

            RequestRefresh?.Invoke(this);
        };

        return locateable;
    }

    private void RegisterMapOptionsForVertices(MouseButtonEventArgs e, IPoint point, Locateable locateable)
    {
        var presenter = new MapOptionsViewModel(
                //rightToolTip: _copy,
                //leftToolTip: _displayCoordinates,
                //middleToolTip: _delete,
                rightToolTip: IRI.Maptor.Jab.Common.Properties.Resources.mapPanel_currentPoint_copyCoordinate,
                leftToolTip: IRI.Maptor.Jab.Common.Properties.Resources.mapPanel_currentPoint_displayCoordinate,
                middleToolTip: IRI.Maptor.Jab.Common.Properties.Resources.mapPanel_currentPoint_delete,

                rightSymbol: MahApps.Metro.IconPacks.PackIconModernKind.PageCopy,
                leftSymbol: MahApps.Metro.IconPacks.PackIconModernKind.AxisXy,
                middleSymbol: MahApps.Metro.IconPacks.PackIconModernKind.Delete);

        presenter.RightCommandAction = i =>
        {
            //var geodetic = MapProjects.WebMercatorToGeodeticWgs84(presenter.Location);

            //Clipboard.SetDataObject($"{geodetic.X.ToString("n4")},{geodetic.Y.ToString("n4")}");
            var mode = RequestGetCoordinateDisplayMode?.Invoke() ?? CoordinateDisplayMode.GeodeticDecimal;

            ClipboardHelper.CopyToClipboard(new Point(point.X, point.Y), mode, null, null, null, null);

            //var format = CoordinateHelper.Format(/*presenter.Location*/new Point(point.X, point.Y), mode, thousandSeparator: false, null, null, null, null);

            //if (mode == CoordinateDisplayMode.GeodeticDms || mode == CoordinateDisplayMode.GeodeticDecimal)
            //{
            //    Clipboard.SetDataObject($"{format.y};{format.x}");
            //}
            //else
            //{
            //    Clipboard.SetDataObject($"{format.x};{format.y}");
            //}


            this.RemoveMapOptions();
        };

        presenter.LeftCommandAction = i =>
        {
            //RequestFinishEditing?.Invoke(this._mercatorGeometry);
            if (_primaryVerticesLabelLayer.Items.Any(l => l.Id == locateable.Id))
            {
                _primaryVerticesLabelLayer.Remove(locateable.Id);
            }
            else
            {
                var displayMode = RequestGetCoordinateDisplayMode?.Invoke();

                var element = new Views.MapMarkers.CoordinateMarker(locateable, displayMode);

                var auxLocateable = new Locateable(AncherFunctionHandlers.CenterLeft) { Element = element, X = point.X, Y = point.Y, Id = locateable.Id };

                _primaryVerticesLabelLayer.Items.Add(auxLocateable);
            }

            this.RemoveMapOptions();
        };

        presenter.MiddleCommandAction = i =>
        {
            this._primaryVerticesLabelLayer.Remove(locateable.Id);

            TryDeleteVertex(point, this._webMercatorGeometry, _webMercatorGeometry.Type == GeometryType.Polygon);

            this.RequestRefresh?.Invoke(this);

            this.RemoveMapOptions();
        };

        RequestRightClickOptions?.Invoke(new Views.MapOptions.MapThreeOptions(false), e, presenter);

    }

    private void RegisterMapOptionsForEditPath(MouseButtonEventArgs e)
    {
        var presenter = new MapOptionsViewModel(
            //leftToolTip: _cancel,
            //rightToolTip: _finish,
            //middleToolTip: _delete,
            leftToolTip: IRI.Maptor.Jab.Common.Properties.Resources.mapPanel_edit_cancel,
            rightToolTip: IRI.Maptor.Jab.Common.Properties.Resources.mapPanel_edit_finish,
            middleToolTip: IRI.Maptor.Jab.Common.Properties.Resources.mapPanel_edit_delete,

            leftSymbol: MahApps.Metro.IconPacks.PackIconModernKind.Close,
            rightSymbol: MahApps.Metro.IconPacks.PackIconModernKind.Check,
            middleSymbol: MahApps.Metro.IconPacks.PackIconModernKind.Delete);

        presenter.RightCommandAction = i =>
        {
            //RequestFinishEditing?.Invoke(this._webMercatorGeometry);
            this.FinishEditing();

            this.RemoveMapOptions();
        };

        presenter.LeftCommandAction = i =>
        {
            RequestCancelEditing?.Invoke(this);

            this.RemoveMapOptions();
        };

        presenter.MiddleCommandAction = i =>
        {
            this.RequestCancelEditing?.Invoke(this);

            this.OnRequestDeleteGeometry?.Invoke(this, EventArgs.Empty);

            this.RemoveMapOptions();
        };

        RequestRightClickOptions?.Invoke(new Views.MapOptions.MapThreeOptions(false), e, presenter);

    }

    private void RemoveMapOptions()
    {
        this.RequestRemoveRightClickOptions?.Invoke();
    }

    private int GetLeftMidPointIndex(int primaryIndex, int length)
    {
        int left;

        if (primaryIndex == 0 && _webMercatorGeometry.IsRingBase())
        {
            left = length - 1;
        }
        else if (primaryIndex == 0 && !_webMercatorGeometry.IsRingBase())
        {
            left = int.MinValue;
        }
        else
        {
            left = primaryIndex - 1;
        }

        return left;
    }

    private int GetRightMidPointIndex(int primaryIndex, int length)
    {
        int right;

        if (primaryIndex == length && !_webMercatorGeometry.IsRingBase())
        {
            right = int.MinValue;
        }
        else
        {
            right = primaryIndex;
        }

        return right;
    }

    private bool TryInsertPoint(Point webMercatorPoint, IPoint startLineSegment, IPoint endLineSegment, Geometry geometry)
    {
        var point = this.ToScreen(webMercatorPoint.AsWpfPoint());

        if (geometry.Points != null)
        {
            for (int i = 0; i < geometry.Points.Count; i++)
            {
                if (geometry.Points[i] == startLineSegment)
                {

                    geometry.InsertPoint(webMercatorPoint, i + 1);

                    ReconstructLocateables();

                    return true;
                }
            }
        }
        else
        {
            for (int g = 0; g < geometry.Geometries.Count; g++)
            {
                if (TryInsertPoint(webMercatorPoint, startLineSegment, endLineSegment, geometry.Geometries[g]))
                    return true;

            }
        }

        return false;
    }

    private bool TryDeleteVertex(IPoint point, Geometry geometry, bool isRing)
    {
        if (geometry.Points != null)
        {
            var minimumPoints = isRing ? 3 : 2;

            if (geometry.Points.Count() <= minimumPoints)
                return false;

            for (int i = 0; i < geometry.Points.Count; i++)
            {
                if (geometry.Points[i] == point)
                {
                    geometry.Remove(geometry.Points[i]);

                    MakePathGeometry();

                    ReconstructLocateables();

                    return true;
                }
            }
        }
        else
        {
            for (int g = 0; g < geometry.Geometries.Count; g++)
            {
                if (TryDeleteVertex(point, geometry.Geometries[g], geometry.Type == GeometryType.Polygon))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryDeleteVertex(double x, double y, Geometry geometry, bool isRing)
    {
        if (geometry.Points != null)
        {
            var minimumPoints = isRing ? 3 : 2;

            if (geometry.Points.Count() <= minimumPoints)
                return false;

            geometry.Remove(x, y);

            MakePathGeometry();

            ReconstructLocateables();

            return true;

        }
        else
        {
            for (int g = 0; g < geometry.Geometries.Count; g++)
            {
                if (TryDeleteVertex(x, y, geometry.Geometries[g], geometry.Type == GeometryType.Polygon))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void UpdateLineSegment(Point point, Point newValue)
    {
        var oldPoint = ToScreen(point.AsWpfPoint());

        var screenPoint = ToScreen(newValue.AsWpfPoint());

        for (int f = this._pathGeometry.Figures.Count - 1; f >= 0; f--)
        {
            var dif = this._pathGeometry.Figures[f].StartPoint - oldPoint;

            if (this._pathGeometry.Figures[f].StartPoint.AsPoint().AreExactlyTheSame(oldPoint.AsPoint()))
            {
                this._pathGeometry.Figures[f].StartPoint = screenPoint;

                break;
            }
            else
            {
                bool updated = false;

                for (int s = 0; s < this._pathGeometry.Figures[f].Segments.Count; s++)
                {
                    var lineSegment = (this._pathGeometry.Figures[f].Segments[s] as LineSegment);

                    dif = lineSegment.Point - oldPoint;

                    if (lineSegment.Point.AsPoint().AreExactlyTheSame(oldPoint.AsPoint()))
                    {
                        lineSegment.Point = screenPoint;

                        updated = true;

                        break;
                    }
                }

                if (updated)
                    break;
            }
        }

        var matched = UpdateLineSegment(point, newValue, this._webMercatorGeometry, this._midVertices);

        if (!matched)
        {
            throw new NotImplementedException();
        }
    }

    private bool UpdateLineSegment(Point point, Point newPoint, Geometry geometry, RecursiveCollection<Locateable> midCollection)
    {
        if (geometry.Points != null)
        {
            for (int i = 0; i < geometry.Points.Count; i++)
            {
                if (geometry.Points[i] == point)
                {
                    UpdateMidPoints(point, newPoint, i, midCollection);

                    return true;
                }
            }
        }
        else
        {
            for (int g = 0; g < geometry.Geometries.Count; g++)
            {
                var matched = UpdateLineSegment(point, newPoint, geometry.Geometries[g], midCollection.Collections[g]);

                if (matched)
                    return true;
            }
        }

        return false;
    }

    private void UpdateMidPoints(Point point, Point newPoint, int pointIndex, RecursiveCollection<Locateable> midCollection)
    {
        try
        {
            var displacement = new Point((newPoint.X - point.X) / 2.0, (newPoint.Y - point.Y) / 2.0);

            var length = midCollection.Values.Count;

            var leftIndex = GetLeftMidPointIndex(pointIndex, length);

            var rightIndex = GetRightMidPointIndex(pointIndex, length);

            Locateable left = leftIndex == int.MinValue ? null : midCollection.Values[leftIndex];

            if (left != null)
            {
                left.X = left.X + displacement.X;
                left.Y = left.Y + displacement.Y;
            }

            Locateable right = rightIndex == int.MinValue ? null : midCollection.Values[rightIndex];

            if (right != null)
            {
                right.X = right.X + displacement.X;
                right.Y = right.Y + displacement.Y;
            }

        }
        catch (Exception ex)
        {

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

            //if (!MassEdit)
            //{
            this._primaryVerticesLayer.Items.Add(locateable);
            //}

            // Update path geometry to reflect the new point immediately
            MakePathGeometry();

            if (geometry.Points.Count > 1 && Options.IsEdgeLabelVisible)
            {
                this._edgeLabelLayer.Items.Add(ToEdgeLengthLocatable(geometry.Points[geometry.Points.Count - 2], webMercatorPoint));
            }

            return locateable;
        }
        else
        {
            return AddVertex(webMercatorPoint, geometry.Geometries.Last(), primaryCollection.Collections.Last());
        }
    }

    private Locateable ToEdgeLengthLocatable(Point first, Point second)
    {
        Func<Point, Point> toGeodeticWgs84 = p => MapProjects.WebMercatorToGeodeticWgs84(p);

        var edge = new LineSegment<Point>(first, second);

        var element = new Views.MapMarkers.RectangleLabelMarker(SpatialUtility.GetLengthLabel(edge, toGeodeticWgs84));

        //var offset = _screenToMap(15);

        return new Locateable(AncherFunctionHandlers.BottomCenter) { Element = element, X = edge.Middle.X, Y = edge.Middle.Y };
    }

    #endregion


    #region Measures


    public double MeasureValue => SpatialUtility.GetEllipsoidMeasure(_webMercatorGeometry, MapProjects.WebMercatorToGeodeticWgs84);

    public string MeasureLabel => SpatialUtility.GetEllipsoidMeasureLabel(_webMercatorGeometry, MapProjects.WebMercatorToGeodeticWgs84);

    public string AreaLabel => UnitHelper.GetAreaLabel(SpatialUtility.GetEllipsoidalArea(_webMercatorGeometry, MapProjects.WebMercatorToGeodeticWgs84));

    public string LengthLabel => UnitHelper.GetLengthLabel(GetGeodeticWgs84Geometery().GetEllipsoidalLength(/*MapProjects.WebMercatorToGeodeticWgs84*/));


    public double ScaleFactor => MapProjects.CalculateUTMScaleFactor(GetGeodeticWgs84Geometery().GetCentroidPlus().AsPoint());


    public double EuclideanLengthInUtm => GetUtmGeometry().GetEuclideanLength();

    public double EuclideanLengthInUtm_Refined => EuclideanLengthInUtm * (1.0 / ScaleFactor) * (1.0 + Height / WebMercatorUtility.EarthRadius);

    public double SphericalLength => GetGeodeticWgs84Geometery().GetSphericalLength();

    public double EllipsoidalLength => GetGeodeticWgs84Geometery().GetEllipsoidalLength();

    public double GroundLength => EllipsoidalLength * (1.0 + Height / WebMercatorUtility.EarthRadius);


    public double EuclideanArea => GetUtmGeometry().EuclideanArea;

    public double EllipsoidalArea => SpatialUtility.GetEllipsoidalArea(GetGeodeticWgs84Geometery());

    public double AuthalicSphereArea => SpatialUtility.GetAreaOnAuthalicSphere(GetGeodeticWgs84Geometery());

    public double KarneyArea => SpatialUtility.GetKarneyArea(GetGeodeticWgs84Geometery());

    //public double GroundArea => EllipsoidalArea * (1.0 + 2 * Height / WebMercatorUtility.EarthRadius);
    public double GroundArea => SpatialUtility.GetGroundArea(GetGeodeticWgs84Geometery(), Height);

    #endregion


    #region Public Methods

    public Geometry GetGeodeticWgs84Geometery() => _webMercatorGeometry.Transform(MapProjects.WebMercatorToGeodeticWgs84, SridHelper.GeodeticWGS84);

    public Geometry GetUtmGeometry()
    {
        var geography = GetGeodeticWgs84Geometery();

        var boundary = geography.GetBoundingBox();

        var zone = MapProjects.FindUtmZone(boundary.MiddleBottom.X);

        return geography.Transform(p => MapProjects.GeodeticToUTM(p, Ellipsoids.WGS84, zone, boundary.YMin > 0), SridHelper.GetUtmSrid(zone));
    }

    public bool HasAnyPoint() => this._webMercatorGeometry != null && this._webMercatorGeometry.HasAnyPoint();

    public Path GetPath(Transform transform)
    {
        this._toScreen = transform;

        MakePathGeometry();

        return this._feature;
    }

    public SpecialPointLayer GetVertices() => _primaryVerticesLayer;

    public SpecialPointLayer GetMidVertices() => _midVerticesLayer;

    public SpecialPointLayer GetEdgeLengthes()
    {
        UpdateEdgeLables();

        return _edgeLabelLayer;
    }

    public SpecialPointLayer GetPrimaryVerticesLabels() => _primaryVerticesLabelLayer;

    public Geometry GetFinalGeometry() => this._webMercatorGeometry;

    public Locateable? AddVertex(Point webMercatorPoint)
    {
        return AddVertex(webMercatorPoint, this._webMercatorGeometry, this._vertices);
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
            return AddVertex(webMercatorPoint, this._webMercatorGeometry, this._vertices);
        }
        else if (polygonIndex != null)
        {
            return AddVertex(webMercatorPoint,
                            this._webMercatorGeometry.Geometries[polygonIndex.Value].Geometries[partIndex],
                            _vertices.Collections[polygonIndex.Value].Collections[partIndex]);
        }
        else
        {
            return AddVertex(webMercatorPoint, this._webMercatorGeometry.Geometries[partIndex], _vertices.Collections[partIndex]);
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
        var point = this.ToScreen(webMercatorPoint.AsWpfPoint());

        this._pathGeometry.Figures.Last().Segments.Add(new LineSegment(new WpfPoint(point.X, point.Y), true));
    }

    public void UpdateLastSemiVertexLocation(Point newMercatorPoint)
    {
        if (_pathGeometry.Figures?.Last()?.Segments?.Count() < 1)
            AddSemiVertex(newMercatorPoint);

        if (_pathGeometry.Figures.Last().Segments.Count < this._webMercatorGeometry.GetLastPart().Count)
        {
            AddSemiVertex(newMercatorPoint);
        }

        var newPoint = this.ToScreen(newMercatorPoint.AsWpfPoint());

        //var lastSegment = ((LineSegment)_pathGeometry.Figures.Last().Segments.Last()).Point = new WpfPoint(newPoint.X, newPoint.Y);
        ((LineSegment)_pathGeometry.Figures.Last().Segments.Last()).Point = new WpfPoint(newPoint.X, newPoint.Y);
    }

    public void FinishEditing()
    {
        this._webMercatorGeometry.ClearEmptyGeometries();

        this.RequestFinishEditing?.Invoke(this._webMercatorGeometry);
    }

    private void GoToPreviousPoint() => this._primaryVerticesLayer.SelectPreviousLocatable();

    private void GoToNextPoint() => this._primaryVerticesLayer.SelectNextLocatable();

    public void TryDeleteCurrentPoint()
    {
        var locateable = _primaryVerticesLayer.FindSelectedLocatable();

        if (locateable == null)
            return;

        this._primaryVerticesLabelLayer.Remove(locateable.Id);

        TryDeleteVertex(locateable.X, locateable.Y, this._webMercatorGeometry, _webMercatorGeometry.Type == GeometryType.Polygon || _webMercatorGeometry.Type == GeometryType.MultiPolygon);

        this.RequestRefresh?.Invoke(this);
    }

    private void ZoomToCurrentPoint()
    {
        var currentPoint = this._primaryVerticesLayer.FindSelectedLocatable();

        if (currentPoint == null)
            return;

        this.RequestZoomToPoint?.Invoke(new Point(currentPoint.X, currentPoint.Y));
    }

    private void CopyCurrentPointCoordinateToClipboard(CoordinateDisplayMode mode)
    {
        var currentPoint = this._primaryVerticesLayer.FindSelectedLocatable();

        if (currentPoint == null)
            return;

        Point point = new(currentPoint.X, currentPoint.Y);

        ClipboardHelper.CopyToClipboard(point, mode, null, null, null, null);

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

        this.RequestRefresh?.Invoke(this);
    }

    private void ZoomToCurrentPart()
    {
        var currentPoint = _primaryVerticesLayer.FindSelectedLocatable();

        if (currentPoint == null)
            return;

        var part = _webMercatorGeometry.GetRingOrLineStringPassingPoint(currentPoint.X, currentPoint.Y);

        this.RequestZoomToGeometry?.Invoke(part);
    }

    public void FindNearestPoint(Point point)
    {
        var nearestPoint = _webMercatorGeometry.GetNearestPoint(point);

        this._primaryVerticesLabelLayer.Items.Clear();

        this._primaryVerticesLabelLayer.Items.Add(ToPrimaryLocateable(nearestPoint));
    }

    public void SelectPoint(int index)
    {
        this._primaryVerticesLayer.SelectLocatable(index);
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
                this.RequestRefresh?.Invoke(this);
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Overrides

    public override Task<FeatureSet<Point>> GetFeatureSet(BoundingBox mapExtent, double mapScale)
    {
        var geometry = this.GetFinalGeometry();
        if (geometry == null || geometry.IsNullOrEmpty())
            return Task.FromResult(FeatureSet<Point>.Empty);

        // Check if geometry intersects bounding box
        var extentGeometry = mapExtent.AsGeometry<Point>(SridHelper.WebMercator);

        if (!geometry.Intersects(extentGeometry))
            return Task.FromResult(FeatureSet<Point>.Empty);

        return Task.FromResult(FeatureSet<Point>.Create($"{nameof(EditableFeatureLayer)}-{this.LayerId}", [geometry.AsFeature()]));
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
                _cancelEditingCommand = new RelayCommand(param => this.RequestCancelEditing?.Invoke(this));

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
                    this.RequestCancelEditing?.Invoke(this);
                    this.OnRequestDeleteGeometry?.Invoke(this, EventArgs.Empty);
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
                _cancelDrawingCommand = new RelayCommand(param => this.CancelDrawing());

            return _cancelDrawingCommand;
        }
    }


    private RelayCommand? _goToPreviousPointCommand;
    public RelayCommand GoToPreviousPointCommand
    {
        get
        {
            if (_goToPreviousPointCommand == null)
                _goToPreviousPointCommand = new RelayCommand(param => this.GoToPreviousPoint());

            return _goToPreviousPointCommand;
        }
    }


    private RelayCommand? _goToNextPointCommand;
    public RelayCommand GoToNextPointCommand
    {
        get
        {
            if (_goToNextPointCommand == null)
                _goToNextPointCommand = new RelayCommand(param => this.GoToNextPoint());

            return _goToNextPointCommand;
        }
    }


    private RelayCommand? _deleteCurrentPointCommand;
    public RelayCommand DeleteCurrentPointCommand
    {
        get
        {
            if (_deleteCurrentPointCommand == null)
                _deleteCurrentPointCommand = new RelayCommand(param => this.TryDeleteCurrentPoint());

            return _deleteCurrentPointCommand;
        }
    }


    private RelayCommand? _zoomToCurrentPointCommand;
    public RelayCommand ZoomToCurrentPointCommand
    {
        get
        {
            if (_zoomToCurrentPointCommand == null)
                _zoomToCurrentPointCommand = new RelayCommand(param => this.ZoomToCurrentPoint());

            return _zoomToCurrentPointCommand;
        }
    }


    private RelayCommand? _copyCurrentPointCommand;
    public RelayCommand CopyCurrentPointCommand
    {
        get
        {
            if (_copyCurrentPointCommand == null)
                _copyCurrentPointCommand = new RelayCommand(param => this.CopyCurrentPointCoordinateToClipboard((CoordinateDisplayMode)param));

            return _copyCurrentPointCommand;
        }
    }


    private RelayCommand? _deleteCurrentPartCommand;
    public RelayCommand DeleteCurrentPartCommand
    {
        get
        {
            if (_deleteCurrentPartCommand == null)
                _deleteCurrentPartCommand = new RelayCommand(param => this.DeleteCurrentPart());

            return _deleteCurrentPartCommand;
        }
    }


    private RelayCommand? _zoomToCurrentPartCommand;
    public RelayCommand ZoomToCurrentPartCommand
    {
        get
        {
            if (_zoomToCurrentPartCommand == null)
                _zoomToCurrentPartCommand = new RelayCommand(param => this.ZoomToCurrentPart());

            return _zoomToCurrentPartCommand;
        }
    }



    private RelayCommand? _convertToDrawingItemCommand;
    public RelayCommand ConvertToDrawingItemCommand
    {
        get
        {
            if (_convertToDrawingItemCommand == null)
                _convertToDrawingItemCommand = new RelayCommand(param => this.RequestConvertToDrawingItem?.Invoke(this._webMercatorGeometry));

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
                _showGeometryDetailsCommand = new RelayCommand(param => this.RequestShowGeometryDetails?.Invoke(this));
            }
            return _showGeometryDetailsCommand;
        }
    }



    #endregion
}
