using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Shapes;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis.SFC;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Res.Sfc.View.Components;

/// <summary>
/// Interaction logic for MTF.xaml
/// </summary>
public partial class MTF : UserControl
{
    bool isChoosen = false;

    List<Transform> transforms = new List<Transform>();

    List<Move> moves;

    public MTF()
    {
        InitializeComponent();
    }

    public void SetMoves(List<Move> moves)
    {
        this.moves = moves;

        Refresh();
    }

    public void Reset()
    {
        this.layoutCanvas.Children.Clear();

        this.moves.Clear();

        this.transforms.Clear();
    }

    public void ChangeState(bool isChoosenState)
    {
        isChoosen = isChoosenState;

        if (isChoosen == true)
        {
            this.scaleTransform.ScaleX = 1.2;
            this.scaleTransform.ScaleY = 1.2;
            this.Opacity = 1;
        }
        else
        {
            this.scaleTransform.ScaleX = 1;
            this.scaleTransform.ScaleY = 1;
            this.Opacity = .7;
        }
    }

    public void AddTransform(Transform transform)
    {
        this.transforms.Add(transform);

        Refresh();
    }

    public void RemoveTransform(Transform transform)
    {
        this.transforms.Remove(transform);

        Refresh();
    }

    public int TransformCount { get { return this.transforms.Count; } }

    public Transform GetTransform(int index)
    {
        return this.transforms[index];
    }

    public Transform GetCombinedTransform()
    {
        if (this.transforms.Count < 1)
        {
            return Transforms.DoNothing;
        }
        else if (this.transforms.Count == 1)
        {
            return this.transforms[0];
        }
        else
        {
            Transform result = this.transforms[0];

            for (int i = 1; i < this.transforms.Count; i++)
            {
                result = Transforms.CompositeTransform(result, this.transforms[i]);
            }

            return result;
        }
    }

    private void Refresh()
    {
        this.layoutCanvas.Children.Clear();

        List<Point> points = this.GetPoints();

        for (int i = 1; i < points.Count; i++)
        {
            Line line = new Line()
            {
                Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black),
                StrokeThickness = 15,
                X1 = points[i - 1].X * 45 + 15,
                X2 = points[i].X * 45 + 15,
                Y1 = points[i - 1].Y * 45 + 15,
                Y2 = points[i].Y * 45 + 15,
                StrokeEndLineCap = System.Windows.Media.PenLineCap.Triangle,
                StrokeStartLineCap = System.Windows.Media.PenLineCap.Round,
                Opacity = .7,
                Tag = "line"
            };

            this.layoutCanvas.Children.Add(line);

            Canvas.SetZIndex(line, 0);
        }

    }

    private Point GetStartPoint(Move[] moves)
    {
        Point point = new Point(0, 0);

        double deltaX = 0, deltaY = 0;

        for (int i = 0; i < moves.Length - 1; i++)
        {
            point = moves[i](point, 1);

            deltaX = Math.Min(point.X, deltaX);

            deltaY = Math.Min(point.Y, deltaY);
        }

        return new Point(-deltaX, -deltaY);
    }

    private Move[] GetMoves(List<Move> moves)
    {
        List<Move> result = new List<Move>();

        foreach (Move item in moves)
        {
            Move move = item;

            for (int i = 0; i < this.transforms.Count; i++)
            {
                move = this.transforms[i](move);
            }

            result.Add(move);
        }

        return result.ToArray();
    }

    private List<Point> GetPoints()
    {
        Move[] movements = GetMoves(this.moves);

        Point startPoint = GetStartPoint(movements);

        List<Point> result = [startPoint];

        for (int i = 0; i < movements.Length; i++)
        {
            startPoint = movements[i](startPoint, 1);

            result.Add(startPoint);
        }

        return result;
    }
}
