using System;
using System.Linq;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;

namespace IRI.Maptor.Jab.Common;

public class SpecialLineLayer : SymbolizableLayer
{
    public override LayerType Type => LayerType.Complex;

    public const string DefaultArrowString = "F1 M 6.75,9L 8.75,11L 16,18L 9.5,18L 0,9L 9.5,0L 16,0L 8.75,7L 6.75,9 Z";

    public static readonly System.Windows.Media.Geometry DefaultArrow = System.Windows.Media.Geometry.Parse(DefaultArrowString);
     
    List<Point> _pointCollection;

    bool _isPolyBezierMode;

    System.Windows.Media.Geometry _symbol;
    public System.Windows.Media.Geometry Symbol
    {
        get { return _symbol; }
        set { this._symbol = value; }
    }

    public bool CanEdit { get; set; } = true;

    /// <summary>
    /// In case of PolyBezierMode, control points must be included
    /// </summary>
    /// <param name="pointCollection"></param>
    /// <param name="extent"></param>
    /// <param name="polyBezierMode"></param>
    public SpecialLineLayer(System.Windows.Media.Geometry symbol, VisualParameters parameters, List<Point> pointCollection, bool canEdit = true, bool polyBezierMode = true)
    {
        if (!polyBezierMode)
            throw new NotImplementedException();

        var visualParameters = parameters ?? VisualParameters.CreateNew(1);
         
        this.SetSymbolizer(new SimpleSymbolizer(visualParameters));

        this.ZIndex = int.MaxValue;

        Update(symbol, pointCollection, canEdit, polyBezierMode);
    }

    private PathGeometry GetPathGeometry(Transform toScreen)
    {
        PathFigure figure = new PathFigure() { StartPoint = toScreen.Transform(_pointCollection[0].AsWpfPoint()) };

        PolyBezierSegment segment = new PolyBezierSegment();

        segment.Points = new PointCollection(_pointCollection.Skip(1).Select(p => toScreen.Transform(p.AsWpfPoint())));

        figure.Segments.Add(segment);

        return new PathGeometry(new List<PathFigure>() { figure });
    }

    public void Update(System.Windows.Media.Geometry symbol, List<Point> pointCollection, bool canEdit = true, bool polyBezierMode = true)
    {
        this._symbol = symbol;

        this.Extent = BoundingBox.CalculateBoundingBox(pointCollection);

        _pointCollection = pointCollection;

        this._isPolyBezierMode = polyBezierMode;
    }

    public List<Path> GetPaths(Transform toScreen, BoundingBox mapBoundingBox, Action mouseDownAction = null)
    {
        if (_symbol == null)
            return new List<Path>();

        var pathGeometry = GetPathGeometry(toScreen);

        var firstScreen = toScreen.Transform(mapBoundingBox.TopLeft.AsWpfPoint());
        var secondScreen = toScreen.Transform(mapBoundingBox.BottomRight.AsWpfPoint());
        var screenLimit = new BoundingBox(firstScreen.X, firstScreen.Y, secondScreen.X, secondScreen.Y);

        var size = toScreen.TransformBounds(Extent.AsRect());

        var size2 = pathGeometry.Bounds;

        var distance = Math.Max(size2.Width, size2.Height);

        var unitSize = Math.Max(_symbol.Bounds.Height, _symbol.Bounds.Width);

        var tolerance = distance / unitSize;

        List<Path> result = new List<Path>();

        var bound = _symbol.Bounds;

        for (int i = 0; i <= tolerance; i++)
        {
            double fraction = (i) / tolerance;

            System.Windows.Point location, direction;

            pathGeometry.GetPointAtFractionLength(fraction, out location, out direction);

            if (!screenLimit.Intersects(new Point(location.X, location.Y)))
                continue;

            var param = this.GetMainOrDefaultSymbology();

            Path tempPath = new Path() { Fill = param.Fill, Data = _symbol };

            if (CanEdit && mouseDownAction != null)
            {
                tempPath.MouseLeftButtonDown += (sender, e) =>
                {
                    if (e.ClickCount > 1)
                    {
                        mouseDownAction();
                    }
                };
            }

            Matrix matrix = Matrix.Identity;

            var rotation = Math.Atan2(direction.Y, direction.X) * 180 / Math.PI;

            matrix.RotateAt(rotation, (bound.Width + bound.X) / 2.0, (bound.Height + bound.Y) / 2.0);

            matrix.Translate(location.X - bound.Width / 2.0 - bound.X / 2.0, location.Y - bound.Height / 2.0 - bound.Y / 2.0);

            TransformGroup group = new TransformGroup();

            group.Children.Add(new MatrixTransform(matrix));

            tempPath.RenderTransform = group;

            //tempPath.Tag = new LayerTag(-1) { IsTiled = false, LayerType = LayerType.AnimatingItem };

            result.Add(tempPath);
        }

        return result;
    }


}
