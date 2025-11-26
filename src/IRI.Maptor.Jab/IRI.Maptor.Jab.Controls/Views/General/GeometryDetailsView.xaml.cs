using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Controls.Models.GeometryDetails;
using IRI.Maptor.Jab.Controls.ViewModels;
using IRI.Maptor.Jab.Controls.Services.Dialog;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Jab.Controls.Views;

/// <summary>
/// Interaction logic for GeometryDetailsView.xaml
/// </summary>
public partial class GeometryDetailsView : UserControl
{
    public static readonly DependencyProperty GeometryProperty =
        DependencyProperty.Register(
            nameof(Geometry),
            typeof(IGeometry),
            typeof(GeometryDetailsView),
            new PropertyMetadata(null, OnGeometryChanged));

    public static readonly DependencyProperty DialogServiceProperty =
        DependencyProperty.Register(
            nameof(DialogService),
            typeof(IDialogService),
            typeof(GeometryDetailsView),
            new PropertyMetadata(null, OnDialogServiceChanged));

    public IGeometry Geometry
    {
        get => (IGeometry)GetValue(GeometryProperty);
        set => SetValue(GeometryProperty, value);
    }

    public IDialogService DialogService
    {
        get => (IDialogService)GetValue(DialogServiceProperty);
        set => SetValue(DialogServiceProperty, value);
    }

    public Action<IRI.Maptor.Sta.Common.Primitives.Point>? RequestZoomToPoint { get; set; }

    private GeometryDetailsViewModel? ViewModel { get; set; }

    public GeometryDetailsView()
    {
        InitializeComponent();
        Loaded += GeometryDetailsView_Loaded;
    }

    private void GeometryDetailsView_Loaded(object sender, RoutedEventArgs e)
    {
        InitializePresenter();
    }

    private void InitializePresenter()
    {
        if (DialogService == null)
        {
            // Try to find parent window
            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                DialogService = new DefaultDialogService(parentWindow);
            }
        }

        if (ViewModel == null && DialogService != null)
        {
            ViewModel = new GeometryDetailsViewModel(DialogService);
            if (RequestZoomToPoint != null)
            {
                ViewModel.RequestZoomToPoint = RequestZoomToPoint;
            }
            DataContext = ViewModel;
            
            // If Geometry was set before viewmodel was created, set it now
            if (Geometry != null)
            {
                ViewModel.Geometry = Geometry;
            }
        }
    }

    private static void OnGeometryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GeometryDetailsView view)
        {
            // If viewmodel exists, set geometry on it
            if (view.ViewModel != null)
            {
                view.ViewModel.Geometry = e.NewValue as IGeometry;
            }
            // Otherwise, if view is loaded, initialize viewmodel
            else if (view.IsLoaded)
            {
                view.InitializePresenter();
                if (view.ViewModel != null && e.NewValue != null)
                {
                    view.ViewModel.Geometry = e.NewValue as IGeometry;
                }
            }
        }
    }

    private static void OnDialogServiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GeometryDetailsView view)
        {
            if (view.ViewModel == null && e.NewValue is IDialogService dialogService)
            {
                view.ViewModel = new GeometryDetailsViewModel(dialogService);
                if (view.RequestZoomToPoint != null)
                {
                    view.ViewModel.RequestZoomToPoint = view.RequestZoomToPoint;
                }
                view.DataContext = view.ViewModel;
                
                // If Geometry was set before viewmodel was created, set it now
                if (view.Geometry != null)
                {
                    view.ViewModel.Geometry = view.Geometry;
                }
            }
            else if (view.ViewModel != null && e.NewValue is IDialogService newDialogService)
            {
                // Update dialog service if viewmodel already exists
                // Note: This would require making DialogService settable in viewmodel
            }
        }
    }

    private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridRow row && row.DataContext is PointInfo pointInfo)
        {
            var point = new IRI.Maptor.Sta.Common.Primitives.Point(pointInfo.X, pointInfo.Y);
            RequestZoomToPoint?.Invoke(point);
        }
    }
}

