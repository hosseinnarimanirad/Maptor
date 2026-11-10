using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IRI.Maptor.Presentation.Wpf.Helpers;

/// <summary>Which grid definition beside a <see cref="GridSplitter"/> a double-click resets.</summary>
public enum GridSplitterResetSide
{
    /// <summary>The column left of the splitter (right of it in right-to-left), or the row above it.</summary>
    Previous,

    /// <summary>The column right of the splitter (left of it in right-to-left), or the row below it.</summary>
    Next,
}

/// <summary>
/// Double-click on a <see cref="GridSplitter"/> gives one of the two panels it separates its default size back:
/// <code>
/// &lt;GridSplitter Grid.Column="1" Style="{StaticResource IRI.Maptor.Styles.GridSplitter.Panel.Vertical}"
///               helpers:GridSplitterReset.Length="400" helpers:GridSplitterReset.Side="Previous"/&gt;
/// </code>
/// The direction is the splitter's <see cref="GridSplitter.ResizeDirection"/>; with <c>Auto</c> it is worked out
/// as GridSplitter itself does (a splitter that is not stretched horizontally, or is narrower than tall, resizes
/// columns). Columns and rows are counted in the grid's own order, so in a right-to-left layout "previous" is
/// still the column before the splitter in XAML. Nothing happens while <see cref="LengthProperty"/> is not set.
/// </summary>
public static class GridSplitterReset
{
    public static readonly DependencyProperty LengthProperty = DependencyProperty.RegisterAttached(
        "Length", typeof(double), typeof(GridSplitterReset), new PropertyMetadata(double.NaN, OnLengthChanged));

    /// <summary>The pixel width (columns) or height (rows) the definition goes back to.</summary>
    public static double GetLength(DependencyObject d) => (double)d.GetValue(LengthProperty);

    public static void SetLength(DependencyObject d, double value) => d.SetValue(LengthProperty, value);

    public static readonly DependencyProperty SideProperty = DependencyProperty.RegisterAttached(
        "Side", typeof(GridSplitterResetSide), typeof(GridSplitterReset), new PropertyMetadata(GridSplitterResetSide.Previous));

    public static GridSplitterResetSide GetSide(DependencyObject d) => (GridSplitterResetSide)d.GetValue(SideProperty);

    public static void SetSide(DependencyObject d, GridSplitterResetSide value) => d.SetValue(SideProperty, value);

    private static void OnLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not GridSplitter splitter)
            return;

        // remove first, so setting the length twice does not hook the handler twice
        splitter.MouseDoubleClick -= Splitter_MouseDoubleClick;

        if (e.NewValue is double length && !double.IsNaN(length) && length > 0)
            splitter.MouseDoubleClick += Splitter_MouseDoubleClick;
    }

    private static void Splitter_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is GridSplitter splitter && Reset(splitter))
            e.Handled = true;
    }

    /// <summary>Applies the reset; false when the splitter is not in a grid or has no definition on that side.</summary>
    public static bool Reset(GridSplitter splitter)
    {
        var length = GetLength(splitter);

        if (double.IsNaN(length) || length <= 0 || splitter.Parent is not Grid grid)
            return false;

        var side = GetSide(splitter);

        if (ResizesColumns(splitter))
        {
            var index = side == GridSplitterResetSide.Previous
                ? Grid.GetColumn(splitter) - 1
                : Grid.GetColumn(splitter) + Grid.GetColumnSpan(splitter);

            if (index < 0 || index >= grid.ColumnDefinitions.Count)
                return false;

            var column = grid.ColumnDefinitions[index];
            column.Width = new GridLength(System.Math.Clamp(length, column.MinWidth, column.MaxWidth));
        }
        else
        {
            var index = side == GridSplitterResetSide.Previous
                ? Grid.GetRow(splitter) - 1
                : Grid.GetRow(splitter) + Grid.GetRowSpan(splitter);

            if (index < 0 || index >= grid.RowDefinitions.Count)
                return false;

            var row = grid.RowDefinitions[index];
            row.Height = new GridLength(System.Math.Clamp(length, row.MinHeight, row.MaxHeight));
        }

        return true;
    }

    // GridSplitter.GetEffectiveResizeDirection, which is internal
    private static bool ResizesColumns(GridSplitter splitter)
    {
        if (splitter.ResizeDirection != GridResizeDirection.Auto)
            return splitter.ResizeDirection == GridResizeDirection.Columns;

        if (splitter.HorizontalAlignment != HorizontalAlignment.Stretch)
            return true;

        if (splitter.VerticalAlignment != VerticalAlignment.Stretch)
            return false;

        return splitter.ActualWidth <= splitter.ActualHeight;
    }
}
