using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

using IRI.Maptor.Core.Pdf;
using IRI.Maptor.Core.Spatial.IO.Dxf;
using IRI.Maptor.Presentation.Core.Models.MapText;
using IRI.Maptor.Presentation.Wpf.Controls.MapMarkers;
using IRI.Maptor.Presentation.Wpf.Converters;
using IRI.Maptor.Presentation.Wpf.ViewModels.Map;

using StaPoint = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Wpf.Helpers;

/// <summary>
/// Text-on-map labels for the PDF export, as vector shapes. The label is laid out by the real
/// <see cref="TextboxMarker"/> (a fresh copy, never shown), and its drawing — frame, text field,
/// glyphs, underline — is read back as geometry, so the printed label wraps, aligns and pads
/// exactly like the screen. Effects are not part of a drawing, so the halo is rebuilt from the
/// glyph outlines.
/// </summary>
public static class PdfMapTextHelper
{
    // the screen's halo is two stacked 3 px glows, dense only close to the glyphs; a crisp
    // outline of the full 2 px SLD radius (MapTextSld) prints noticeably heavier
    private const double HaloRadius = 1.5;

    // px; fine enough that curves stay smooth when the PDF is zoomed in
    private const double Tolerance = 0.05;

    // the halo's outer edge is a soft outline; a coarser curve there is invisible and much faster
    private const double HaloTolerance = 0.1;

    /// <summary>
    /// The label of <paramref name="viewModel"/> with its bottom-centre at <paramref name="anchor"/>
    /// (map coordinates); null for an empty label. Rotation and width come with the laid-out copy.
    /// </summary>
    public static PdfWriter.PdfMapText? BuildMapText(TextboxMarkerViewModel viewModel, StaPoint anchor)
    {
        if (string.IsNullOrWhiteSpace(viewModel?.LabelValue))
            return null;

        // a copy rather than the live marker: that one may be hovered (drag pill), focused (caret,
        // selection) or not on the canvas at all
        var marker = new TextboxMarker { DataContext = viewModel };

        marker.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        marker.Arrange(new Rect(marker.DesiredSize));
        marker.UpdateLayout();

        var width = marker.ActualWidth;
        var height = marker.ActualHeight;

        if (width <= 0 || height <= 0)
            return null;

        var shapes = new List<Shape>();

        Collect(marker, marker, 1.0, false, shapes);

        var parts = new List<(PathGeometry Outline, Color Color)>();
        var textGeometry = new GeometryGroup { FillRule = FillRule.Nonzero };
        var haloIndex = -1;

        // same-coloured neighbours become one outlined part: glyphs of one colour (and their
        // underline) merge, so overlapping joined letters leave no even-odd holes. Every group is
        // Nonzero (GeometryGroup defaults to EvenOdd): the text view draws each underline twice,
        // and an even-odd merge would cancel the pair out
        for (int i = 0; i < shapes.Count;)
        {
            var group = new GeometryGroup { FillRule = FillRule.Nonzero };
            var first = shapes[i];

            for (; i < shapes.Count && shapes[i].Color == first.Color && shapes[i].IsText == first.IsText; i++)
            {
                group.Children.Add(shapes[i].Geometry);

                if (shapes[i].IsText)
                    textGeometry.Children.Add(shapes[i].Geometry);
            }

            if (first.IsText && haloIndex < 0)
                haloIndex = parts.Count;

            AddPart(parts, group, first.Color);
        }

        if (viewModel.Formatting_Frame == MapTextFrame.Halo && textGeometry.Children.Count > 0 && haloIndex >= 0)
        {
            var pen = new Pen(Brushes.Black, 2 * HaloRadius) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

            // widening the merged outline, not the raw glyph runs, keeps a long label fast
            var textOutline = textGeometry.GetOutlinedPathGeometry(Tolerance, ToleranceType.Absolute);

            var halo = new GeometryGroup { FillRule = FillRule.Nonzero, Children = { textOutline, textOutline.GetWidenedPathGeometry(pen, HaloTolerance, ToleranceType.Absolute) } };

            var haloColor = HaloColorConverter.GetHaloColor(viewModel.Formatting_HaloColor, viewModel.Formatting_TextColor);

            var haloParts = new List<(PathGeometry Outline, Color Color)>();
            AddPart(haloParts, halo, haloColor);
            parts.InsertRange(haloIndex, haloParts);
        }

        return ToMapText(parts, anchor, new Point(width / 2, height));
    }

    // the block is the bounds of what is drawn: a rotated label (the turn is inside the marker)
    // reaches outside its upright layout box, so the anchor is placed within those bounds
    private static PdfWriter.PdfMapText? ToMapText(List<(PathGeometry Outline, Color Color)> parts, StaPoint anchor, Point anchorInMarker)
    {
        if (parts.Count == 0)
            return null;

        var bounds = Rect.Empty;

        foreach (var part in parts)
            bounds.Union(part.Outline.Bounds);

        if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
            return null;

        var mapText = new PdfWriter.PdfMapText
        {
            Anchor = anchor,
            Width = bounds.Width,
            Height = bounds.Height,
            AnchorX = anchorInMarker.X - bounds.Left,
            AnchorY = anchorInMarker.Y - bounds.Top,
        };

        foreach (var (outline, color) in parts)
        {
            var figures = PdfDecorationHelper.ToMarkerFigures(
                outline.GetFlattenedPathGeometry(Tolerance, ToleranceType.Absolute),
                p => new StaPoint(p.X - bounds.Left, p.Y - bounds.Top));

            if (figures.Count == 0)
                continue;

            mapText.Parts.Add(new PdfWriter.PdfMapTextPart
            {
                Shape = new PdfVectorLogo { Figures = figures, SourceWidth = bounds.Width, SourceHeight = bounds.Height },
                Color = new RgbColor(color.R, color.G, color.B, color.A),
            });
        }

        return mapText.IsValid ? mapText : null;
    }

    private readonly record struct Shape(Geometry Geometry, Color Color, bool IsText);

    // walks the visual tree in drawing order; the drag handle is an editing aid and is left out
    private static void Collect(Visual visual, TextboxMarker root, double opacity, bool inTextBox, List<Shape> shapes)
    {
        if (ReferenceEquals(visual, root.dragHandle))
            return;

        opacity *= VisualTreeHelper.GetOpacity(visual);

        if (opacity <= 0 || visual is UIElement { Visibility: not Visibility.Visible })
            return;

        inTextBox |= ReferenceEquals(visual, root.labelBox);

        if (VisualTreeHelper.GetDrawing(visual) is { } drawing)
        {
            var matrix = ReferenceEquals(visual, root) ? Matrix.Identity : (visual.TransformToAncestor(root) as Transform)?.Value ?? Matrix.Identity;

            // the text lines are the plain visuals inside the TextBox; its template parts
            // (field border, scroll host) are FrameworkElements
            AddDrawing(drawing, matrix, opacity, inTextBox && visual is not FrameworkElement, shapes);
        }

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
        {
            if (VisualTreeHelper.GetChild(visual, i) is Visual child)
                Collect(child, root, opacity, inTextBox, shapes);
        }
    }

    private static void AddDrawing(Drawing drawing, Matrix matrix, double opacity, bool isText, List<Shape> shapes)
    {
        switch (drawing)
        {
            case DrawingGroup group:
                var groupMatrix = group.Transform?.Value ?? Matrix.Identity;
                groupMatrix.Append(matrix);

                foreach (var child in group.Children)
                    AddDrawing(child, groupMatrix, opacity * group.Opacity, isText, shapes);
                break;

            case GlyphRunDrawing glyphs when glyphs.GlyphRun is not null:
                AddShape(glyphs.GlyphRun.BuildGeometry(), glyphs.ForegroundBrush, matrix, opacity, isText, shapes);
                break;

            case GeometryDrawing geometry when geometry.Geometry is not null:
                AddShape(geometry.Geometry, geometry.Brush, matrix, opacity, isText, shapes);

                if (geometry.Pen is { Thickness: > 0 } pen)
                    AddShape(geometry.Geometry.GetWidenedPathGeometry(pen, Tolerance, ToleranceType.Absolute), pen.Brush, matrix, opacity, isText, shapes);
                break;
        }
    }

    private static void AddShape(Geometry geometry, Brush? brush, Matrix matrix, double opacity, bool isText, List<Shape> shapes)
    {
        if (brush is not SolidColorBrush solid)
            return;

        var color = solid.Color;
        color.A = (byte)System.Math.Round(color.A * solid.Opacity * opacity);

        if (color.A == 0)
            return;

        shapes.Add(new Shape(new GeometryGroup { FillRule = FillRule.Nonzero, Children = { geometry }, Transform = new MatrixTransform(matrix) }, color, isText));
    }

    private static void AddPart(List<(PathGeometry Outline, Color Color)> parts, Geometry geometry, Color color)
    {
        var outline = geometry.GetOutlinedPathGeometry(Tolerance, ToleranceType.Absolute);

        if (!outline.IsEmpty())
            parts.Add((outline, color));
    }
}
