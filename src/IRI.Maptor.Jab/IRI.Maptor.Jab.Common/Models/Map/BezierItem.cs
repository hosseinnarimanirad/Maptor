using System.Linq;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Jab.Common.Models;

public class BezierItem
{
    private Locateable StartLocateable { get; set; }

    private Locateable EndLocateable { get; set; }

    private Locateable StartControlLocateable { get; set; }

    private Locateable EndControlLocateable { get; set; }

    private PathFigure _bezierFigure;

    private PathFigure _startControlLineFigure;

    private PathFigure _endControlLineFigure;

    private Transform _toScreen;

    private bool _controlLinesVisible;

    public bool IsStartControlLineVisible
    {
        get { return _controlLinesVisible; }
        set
        {
            _controlLinesVisible = value;                
        }
    }


    public BezierItem(Point startPoint, Point endPoint, Transform toScreen)
    {
        this._toScreen = toScreen;

        var start = toScreen.Transform(startPoint.AsWpfPoint());

        var end = toScreen.Transform(endPoint.AsWpfPoint());

        var startControlPoint = toScreen.Transform(startPoint.AsWpfPoint());

        var endControlPoint = toScreen.Transform(endPoint.AsWpfPoint());


        var bezierSegment = new BezierSegment(startControlPoint, endControlPoint, end, true);

        _bezierFigure = new PathFigure() { StartPoint = start };

        _bezierFigure.Segments.Add(bezierSegment);

        PathGeometry bezierPathGeometry = new PathGeometry(new List<PathFigure>() { _bezierFigure });
        //pathGeometry.Transform = this.panTransformForPoints;


        _startControlLineFigure = new PathFigure() { StartPoint = start };

        _startControlLineFigure.Segments.Add(new LineSegment(startControlPoint, true));


        _endControlLineFigure = new PathFigure() { StartPoint = end };

        _endControlLineFigure.Segments.Add(new LineSegment(endControlPoint, true));

        StartLocateable = new Locateable(MapProjects.WebMercatorToGeodeticWgs84(startPoint)) { Element = new View.MapMarkers.Circle(1, new SolidColorBrush(Colors.Green)) };

        StartLocateable.OnPositionChanged += (sender, e) =>
        {
            var locateable = (Locateable)sender;

            var newPoint = toScreen.Transform(new System.Windows.Point(locateable.X, locateable.Y));

            _bezierFigure.StartPoint = newPoint;

            _startControlLineFigure.StartPoint = newPoint;

            //update();
        };


        EndLocateable = new Locateable(MapProjects.WebMercatorToGeodeticWgs84(endPoint)) { Element = new View.MapMarkers.Circle(1, new SolidColorBrush(Colors.Green)) };

        EndLocateable.OnPositionChanged += (sender, e) =>
        {
            var locateable = (Locateable)sender;

            var newPoint = toScreen.Transform(new System.Windows.Point(locateable.X, locateable.Y));

            bezierSegment.Point3 = newPoint;

            _endControlLineFigure.StartPoint = newPoint;

            //update();
        };


        StartControlLocateable = new Locateable(MapProjects.WebMercatorToGeodeticWgs84(startControlPoint.AsPoint())) { Element = new View.MapMarkers.Circle(1, new SolidColorBrush(Colors.Green)) };

        StartControlLocateable.OnPositionChanged += (sender, e) =>
        {
            var locateable = (Locateable)sender;

            var newPoint = toScreen.Transform(new System.Windows.Point(locateable.X, locateable.Y));

            bezierSegment.Point1 = newPoint;

            (_startControlLineFigure.Segments.First() as LineSegment).Point = newPoint;

            //update();
        };


        EndControlLocateable = new Locateable(MapProjects.WebMercatorToGeodeticWgs84(endControlPoint.AsPoint())) { Element = new View.MapMarkers.Circle(1, new SolidColorBrush(Colors.Green)) };

        EndControlLocateable.OnPositionChanged += (sender, e) =>
        {
            var locateable = (Locateable)sender;

            var newPoint = toScreen.Transform(new System.Windows.Point(locateable.X, locateable.Y));

            bezierSegment.Point2 = newPoint;

            (_endControlLineFigure.Segments.First() as LineSegment).Point = newPoint;

            //update();
        };



        Path temp = new Path();
        temp.Data = bezierPathGeometry;
        temp.Stroke = new SolidColorBrush(Colors.Black);
        temp.StrokeThickness = 2;
        //temp.RenderTransform = this.panTransformForPoints;
        //temp.Tag = new LayerTag(0) { IsTiled = false, LayerType = LayerType.Drawing };

        //this.mapView.Children.Add(temp);

        //Path axLine2 = new Path() { Stroke = new SolidColorBrush(Colors.Gray), StrokeThickness = 1, RenderTransform = panTransformForPoints };
        Path axLine2 = new Path() { Stroke = new SolidColorBrush(Colors.Gray), StrokeThickness = 1 };
        axLine2.Data = new PathGeometry(new List<PathFigure>() { _startControlLineFigure });
        //axLine2.Tag = new LayerTag(-1) { IsTiled = false, LayerType = LayerType.Drawing };
        //this.mapView.Children.Add(axLine2);

        //Path axLine3 = new Path() { Stroke = new SolidColorBrush(Colors.Gray), StrokeThickness = 1, RenderTransform = panTransformForPoints };
        Path axLine3 = new Path() { Stroke = new SolidColorBrush(Colors.Gray), StrokeThickness = 1 };
        axLine3.Data = new PathGeometry(new List<PathFigure>() { _endControlLineFigure });
        //axLine3.Tag = new LayerTag(-1) { IsTiled = false, LayerType = LayerType.Drawing };
        //this.mapView.Children.Add(axLine3);
    }
}