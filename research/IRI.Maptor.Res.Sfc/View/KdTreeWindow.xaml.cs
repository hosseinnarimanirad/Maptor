using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Collections.Generic;

using IRI.Maptor.Sta.Spatial.AdvancedStructures;
using IRI.Maptor.Sta.Spatial.Analysis.SFC;
using spatial = IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Res.Sfc.View;

/// <summary>
/// Interaction logic for KdTreeWindow.xaml
/// </summary>
public partial class KdTreeWindow : Window
{
    List<spatial.Point> points = new List<spatial.Point>();

    public KdTreeWindow()
    {
        InitializeComponent();
    }

    private void canvas1_MouseDown(object sender, MouseButtonEventArgs e)
    {
        Point newPoint = e.GetPosition(this.canvas1);

        this.points.Add(new spatial.Point(newPoint.X, newPoint.Y));

        Referesh();
    }

    private void Referesh()
    {
        //spatial.Primitives.Boundary b = PointOrdering.GetBoundary(points.ToArray(), 2);


        //******************************************* 01
        DoTheJob(this.points.ToArray(), this.canvas1, this.title1, "Simple K-d Tree");

        //******************************************* 02
        var result = OrderSFCOrderdPoints(PointOrdering.HilbertSorter(this.points.ToArray()).ToList()).ToArray();

        DoTheJob(result, this.canvas2, this.title2, "Hilbert");

        //******************************************* 03 
        result = OrderSFCOrderdPoints(PointOrdering.ZOrderingSorter(this.points.ToArray()).ToList()).ToArray();

        DoTheJob(result, this.canvas3, this.title3, "Z-Ordering/Morton");

        //******************************************* 04 
        result = OrderSFCOrderdPoints(PointOrdering.GraySorter(this.points.ToArray()).ToList()).ToArray();

        DoTheJob(result, this.canvas4, this.title4, "Gray");

        //******************************************* 05
        result = OrderSFCOrderdPoints(PointOrdering.UOrderOrLebesgueSquareSorter(this.points.ToArray()).ToList()).ToArray();

        DoTheJob(result, this.canvas5, this.title5, "U-Ordering");

        //******************************************* 06 
        result = OrderSFCOrderdPoints(PointOrdering.DiagonalLebesgueSorter(this.points.ToArray()).ToList()).ToArray();


        DoTheJob(result, this.canvas6, this.title6, "Variant of Lebesgue");

        //******************************************* 07 
        result = OrderSFCOrderdPoints(PointOrdering.MooreSorter(this.points.ToArray()).ToList()).ToArray();


        DoTheJob(result, this.canvas7, this.title7, "Variant of Moore");

        //******************************************* 08 
        result = OrderSFCOrderdPoints(PointOrdering.PeanoSorter(this.points.ToArray()).ToList()).ToArray();


        DoTheJob(result, this.canvas8, this.title8, "Peano");

        ////******************************************* 09 
        //result = OrderSFCOrderdPoints(PointOrdering.HosseinSorter(this.points.ToArray()).ToList()).ToArray();

        //DoTheJob(result, this.canvas9, this.title9, "K-d Tree (Hossein's SFC)");
    }

    private void DoTheJob(spatial.Point[] points, Canvas canvas, Label label, string labelString)
    {
        canvas.Children.Clear();

        Func<spatial.Point, spatial.Point, int> simpleFuncX = (p1, p2) => p1.X.CompareTo(p2.X);
        Func<spatial.Point, spatial.Point, int> simpleFuncY = (p1, p2) => p1.Y.CompareTo(p2.Y);
        List<Func<spatial.Point, spatial.Point, int>> comparers = new List<Func<spatial.Point, spatial.Point, int>> { simpleFuncX, simpleFuncY };

        int height = 0;

        var kdTree02 = new KdTree<spatial.Point>(points, comparers);

        Draw(canvas, kdTree02.Root, 0, 0, canvas.ActualHeight, 0, canvas.ActualWidth, ref height);

        label.Content = string.Format("{0}; Height={1}", labelString, height.ToString());
    }

    private List<spatial.Point> OrderSFCOrderdPoints(List<spatial.Point> points)
    {
        var middleIndex = points.Count / 2;

        List<spatial.Point> result = new List<spatial.Point>();

        var middle = points[middleIndex];

        result.Add(middle);

        if (middleIndex > 0)
        {
            result.AddRange(OrderSFCOrderdPoints(points.Take(middleIndex).ToList()));
        }
        if (middleIndex < points.Count - 1)
        {
            var length = points.Count - middleIndex - 1;

            result.AddRange(OrderSFCOrderdPoints(points.Skip(middleIndex + 1).Take(length).ToList()));
        }

        return result;
    }

    private void Draw(Canvas canvas, KdTreeNode<spatial.Point> node, int level, double y1, double y2, double x1, double x2, ref int height)
    {
        height = Math.Max(height, level);

        bool xBased = (level % 2 == 0);

        if (node.LeftChild != null)
        {
            if (xBased)
            {
                Draw(canvas, node.LeftChild, level + 1, y1, y2, x1, node.Point.X, ref height);
            }
            else
            {
                Draw(canvas, node.LeftChild, level + 1, y1, node.Point.Y, x1, x2, ref height);
            }

        }
        if (node.RightChild != null)
        {
            if (xBased)
            {
                Draw(canvas, node.RightChild, level + 1, y1, y2, node.Point.X, x2, ref height);
            }
            else
            {
                Draw(canvas, node.RightChild, level + 1, node.Point.Y, y2, x1, x2, ref height);
            }
        }

        Line line = new Line() { StrokeThickness = 2, Stroke = new SolidColorBrush(Colors.Green) };

        double x = node.Point.X; double y = node.Point.Y;

        if (xBased)
        {
            line.X1 = x; line.X2 = x; line.Y1 = y1; line.Y2 = y2;
        }
        else
        {
            line.X1 = x1; line.X2 = x2; line.Y1 = y; line.Y2 = y;
        }

        EllipseGeometry geometry = new EllipseGeometry(new Point(x, y), 3, 3);

        Path rectangle = new Path() { Data = geometry, Fill = new SolidColorBrush(Colors.Red) };

        canvas.Children.Add(line);

        canvas.Children.Add(rectangle);
    }

    private void canvas1_MouseMove(object sender, MouseEventArgs e)
    {
        Point currentPoint = e.GetPosition(this.canvas1);

        this.location.Content = string.Format("X:{0}, Y:{1}", currentPoint.X, currentPoint.Y);
    }

    private void Button_Click_2(object sender, RoutedEventArgs e)
    {
        this.points.Clear();

        this.canvas1.Children.Clear();
        this.canvas2.Children.Clear();
        this.canvas3.Children.Clear();
        this.canvas4.Children.Clear();
        this.canvas5.Children.Clear();
        this.canvas6.Children.Clear();
        this.canvas7.Children.Clear();
        this.canvas8.Children.Clear();
        //this.canvas9.Children.Clear();
    }
}
