using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using IRI.Maptor.Presentation.Wpf.Models.LayerSettingsAdmin;
using IRI.Maptor.Presentation.Wpf.ViewModels.LayerSettingsAdmin;

namespace IRI.Maptor.Presentation.Wpf.Controls.LayerSettingsAdmin;

/// <summary>
/// Master–detail editor of the server's layer settings; see <see cref="ServerLayerSettingsViewModel"/>.
/// The code here is view-only: drag-and-drop in the tree (the view model decides what may go where),
/// right-click selection, keyboard shortcuts and focus.
/// </summary>
public partial class ServerLayerSettingsView : UserControl
{
    private ServerLayerSettingsViewModel? _viewModel;

    private Point? _dragStart;

    private ServerLayerSettingItem? _dragItem;

    private DropMarkerAdorner? _marker;

    public ServerLayerSettingsView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>Moves keyboard focus to the search box.</summary>
    public void FocusSearch() => filterBox.Focus();

    /// <summary>Shows the General tab and puts the cursor in the title, all of it selected (new group, Rename).</summary>
    public void FocusTitle()
    {
        tabs.SelectedItem = generalTab;

        Dispatcher.BeginInvoke(() =>
        {
            titleBox.Focus();
            titleBox.SelectAll();
        }, DispatcherPriority.Input);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.RequestFocusTitle = null;
        }

        _viewModel = DataContext as ServerLayerSettingsViewModel;

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.RequestFocusTitle = FocusTitle;
        }
    }

    // A group has only the General tab; a hidden tab that stays selected would still show its content.
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ServerLayerSettingsViewModel.SelectedItem) && _viewModel?.SelectedItem is { IsGroup: true })
            tabs.SelectedItem = generalTab;
    }

    #region Tree: keyboard, right click, Move to

    // a style row is mostly its title box, which takes the click: select the row then, for good
    private void OnStyleItemPreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is ListBoxItem item)
            item.IsSelected = true;
    }

    private void OnTreePreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_viewModel is null)
            return;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        ICommand? command = (key, Keyboard.Modifiers) switch
        {
            (Key.Up, ModifierKeys.Alt) => _viewModel.MoveUpCommand,
            (Key.Down, ModifierKeys.Alt) => _viewModel.MoveDownCommand,
            (Key.F2, ModifierKeys.None) => _viewModel.RenameCommand,
            (Key.Delete, ModifierKeys.None) => _viewModel.DeleteGroupCommand,
            _ => null,
        };

        if (command is null)
            return;

        if (command.CanExecute(null))
            command.Execute(null);

        e.Handled = true;
    }

    private void OnTreePreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ItemAt(e.OriginalSource) is { } container)
        {
            container.IsSelected = true;
            container.Focus();
        }
    }

    private void OnMoveToButtonClick(object sender, RoutedEventArgs e)
    {
        if (moveToButton.ContextMenu is not { } menu)
            return;

        menu.PlacementTarget = moveToButton;
        menu.Placement = PlacementMode.Top;
        menu.DataContext = DataContext;
        menu.IsOpen = true;
    }

    #endregion

    #region Tree: drag and drop

    private void OnTreePreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // not from the expander arrow or a scroll bar: only a press on a row starts a drag
        _dragItem = e.OriginalSource is DependencyObject source && FindAncestor<ToggleButton>(source) is null && FindAncestor<ScrollBar>(source) is null
            ? ItemAt(source)?.DataContext as ServerLayerSettingItem
            : null;

        _dragStart = _dragItem is null ? null : e.GetPosition(tree);
    }

    private void OnTreePreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragStart is not Point start || _dragItem is null || e.LeftButton != MouseButtonState.Pressed || _viewModel is null || _viewModel.IsBusy)
            return;

        var delta = e.GetPosition(tree) - start;

        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        var item = _dragItem;
        _dragStart = null;
        _dragItem = null;

        try
        {
            DragDrop.DoDragDrop(tree, new DataObject(typeof(ServerLayerSettingItem), item), DragDropEffects.Move);
        }
        finally
        {
            RemoveMarker();
        }
    }

    private void OnTreeDragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.None;
        e.Handled = true;

        AutoScroll(e);

        if (!TryGetDrop(e, out var container, out var target, out var position, out var dragged))
        {
            RemoveMarker();
            return;
        }

        e.Effects = DragDropEffects.Move;
        ShowMarker(container!, position);
    }

    private void OnTreeDragLeave(object sender, DragEventArgs e)
    {
        // DragLeave also fires when moving between rows; only a pointer outside the tree removes the marker
        var p = e.GetPosition(tree);

        if (p.X < 0 || p.Y < 0 || p.X > tree.ActualWidth || p.Y > tree.ActualHeight)
            RemoveMarker();
    }

    private void OnTreeDrop(object sender, DragEventArgs e)
    {
        RemoveMarker();

        if (TryGetDrop(e, out _, out var target, out var position, out var dragged))
            _viewModel!.Drop(dragged!, target!, position);

        e.Handled = true;
    }

    /// <summary>
    /// The row under the pointer and where the dragged row would land: on a group's middle half a layer
    /// goes into it; otherwise the upper or lower half of the row means before or after it.
    /// </summary>
    private bool TryGetDrop(DragEventArgs e, out TreeViewItem? container, out ServerLayerSettingItem? target,
        out ServerLayerDropPosition position, out ServerLayerSettingItem? dragged)
    {
        position = ServerLayerDropPosition.Before;
        target = null;
        dragged = e.Data.GetData(typeof(ServerLayerSettingItem)) as ServerLayerSettingItem;
        container = ItemAt(e.OriginalSource);

        if (_viewModel is null || dragged is null || container?.DataContext is not ServerLayerSettingItem over)
            return false;

        target = over;

        // the row's own line, not the children under an expanded group
        var header = container.Template.FindName("PART_Header", container) as FrameworkElement ?? container;
        var y = e.GetPosition(header).Y;
        var height = Math.Max(1, header.ActualHeight);

        if (over.IsGroup && dragged.IsLayer && y > height * 0.25 && y < height * 0.75)
            position = ServerLayerDropPosition.Into;
        else
            position = y < height / 2 ? ServerLayerDropPosition.Before : ServerLayerDropPosition.After;

        // a layer dropped just below an expanded group belongs at the top of that group
        if (position == ServerLayerDropPosition.After && over.IsGroup && over.IsExpanded && over.Children.Count > 0 && dragged.IsLayer)
            position = ServerLayerDropPosition.Into;

        return _viewModel.CanDrop(dragged, over, position);
    }

    private void AutoScroll(DragEventArgs e)
    {
        if (FindDescendant<ScrollViewer>(tree) is not { } scroll)
            return;

        var y = e.GetPosition(tree).Y;

        if (y < 24)
            scroll.LineUp();
        else if (y > tree.ActualHeight - 24)
            scroll.LineDown();
    }

    private void ShowMarker(TreeViewItem container, ServerLayerDropPosition position)
    {
        var header = container.Template.FindName("PART_Header", container) as FrameworkElement ?? container;

        if (_marker is not null && ReferenceEquals(_marker.AdornedElement, header))
        {
            _marker.Position = position;
            return;
        }

        RemoveMarker();

        var layer = AdornerLayer.GetAdornerLayer(header);

        if (layer is null)
            return;

        _marker = new DropMarkerAdorner(header, (Brush)FindResource("MahApps.Brushes.Accent")) { Position = position };
        layer.Add(_marker);
    }

    private void RemoveMarker()
    {
        if (_marker is null)
            return;

        AdornerLayer.GetAdornerLayer(_marker.AdornedElement)?.Remove(_marker);
        _marker = null;
    }

    #endregion

    #region Helpers

    private static TreeViewItem? ItemAt(object? source) => source is DependencyObject d ? FindAncestor<TreeViewItem>(d) : null;

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
                return match;

            current = current is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
        }

        return null;
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);

            if (child is T match)
                return match;

            if (FindDescendant<T>(child) is { } found)
                return found;
        }

        return null;
    }

    /// <summary>The drop marker: a line above or below the row, or a frame around a group being dropped into.</summary>
    private sealed class DropMarkerAdorner : Adorner
    {
        private readonly Pen _pen;

        private ServerLayerDropPosition _position;

        public DropMarkerAdorner(UIElement adorned, Brush brush) : base(adorned)
        {
            _pen = new Pen(brush, 2);
            _pen.Freeze();
            IsHitTestVisible = false;
        }

        public ServerLayerDropPosition Position
        {
            get => _position;
            set
            {
                if (_position == value)
                    return;

                _position = value;
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var size = AdornedElement.RenderSize;

            switch (Position)
            {
                case ServerLayerDropPosition.Into:
                    drawingContext.DrawRoundedRectangle(null, _pen, new Rect(1, 1, Math.Max(0, size.Width - 2), Math.Max(0, size.Height - 2)), 4, 4);
                    break;

                case ServerLayerDropPosition.Before:
                    DrawLine(drawingContext, 1, size.Width);
                    break;

                default:
                    DrawLine(drawingContext, size.Height - 1, size.Width);
                    break;
            }
        }

        // a line with a dot at its start, as file managers draw an insertion point
        private void DrawLine(DrawingContext drawingContext, double y, double width)
        {
            drawingContext.DrawEllipse(_pen.Brush, null, new Point(4, y), 3, 3);
            drawingContext.DrawLine(_pen, new Point(4, y), new Point(width, y));
        }
    }

    #endregion
}
