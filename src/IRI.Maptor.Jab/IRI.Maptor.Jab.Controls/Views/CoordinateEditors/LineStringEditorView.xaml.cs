using IRI.Maptor.Jab.Common.Models.CoordinateEditor;
using IRI.Maptor.Jab.Common.Presenters.CoordinateEditor;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;  
using Point = IRI.Maptor.Sta.Common.Primitives.Point;

namespace IRI.Maptor.Jab.Controls.Views.General.CoordinateEditors;

/// <summary>
/// Interaction logic for LineStringEditorView.xaml
/// </summary>
public partial class LineStringEditorView : UserControl
{
    public static readonly DependencyProperty MaxPointsPerPageProperty =
        DependencyProperty.Register(
            nameof(MaxPointsPerPage),
            typeof(int),
            typeof(LineStringEditorView),
            new PropertyMetadata(10, OnMaxPointsPerPageChanged));

    public int MaxPointsPerPage
    {
        get => (int)GetValue(MaxPointsPerPageProperty);
        set => SetValue(MaxPointsPerPageProperty, value);
    }

    public static readonly DependencyProperty IsEditableProperty =
        DependencyProperty.Register(
            nameof(IsEditable),
            typeof(bool),
            typeof(LineStringEditorView),
            new PropertyMetadata(false, OnIsEditableChanged));

    public bool IsEditable
    {
        get => (bool)GetValue(IsEditableProperty);
        set => SetValue(IsEditableProperty, value);
    }

    private static void OnMaxPointsPerPageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LineStringEditorView view && view.DataContext is LineStringEditorPresenter presenter)
        {
            presenter.MaxPointsPerPage = (int)e.NewValue;
            //// Also update CurrentPart if in multi-line mode
            //if (presenter.CurrentPart != null)
            //{
            //    presenter.CurrentPart.MaxPointsPerPage = (int)e.NewValue;
            //}
        }
    }

    private static void OnIsEditableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LineStringEditorView view && view.DataContext is LineStringEditorPresenter presenter)
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
    public event Action<PointInfo>? RequestCopyCoordinate;

    public LineStringEditorView()
    {
        InitializeComponent();
        this.DataContextChanged += LineStringEditorView_DataContextChanged;
    }

    private LineStringEditorPresenter? _currentPresenter;
    //private LineStringEditorPresenter? _currentPartPresenter;

    private void LineStringEditorView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is LineStringEditorPresenter oldPresenter)
        {
            //oldPresenter.RequestPanToPoint -= Presenter_RequestPanToPoint;
            //oldPresenter.RequestZoomToPoint -= Presenter_RequestZoomToPoint;
            //oldPresenter.RequestCopyCoordinate -= Presenter_RequestCopyCoordinate;
            //oldPresenter.PropertyChanged -= Presenter_PropertyChanged;
        }
        
        if (e.NewValue is LineStringEditorPresenter presenter)
        {
            _currentPresenter = presenter;
            presenter.MaxPointsPerPage = MaxPointsPerPage;
            presenter.IsEditable = IsEditable;
            //presenter.RequestPanToPoint += Presenter_RequestPanToPoint;
            //presenter.RequestZoomToPoint += Presenter_RequestZoomToPoint;
            //presenter.RequestCopyCoordinate += Presenter_RequestCopyCoordinate;
            //presenter.PropertyChanged += Presenter_PropertyChanged;
            
            // Subscribe to CurrentPart if in multi-line mode
            //UpdateCurrentPartSubscription();
        }
    }

    //private void Presenter_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    //{
    //    if (e.PropertyName == nameof(LineStringEditorPresenter.CurrentPart))
    //    {
    //        UpdateCurrentPartSubscription();
    //    }
    //}

    //private void UpdateCurrentPartSubscription()
    //{
    //    if (_currentPresenter == null)
    //        return;

    //    // Unsubscribe from old CurrentPart
    //    if (_currentPartPresenter != null)
    //    {
    //        //_currentPartPresenter.RequestPanToPoint -= Presenter_RequestPanToPoint;
    //        //_currentPartPresenter.RequestZoomToPoint -= Presenter_RequestZoomToPoint;
    //        //_currentPartPresenter.RequestCopyCoordinate -= Presenter_RequestCopyCoordinate;
    //    }

    //    // Subscribe to new CurrentPart if in multi-line mode
    //    _currentPartPresenter = _currentPresenter.CurrentPart;
    //    if (_currentPartPresenter != null)
    //    {
    //        _currentPartPresenter.MaxPointsPerPage = MaxPointsPerPage;
    //        _currentPartPresenter.IsEditable = IsEditable;
    //        //_currentPartPresenter.RequestPanToPoint += Presenter_RequestPanToPoint;
    //        //_currentPartPresenter.RequestZoomToPoint += Presenter_RequestZoomToPoint;
    //        //_currentPartPresenter.RequestCopyCoordinate += Presenter_RequestCopyCoordinate;
    //    }
        
    //    // Also update the main presenter's MaxPointsPerPage and IsEditable if switching to single-line mode
    //    if (_currentPartPresenter == null && _currentPresenter != null)
    //    {
    //        _currentPresenter.MaxPointsPerPage = MaxPointsPerPage;
    //        _currentPresenter.IsEditable = IsEditable;
    //    }
    //}

    private void Presenter_RequestPanToPoint(PointInfo pointInfo)
    {
        var point = new Point(pointInfo.X, pointInfo.Y);
        RequestPanToPoint?.Invoke(point);
    }

    private void Presenter_RequestZoomToPoint(PointInfo pointInfo)
    {
        var point = new Point(pointInfo.X, pointInfo.Y);
        RequestZoomToPoint?.Invoke(point);
    }

    private void Presenter_RequestCopyCoordinate(PointInfo pointInfo)
    {
        RequestCopyCoordinate?.Invoke(pointInfo);
    }
     
}

