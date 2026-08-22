using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;

using IRI.Maptor.Core.Spatial.Analysis.SFC;
using spatial = IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Res.Sfc.View;

/// <summary>
/// Interaction logic for PointDistributionOrderingWindow.xaml
/// </summary>
public partial class PointDistributionOrderingWindow : Window
{
    public PointDistributionOrderingWindow()
    {
        InitializeComponent();
    }

    int level = 1;

    List<spatial.Point> points = new List<spatial.Point>();

    Boundary boundary;

    private void canvas1_MouseMove(object sender, MouseEventArgs e)
    {
        Point currentPoint = e.GetPosition(this.canvas1);

        this.location.Content = string.Format("X:{0}, Y:{1}", currentPoint.X, currentPoint.Y);
    }

    private void canvas1_MouseDown(object sender, MouseButtonEventArgs e)
    {
        Point newPoint = e.GetPosition(this.canvas1);

        this.points.Add(new spatial.Point(newPoint.X, newPoint.Y));

        this.boundary = PointOrdering.GetBoundary(points.ToArray(), 2);

        spatial.Point[] result1 = PointOrdering.HilbertSorter(points.ToArray());

        Referesh(result1, canvas1);

        spatial.Point[] result2 = PointOrdering.ZOrderingSorter(points.ToArray());

        Referesh(result2, canvas2);

        spatial.Point[] result3 = PointOrdering.GraySorter(points.ToArray());

        Referesh(result3, canvas3);

        spatial.Point[] result4 = PointOrdering.UOrderOrLebesgueSquareSorter(points.ToArray());

        Referesh(result4, canvas4);

        spatial.Point[] result5 = PointOrdering.DiagonalLebesgueSorter(points.ToArray());

        Referesh(result5, canvas5);

        spatial.Point[] result6 = PointOrdering.MooreSorter(points.ToArray());

        Referesh(result6, canvas6);

        spatial.Point[] result7 = PointOrdering.PeanoSorter(points.ToArray());

        Referesh(result7, canvas7);

        spatial.Point[] result8 = PointOrdering.Peano02Sorter(points.ToArray());

        Referesh(result8, canvas8);

        //spatial.Point[] result9 = PointOrdering.HosseinSorter(points.ToArray());

        //Referesh(result9, canvas9);
    }

    private void Referesh(spatial.Point[] points, Canvas canvas)
    {
        ClearCanvas(canvas, "data");

        PathGeometry geo = new PathGeometry();
        EllipseGeometry temp = new EllipseGeometry(new Point(points[0].X, points[0].Y), 4, 4);
        geo.AddGeometry(temp);

        for (int i = 1; i < points.Length; i++)
        {
            geo.AddGeometry(new EllipseGeometry(new Point(points[i].X, points[i].Y), 4, 4));

            Line line = new Line() { X1 = points[i - 1].X, X2 = points[i].X, Y1 = points[i - 1].Y, Y2 = points[i].Y, Stroke = Brushes.Green, StrokeThickness = 2, Opacity = .8, Tag = "data" };

            canvas.Children.Add(line);
        }

        Path path = new Path() { Data = geo, Fill = new SolidColorBrush(Colors.Red), Opacity = .9, Tag = "data" };

        canvas.Children.Add(path);
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        this.level++;

        RefereshGrids(this.boundary);
    }

    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        this.level--;

        RefereshGrids(this.boundary);
    }

    void RefereshGrids(Canvas canvas, int baseSize, Boundary boundary)
    {
        if (this.level < 0)
        {
            this.level = 0;
        }

        //double regionSize = Math.Max(canvas.ActualHeight, canvas.ActualWidth);
        double regionSize = Math.Max(boundary.Height, boundary.Width);

        ClearCanvas(canvas, "grid");

        for (int i = 1; i <= this.level; i++)
        {
            int numberOfLines = (int)Math.Pow(baseSize, i);

            double lineSpace = regionSize / numberOfLines;

            for (int j = 0; j < numberOfLines - 1; j++)
            {
                double temp = (j + 1) * lineSpace;

                Line verticalLine = new Line() { X1 = boundary.MinX + temp, Y1 = boundary.MinY, X2 = boundary.MinX + temp, Y2 = boundary.MinY + boundary.Height, Stroke = Brushes.Red, StrokeThickness = this.level - i + .5, Tag = "grid" };

                Line horizontalLine = new Line() { X1 = boundary.MinX, Y1 = boundary.MinY + temp, X2 = boundary.MinX + boundary.Width, Y2 = boundary.MinY + temp, Stroke = Brushes.Red, StrokeThickness = this.level - i + .5, Tag = "grid" };

                canvas.Children.Add(verticalLine);

                canvas.Children.Add(horizontalLine);

            }
        }
    }

    void ClearCanvas(Canvas canvas, string tag)
    {
        for (int i = canvas.Children.Count - 1; i >= 0; i--)
        {
            if (((Shape)canvas.Children[i]).Tag.Equals(tag))
            {
                canvas.Children.RemoveAt(i);
            }
        }
    }

    void RefereshGrids(Boundary boundary)
    {
        if (this.boundary.Height == 0)
        {
            return;
        }

        this.RefereshGrids(this.canvas1, 2, boundary);
        this.RefereshGrids(this.canvas2, 2, boundary);
        this.RefereshGrids(this.canvas3, 2, boundary);
        this.RefereshGrids(this.canvas4, 2, boundary);
        this.RefereshGrids(this.canvas5, 3, boundary);
        this.RefereshGrids(this.canvas6, 3, boundary);

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
        this.canvas5.Children.Clear();
        this.canvas6.Children.Clear();
        this.canvas7.Children.Clear();
        this.canvas8.Children.Clear();

    }
}
