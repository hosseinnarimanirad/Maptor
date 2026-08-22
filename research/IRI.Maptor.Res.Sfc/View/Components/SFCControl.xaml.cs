using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis.SFC;
using IRI.Maptor.Core.Spatial.Primitives;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IRI.Maptor.Res.Sfc.View.Components;

/// <summary>
/// Interaction logic for SFCControl.xaml
/// </summary>
public partial class SFCControl : UserControl
{
    public SFCControl()
    {
        InitializeComponent();
    }

    List<Point> points = new List<Point>();

    List<Line> lines = new List<Line>();

    public void Reset()
    {
        for (int i = this.layoutCanvas.Children.Count - 1; i >= 0; i--)
        {
            if (this.layoutCanvas.Children[i].GetType() == typeof(Line))
            {
                this.layoutCanvas.Children.RemoveAt(i);
            }
        }

        this.ellipse00.MouseDown -= Ellipse_MouseDown;
        this.ellipse01.MouseDown -= Ellipse_MouseDown;
        this.ellipse10.MouseDown -= Ellipse_MouseDown;
        this.ellipse11.MouseDown -= Ellipse_MouseDown;

        this.points.Clear();

        this.lines.Clear();

    }

    public void Define()
    {
        this.points = new List<Point>();

        for (int i = this.layoutCanvas.Children.Count - 1; i >= 0; i--)
        {
            if (this.layoutCanvas.Children[i].GetType() == typeof(Line))
            {
                this.layoutCanvas.Children.RemoveAt(i);
            }
        }

        this.ellipse00.MouseDown -= Ellipse_MouseDown;
        this.ellipse01.MouseDown -= Ellipse_MouseDown;
        this.ellipse10.MouseDown -= Ellipse_MouseDown;
        this.ellipse11.MouseDown -= Ellipse_MouseDown;

        this.ellipse00.MouseDown += Ellipse_MouseDown;
        this.ellipse01.MouseDown += Ellipse_MouseDown;
        this.ellipse10.MouseDown += Ellipse_MouseDown;
        this.ellipse11.MouseDown += Ellipse_MouseDown;
    }

    private void Ellipse_MouseDown(object sender, MouseButtonEventArgs e)
    {
        ((Ellipse)sender).MouseDown -= Ellipse_MouseDown;

        string[] temp = (((Ellipse)sender).Tag).ToString().Split(',');

        this.points.Add(new Point(int.Parse(temp[0]), int.Parse(temp[1])));

        int i = this.points.Count;

        if (i > 1)
        {
            Line line = new Line()
            {
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 10,
                X1 = this.points[i - 2].X * 45 + 15,
                X2 = this.points[i - 1].X * 45 + 15,
                Y1 = this.points[i - 2].Y * 45 + 15,
                Y2 = this.points[i - 1].Y * 45 + 15,
                StrokeEndLineCap = PenLineCap.Triangle,
                StrokeStartLineCap = PenLineCap.Round,
                Opacity = .7,
                Tag = "line"
            };

            this.layoutCanvas.Children.Add(line);

            Canvas.SetZIndex(line, 0);

            this.lines.Add(line);
        }

        if (i == 4)
        {
            DoTheJob();
        }
    }

    public delegate void DoTheJobEventHandler();

    public event DoTheJobEventHandler DoTheJob;

    //private Line GetLine(int index)
    //{
    //    Line line = new Line()
    //    {
    //        Stroke = this.lines[index].Stroke,
    //        StrokeThickness = this.lines[index].StrokeThickness,
    //        X1 = this.lines[index].X1,
    //        X2 = this.lines[index].X2,
    //        Y1 = this.lines[index].Y1,
    //        Y2 = this.lines[index].Y2,
    //        StrokeStartLineCap = this.lines[index].StrokeStartLineCap,
    //        StrokeEndLineCap = this.lines[index].StrokeEndLineCap,
    //        //Opacity = this.lines[index].Opacity,
    //        Tag = "line"
    //    };

    //    return line;
    //}

    public Line[] GetLines()
    {
        return this.lines.ToArray();
    }

    public List<Move> GetMoves()
    {
        List<Move> result = new List<Move>();

        for (int i = 1; i < points.Count; i++)
        {
            double dx = points[i].X - points[i - 1].X;
            double dy = points[i].Y - points[i - 1].Y;

            result.Add((p, step) => new Point(p.X + step * dx,
                                                                            p.Y + step * dy));
        }

        return result;
    }

    public Point[] GetPoints()
    {
        return this.points.ToArray();
    }
}
