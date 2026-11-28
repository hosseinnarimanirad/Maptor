using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

using Strings = IRI.Maptor.Presentation.Core.Properties.Resources;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>The cases the drawing guide shows, one figure each.</summary>
public enum DrawingGuideFigureKind
{
    SnapEnd,
    SnapVertex,
    SnapIntersection,
    SnapMidpoint,
    SnapEdge,
    NoSnap,
    GuideExtension,
    GuidePerpendicular,
    AngleSteps,
    GuideFromLine,
    GuideParallel,
    CursorBox,
    TypedLength,
    MoveVertex,
    AddVertex,
    DeleteVertex,
    Finish,
}

/// <summary>
/// One figure of the drawing guide (Guides/Drawing*.xaml): a small picture of one case of snapping, the guides, the
/// length and angle box or vertex editing. It draws what the map draws in that case, with the same shapes and theme
/// brushes as <c>SnapMarker</c>, <c>GuideOverlay</c>, <c>CursorBox</c>, <c>VertexHandle</c> and <c>EdgeHandleMarker</c>,
/// on a plain tile instead of a base map. Existing features are grey, the shape being drawn or edited is in the accent
/// colour. It is a picture only: it takes no input and reads nothing from a map. Always left to right, so a figure
/// looks the same in a right-to-left page; the words in it follow the language.
/// </summary>
public sealed class DrawingGuideFigure : Canvas
{
    private const double TileWidth = 250;
    private const double TileHeight = 140;

    private const string Accent = "MahApps.Brushes.Accent";
    private const string Strong = "MahApps.Brushes.Highlight";
    private const string Light = "IRI.Maptor.Brushes.OnMap.HandleFill";
    private const string OnMapText = "IRI.Maptor.Brushes.OnMap.Text";
    private const string OnMapSurface = "IRI.Maptor.Brushes.OnMap.Surface";
    private const string OnMapBorder = "IRI.Maptor.Brushes.OnMap.Border";
    private const string Tile = "IRI.Maptor.Brushes.Surface.Canvas";
    private const string TileEdge = "IRI.Maptor.Brushes.Surface.Border";
    private const string Feature = "MahApps.Brushes.Gray5";
    private const string FeatureFill = "MahApps.Brushes.Gray9";
    private const string Faint = "MahApps.Brushes.Gray7";

    private static readonly FontFamily LatinFont = new FontFamily("Segoe UI");

    public DrawingGuideFigure()
    {
        Width = TileWidth;
        Height = TileHeight;
        FlowDirection = FlowDirection.LeftToRight;
        ClipToBounds = true;
        SnapsToDevicePixels = true;
        HorizontalAlignment = HorizontalAlignment.Center;

        Build();
    }

    public DrawingGuideFigureKind Kind
    {
        get => (DrawingGuideFigureKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public static readonly DependencyProperty KindProperty =
        DependencyProperty.Register(nameof(Kind), typeof(DrawingGuideFigureKind), typeof(DrawingGuideFigure),
            new PropertyMetadata(DrawingGuideFigureKind.SnapEnd, (d, _) => ((DrawingGuideFigure)d).Build()));

    private void Build()
    {
        Children.Clear();

        var tile = new Rectangle { Width = TileWidth, Height = TileHeight, RadiusX = 6, RadiusY = 6, StrokeThickness = 1 };
        Use(tile, Shape.FillProperty, Tile);
        Use(tile, Shape.StrokeProperty, TileEdge);
        Children.Add(tile);

        switch (Kind)
        {
            case DrawingGuideFigureKind.SnapEnd:
                ExistingLine(P(60, 118), P(120, 88), P(192, 70));
                Snap(SnapShape.End, from: P(38, 36), target: P(192, 70), Strings.sketchBar_snapping_kind_end);
                break;

            case DrawingGuideFigureKind.SnapVertex:
                ExistingPolygon(P(112, 42), P(204, 50), P(194, 112), P(122, 106));
                Snap(SnapShape.Vertex, from: P(30, 100), target: P(112, 42), Strings.sketchBar_snapping_kind_vertex, labelBelow: false);
                break;

            case DrawingGuideFigureKind.SnapIntersection:
                ExistingLine(P(70, 30), P(210, 110));
                ExistingLine(P(80, 115), P(200, 35));
                Snap(SnapShape.Intersection, from: P(28, 76), target: P(144, 72.3), Strings.sketchBar_snapping_kind_intersection);
                break;

            case DrawingGuideFigureKind.SnapMidpoint:
                ExistingLine(P(92, 112), P(212, 42));
                Snap(SnapShape.Midpoint, from: P(38, 38), target: P(152, 77), Strings.sketchBar_snapping_kind_midpoint);
                break;

            case DrawingGuideFigureKind.SnapEdge:
                ExistingPolygon(P(96, 112), P(120, 44), P(214, 34), P(224, 104));
                Snap(SnapShape.Edge, from: P(34, 44), target: P(107.3, 80), Strings.sketchBar_snapping_kind_edge);
                break;

            case DrawingGuideFigureKind.NoSnap:
                ExistingPolygon(P(112, 42), P(204, 50), P(194, 112), P(122, 106));
                Sketch(P(30, 100));
                Rubber(P(30, 100), P(104, 50));
                Handle(P(30, 100), HandleState.First);
                Pointer(P(104, 50));
                Keys(P(12, 112), "Ctrl");
                break;

            case DrawingGuideFigureKind.GuideExtension:
            {
                var (previous, last) = (P(36, 106), P(108, 76));
                var along = Unit(previous, last);
                var on = last + along * 72;
                Guide(last, last + along * 150);
                DrawnSegment(previous, last);
                Rubber(last, on);
                GuideMark(on);
                Pointer(on + new Vector(5, 7));
                Chip(Strings.sketchBar_guide_extension, on + new Vector(-34, -30));
                break;
            }

            case DrawingGuideFigureKind.GuidePerpendicular:
            {
                var (previous, last) = (P(40, 62), P(120, 92));
                var across = Normal(previous, last);
                var on = last + across * 52;
                Guide(last - across * 40, last + across * 100);
                DrawnSegment(previous, last);
                RightAngle(last, Unit(previous, last) * -1, across);
                Rubber(last, on);
                GuideMark(on);
                Pointer(on + new Vector(6, 6));
                Chip(Strings.sketchBar_guide_perpendicular, on + new Vector(16, -22));
                break;
            }

            case DrawingGuideFigureKind.AngleSteps:
            {
                var (previous, last) = (P(34, 104), P(100, 86));
                var baseAngle = Math.Atan2(last.Y - previous.Y, last.X - previous.X);

                foreach (var step in Enumerable.Range(-5, 11))
                {
                    var angle = baseAngle + step * Math.PI / 12;
                    var ray = new Line { X1 = last.X, Y1 = last.Y, X2 = last.X + 86 * Math.Cos(angle), Y2 = last.Y + 86 * Math.Sin(angle), StrokeThickness = 1 };
                    Use(ray, Shape.StrokeProperty, Faint);
                    Children.Add(ray);
                }

                var chosen = baseAngle - 3 * Math.PI / 12;
                var direction = new Vector(Math.Cos(chosen), Math.Sin(chosen));
                var on = last + direction * 70;
                Guide(last, last + direction * 96);
                DrawnSegment(previous, last);
                Rubber(last, on);
                GuideMark(on);
                Pointer(on + new Vector(8, 6));
                Chip("45°", on + new Vector(14, -20), latin: true);
                Keys(P(12, 112), "Shift");
                break;
            }

            case DrawingGuideFigureKind.GuideFromLine:
            {
                var (start, end) = (P(150, 28), P(222, 100));
                ExistingPolygon(start, end, P(236, 60), P(200, 22));
                Reference(start, end);
                var (previous, last) = (P(34, 124), P(92, 108));
                var foot = P(161, 39);
                Guide(last - Unit(last, foot) * 26, foot + Unit(last, foot) * 14);
                DrawnSegment(previous, last);
                Rubber(last, foot);
                RightAngle(foot, Unit(foot, last), Unit(start, end));
                GuideMark(foot);
                Pointer(foot + new Vector(9, 14));
                Chip(Strings.sketchBar_guide_perpendicular, P(24, 44));
                break;
            }

            case DrawingGuideFigureKind.GuideParallel:
            {
                var (start, end) = (P(118, 48), P(216, 26));
                ExistingLine(start, end);
                Reference(start, end);
                var (previous, last) = (P(30, 124), P(56, 96));
                var along = Unit(start, end);
                var on = last + along * 104;
                Guide(last - along * 24, last + along * 180);
                DrawnSegment(previous, last);
                Rubber(last, on);
                GuideMark(on);
                Pointer(on + new Vector(5, 7));
                Chip(Strings.sketchBar_guide_parallel, on + new Vector(-50, 14));
                break;
            }

            case DrawingGuideFigureKind.CursorBox:
            {
                var (previous, last, pointer) = (P(30, 108), P(96, 84), P(150, 52));
                DrawnSegment(previous, last);
                Rubber(last, pointer);
                Pointer(pointer);
                Box(pointer + new Vector(16, 14), ("L", "62.80 m", false), ("A", "50.7°", false), ("B", "59.3°", false));
                break;
            }

            case DrawingGuideFigureKind.TypedLength:
            {
                var (previous, last) = (P(30, 94), P(96, 70));
                var placed = last + Unit(last, P(150, 38)) * 44;
                DrawnSegment(previous, last);
                Rubber(last, placed);
                GuideMark(placed);
                Pointer(P(158, 32));
                Box(P(176, 46), ("L", "25", true), ("A", "50.7°", false), ("B", "59.3°", false));
                Keys(P(12, 112), "L", "2", "5", "Enter");
                break;
            }

            case DrawingGuideFigureKind.MoveVertex:
            {
                var (a, b, c, d) = (P(48, 104), P(72, 40), P(170, 34), P(196, 108));
                var moved = P(206, 58);
                Edited(closed: true, a, b, moved, d);
                Ghost(b, c, d);
                Arrow(c + new Vector(9, 6), moved + new Vector(-9, -6));
                Handle(a, HandleState.First);
                Handle(b, HandleState.Plain);
                Handle(d, HandleState.Plain);
                Handle(moved, HandleState.Selected);
                Pointer(moved + new Vector(2, 2));
                break;
            }

            case DrawingGuideFigureKind.AddVertex:
            {
                var (a, b, c) = (P(40, 104), P(124, 44), P(214, 96));
                var on = b + (c - b) * 0.5;
                Edited(closed: false, a, b, c);
                Handle(a, HandleState.First);
                Handle(b, HandleState.Plain);
                Handle(c, HandleState.Plain);
                EdgeHandle(on);
                Pointer(on + new Vector(4, 5), plus: true);
                break;
            }

            case DrawingGuideFigureKind.DeleteVertex:
            {
                var (a, b, c, d) = (P(44, 100), P(96, 42), P(150, 92), P(212, 50));
                Edited(closed: false, a, b, c, d);
                var after = new Line { X1 = b.X, Y1 = b.Y, X2 = d.X, Y2 = d.Y, StrokeThickness = 1.5, StrokeDashArray = new DoubleCollection { 3, 3 } };
                Use(after, Shape.StrokeProperty, Accent);
                Children.Add(after);
                Handle(a, HandleState.First);
                Handle(b, HandleState.Plain);
                Handle(d, HandleState.Plain);
                Handle(c, HandleState.Selected);
                Keys(P(12, 112), "Delete");
                break;
            }

            case DrawingGuideFigureKind.Finish:
            {
                var (a, b, c) = (P(40, 98), P(110, 40), P(190, 78));
                DrawnSegment(a, b);
                DrawnSegment(b, c, firstHandle: false);
                Handle(c, HandleState.Selected);
                Pointer(c + new Vector(2, 2));
                Keys(P(12, 112), "Enter");
                break;
            }
        }
    }

    #region what is on the map

    private void ExistingLine(params Point[] points)
    {
        Children.Add(Stroked(new Polyline { Points = new PointCollection(points), StrokeThickness = 2, StrokeLineJoin = PenLineJoin.Round }, Feature));

        foreach (var point in points)
            ExistingVertex(point);
    }

    private void ExistingPolygon(params Point[] points)
    {
        var polygon = Stroked(new Polygon { Points = new PointCollection(points), StrokeThickness = 2, StrokeLineJoin = PenLineJoin.Round }, Feature);
        Use(polygon, Shape.FillProperty, FeatureFill);
        Children.Add(polygon);

        foreach (var point in points)
            ExistingVertex(point);
    }

    // a small dot, so it is clear where an existing feature has a vertex
    private void ExistingVertex(Point point)
    {
        var dot = new Ellipse { Width = 5, Height = 5 };
        Use(dot, Shape.FillProperty, Feature);
        Place(dot, point, 5);
    }

    /// <summary>The start of a sketch: only its first point.</summary>
    private void Sketch(Point first) => Handle(first, HandleState.First);

    /// <summary>A segment already drawn, with its two points.</summary>
    private void DrawnSegment(Point from, Point to, bool firstHandle = true)
    {
        Children.Add(Stroked(new Line { X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y, StrokeThickness = 2 }, Accent));

        Handle(from, firstHandle ? HandleState.First : HandleState.Plain);
        Handle(to, HandleState.Plain);
    }

    /// <summary>The dashed segment from the last point to where the next one would go.</summary>
    private void Rubber(Point from, Point to)
        => Children.Insert(1, Stroked(new Line { X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y, StrokeThickness = 2, StrokeDashArray = new DoubleCollection { 4, 3 } }, Accent));

    /// <summary>A whole snap case: the sketch's point, the dashed segment onto the target, its marker, the pointer a little off it, and the kind's name.</summary>
    private void Snap(SnapShape shape, Point from, Point target, string name, bool labelBelow = true)
    {
        Children.Add(Stroked(new Line { X1 = from.X, Y1 = from.Y, X2 = target.X, Y2 = target.Y, StrokeThickness = 2, StrokeDashArray = new DoubleCollection { 4, 3 } }, Accent));
        Handle(from, HandleState.First);
        Marker(shape, target);
        Pointer(target + new Vector(7, 8));
        Chip(name, target + (labelBelow ? new Vector(-18, 26) : new Vector(14, -26)));
    }

    #endregion

    #region marks

    private enum SnapShape { End, Vertex, Intersection, Midpoint, Edge }

    private enum HandleState { Plain, First, Selected }

    // the snap markers, as SnapMarker draws them (12 px)
    private void Marker(SnapShape shape, Point at)
    {
        const double size = 12;

        if (shape == SnapShape.Intersection)
        {
            foreach (var (brush, thickness) in new[] { (Light, 5d), (Strong, 2.5d) })
            {
                var cross = new Path
                {
                    Data = Geometry.Parse($"M 1,1 L {size - 1},{size - 1} M {size - 1},1 L 1,{size - 1}"),
                    StrokeThickness = thickness,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Width = size,
                    Height = size,
                };
                Use(cross, Shape.StrokeProperty, brush);
                Place(cross, at, size);
            }

            return;
        }

        Shape mark = shape switch
        {
            SnapShape.Midpoint => new Polygon { Points = new PointCollection { P(size / 2, 1), P(size - 1, size - 1), P(1, size - 1) }, StrokeLineJoin = PenLineJoin.Round },
            SnapShape.Edge => new Ellipse(),
            _ => new Rectangle(),
        };

        mark.Width = size;
        mark.Height = size;
        mark.StrokeThickness = shape == SnapShape.End ? 1.5 : 2;
        Use(mark, Shape.FillProperty, shape == SnapShape.End ? Strong : Light);
        Use(mark, Shape.StrokeProperty, shape == SnapShape.End ? Light : Strong);
        Place(mark, at, size);
    }

    // a vertex handle, as VertexHandle draws it: a square, the first one bigger, the selected one filled
    private void Handle(Point at, HandleState state)
    {
        var size = state == HandleState.First ? 10d : 8d;
        var square = new Rectangle { Width = size, Height = size, StrokeThickness = state == HandleState.First ? 1.5 : 1 };
        Use(square, Shape.FillProperty, state == HandleState.Selected ? Strong : Light);
        Use(square, Shape.StrokeProperty, state == HandleState.Selected ? Light : OnMapText);
        Place(square, at, size);
    }

    // the diamond on an edge where a click adds a vertex, as EdgeHandleMarker draws it
    private void EdgeHandle(Point at) => Diamond(at, 10);

    // the small diamond where the point sits on a guide, as GuideOverlay draws it
    private void GuideMark(Point at) => Diamond(at, 9);

    private void Diamond(Point at, double size)
    {
        var half = size / 2;
        var diamond = new Polygon { Points = new PointCollection { P(half, 0), P(size, half), P(half, size), P(0, half) }, Width = size, Height = size, StrokeThickness = 1.5 };
        Use(diamond, Shape.FillProperty, Light);
        Use(diamond, Shape.StrokeProperty, Strong);
        Place(diamond, at, size);
    }

    // a guide: a light halo under a dashed line, as GuideOverlay draws it
    private void Guide(Point from, Point to)
    {
        var halo = new Line { X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y, StrokeThickness = 3, Opacity = .6 };
        Use(halo, Shape.StrokeProperty, Light);
        Children.Add(halo);

        Children.Add(Stroked(new Line { X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y, StrokeThickness = 1.3, StrokeDashArray = new DoubleCollection { 6, 4 } }, Strong));
    }

    // the existing line the guides are drawn from: highlighted
    private void Reference(Point from, Point to)
    {
        var halo = new Line { X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y, StrokeThickness = 7, Opacity = .7, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
        Use(halo, Shape.StrokeProperty, Light);
        Children.Add(halo);

        Children.Add(Stroked(new Line { X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y, StrokeThickness = 3, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round }, Strong));
    }

    // the little square of a right angle at a corner, between two unit directions
    private void RightAngle(Point corner, Vector first, Vector second)
    {
        const double side = 9;
        var a = corner + first * side;
        var b = a + second * side;
        var c = corner + second * side;

        Children.Add(Stroked(new Polyline { Points = new PointCollection { a, b, c }, StrokeThickness = 1 }, OnMapText));
    }

    #endregion

    #region the shape being edited

    private void Edited(bool closed, params Point[] points)
    {
        Shape shape = closed
            ? new Polygon { Points = new PointCollection(points) }
            : new Polyline { Points = new PointCollection(points) };

        shape.StrokeThickness = 2;
        shape.StrokeLineJoin = PenLineJoin.Round;
        Children.Add(Stroked(shape, Accent));
    }

    // where the shape was before the change
    private void Ghost(params Point[] points)
    {
        Children.Insert(1, Stroked(new Polyline { Points = new PointCollection(points), StrokeThickness = 1.5, StrokeDashArray = new DoubleCollection { 3, 3 } }, Feature));
    }

    private void Arrow(Point from, Point to)
    {
        var along = Unit(from, to);
        var across = new Vector(-along.Y, along.X);
        var head = new Polygon { Points = new PointCollection { to, to - along * 8 + across * 4, to - along * 8 - across * 4 } };
        Use(head, Shape.FillProperty, OnMapText);

        Children.Add(Stroked(new Line { X1 = from.X, Y1 = from.Y, X2 = to.X - along.X * 6, Y2 = to.Y - along.Y * 6, StrokeThickness = 1.5 }, OnMapText));
        Children.Add(head);
    }

    #endregion

    #region pointer, labels, keys

    // the mouse pointer, its tip at the point; with a plus for "a click adds a vertex here"
    private void Pointer(Point tip, bool plus = false)
    {
        var arrow = new Path
        {
            Data = Geometry.Parse("M 0,0 L 0,15 L 4,11.2 L 6.6,17 L 9,16 L 6.4,10.3 L 11.5,10.3 Z"),
            Fill = Brushes.Black,
            Stroke = Brushes.White,
            StrokeThickness = 1,
            StrokeLineJoin = PenLineJoin.Round,
        };

        SetLeft(arrow, tip.X);
        SetTop(arrow, tip.Y);
        Children.Add(arrow);

        if (!plus)
            return;

        var badge = new Path { Data = Geometry.Parse("M 4,0 L 4,8 M 0,4 L 8,4"), StrokeThickness = 2 };
        Use(badge, Shape.StrokeProperty, Strong);
        SetLeft(badge, tip.X + 13);
        SetTop(badge, tip.Y + 12);
        Children.Add(badge);
    }

    // the label beside a marker or a guide, as SnapMarker draws it
    private void Chip(string text, Point topLeft, bool latin = false)
    {
        var label = new TextBlock { Text = text, FontSize = 11 };
        Use(label, TextBlock.ForegroundProperty, OnMapText);

        if (latin)
            label.FontFamily = LatinFont;
        else
            label.SetResourceReference(TextBlock.FontFamilyProperty, "iranSans");

        var chip = new Border { Child = label, Padding = new Thickness(4, 1, 4, 1), CornerRadius = new CornerRadius(3), BorderThickness = new Thickness(1) };
        Use(chip, Border.BackgroundProperty, OnMapSurface);
        Use(chip, Border.BorderBrushProperty, OnMapBorder);

        // kept on the tile whatever the word's length in a language
        chip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        SetLeft(chip, Math.Max(4, Math.Min(topLeft.X, TileWidth - chip.DesiredSize.Width - 4)));
        SetTop(chip, Math.Max(4, Math.Min(topLeft.Y, TileHeight - chip.DesiredSize.Height - 4)));
        Children.Add(chip);
    }

    // the length / angle / bearing box beside the pointer, as CursorBox draws it; the typed field framed
    private void Box(Point topLeft, params (string Key, string Value, bool IsTyped)[] rows)
    {
        var panel = new StackPanel();

        foreach (var (key, value, isTyped) in rows)
        {
            var keyText = new TextBlock { Text = key, FontSize = 10.5, Opacity = .65, Width = 13, VerticalAlignment = VerticalAlignment.Center, FontFamily = LatinFont };
            Use(keyText, TextBlock.ForegroundProperty, OnMapText);

            var valueText = new TextBlock { Text = value, FontSize = 12, FontFamily = LatinFont };
            Use(valueText, TextBlock.ForegroundProperty, isTyped ? Strong : OnMapText);

            var frame = new Border { Child = valueText, Padding = new Thickness(2, 0, 2, 0), CornerRadius = new CornerRadius(2), BorderThickness = new Thickness(1), MinWidth = isTyped ? 44 : 0 };

            if (isTyped)
                Use(frame, Border.BorderBrushProperty, Strong);
            else
                frame.BorderBrush = Brushes.Transparent;

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(keyText);
            row.Children.Add(frame);
            panel.Children.Add(row);
        }

        var box = new Border { Child = panel, Padding = new Thickness(5, 2, 5, 3), CornerRadius = new CornerRadius(3), BorderThickness = new Thickness(1) };
        Use(box, Border.BackgroundProperty, OnMapSurface);
        Use(box, Border.BorderBrushProperty, OnMapBorder);
        SetLeft(box, topLeft.X);
        SetTop(box, topLeft.Y);
        Children.Add(box);
    }

    // the keys of the case, as key caps in the tile's corner
    private void Keys(Point topLeft, params string[] keys)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };

        foreach (var key in keys)
        {
            var text = new TextBlock { Text = key, FontSize = 11, FontFamily = LatinFont, FontWeight = FontWeights.SemiBold };
            text.SetResourceReference(TextBlock.ForegroundProperty, "MahApps.Brushes.ThemeForeground");

            var cap = new Border { Child = text, Padding = new Thickness(6, 1, 6, 2), Margin = new Thickness(0, 0, 3, 0), CornerRadius = new CornerRadius(3), BorderThickness = new Thickness(1, 1, 1, 2), MinWidth = 20 };
            text.HorizontalAlignment = HorizontalAlignment.Center;
            cap.SetResourceReference(Border.BackgroundProperty, "MahApps.Brushes.ThemeBackground");
            Use(cap, Border.BorderBrushProperty, Feature);
            row.Children.Add(cap);
        }

        SetLeft(row, topLeft.X);
        SetTop(row, topLeft.Y);
        Children.Add(row);
    }

    #endregion

    #region small helpers

    private static Point P(double x, double y) => new Point(x, y);

    private static Vector Unit(Point from, Point to)
    {
        var vector = to - from;
        vector.Normalize();

        return vector;
    }

    // the unit direction at 90° to from→to, the one pointing up the tile
    private static Vector Normal(Point from, Point to)
    {
        var along = Unit(from, to);
        var normal = new Vector(along.Y, -along.X);

        return normal.Y > 0 ? -normal : normal;
    }

    private static T Stroked<T>(T shape, string brush) where T : Shape
    {
        shape.SetResourceReference(Shape.StrokeProperty, brush);

        return shape;
    }

    private static void Use(FrameworkElement element, DependencyProperty property, string brush) => element.SetResourceReference(property, brush);

    // centres a mark of the given size on a point
    private void Place(FrameworkElement element, Point centre, double size)
    {
        SetLeft(element, centre.X - size / 2);
        SetTop(element, centre.Y - size / 2);
        Children.Add(element);
    }

    #endregion
}
