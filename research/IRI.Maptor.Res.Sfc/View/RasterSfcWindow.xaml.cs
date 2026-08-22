using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

using IRI.Maptor.Res.Sfc.View.Components;
using IRI.Maptor.Core.Spatial.Analysis.SFC;
using spatial = IRI.Maptor.Core.Common.Primitives;
using sfc = IRI.Maptor.Core.Spatial.Analysis.SFC;
using Transform = IRI.Maptor.Core.Spatial.Analysis.SFC.Transform;

namespace IRI.Maptor.Res.Sfc.View;

/// <summary>
/// Interaction logic for RasterSfcWindow.xaml
/// </summary>
public partial class RasterSfcWindow : Window
{
    public RasterSfcWindow()
    {
        InitializeComponent();

        this.sFCControl1.DoTheJob += DoTheJob;

        this.rotate180.Tag = Transforms.Rotate180;

        this.rotate90Ccw.Tag = Transforms.Rotate90CW;

        this.rotate90Cw.Tag = Transforms.Rotate90CCW;

        this.reflectY.Tag = Transforms.HorizontalReflection;

        this.reflectX.Tag = Transforms.VerticalReflection;

        this.dictionary.Add(Transforms.Rotate90CW, this.rotate90Ccw);
        this.dictionary.Add(Transforms.Rotate90CCW, this.rotate90Cw);
        this.dictionary.Add(Transforms.Rotate180, this.rotate180);
        this.dictionary.Add(Transforms.HorizontalReflection, this.reflectY);
        this.dictionary.Add(Transforms.VerticalReflection, this.reflectX);

        this.mtfOrder.Add(new spatial.Point(0, 0), this.mTF1);
        this.mtfOrder.Add(new spatial.Point(0, 1), this.mTF2);
        this.mtfOrder.Add(new spatial.Point(1, 0), this.mTF3);
        this.mtfOrder.Add(new spatial.Point(1, 1), this.mTF4);
    }

    MTF currentMTF;

    Dictionary<Transform, System.Windows.Controls.Primitives.ToggleButton> dictionary = new Dictionary<Transform, System.Windows.Controls.Primitives.ToggleButton>();

    Dictionary<spatial.Point, MTF> mtfOrder = new Dictionary<spatial.Point, MTF>();

    private void defineBMFs_Click(object sender, RoutedEventArgs e)
    {
        this.defineBMFs.IsEnabled = false;

        sFCControl1.Define();
    }

    private void DoTheJob()
    {
        for (int i = 0; i < 3; i++)
        {
            List<Move> moves = sFCControl1.GetMoves();

            this.mTF1.SetMoves(moves);
            this.mTF2.SetMoves(moves);
            this.mTF3.SetMoves(moves);
            this.mTF4.SetMoves(moves);
        }

        DrawTheCurve();
    }

    private void reset_Click(object sender, RoutedEventArgs e)
    {
        this.defineBMFs.IsEnabled = true;

        sFCControl1.Reset();

        this.mTF1.Reset();
        this.mTF2.Reset();
        this.mTF3.Reset();
        this.mTF4.Reset();
    }

    private void mTF_MouseDown(object sender, MouseButtonEventArgs e)
    {
        mTF1.ChangeState(false);
        mTF2.ChangeState(false);
        mTF3.ChangeState(false);
        mTF4.ChangeState(false);

        this.currentMTF = (MTF)sender;

        this.currentMTF.ChangeState(true);

        SetMTFEvents(false);

        SetMTFChecked(false);

        for (int i = 0; i < this.currentMTF.TransformCount; i++)
        {
            this.dictionary[this.currentMTF.GetTransform(i)].IsChecked = true;
        }


        SetMTFEvents(true);
    }

    private void rotate90Cw_Checked(object sender, RoutedEventArgs e)
    {
        if (object.Equals(this.currentMTF, null))
        {
            e.Handled = false;

            return;
        }

        System.Windows.Controls.Primitives.ToggleButton temp = (System.Windows.Controls.Primitives.ToggleButton)sender;

        if (temp.IsChecked == false)
        {
            this.currentMTF.RemoveTransform((Transform)temp.Tag);
        }
        else
        {
            this.currentMTF.AddTransform((Transform)temp.Tag);
        }

        DrawTheCurve();
    }

    private void SetMTFEvents(bool hasEvent)
    {
        if (hasEvent)
        {
            this.rotate180.Checked += rotate90Cw_Checked;
            this.rotate90Ccw.Checked += rotate90Cw_Checked;
            this.rotate90Cw.Checked += rotate90Cw_Checked;
            this.reflectX.Checked += rotate90Cw_Checked;
            this.reflectY.Checked += rotate90Cw_Checked;

            this.rotate180.Unchecked += rotate90Cw_Checked;
            this.rotate90Ccw.Unchecked += rotate90Cw_Checked;
            this.rotate90Cw.Unchecked += rotate90Cw_Checked;
            this.reflectX.Unchecked += rotate90Cw_Checked;
            this.reflectY.Unchecked += rotate90Cw_Checked;
        }
        else
        {
            this.rotate180.Checked -= rotate90Cw_Checked;
            this.rotate90Ccw.Checked -= rotate90Cw_Checked;
            this.rotate90Cw.Checked -= rotate90Cw_Checked;
            this.reflectX.Checked -= rotate90Cw_Checked;
            this.reflectY.Checked -= rotate90Cw_Checked;

            this.rotate180.Unchecked -= rotate90Cw_Checked;
            this.rotate90Ccw.Unchecked -= rotate90Cw_Checked;
            this.rotate90Cw.Unchecked -= rotate90Cw_Checked;
            this.reflectX.Unchecked -= rotate90Cw_Checked;
            this.reflectY.Unchecked -= rotate90Cw_Checked;
        }
    }

    private void SetMTFChecked(bool isChecked)
    {
        this.rotate180.IsChecked = isChecked;
        this.rotate90Ccw.IsChecked = isChecked;
        this.rotate90Cw.IsChecked = isChecked;
        this.reflectX.IsChecked = isChecked;
        this.reflectY.IsChecked = isChecked;
    }

    private void DrawTheCurve()
    {
        int level = int.Parse(this.level.SelectedValuePath.ToString()) + 1;

        int size = (int)Math.Pow(2, level);

        List<sfc.Move> moves = this.sFCControl1.GetMoves();

        if (moves.Count < 1)
        {
            return;
        }

        sfc.SpaceFillingCurve path = new sfc.SpaceFillingCurve(2, moves, this.GetMTFs());

        List<spatial.Point> pnts = SpaceFillingCurve.ConstructRasterSFC(size, size, path);

        this.canvas1.Children.Clear();

        //PathGeometry geo = new PathGeometry();
        //EllipseGeometry temp = new EllipseGeometry(new Point(pnts[0].X * 25 + 5, pnts[0].Y * 25 + 5), 4, 4);
        //geo.AddGeometry(temp);

        for (int i = 1; i < pnts.Count; i++)
        {
            //geo.AddGeometry(new EllipseGeometry(new Point(pnts[i].X * 25 + 5, pnts[i].Y * 25 + 5), 4, 4));

            Line line = new Line()
            {
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 4,
                X1 = pnts[i - 1].X * 25 + 5,
                X2 = pnts[i].X * 25 + 5,
                Y1 = pnts[i - 1].Y * 25 + 5,
                Y2 = pnts[i].Y * 25 + 5,
                StrokeEndLineCap = PenLineCap.Triangle,
                StrokeStartLineCap = PenLineCap.Round,
                Opacity = .4,
                Tag = "line"
            };

            this.canvas1.Children.Add(line);

            Canvas.SetZIndex(line, 1);
        }

        //Path tempPath = new Path() { Opacity = .4, Data = geo, Fill = new SolidColorBrush(Colors.Red), Stroke = new SolidColorBrush(Colors.Red), StrokeThickness = 2, Tag = "data" };

        //this.canvas1.Children.Add(tempPath);

        //Canvas.SetZIndex(tempPath, 0);
    }

    private List<sfc.Transform> GetMTFs()
    {
        spatial.Point[] points = sFCControl1.GetPoints();

        List<sfc.Transform> result = new List<sfc.Transform>();

        foreach (spatial.Point item in points)
        {
            var key = this.mtfOrder.Keys.First(i => i.Equals(item));

            result.Add(this.mtfOrder[key].GetCombinedTransform());
            //result.Add(this.mtfOrder[item].GetCombinedTransform());
        }

        return result;
    }

    private void level_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DrawTheCurve();
    }
}
