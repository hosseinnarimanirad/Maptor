using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.SpatialReferenceSystem; 

namespace IRI.Maptor.Jab.Controls;

/// <summary>
/// Interaction logic for UserControl1.xaml
/// </summary>
public partial class ImageViewer : UserControl
{
    TransformGroup viewTransform;

    TranslateTransform panTransform;

    ScaleTransform zoomTransform;

    public double ScreenScale
    {
        get { return this.zoomTransform.ScaleX; }
    }

    public double MapScale
    {
        get { return ToMapScale(this.ScreenScale); }
    }

    #region Conversions

    private double ToScreenScale(double mapScale)
    {
        PresentationSource source = PresentationSource.FromVisual(this.mapView);

        double dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;

        double unitDistance = ConversionHelper.InchToMeterFactor / dpiX;                     // Meter

        return mapScale / unitDistance;
    }

    private double ToMapScale(double screenScale)
    {
        PresentationSource source = PresentationSource.FromVisual(this.mapView);

        double dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;

        double unitDistance = ConversionHelper.InchToMeterFactor / dpiX;

        return screenScale * unitDistance;
    }

    public System.Windows.Point ScreenToIntermidiate(System.Windows.Point point)
    {
        return this.viewTransform.Inverse.Transform(point);
    }

    public System.Windows.Point MapToGeodetic(System.Windows.Point point)
    {
        return this.MercatorToGeodetic(point);
    }

    //public System.Windows.Point GeodeticToMap(System.Windows.Point point)
    //{
    //    double[][] result = IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections.MapProjects.GeodeticToMercator(
    //                                                            new double[] { point.X },
    //                                                            new double[] { point.Y },
    //                                                             IRI.Maptor.Sta.SpatialReferenceSystem.Ellipsoids.WGS84);

    //    return new System.Windows.Point(result[0][0], result[1][0]);
    //}

    public System.Windows.Point IntermidiateToScreen(System.Windows.Point point)
    {
        return this.viewTransform.Transform(point);
    }




    private System.Windows.Point GeodeticToWebMercator(System.Windows.Point point)
    {
        try
        {
            var result = MapProjects.GeodeticWgs84ToWebMercator(point.AsPoint());

            return new System.Windows.Point(result.X, result.Y);
        }
        catch (Exception)
        {
            return new System.Windows.Point(double.NaN, double.NaN);
        }

    }

    private System.Windows.Point GeodeticToUTM(System.Windows.Point point)
    {
        try
        {
            var result =
                MapProjects.GeodeticToUTM(new IRI.Maptor.Sta.Common.Primitives.Point(point.X, point.Y), Ellipsoids.WGS84);

            return new System.Windows.Point(result.X, result.Y);
        }
        catch (Exception)
        {
            return new System.Windows.Point(double.NaN, double.NaN);
        }

    }

    private System.Windows.Point GeodeticToAlbers(System.Windows.Point point)
    {
        try
        {
            double[][] result =
                MapProjects.GeodeticToCylindricalEqualArea(
                    new double[] { point.X },
                    new double[] { point.Y }, Ellipsoids.WGS84,
                    0, 0);

            return new System.Windows.Point(result[0][0], result[1][0]);
        }
        catch (Exception)
        {
            return new System.Windows.Point(double.NaN, double.NaN);
        }

    }

    private System.Windows.Point MercatorToGeodetic(System.Windows.Point point)
    {
        try
        {
            double[][] result = MapProjects.MercatorToGeodetic(
                                                                new double[] { point.X },
                                                                new double[] { point.Y },
                                                                Ellipsoids.WGS84);

            return new System.Windows.Point(result[0][0], result[1][0]);
        }
        catch (Exception)
        {

            return new System.Windows.Point(double.NaN, double.NaN);
        }

    }

    #endregion

    private void Reset()
    {
        this.viewTransform = new TransformGroup();

        this.panTransform = new TranslateTransform();

        this.zoomTransform = new ScaleTransform();

        viewTransform.Children.Add(panTransform);

        viewTransform.Children.Add(zoomTransform);
    }

    public ImageViewer()
    {
        InitializeComponent();

        Reset();
    }

    public void SetImage(string fileName)
    {
        Reset();

        BitmapImage image = new BitmapImage(new Uri(fileName));

        RectangleGeometry geometry = new RectangleGeometry(new Rect(0, 0, image.Width, image.Height));

        geometry.Transform = this.viewTransform;

        ImageBrush brush = new ImageBrush(image);

        Path path = new Path()
        {
            Data = geometry,
            Fill = brush,
            Stroke = new SolidColorBrush(Colors.Black),
            StrokeThickness = 1,
            Opacity = 1,
        };

        this.mapView.Children.Add(path);

    }

    private void ZoomToPoint(System.Windows.Point windowPoint, double deltaZoom)
    {
        this.Cursor = Cursors.Wait;

        System.Windows.Point layerPoint = this.viewTransform.Inverse.Transform(windowPoint);

        this.panTransform.X = this.mapView.ActualWidth / 2.0 - layerPoint.X;

        this.panTransform.Y = this.mapView.ActualHeight / 2.0 - layerPoint.Y;

        double scale = this.zoomTransform.ScaleX * deltaZoom;


        //
        this.zoomTransform.CenterX = this.mapView.ActualWidth / 2.0;

        this.zoomTransform.CenterY = this.mapView.ActualHeight / 2.0;

        this.zoomTransform.ScaleX = scale;

        this.zoomTransform.ScaleY = scale;


        this.Cursor = Cursors.Arrow;
    }

    bool isPanning = false;

    System.Windows.Point prevMouseLocation;

    public void Pan()
    {
        this.mapView.MouseDown -= mapView_MouseDownForPan;
        this.mapView.MouseDown += mapView_MouseDownForPan;

        this.mapView.MouseUp -= mapView_MouseUpForPan;
        this.mapView.MouseUp += mapView_MouseUpForPan;
    }

    public void ReleasePan()
    {
        this.mapView.MouseDown -= mapView_MouseDownForPan;

        this.mapView.MouseUp -= mapView_MouseUpForPan;
    }

    private void mapView_MouseDownForPan(object sender, MouseButtonEventArgs e)
    {
        Mouse.Capture(this.mapView);

        this.isPanning = true;

        this.prevMouseLocation = e.GetPosition(this.mapView);

        this.MouseMove += mapView_MouseMoveForPan;
    }

    private void mapView_MouseUpForPan(object sender, MouseButtonEventArgs e)
    {
        if (!isPanning)
        {
            return;
        }

        this.isPanning = false;

        this.mapView.ReleaseMouseCapture();

        this.MouseMove -= mapView_MouseMoveForPan;

    }

    private void mapView_MouseMoveForPan(object sender, MouseEventArgs e)
    {
        if (!isPanning)
        {
            return;
        }

        System.Windows.Point currentMouseLocation = e.GetPosition(this.mapView);

        double xOffset = currentMouseLocation.X - this.prevMouseLocation.X;

        double yOffset = currentMouseLocation.Y - this.prevMouseLocation.Y;

        if (Math.Abs(xOffset) > 2 || Math.Abs(yOffset) > 2)
        {
            this.panTransform.X += xOffset * 1.0 / this.zoomTransform.ScaleX;

            this.panTransform.Y += yOffset * 1.0 / this.zoomTransform.ScaleY;

            this.prevMouseLocation = currentMouseLocation;
        }

    }


    bool isBusy = false;

    private void mapView_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (isBusy)
            return;

        this.isBusy = true;

        System.Windows.Point point = e.GetPosition(this.mapView);

        double delta = e.Delta > 0 ? 1.5 : 0.5;

        this.ZoomToPoint(point, delta);

        this.isBusy = false;
    }

    public void EnableZoomingOnMouseWheel()
    {
        this.mapView.MouseWheel -= mapView_MouseWheel;
        this.mapView.MouseWheel += mapView_MouseWheel;
    }

    public void DisableZoomingOnMouseWheel()
    {
        this.mapView.MouseWheel -= mapView_MouseWheel;
    }


}

