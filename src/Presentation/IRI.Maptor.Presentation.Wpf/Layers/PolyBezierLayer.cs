using System;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Presentation.Wpf.Events;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Presentation.Wpf.ViewModels;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Wpf.Controls.MapMarkers;
using IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;

using WpfPoint = System.Windows.Point;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Presentation.Wpf.Controls.MapOptions;
using LineSegment = System.Windows.Media.LineSegment;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Layers;
using IRI.Maptor.Presentation.Core.Models;

namespace IRI.Maptor.Presentation.Wpf.Layers;

public class PolyBezierLayer : SymbolizableLayer
{
    static readonly Brush _stroke = BrushHelper.CreateBrush("#FF1CA1E2");

    #region ILayerMembers

    public override LayerType Type => LayerType.EditableItem;

    public override BoundingBox Extent
    {
        get => BoundingBox.CalculateBoundingBox(mercatorPolyline);

        protected set => throw new NotImplementedException();
    }

    //public override RenderingApproach Rendering
    //{
    //    get => RenderingApproach.Default;

    //    protected set => throw new NotImplementedException();
    //}

    #endregion

    List<Locateable> _mainLocateables = new List<Locateable>();

    List<Locateable> _controlLocateables = new List<Locateable>();

    List<PathFigure> _controlLines = new List<PathFigure>();

    PolyBezierSegment _polyBezier = new PolyBezierSegment();

    Transform _toScreen;

    SpecialPointLayer _mainLayer;

    SpecialPointLayer _controlLayer;

    Path _mainPath;

    Path _controlPath;

    List<Point> mercatorPolyline;

    private SpecialLineLayer _decorateLayer;

    public Action<PolyBezierLayer> RequestRefresh;

    public Action<System.Windows.FrameworkElement, MouseButtonEventArgs, ILocatable> RequestRightClickOptions;

    public Action RequestRemoveRightClickOptions;

    public Action<PolyBezierLayer> RequestFinishEditing;

    public bool IsDecorated { get; set; }

    public bool IsBezierShown { get; set; } = true;

    public bool IsControlsShown { get; set; } = true;

    public Action<ILayer> RequestAddLayer;

    public Action<ILayer> RequestRemoveLayer;

    private PolyBezierLayer(VisualParameters? parameters)
    {
        LayerId = Guid.NewGuid();

        VisibleRange = ScaleInterval.All;

        //this.VisualParameters = new VisualParameters(Colors.Black, Colors.Gray, 2, .9);
        //this.VisualParameters = parameters ?? VisualParameters.CreateNew(1);
        SetSymbolizer(new SimpleSymbolizer(parameters ?? VisualParameters.CreateNew()));
    }

    public PolyBezierLayer(List<Point> mercatorPolyline, Transform toScreen, Geometry decoration, VisualParameters parameters) : this(parameters)
    {
        _toScreen = toScreen;

        if (mercatorPolyline?.Count() < 2)
        {
            throw new NotImplementedException();
        }

        this.mercatorPolyline = mercatorPolyline;

        _decorateLayer = new SpecialLineLayer(decoration, parameters, null);

        Initialize();
    }

    public static PolyBezierLayer Create(string name, List<Point> mercatorPolyBezierPoints, Transform toScreen, Geometry decoration, VisualParameters parameters)
    {
        if (mercatorPolyBezierPoints?.Count() < 2)
            throw new NotImplementedException();

        if ((mercatorPolyBezierPoints.Count - 1) % 3 != 0)
            throw new NotImplementedException();

        PolyBezierLayer result = new PolyBezierLayer(parameters);

        result.LayerName = name;

        result._toScreen = toScreen;

        result.mercatorPolyline = new List<Point>();

        //this.mercatorPolyline = mercatorPolyline;

        result._decorateLayer = new SpecialLineLayer(decoration, parameters, null);

        result.mercatorPolyline.Add(mercatorPolyBezierPoints.First());

        var numberOfSegments = (mercatorPolyBezierPoints.Count - 1) / 3;

        for (int i = 0; i < numberOfSegments; i++)
        {
            result.mercatorPolyline.Add(mercatorPolyBezierPoints[i * 3 + 3]);
        }

        result.Initialize();

        for (int i = 0; i < numberOfSegments; i++)
        {
            result._controlLocateables[2 * i].X = mercatorPolyBezierPoints[i * 3 + 1].X;
            result._controlLocateables[2 * i].Y = mercatorPolyBezierPoints[i * 3 + 1].Y;

            result._controlLocateables[2 * i + 1].X = mercatorPolyBezierPoints[i * 3 + 2].X;
            result._controlLocateables[2 * i + 1].Y = mercatorPolyBezierPoints[i * 3 + 2].Y;
        }

        return result;
    }


    public void Initialize()
    {
        _mainLocateables.Clear();

        _controlLocateables.Clear();

        _controlLines.Clear();

        _polyBezier.Points.Clear();

        _mainLocateables = mercatorPolyline.Select(i => AsLocateable(i, Colors.Green)).ToList();

        for (int i = 0; i < _mainLocateables.Count; i++)
        {
            _mainLocateables[i].OnPositionChanged += mainLocateable_OnPositionChanged;

            var locateable = _mainLocateables[i];

            _mainLocateables[i].Element.MouseRightButtonDown += (sender, e) =>
            {
                mainElement_MouseRightButtonDown(locateable, e);
            };

            var point = mercatorPolyline[i];

            var control1 = AsLocateable(point, Colors.Gray);

            var controlLine1 = new PathFigure() { StartPoint = _toScreen.Transform(_mainLocateables[i].Location) };

            controlLine1.Segments.Add(new LineSegment() { Point = _toScreen.Transform(control1.Location) });

            control1.OnPositionChanged += controlLocateable_OnPositionChanged;

            _controlLocateables.Add(control1);

            _controlLines.Add(controlLine1);

            if (i == 0 || i == _mainLocateables.Count - 1)
                continue;

            var control2 = AsLocateable(point, Colors.Gray);

            var controlLine2 = new PathFigure() { StartPoint = _toScreen.Transform(_mainLocateables[i].Location) };
            controlLine2.Segments.Add(new LineSegment() { Point = _toScreen.Transform(control2.Location) });

            control2.OnPositionChanged += controlLocateable_OnPositionChanged;

            _controlLocateables.Add(control2);

            _controlLines.Add(controlLine2);
        }

        for (int i = 1; i < _mainLocateables.Count; i++)
        {
            int index = 2 * i - 2;

            _polyBezier.Points.Add(_toScreen.Transform(_controlLocateables[index].Location));
            _polyBezier.Points.Add(_toScreen.Transform(_controlLocateables[index + 1].Location));
            _polyBezier.Points.Add(_toScreen.Transform(_mainLocateables[i].Location));

        }

        PathFigure mainFigure = new PathFigure() { StartPoint = _toScreen.Transform(_mainLocateables[0].Location) };

        mainFigure.Segments.Add(_polyBezier);

        PathGeometry mainGeometry = new PathGeometry(new List<PathFigure>() { mainFigure });

        _mainPath = new Path() { Data = mainGeometry, Stroke = _stroke, StrokeThickness = 3, Opacity = .9 };


        PathFigureCollection controlFigureCollection = new PathFigureCollection(_controlLines);

        PathGeometry controlGeometry = new PathGeometry(controlFigureCollection);

        _controlPath = new Path() { Data = controlGeometry, Stroke = new SolidColorBrush(Colors.Red), StrokeThickness = 1 };

        _mainLayer = new SpecialPointLayer("1", _mainLocateables, .9, ScaleInterval.All, LayerType.EditableItem /*| LayerType.MoveableItem*/) { AlwaysTop = true, IsMovable = true };

        _controlLayer = new SpecialPointLayer("2", _controlLocateables, .9, ScaleInterval.All, LayerType.EditableItem /*| LayerType.MoveableItem*/) { AlwaysTop = true, IsMovable = true };
    }

    private void mainElement_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var mainLocateable = sender as Locateable;

        var presenter = new MapOptionsViewModel(
            rightToolTip: string.Empty,
            leftToolTip: string.Empty,
            middleToolTip: string.Empty,

            rightSymbol: MapOptionsIcon.FromMaterial(MahApps.Metro.IconPacks.PackIconMaterialKind.ContentCopy),
            leftSymbol: MapOptionsIcon.FromMaterial(MahApps.Metro.IconPacks.PackIconMaterialKind.Plus),
            middleSymbol: MapOptionsIcon.FromMaterial(MahApps.Metro.IconPacks.PackIconMaterialKind.Delete));

        presenter.RightCommandAction = i =>
        {
            //var geodetic = MapProjects.WebMercatorToGeodeticWgs84(mainLocateable.Location.AsPoint());

            //System.Windows.Clipboard.SetDataObject($"{geodetic.X.ToString("n4")},{geodetic.Y.ToString("n4")}");

            var options = CopyCoordinateOptions.Create();

            ClipboardHelper.CopyToClipboard(mainLocateable.Location.AsPoint(), CoordinateDisplayMode.GeodeticDecimal, options/*null, null, null, null*/);

            RemoveMapOptions();
        };

        presenter.LeftCommandAction = i =>
        {
            Add(mainLocateable);

            RemoveMapOptions();
        };

        presenter.MiddleCommandAction = i =>
        {
            //delete mainlocateable
            Remove(mainLocateable);

            RemoveMapOptions();
        };

        if (RequestRightClickOptions != null)
        {
            RequestRightClickOptions(new MapThreeOptions(), e, presenter);
        }
    }

    private void Add(Locateable mainLocateable)
    {
        var index = _mainLocateables.IndexOf(mainLocateable);

        if (index == 0)
            return;

        var point = new Point((mainLocateable.X + _mainLocateables[index - 1].X) / 2.0, (mainLocateable.Y + _mainLocateables[index - 1].Y) / 2.0);

        var newMainLocateable = AsLocateable(point, Colors.Green);

        newMainLocateable.OnPositionChanged += mainLocateable_OnPositionChanged;

        newMainLocateable.Element.MouseRightButtonDown += (sender, e) =>
        {
            mainElement_MouseRightButtonDown(newMainLocateable, e);
        };

        var newControl1 = AsLocateable(point, Colors.Gray);

        newControl1.OnPositionChanged += controlLocateable_OnPositionChanged;

        var newControl2 = AsLocateable(point, Colors.Gray);

        newControl2.OnPositionChanged += controlLocateable_OnPositionChanged;

        _mainLocateables.Insert(index, newMainLocateable);

        mercatorPolyline.Insert(index, point);

        _controlLocateables.Insert(2 * index - 1, newControl2);

        _controlLocateables.Insert(2 * index - 1, newControl1);

        Refresh();
    }

    private void Remove(Locateable mainLocateable)
    {
        if (_mainLocateables.Count <= 2)
        {
            return;
        }

        var index = _mainLocateables.IndexOf(mainLocateable);

        if (index > 0)
        {
            _controlLocateables.RemoveAt(2 * index - 1);

            _controlLayer.Items.RemoveAt(2 * index - 1);

            if (index == _mainLocateables.Count - 1)
            {
                _controlLocateables.RemoveAt(2 * index - 2);

                _controlLayer.Items.RemoveAt(2 * index - 2);
            }
            else
            {
                _controlLocateables.RemoveAt(2 * index - 1);

                _controlLayer.Items.RemoveAt(2 * index - 1);
            }
        }
        else
        {
            _controlLocateables.RemoveAt(0);

            _controlLocateables.RemoveAt(0);

            _controlLayer.Items.RemoveAt(0);

            _controlLayer.Items.RemoveAt(0);
        }

        _mainLocateables.Remove(mainLocateable);

        _mainLayer.Items.Remove(mainLocateable);

        mercatorPolyline.RemoveAt(index);

        Refresh();
    }

    private void AddLayer(ILayer layer)
    {
        RequestAddLayer?.Invoke(layer);
    }

    private void RemoveLayer(ILayer layer)
    {
        RequestRemoveLayer?.Invoke(layer);
    }

    private void Refresh()
    {
        RequestRefresh?.Invoke(this);
    }

    private void FinishEditing()
    {
        if (RequestFinishEditing != null)
        {
            RequestFinishEditing(this);
        }
    }

    private void RemoveMapOptions()
    {
        if (RequestRemoveRightClickOptions != null)
        {
            RequestRemoveRightClickOptions();
        }
    }

    public void Redraw(Transform toScreen)
    {
        _controlLines.Clear();

        _polyBezier.Points.Clear();

        _toScreen = toScreen;

        for (int i = 0; i < _controlLocateables.Count; i++)
        {
            int index = (int)Math.Ceiling(i / 2.0);

            var controlLine1 = new PathFigure() { StartPoint = toScreen.Transform(_mainLocateables[index].Location) };

            controlLine1.Segments.Add(new LineSegment() { Point = toScreen.Transform(_controlLocateables[i].Location) });

            _controlLines.Add(controlLine1);
        }

        for (int i = 1; i < _mainLocateables.Count; i++)
        {
            int index = 2 * i - 2;

            _polyBezier.Points.Add(toScreen.Transform(_controlLocateables[index].Location));
            _polyBezier.Points.Add(toScreen.Transform(_controlLocateables[index + 1].Location));
            _polyBezier.Points.Add(toScreen.Transform(_mainLocateables[i].Location));

        }

        PathFigure mainFigure = new PathFigure() { StartPoint = toScreen.Transform(_mainLocateables[0].Location) };

        mainFigure.Segments.Add(_polyBezier);

        PathGeometry mainGeometry = new PathGeometry(new List<PathFigure>() { mainFigure });

        _mainPath = new Path() { Tag = "PolyBezier _mainPath temp Tag", Data = mainGeometry, Stroke = _stroke, StrokeThickness = 4, Opacity = .9, Cursor = Cursors.Hand };

        _mainPath.Tag = new LayerTag(0) { Layer = this, IsTiled = false, LayerType = LayerType.EditableItem };



        _mainPath.MouseRightButtonDown += _mainPath_MouseRightButtonDown;

        _mainPath.MouseEnter += (sender, e) => { _mainPath.StrokeThickness = 6; };
        _mainPath.MouseLeave += (sender, e) => { _mainPath.StrokeThickness = 4; };

        PathFigureCollection controlFigureCollection = new PathFigureCollection(_controlLines);

        PathGeometry controlGeometry = new PathGeometry(controlFigureCollection);

        _controlPath = new Path() { Data = controlGeometry, Stroke = new SolidColorBrush(Colors.Red), StrokeThickness = 1 };

        _controlPath.Tag = new LayerTag(0) { Layer = this, IsTiled = false, LayerType = LayerType.EditableItem };

        _mainLayer = new SpecialPointLayer($"POLYBEZIER MAIN {LayerId}", _mainLocateables, .9, ScaleInterval.All, LayerType.EditableItem /*| LayerType.MoveableItem*/) { AlwaysTop = true, IsMovable = true };

        _controlLayer = new SpecialPointLayer($"POLYBEZIER CONTROL {LayerId}", _controlLocateables, .9, ScaleInterval.All, LayerType.EditableItem /*| LayerType.MoveableItem*/) { AlwaysTop = true, IsMovable = true };


        _mainPath.MouseLeftButtonDown += (sender, e) =>
        {
            IsControlsShown = !IsControlsShown;

            var newVisibility = IsControlsShown ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

            GetControlPath().Visibility = newVisibility;

            if (IsControlsShown)
            {
                AddLayer(GetControlPointLayer());
            }
            else
            {
                RemoveLayer(GetControlPointLayer());
            }
        };

        //if (IsDecorated)
        //{
        Decorate();
        //}

    }

    private void _mainPath_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var presenter = new MapOptionsViewModel(
            rightToolTip: string.Empty,
            leftToolTip: string.Empty,
            middleToolTip: string.Empty,

            rightSymbol: MapOptionsIcon.FromMaterial(MahApps.Metro.IconPacks.PackIconMaterialKind.City),
            leftSymbol: MapOptionsIcon.FromMaterial(MahApps.Metro.IconPacks.PackIconMaterialKind.CheckBold),
            middleSymbol: null);

        presenter.RightCommandAction = i =>
        {
            IsDecorated = !IsDecorated;

            Decorate();

            RemoveMapOptions();
        };

        presenter.LeftCommandAction = i =>
        {
            FinishEditing();

            RemoveMapOptions();
        };

        RequestRightClickOptions?.Invoke(new MapTwoOptions(), e, presenter);
    }

    private void Decorate()
    {
        if (!(_decorateLayer?.Symbol != null))
            return;

        RemoveLayer(_decorateLayer);

        if (IsDecorated)
        {
            _decorateLayer.Update(_decorateLayer.Symbol, GetPolyBezierMapPoints());

            AddLayer(_decorateLayer);
        }
    }


    /// <summary>
    /// Returns collection of main points and control points of the PolyBezier
    /// </summary>
    /// <param name="toScreen"></param>
    /// <returns></returns>
    public List<Point> GetPolyBezierMapPoints()
    {
        List<Point> result = new List<Point>();

        var inverse = _toScreen.Inverse;

        var figure = (_mainPath.Data as PathGeometry).Figures.First();

        result.Add(inverse.Transform(figure.StartPoint).AsPoint());

        result.AddRange((figure.Segments.First() as PolyBezierSegment).Points.Select(i => inverse.Transform(i).AsPoint()));

        return result;
    }

    private void controlLocateable_OnPositionChanged(object sender, ChangeEventArgs<WpfPoint> e)
    {
        var locateable = sender as Locateable;

        var index = _controlLocateables.IndexOf(locateable);

        _polyBezier.Points[index + index / 2] = _toScreen.Transform(locateable.Location);

        (_controlLines[index].Segments[0] as LineSegment).Point = _toScreen.Transform(locateable.Location);

        //if (IsDecorated)
        //{
        //RemoveLayer(_decorateLayer);

        Decorate();

        //    AddLayer(_decorateLayer);
        //}
    }

    private void mainLocateable_OnPositionChanged(object? sender, ChangeEventArgs<WpfPoint> e)
    {
        var locateable = sender as Locateable;

        if (locateable is null)
            return;

        var index = _mainLocateables.IndexOf(locateable);

        //var newScreen = _toScreen.Transform(e.NewValue);

        //var oldScreen = _toScreen.Transform(e.OldValue);

        //var displacement = newScreen - oldScreen;

        if (index > 0)
        {
            _polyBezier.Points[3 * index - 1] = _toScreen.Transform(locateable.Location);

            _controlLines[2 * index - 1].StartPoint = _toScreen.Transform(locateable.Location);

            _controlLocateables[2 * index - 1].X += e.NewValue.X - e.OldValue.X;
            _controlLocateables[2 * index - 1].Y += e.NewValue.Y - e.OldValue.Y;

            if (index < _mainLocateables.Count - 1)
            {
                _controlLines[2 * index].StartPoint = _toScreen.Transform(locateable.Location);

                _controlLocateables[2 * index].X += e.NewValue.X - e.OldValue.X;
                _controlLocateables[2 * index].Y += e.NewValue.Y - e.OldValue.Y;
            }
        }
        else
        {
            (_mainPath.Data as PathGeometry)!.Figures.First().StartPoint = _toScreen.Transform(locateable.Location);

            _controlLines[0].StartPoint = _toScreen.Transform(locateable.Location);

            _controlLocateables[0].X += e.NewValue.X - e.OldValue.X;
            _controlLocateables[0].Y += e.NewValue.Y - e.OldValue.Y;
        }

        //if (IsDecorated)
        //{
        //RemoveLayer(_decorateLayer);

        Decorate();

        //    AddLayer(_decorateLayer);
        //}
    }

    private Locateable AsLocateable(Point webMercatorPoint, Color color)
    {
        return new Locateable(MapProjects.WebMercatorToGeodeticWgs84(webMercatorPoint)) { Element = new Circle(1, new SolidColorBrush(color)) };
    }

    public Path GetMainPath()
    {
        return _mainPath;
    }

    public Path GetControlPath()
    {
        return _controlPath;
    }


    public SpecialPointLayer GetMainPointLayer()
    {
        return _mainLayer;
    }

    public SpecialPointLayer GetControlPointLayer()
    {
        return _controlLayer;
    }

    public SpecialLineLayer GetDecorateLayer()
    {
        return _decorateLayer;
    }

    public override Task<FeatureSet<Point>> GetFeatureSet(BoundingBox mapExtent, double mapScale) => Task.FromResult(FeatureSet<Point>.Empty);

}
