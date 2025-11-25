using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Controls.Models.GeometryDetails;
using IRI.Maptor.Jab.Controls.Presenters;
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

    private GeometryDetailsPresenter? Presenter { get; set; }

    public GeometryDetailsView()
    {
        InitializeComponent();
        Loaded += GeometryDetailsView_Loaded;
    }

    private void GeometryDetailsView_Loaded(object sender, RoutedEventArgs e)
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

        if (Presenter == null && DialogService != null)
        {
            Presenter = new GeometryDetailsPresenter(DialogService);
            if (RequestZoomToPoint != null)
            {
                Presenter.RequestZoomToPoint = RequestZoomToPoint;
            }
            DataContext = Presenter;
        }
    }

    private static void OnGeometryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GeometryDetailsView view && view.Presenter != null)
        {
            view.Presenter.Geometry = e.NewValue as IGeometry;
        }
    }

    private static void OnDialogServiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GeometryDetailsView view)
        {
            if (view.Presenter == null && e.NewValue is IDialogService dialogService)
            {
                view.Presenter = new GeometryDetailsPresenter(dialogService);
                if (view.RequestZoomToPoint != null)
                {
                    view.Presenter.RequestZoomToPoint = view.RequestZoomToPoint;
                }
                view.DataContext = view.Presenter;
            }
            else if (view.Presenter != null && e.NewValue is IDialogService newDialogService)
            {
                // Update dialog service if presenter already exists
                // Note: This would require making DialogService settable in presenter
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

