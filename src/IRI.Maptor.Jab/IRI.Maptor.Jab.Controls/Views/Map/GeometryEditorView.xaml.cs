using System;
using System.Windows;
using System.Windows.Controls;

using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Common.ViewModels.CoordinateEditor;

using Point = IRI.Maptor.Sta.Common.Primitives.Point;

namespace IRI.Maptor.Jab.Controls.Views;

/// <summary>
/// Interaction logic for LineStringEditorView.xaml
/// </summary>
public partial class GeometryEditorView : UserControl
{
    public static readonly DependencyProperty SelectedPageSizeProperty =
        DependencyProperty.Register(
            nameof(SelectedPageSize),
            typeof(int),
            typeof(GeometryEditorView),
            new PropertyMetadata(10, OnSelectedPageSizeChanged));

    public int SelectedPageSize
    {
        get => (int)GetValue(SelectedPageSizeProperty);
        set => SetValue(SelectedPageSizeProperty, value);
    }

    public static readonly DependencyProperty IsEditableProperty =
        DependencyProperty.Register(
            nameof(IsEditable),
            typeof(bool),
            typeof(GeometryEditorView),
            new PropertyMetadata(false, OnIsEditableChanged));

    public bool IsEditable
    {
        get => (bool)GetValue(IsEditableProperty);
        set => SetValue(IsEditableProperty, value);
    }

    private static void OnSelectedPageSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GeometryEditorView view && view.DataContext is GeometryEditorViewModel presenter)
        {
            presenter.SelectedPageSize = (int)e.NewValue;
            //// Also update CurrentPart if in multi-line mode
            //if (presenter.CurrentPart != null)
            //{
            //    presenter.CurrentPart.SelectedPageSize = (int)e.NewValue;
            //}
        }
    }

    private static void OnIsEditableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GeometryEditorView view && view.DataContext is GeometryEditorViewModel presenter)
        {
            presenter.IsEditable = (bool)e.NewValue;
            //// Also update CurrentPart if in multi-line mode
            //if (presenter.CurrentPart != null)
            //{
            //    presenter.CurrentPart.IsEditable = (bool)e.NewValue;
            //}
        }
    }

    public event Action<Point>? RequestZoomToPoint;
    public event Action<Point>? RequestFlashPoint;
    public event Action<Point>? RequestPanToPoint;
    public event Action<NotifiablePoint>? RequestCopyCoordinate;

    public GeometryEditorView()
    {
        InitializeComponent();
        this.DataContextChanged += LineStringEditorView_DataContextChanged;
    }

    private GeometryEditorViewModel? _currentPresenter;
    //private LineStringEditorPresenter? _currentPartPresenter;

    private void LineStringEditorView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is GeometryEditorViewModel oldPresenter)
        {
            //oldPresenter.RequestPanToPoint -= Presenter_RequestPanToPoint;
            //oldPresenter.RequestZoomToPoint -= Presenter_RequestZoomToPoint;
            //oldPresenter.RequestCopyCoordinate -= Presenter_RequestCopyCoordinate;
            //oldPresenter.PropertyChanged -= Presenter_PropertyChanged;
        }

        if (e.NewValue is GeometryEditorViewModel presenter)
        {
            _currentPresenter = presenter;
            presenter.SelectedPageSize = SelectedPageSize;
            presenter.IsEditable = IsEditable;
            //presenter.RequestPanToPoint += Presenter_RequestPanToPoint;
            //presenter.RequestZoomToPoint += Presenter_RequestZoomToPoint;
            //presenter.RequestCopyCoordinate += Presenter_RequestCopyCoordinate;
            //presenter.PropertyChanged += Presenter_PropertyChanged;

            // Subscribe to CurrentPart if in multi-line mode
            //UpdateCurrentPartSubscription();
        }
    }
      
    private void Presenter_RequestPanToPoint(NotifiablePoint pointInfo)
    {
        var point = new Point(pointInfo.X, pointInfo.Y);
        RequestPanToPoint?.Invoke(point);
    }

    private void Presenter_RequestZoomToPoint(NotifiablePoint pointInfo)
    {
        var point = new Point(pointInfo.X, pointInfo.Y);
        RequestZoomToPoint?.Invoke(point);
    }

    private void Presenter_RequestCopyCoordinate(NotifiablePoint pointInfo)
    {
        RequestCopyCoordinate?.Invoke(pointInfo);
    }

    //
    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    { 
        // in order to automatically scroll into the selected row
        dataGrid.Dispatcher.BeginInvoke(new Action(() =>
        {
            if (dataGrid.SelectedItem is null)
                return; 

            dataGrid.ScrollIntoView(dataGrid.SelectedItem);
        }), System.Windows.Threading.DispatcherPriority.Background);

    }
}

