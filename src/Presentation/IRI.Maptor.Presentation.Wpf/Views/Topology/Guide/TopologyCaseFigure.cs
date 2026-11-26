using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Wpf.Topology;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// One picture of a topology guide: a small map-like scene of points and lines showing one issue kind, or one fix
/// as before → after. Every figure speaks the Topology tab's own visual language, so what the guide shows is what
/// the map shows: an issue wears the overlay's severity colour (<see cref="TopologyOverlay"/>: error reddish purple,
/// warning orange, notice blue) and a fix the preview's colours (before grey and dashed, after green).
/// <para>
/// Drawn in code from a few primitives, so all cases share one style. The scene is designed left to right on a
/// 260 × 140 canvas; in a right-to-left page WPF mirrors it with the page and only the texts are turned back.
/// Colours follow the theme. Distances in the labels are examples.
/// </para>
/// </summary>
public sealed class TopologyCaseFigure : FrameworkElement
{
    public const double FigureWidth = 260;

    public const double FigureHeight = 140;

    public static readonly DependencyProperty CaseProperty = DependencyProperty.Register(
        nameof(Case), typeof(TopologyGuideCase), typeof(TopologyCaseFigure),
        new FrameworkPropertyMetadata(TopologyGuideCase.Legend, FrameworkPropertyMetadataOptions.AffectsRender));

    public TopologyGuideCase Case
    {
        get => (TopologyGuideCase)GetValue(CaseProperty);
        set => SetValue(CaseProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) => new(FigureWidth, FigureHeight);

    #region Palette

    private enum Role { First, Second, Faded, Before, After }

    private Brush _ink = Brushes.Black;
    private Brush _second = Brushes.Teal;
    private Brush _faded = Brushes.Gray;
    private Brush _paper = Brushes.White;
    private Brush _surface = Brushes.WhiteSmoke;
    private Brush _dot = Brushes.LightGray;
    private Brush _text = Brushes.DimGray;

    private static readonly Brush Error = TopologyOverlay.ErrorBrush;
    private static readonly Brush Warning = TopologyOverlay.WarningBrush;
    private static readonly Brush Notice = TopologyOverlay.InfoBrush;
    private static readonly Brush Before = TopologyOverlay.PreviewBeforeBrush;
    private static readonly Brush After = TopologyOverlay.PreviewAfterBrush;

    private Brush Themed(string key, Brush fallback) => TryFindResource(key) as Brush ?? fallback;

    private void ResolvePalette()
    {
        _ink = Themed("MahApps.Brushes.Gray1", new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)));
        _second = Themed("MahApps.Brushes.Accent", new SolidColorBrush(Color.FromRgb(0x00, 0x80, 0x80)));
        _faded = Themed("MahApps.Brushes.Gray6", new SolidColorBrush(Color.FromRgb(0xA8, 0xA8, 0xA8)));
        _paper = Themed("MahApps.Brushes.ThemeBackground", Brushes.White);
        _surface = Themed("MahApps.Brushes.Gray10", new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5)));
        _dot = Themed("MahApps.Brushes.Gray8", new SolidColorBrush(Color.FromRgb(0xD8, 0xD8, 0xD8)));
        _text = Themed("MahApps.Brushes.Gray3", new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)));
    }

    private Brush BrushOf(Role role) => role switch
    {
        Role.Second => _second,
        Role.Faded => _faded,
        Role.Before => Before,
        Role.After => After,
        _ => _ink,
    };

    #endregion

    #region Primitives

    private DrawingContext _dc = null!;

    /// <summary>Horizontal shift of what is drawn next: the second pane of a fix figure.</summary>
    private double _ox;

    private Point At(double x, double y) => new(x + _ox, y);

    private static Pen Stroke(Brush brush, double thickness, bool dashed = false)
    {
        var pen = new Pen(brush, thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
        };

        if (dashed)
        {
            pen.DashStyle = new DashStyle(new[] { 2.2, 2.2 }, 0);
            pen.DashCap = PenLineCap.Flat;
        }

        return pen;
    }

    /// <summary>A line through the given x, y pairs, with its vertices.</summary>
    private void Line(Role role, params double[] xy) => Line(role, 3, true, xy);

    private void Line(Role role, double thickness, bool vertices, params double[] xy)
    {
        var brush = BrushOf(role);
        var dashed = role == Role.Before;

        var geometry = new StreamGeometry();

        using (var g = geometry.Open())
        {
            g.BeginFigure(At(xy[0], xy[1]), false, false);

            for (int i = 2; i < xy.Length; i += 2)
                g.LineTo(At(xy[i], xy[i + 1]), true, true);
        }

        _dc.DrawGeometry(null, Stroke(brush, dashed ? 2 : thickness, dashed), geometry);

        if (!vertices || dashed)
            return;

        for (int i = 0; i < xy.Length; i += 2)
            Vertex(role, xy[i], xy[i + 1]);
    }

    private void Vertex(Role role, double x, double y)
        => _dc.DrawEllipse(_paper, new Pen(BrushOf(role), 1.6), At(x, y), 2.7, 2.7);

    /// <summary>A point feature: a small square, with an optional label above it.</summary>
    private void Pt(double x, double y, string? label = null, Role role = Role.First, bool below = false)
    {
        var brush = BrushOf(role);
        var dashed = role == Role.Before;

        _dc.DrawRoundedRectangle(_paper, Stroke(brush, 2, dashed), new Rect(At(x - 5.5, y - 5.5), new Size(11, 11)), 2, 2);

        if (label is not null)
            Text(x, below ? y + 9 : y - 22, label, role == Role.First ? _ink : brush, 11.5, true);
    }

    /// <summary>The issue's mark, as the overlay draws it: a light fill and a ring in the severity colour.</summary>
    private void Halo(Brush severity, double cx, double cy, double rx, double ry)
    {
        var color = ((SolidColorBrush)severity).Color;
        var fill = new SolidColorBrush(Color.FromArgb(0x38, color.R, color.G, color.B));

        _dc.DrawEllipse(fill, new Pen(severity, 2), At(cx, cy), rx, ry);
    }

    /// <summary>A measured distance: a thin line with end ticks and its value beside it.</summary>
    private void Dim(double x1, double y1, double x2, double y2, double metres, double labelDx = 0, double labelDy = 0)
    {
        var pen = new Pen(_text, 1);

        double dx = x2 - x1, dy = y2 - y1, length = Math.Sqrt(dx * dx + dy * dy);

        if (length < 1e-6)
            return;

        // unit normal, for the end ticks
        double nx = -dy / length * 3.5, ny = dx / length * 3.5;

        _dc.DrawLine(pen, At(x1, y1), At(x2, y2));
        _dc.DrawLine(pen, At(x1 - nx, y1 - ny), At(x1 + nx, y1 + ny));
        _dc.DrawLine(pen, At(x2 - nx, y2 - ny), At(x2 + nx, y2 + ny));

        Text((x1 + x2) / 2 + labelDx, (y1 + y2) / 2 + labelDy - 7, Metres(metres), _text, 10.5, false);
    }

    /// <summary>A text centred on x, its top at y. Turned back when the page mirrors the figure.</summary>
    private void Text(double x, double y, string text, Brush brush, double size, bool bold)
    {
        var typeface = new Typeface(
            (FontFamily)GetValue(System.Windows.Documents.TextElement.FontFamilyProperty),
            FontStyles.Normal,
            bold ? FontWeights.SemiBold : FontWeights.Normal,
            FontStretches.Normal);

        var formatted = new FormattedText(
            text,
            LocalizationManager.Instance.CurrentCulture ?? CultureInfo.CurrentUICulture,
            System.Windows.FlowDirection.LeftToRight,
            typeface,
            size,
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        var centre = At(x, y);

        if (FlowDirection == FlowDirection.RightToLeft)
        {
            _dc.PushTransform(new ScaleTransform(-1, 1, centre.X, 0));
            _dc.DrawText(formatted, new Point(centre.X - formatted.Width / 2, centre.Y));
            _dc.Pop();
        }
        else
        {
            _dc.DrawText(formatted, new Point(centre.X - formatted.Width / 2, centre.Y));
        }
    }

    /// <summary>"3 m", "0.3 m": the panel's unit and digits, without the padding zeros of its issue list.</summary>
    private static string Metres(double value)
    {
        var unit = TopologyMessages.QuantityText(1, ValueUnit.Metres);
        var space = unit.LastIndexOf(' ');

        return LocalizationManager.GetLocalizedNumberString(value.ToString("0.##", CultureInfo.InvariantCulture)) + (space < 0 ? string.Empty : unit.Substring(space));
    }

    private static string Number(int value) => LocalizationManager.GetLocalizedNumberString(value.ToString(CultureInfo.InvariantCulture));

    /// <summary>A round badge with a tick: correct data, or the result of a fix.</summary>
    private void Tick(double x, double y)
    {
        _dc.DrawEllipse(After, null, At(x, y), 8, 8);

        var geometry = new StreamGeometry();

        // a mirrored tick reads as an arrow: in a right-to-left page it is drawn the other way round, so it looks the same
        var side = FlowDirection == FlowDirection.RightToLeft ? -1 : 1;

        using (var g = geometry.Open())
        {
            g.BeginFigure(At(x - 3.6 * side, y + 0.2), false, false);
            g.LineTo(At(x - 1.0 * side, y + 2.9), true, true);
            g.LineTo(At(x + 3.8 * side, y - 2.6), true, true);
        }

        _dc.DrawGeometry(null, Stroke(Brushes.White, 1.8), geometry);
    }

    /// <summary>A small cross: the feature a fix removes, or a geometry that cannot be used.</summary>
    private void Cross(Brush brush, double x, double y, double half = 4.5)
    {
        var pen = Stroke(brush, 2.2);

        _dc.DrawLine(pen, At(x - half, y - half), At(x + half, y + half));
        _dc.DrawLine(pen, At(x - half, y + half), At(x + half, y - half));
    }

    /// <summary>An arrowhead on a line, pointing along (dx, dy): the drawing direction of a part.</summary>
    private void Head(Brush brush, double x, double y, double dx, double dy, double size = 6)
    {
        double length = Math.Sqrt(dx * dx + dy * dy);

        if (length < 1e-6)
            return;

        double ux = dx / length, uy = dy / length;

        var geometry = new StreamGeometry();

        using (var g = geometry.Open())
        {
            g.BeginFigure(At(x + ux * size, y + uy * size), true, true);
            g.LineTo(At(x - ux * size * 0.6 - uy * size * 0.75, y - uy * size * 0.6 + ux * size * 0.75), true, true);
            g.LineTo(At(x - ux * size * 0.6 + uy * size * 0.75, y - uy * size * 0.6 - ux * size * 0.75), true, true);
        }

        _dc.DrawGeometry(brush, null, geometry);
    }

    #endregion

    protected override void OnRender(DrawingContext drawingContext)
    {
        _dc = drawingContext;
        _ox = 0;

        ResolvePalette();

        // the "map": a soft card with a faint grid of dots
        _dc.DrawRoundedRectangle(_surface, null, new Rect(0, 0, FigureWidth, FigureHeight), 8, 8);

        for (double x = 20; x < FigureWidth; x += 20)
            for (double y = 20; y < FigureHeight; y += 20)
                _dc.DrawEllipse(_dot, null, new Point(x, y), 0.9, 0.9);

        _dc.PushClip(new RectangleGeometry(new Rect(0, 0, FigureWidth, FigureHeight), 8, 8));

        Scene(Case);

        _dc.Pop();
    }

    #region Scenes

    private void Scene(TopologyGuideCase guideCase)
    {
        switch (guideCase)
        {
            case TopologyGuideCase.Legend:
                Line(Role.First, 18, 34, 78, 34);
                Line(Role.Second, 18, 70, 78, 70);
                Pt(48, 106);
                Halo(Error, 128, 34, 11, 11);
                Halo(Warning, 128, 70, 11, 11);
                Halo(Notice, 128, 106, 11, 11);
                Line(Role.Before, 170, 34, 240, 34);
                Line(Role.After, 170, 70, 240, 70);
                Tick(205, 106);
                break;

            case TopologyGuideCase.GeometryInvalid:
                // a line whose geometry cannot be used: nothing to draw but its place
                Line(Role.First, 20, 96, 80, 70);
                Line(Role.Faded, 2.5, false, 80, 70, 130, 70);
                Line(Role.First, 180, 70, 240, 44);
                _dc.DrawLine(Stroke(Error, 2, true), At(84, 70), At(176, 70));
                Halo(Error, 130, 70, 20, 15);
                Cross(Error, 130, 70);
                break;

            // ---------------------------------------------------------------- points of one layer
            case TopologyGuideCase.PointsSamePlace:
                Line(Role.First, 3, false, 20, 78, 240, 78);
                Pt(50, 78);
                Pt(210, 78);
                Halo(Warning, 130, 76, 18, 18);
                Pt(128, 75);
                Pt(132, 80);
                break;

            case TopologyGuideCase.PointsClose:
                Line(Role.First, 3, false, 20, 84, 240, 84);
                Pt(46, 84);
                Pt(214, 84);
                Halo(Warning, 130, 84, 26, 17);
                Pt(120, 84);
                Pt(140, 84);
                Dim(120, 56, 140, 56, 0.3, 0, -9);
                break;

            case TopologyGuideCase.PointsChain:
                Line(Role.First, 3, false, 20, 84, 240, 84);
                Halo(Error, 130, 84, 52, 17);
                Pt(94, 84);
                Pt(118, 84);
                Pt(142, 84);
                Pt(166, 84);
                Dim(94, 56, 118, 56, 0.4, 0, -9);
                Dim(142, 56, 166, 56, 0.4, 0, -9);
                break;

            // ---------------------------------------------------------------- points on lines
            case TopologyGuideCase.PointNearLine:
                Line(Role.First, 3, false, 20, 96, 240, 96);
                Pt(50, 96);
                Pt(210, 96);
                Halo(Error, 130, 50, 16, 16);
                Pt(130, 50);
                Dim(160, 50, 160, 96, 3, 22, 0);
                break;

            case TopologyGuideCase.PointFarFromLines:
                Line(Role.First, 3, false, 20, 116, 120, 104, 240, 116);
                Pt(60, 111);
                Pt(190, 111);
                _dc.DrawEllipse(null, Stroke(_faded, 1.4, true), At(150, 44), 40, 34);
                Halo(Error, 150, 44, 16, 16);
                Pt(150, 44);
                break;

            case TopologyGuideCase.LineEndNotCovered:
                Line(Role.First, 3, false, 26, 78, 226, 78);
                Pt(26, 78);
                Pt(96, 78);
                Pt(166, 78);
                Halo(Warning, 226, 78, 15, 15);
                Vertex(Role.First, 226, 78);
                break;

            case TopologyGuideCase.LineTooFewPoints:
                Halo(Warning, 130, 76, 110, 17);
                Line(Role.First, 30, 76, 230, 76);
                Pt(112, 76);
                break;

            case TopologyGuideCase.PointOnSeveralLines:
                Line(Role.First, 3, false, 20, 96, 130, 70);
                Line(Role.Second, 3, false, 130, 70, 240, 44);
                Line(Role.Faded, 3, false, 130, 70, 220, 118);
                Pt(60, 86.5);
                Pt(190, 56);
                Pt(182, 98);
                Halo(Notice, 130, 70, 16, 16);
                Pt(130, 70);
                Tick(152, 30);
                break;

            // ---------------------------------------------------------------- numbers along a line
            case TopologyGuideCase.SequenceDuplicate:
                Numbered(Error, new[] { 11, 12, 12, 13 }, 1, 2);
                break;

            case TopologyGuideCase.SequenceOutOfOrder:
                Numbered(Error, new[] { 11, 13, 12, 14 }, 1, 2);
                break;

            case TopologyGuideCase.SequenceGap:
                Numbered(Notice, new[] { 11, 12, 14, 15 }, 1, 2);
                break;

            case TopologyGuideCase.SequenceMissing:
                Numbered(Warning, new[] { 11, 12, -1, 14 }, 2, 2);
                break;

            case TopologyGuideCase.SequenceDirectionUndecided:
                Numbered(Warning, new[] { 12, 14, 12, 14 }, 0, 3);
                Head(_text, 62, 122, -1, 0, 5);
                _dc.DrawLine(new Pen(_text, 1.2), At(66, 122), At(194, 122));
                Head(_text, 198, 122, 1, 0, 5);
                break;

            // ---------------------------------------------------------------- line ends
            case TopologyGuideCase.EndUnsnapped:
                Line(Role.First, 20, 92, 124, 72);
                Line(Role.Second, 136, 66, 240, 46);
                Halo(Warning, 130, 69, 17, 14);
                Dim(124, 96, 136, 96, 0.3, 0, 12);
                break;

            case TopologyGuideCase.EndOvershoot:
                Line(Role.First, 20, 62, 240, 62);
                Line(Role.Second, 130, 124, 130, 48);
                Halo(Warning, 130, 54, 13, 14);
                Dim(152, 48, 152, 62, 1, 20, 0);
                break;

            case TopologyGuideCase.EndUndershoot:
                Line(Role.First, 20, 44, 240, 44);
                Line(Role.Second, 130, 124, 130, 64);
                Halo(Warning, 130, 55, 13, 15);
                Dim(152, 44, 152, 64, 2, 20, 0);
                break;

            case TopologyGuideCase.EndFree:
                Line(Role.First, 20, 100, 110, 86, 170, 60);
                Halo(Notice, 170, 60, 15, 15);
                Vertex(Role.First, 170, 60);
                _dc.DrawEllipse(null, Stroke(_faded, 1.4, true), At(170, 60), 44, 40);
                break;

            case TopologyGuideCase.EndChain:
                Line(Role.First, 20, 70, 110, 70);
                Line(Role.Second, 122, 70, 134, 70, 240, 44);
                Line(Role.Faded, 146, 78, 240, 108);
                Halo(Error, 128, 73, 30, 16);
                Vertex(Role.First, 110, 70);
                Vertex(Role.Second, 122, 70);
                Vertex(Role.Faded, 146, 78);
                break;

            // ---------------------------------------------------------------- parts of one line
            case TopologyGuideCase.PartsGap:
                Line(Role.First, 20, 92, 112, 74);
                Line(Role.First, 148, 66, 240, 48);
                Halo(Warning, 130, 70, 28, 15);
                Dim(112, 100, 148, 93, 6, 0, 13);
                break;

            case TopologyGuideCase.PartsBranch:
                Line(Role.First, 20, 70, 130, 70);
                Line(Role.First, 130, 70, 240, 40);
                Line(Role.First, 130, 70, 236, 110);
                Halo(Warning, 130, 70, 15, 15);
                Vertex(Role.First, 130, 70);
                break;

            case TopologyGuideCase.PartsOutOfOrder:
                Line(Role.First, 20, 80, 90, 80);
                Line(Role.First, 90, 80, 170, 80);
                Line(Role.First, 170, 80, 240, 80);
                Halo(Notice, 167, 80, 82, 16);
                Text(55, 42, Number(1), _ink, 12.5, true);
                Text(130, 42, Number(3), Notice, 12.5, true);
                Text(205, 42, Number(2), Notice, 12.5, true);
                break;

            case TopologyGuideCase.PartsReversed:
                Line(Role.First, 20, 80, 90, 80);
                Line(Role.First, 90, 80, 170, 80);
                Line(Role.First, 170, 80, 240, 80);
                Halo(Notice, 130, 80, 46, 17);
                Head(_ink, 55, 80, 1, 0, 7);
                Head(Notice, 130, 80, -1, 0, 7);
                Head(_ink, 205, 80, 1, 0, 7);
                break;

            // ---------------------------------------------------------------- lines of one layer
            case TopologyGuideCase.LinesDuplicate:
                Halo(Error, 130, 70, 118, 26);
                Line(Role.Second, 9, false, 24, 88, 110, 64, 236, 54);
                Line(Role.First, 24, 88, 110, 64, 236, 54);
                break;

            case TopologyGuideCase.LinesSharedStretch:
                Halo(Warning, 130, 70, 62, 17);
                Line(Role.Second, 9, false, 44, 22, 80, 70, 180, 70, 216, 22);
                Line(Role.First, 20, 70, 240, 70);
                break;

            case TopologyGuideCase.TouchAndReturn:
                Line(Role.First, 20, 70, 240, 70);
                Line(Role.Second, 44, 22, 130, 78, 216, 22);
                Halo(Notice, 130, 72, 20, 13);
                break;

            // ---------------------------------------------------------------- fixes
            case TopologyGuideCase.FixMoveVertex:
                Panes(
                    () =>
                    {
                        Line(Role.First, 8, 100, 56, 96, 104, 100);
                        Halo(Error, 56, 62, 14, 14);
                        Pt(56, 62);
                    },
                    () =>
                    {
                        Line(Role.Before, 8, 100, 56, 96, 104, 100);
                        Line(Role.After, 8, 100, 56, 62, 104, 100);
                        Pt(56, 62);
                    });
                break;

            case TopologyGuideCase.FixInsertVertex:
                Panes(
                    () =>
                    {
                        Line(Role.First, 8, 98, 104, 98);
                        Halo(Error, 56, 64, 14, 14);
                        Pt(56, 64);
                    },
                    () =>
                    {
                        Line(Role.Before, 8, 98, 104, 98);
                        Line(Role.After, 8, 98, 56, 64, 104, 98);
                        Pt(56, 64);
                    });
                break;

            case TopologyGuideCase.FixSnapEnds:
                Panes(
                    () =>
                    {
                        Line(Role.First, 8, 92, 50, 76);
                        Line(Role.Second, 62, 66, 104, 50);
                        Halo(Warning, 56, 71, 15, 13);
                    },
                    () =>
                    {
                        Line(Role.First, 8, 92, 50, 76);
                        Line(Role.Before, 62, 66, 104, 50);
                        Line(Role.After, 50, 76, 104, 50);
                    });
                break;

            case TopologyGuideCase.FixTrimOvershoot:
                Panes(
                    () =>
                    {
                        Line(Role.First, 8, 60, 104, 60);
                        Line(Role.Second, 56, 122, 56, 44);
                        Halo(Warning, 56, 51, 12, 13);
                    },
                    () =>
                    {
                        Line(Role.First, 8, 60, 104, 60);
                        Line(Role.Before, 56, 60, 56, 44);
                        Line(Role.After, 56, 122, 56, 60);
                    });
                break;

            case TopologyGuideCase.FixExtendAlong:
                Panes(
                    () =>
                    {
                        Line(Role.First, 8, 44, 104, 44);
                        Line(Role.Second, 56, 122, 56, 66);
                        Halo(Warning, 56, 56, 12, 14);
                    },
                    () =>
                    {
                        Line(Role.First, 8, 44, 104, 44);
                        Line(Role.After, 56, 122, 56, 44);
                    });
                break;

            case TopologyGuideCase.FixJoinNearest:
                Panes(
                    () =>
                    {
                        Line(Role.First, 30, 30, 30, 122);
                        Line(Role.Second, 104, 108, 64, 64);
                        Halo(Warning, 50, 64, 18, 13);
                    },
                    () =>
                    {
                        Line(Role.First, 30, 30, 30, 122);
                        Line(Role.Second, 104, 108, 64, 64);
                        Line(Role.After, 64, 64, 30, 64);
                    });
                break;

            case TopologyGuideCase.FixDeleteDuplicatePoint:
                Panes(
                    () =>
                    {
                        Line(Role.First, 3, false, 8, 80, 104, 80);
                        Halo(Warning, 56, 80, 22, 16);
                        Pt(48, 80);
                        Pt(64, 80);
                    },
                    () =>
                    {
                        Line(Role.First, 3, false, 8, 80, 104, 80);
                        Pt(64, 80, null, Role.Before);
                        Cross(Before, 64, 60, 3.5);
                        Pt(48, 80, null, Role.After);
                    });
                break;

            case TopologyGuideCase.FixDeleteDuplicateLine:
                Panes(
                    () =>
                    {
                        Halo(Error, 56, 72, 52, 22);
                        Line(Role.Second, 9, false, 8, 86, 50, 66, 104, 60);
                        Line(Role.First, 8, 86, 50, 66, 104, 60);
                    },
                    () =>
                    {
                        Line(Role.After, 8, 86, 50, 66, 104, 60);
                        Line(Role.Before, 8, 100, 50, 80, 104, 74);
                        Cross(Before, 56, 96, 3.5);
                    });
                break;
        }
    }

    /// <summary>Four numbered points on a line; the points from <paramref name="first"/> to <paramref name="last"/> carry the issue.</summary>
    private void Numbered(Brush severity, int[] numbers, int first, int last)
    {
        double[] xs = { 40, 100, 160, 220 };
        const double y = 84;

        Line(Role.First, 3, false, 16, y, 244, y);

        Halo(severity, (xs[first] + xs[last]) / 2, y - 6, Math.Min((xs[last] - xs[first]) / 2 + 20, 106), 27);

        for (int i = 0; i < numbers.Length; i++)
        {
            var marked = i >= first && i <= last;
            var label = numbers[i] < 0 ? "?" : Number(numbers[i]);

            _dc.DrawRoundedRectangle(_paper, Stroke(_ink, 2), new Rect(At(xs[i] - 5.5, y - 5.5), new Size(11, 11)), 2, 2);

            Text(xs[i], y - 26, label, marked ? severity : _ink, 12, true);
        }
    }

    /// <summary>A fix: the data as found, an arrow, the data after the fix.</summary>
    private void Panes(Action before, Action after)
    {
        _ox = 10;
        before();

        // the arrow between the panes
        _ox = 0;
        _dc.DrawLine(Stroke(_text, 1.8), At(121, 70), At(137, 70));
        Head(_text, 137, 70, 1, 0, 5.5);

        _ox = 138;
        after();

        Tick(100, 22);

        _ox = 0;
    }

    #endregion
}
