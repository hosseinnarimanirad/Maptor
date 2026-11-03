using System;
using System.Windows;
using System.Windows.Controls;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// One row of segments (tabs) that fills the available width, each segment as wide as its content
/// plus an equal share of the space left over. When the content is wider than the row, the
/// segments shrink in proportion to their content, so a long label is not cut off before a short one.
/// </summary>
public class SegmentPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0, height = 0;

        foreach (UIElement child in InternalChildren)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            width += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new Size(double.IsInfinity(availableSize.Width) ? width : Math.Min(width, availableSize.Width), height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var children = InternalChildren;
        if (children.Count == 0)
            return finalSize;

        double content = 0;
        foreach (UIElement child in children)
            content += child.DesiredSize.Width;

        double spare = finalSize.Width - content;
        double x = 0;

        foreach (UIElement child in children)
        {
            double width = spare >= 0
                ? child.DesiredSize.Width + spare / children.Count
                : content > 0 ? child.DesiredSize.Width * finalSize.Width / content : finalSize.Width / children.Count;

            child.Arrange(new Rect(x, 0, width, finalSize.Height));
            x += width;
        }

        return finalSize;
    }
}
