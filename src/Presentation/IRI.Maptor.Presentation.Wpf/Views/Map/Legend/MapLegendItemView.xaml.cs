using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;

using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.Models.Legend;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.ViewModels;
using IRI.Maptor.Presentation.Core.Layers;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// Interaction logic for MapLegendItemWithOptions.xaml
/// </summary>
public partial class MapLegendItemView : UserControl//, IDisposable, INotifyPropertyChanged
{
    //private readonly System.Windows.Threading.DispatcherTimer _popupCloseTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };

    // fires on the same click as the bound MoveLayerUp/DownCommand; after the command has
    // moved the row (live sort), the cursor is placed back on this layer's chevron so the
    // user can click repeatedly without chasing the row
    private void MoveLayerButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
            ReorderCursorFollower.FollowAfterReorder(button, this);
    }

    #region Drag & drop reordering (within the same parent scope; both TOC and drawing panel)

    private const string LayerDragFormat = "IRI.Maptor.Presentation.LayerReorder";

    private Point? _dragStart;

    // the element mouse events are routed to while the button is held: both legends wrap
    // each row in a selection RadioButton (ButtonBase) that CAPTURES the mouse on press,
    // so from that instant this row — a descendant of the capturing element — receives no
    // mouse events at all. The drag threshold must therefore be watched on the wrapper.
    private UIElement? _dragScope;

    private DropIndicatorAdorner? _dropIndicator;

    private static bool IsReorderable(object? dataContext)
        => dataContext is ILayer layer && (layer.CanReorderInToc || layer is DrawingItemLayer);

    private void Row_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DisarmDrag();

        if (!IsReorderable(DataContext))
            return;

        // do not hijack presses on the row's interactive controls (chevrons, visibility
        // checkbox, …). The walk stops at this row, so the selection RadioButton wrapping
        // it is unaffected.
        var node = e.OriginalSource as DependencyObject;

        while (node is not null && !ReferenceEquals(node, this))
        {
            if (node is System.Windows.Controls.Primitives.ButtonBase
                or System.Windows.Controls.Primitives.TextBoxBase
                or System.Windows.Controls.Primitives.RangeBase)
                return;

            node = node is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(node)
                : LogicalTreeHelper.GetParent(node);
        }

        _dragStart = e.GetPosition(this);

        _dragScope = FindAncestor<System.Windows.Controls.Primitives.ButtonBase>(this) as UIElement ?? this;

        _dragScope.PreviewMouseMove += Scope_PreviewMouseMove;
        _dragScope.PreviewMouseLeftButtonUp += Scope_PreviewMouseLeftButtonUp;
    }

    private void Scope_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragStart is not Point start || e.LeftButton != MouseButtonState.Pressed)
        {
            DisarmDrag();
            return;
        }

        var position = e.GetPosition(this);

        // standard system drag threshold: a plain click (select, double-click expand)
        // never turns into a drag
        if (Math.Abs(position.X - start.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(position.Y - start.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        DisarmDrag();

        if (DataContext is not ILayer layer)
            return;

        // the wrapper still holds the capture from its press; release it so the OLE drag
        // owns the mouse and the wrapper's pending click is cancelled
        Mouse.Capture(null);

        DragDrop.DoDragDrop(this, new DataObject(LayerDragFormat, layer), DragDropEffects.Move);
    }

    private void Scope_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => DisarmDrag();

    private void DisarmDrag()
    {
        _dragStart = null;

        if (_dragScope is null)
            return;

        _dragScope.PreviewMouseMove -= Scope_PreviewMouseMove;
        _dragScope.PreviewMouseLeftButtonUp -= Scope_PreviewMouseLeftButtonUp;

        _dragScope = null;
    }

    private void Row_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.None;
        e.Handled = true;

        if (GetValidDropSource(e) is null)
        {
            ClearDropIndicator();
            return;
        }

        e.Effects = DragDropEffects.Move;

        ShowDropIndicator(top: e.GetPosition(this).Y < ActualHeight / 2);

        AutoScrollOnDrag(e);
    }

    private void Row_Drop(object sender, DragEventArgs e)
    {
        var insertAbove = e.GetPosition(this).Y < ActualHeight / 2;

        ClearDropIndicator();

        e.Handled = true;

        if (GetValidDropSource(e) is not ILayer dragged || DataContext is not ILayer target)
            return;

        if (FindAncestorViewModel() is MapViewModelBase presenter)
            presenter.ReorderLayerByDrag(dragged, target, insertAbove);
    }

    private ILayer? GetValidDropSource(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(LayerDragFormat))
            return null;

        if (e.Data.GetData(LayerDragFormat) is not ILayer dragged || DataContext is not ILayer target)
            return null;

        if (ReferenceEquals(dragged, target))
            return null;

        // within-parent reordering only — same rule the view model enforces
        if (!ReferenceEquals(dragged.Parent, target.Parent))
            return null;

        var bothToc = dragged.CanReorderInToc && target.CanReorderInToc && dragged.TocGroup == target.TocGroup;

        var bothDrawing = dragged is DrawingItemLayer && target is DrawingItemLayer;

        return bothToc || bothDrawing ? dragged : null;
    }

    private void ShowDropIndicator(bool top)
    {
        if (_dropIndicator is not null && _dropIndicator.IsTop == top)
            return;

        ClearDropIndicator();

        if (AdornerLayer.GetAdornerLayer(this) is not AdornerLayer layer)
            return;

        _dropIndicator = new DropIndicatorAdorner(this, top);

        layer.Add(_dropIndicator);
    }

    private void ClearDropIndicator()
    {
        if (_dropIndicator is null)
            return;

        AdornerLayer.GetAdornerLayer(this)?.Remove(_dropIndicator);

        _dropIndicator = null;
    }

    private void AutoScrollOnDrag(DragEventArgs e)
    {
        var scrollViewer = FindAncestor<ScrollViewer>(this);

        if (scrollViewer is null)
            return;

        const double edge = 24;
        const double step = 16;

        var y = e.GetPosition(scrollViewer).Y;

        if (y < edge)
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - step);
        else if (y > scrollViewer.ActualHeight - edge)
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + step);
    }

    private MapViewModelBase? FindAncestorViewModel()
    {
        var node = VisualTreeHelper.GetParent(this);

        while (node is not null)
        {
            if (node is FrameworkElement { DataContext: MapViewModelBase viewModel })
                return viewModel;

            node = VisualTreeHelper.GetParent(node);
        }

        return null;
    }

    private static T? FindAncestor<T>(DependencyObject start) where T : DependencyObject
    {
        var node = VisualTreeHelper.GetParent(start);

        while (node is not null)
        {
            if (node is T match)
                return match;

            node = VisualTreeHelper.GetParent(node);
        }

        return null;
    }

    #endregion

    public MapLegendItemView()
    {
        InitializeComponent();

        AllowDrop = true;

        PreviewMouseLeftButtonDown += Row_PreviewMouseLeftButtonDown;

        // rows are virtualized with recycling: never leave a scope subscription or an
        // adorner behind when the container is torn down mid-gesture
        Unloaded += (_, _) => { DisarmDrag(); ClearDropIndicator(); };

        DragOver += Row_DragOver;
        DragLeave += (_, _) => ClearDropIndicator();
        Drop += Row_Drop;
        //_popupCloseTimer.Tick += (_, _) =>
        //{
        //    _popupCloseTimer.Stop();
        //    if (pendingChangesPopup != null)
        //        pendingChangesPopup.IsOpen = false;
        //};
        //LocalizationManager.Instance.LanguageChanged -= Instance_LanguageChanged;
        //LocalizationManager.Instance.LanguageChanged += Instance_LanguageChanged;
    }

    //private void OnPencilIconMouseEnter(object sender, MouseEventArgs e)
    //{
    //    _popupCloseTimer.Stop();
    //    if (pendingChangesPopup != null)
    //        pendingChangesPopup.IsOpen = true;
    //}

    //private void OnPencilIconMouseLeave(object sender, MouseEventArgs e)
    //{
    //    _popupCloseTimer.Stop();
    //    _popupCloseTimer.Start();
    //}

    //private void OnPendingChangesPopupMouseEnter(object sender, MouseEventArgs e)
    //{
    //    _popupCloseTimer.Stop();
    //}

    //private void OnPendingChangesPopupMouseLeave(object sender, MouseEventArgs e)
    //{
    //    _popupCloseTimer.Stop();
    //    _popupCloseTimer.Start();
    //}

    #region DependencyProperties

    //public string Title
    //{
    //    get { return (string)GetValue(TitleProperty); }
    //    set { SetValue(TitleProperty, value); }
    //}
    //public static readonly DependencyProperty TitleProperty =
    //    DependencyProperty.Register(nameof(Title), typeof(string), typeof(MapLegendItemView), new PropertyMetadata(new PropertyChangedCallback((d, dp) =>
    //    {
    //        try
    //        {
    //            ((MapLegendItemView)d).UpdateTitle((string)dp.NewValue);
    //        }
    //        catch (Exception)
    //        {
    //            return;
    //        }
    //    })));


    public double TitleFontSize
    {
        get { return (double)GetValue(TitleFontSizeProperty); }
        set { SetValue(TitleFontSizeProperty, value); }
    }
    public static readonly DependencyProperty TitleFontSizeProperty =
        DependencyProperty.Register(nameof(TitleFontSize), typeof(double), typeof(MapLegendItemView), new PropertyMetadata(13.0));


    public bool IsEditable
    {
        get { return (bool)GetValue(IsEditableProperty); }
        set { SetValue(IsEditableProperty, value); }
    }
    public static readonly DependencyProperty IsEditableProperty =
        DependencyProperty.Register(nameof(IsEditable), typeof(bool), typeof(MapLegendItemView), new PropertyMetadata(false));


    public VisualParameters Symbology
    {
        get { return (VisualParameters)GetValue(SymbologyProperty); }
        set { SetValue(SymbologyProperty, value); }
    }
    public static readonly DependencyProperty SymbologyProperty =
        DependencyProperty.Register(nameof(Symbology), typeof(VisualParameters), typeof(MapLegendItemView), new PropertyMetadata(null));


    public bool IsChecked
    {
        get { return (bool)GetValue(IsCheckedProperty); }
        set { SetValue(IsCheckedProperty, value); }
    }
    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(MapLegendItemView), new PropertyMetadata(false));


    public IEnumerable<ILegendCommand> Commands
    {
        get { return (IEnumerable<ILegendCommand>)GetValue(CommandsProperty); }
        set { SetValue(CommandsProperty, value); }
    }
    public static readonly DependencyProperty CommandsProperty =
        DependencyProperty.Register(nameof(Commands), typeof(IEnumerable<ILegendCommand>), typeof(MapLegendItemView), new PropertyMetadata(null));

     
    public bool ShowReloadData
    {
        get { return (bool)GetValue(ShowReloadDataProperty); }
        set { SetValue(ShowReloadDataProperty, value); }
    }
    public static readonly DependencyProperty ShowReloadDataProperty =
        DependencyProperty.Register(nameof(ShowReloadData), typeof(bool), typeof(MapLegendItemView), new PropertyMetadata(true));


    public bool ShowMoreOptions
    {
        get { return (bool)GetValue(ShowMoreOptionsProperty); }
        set { SetValue(ShowMoreOptionsProperty, value); }
    }
    public static readonly DependencyProperty ShowMoreOptionsProperty =
        DependencyProperty.Register(nameof(ShowMoreOptions), typeof(bool), typeof(MapLegendItemView), new PropertyMetadata(true));

    #endregion

    //private bool _isInScaleRange;

    //public bool IsInScaleRange
    //{
    //    get { return _isInScaleRange; }
    //    set
    //    {
    //        _isInScaleRange = value;
    //        //RaisePropertyChanged();
    //    }
    //}

    //public string SymbologyExpanderHeaderText => LocalizationManager.Instance[LocalizationResourceKeys.legend_symbologyExpanderHeaderText.ToString()];


    //private void UpdateTitle(string newValue)
    //{
    //    if (this.DataContext is DrawingItemLayer layer)
    //    {
    //        layer.LayerName = newValue;
    //    }
    //    //var layer = (this.DataContext as DrawingItemLayer);

    //    //if (layer != null)
    //    //{
    //    //    layer.LayerName = newValue;
    //    //} 
    //}

    //private void pendingChangesPopup_Loaded(object sender, RoutedEventArgs e)
    //{

    //}

    //private void pendingChangesPopup_Opened(object sender, EventArgs e)
    //{
    //    //pendingChangesPopupBorder.DataContext = DataContext;
    //}

    private void layerTitle_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // Move focus to the next focusable element in the tab order
            var request = new TraversalRequest(FocusNavigationDirection.Next);

            (sender as UIElement)?.MoveFocus(request);

            e.Handled = true; // Prevents the Enter key from being treated as input
        }
    }
}
