using System;
using MahApps.Metro.Controls;
using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Common.Localization;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using System.Windows;

namespace IRI.Maptor.Jab.Controls.Views.Dialogs;

/// <summary>
/// Interaction logic for GeometryDetailsDialogView.xaml
/// </summary>
public partial class GeometryDetailsDialogView : MetroWindow
{
    public static readonly DependencyProperty GeometryProperty =
        DependencyProperty.Register(
            nameof(Geometry),
            typeof(IGeometry),
            typeof(GeometryDetailsDialogView),
            new PropertyMetadata(null, OnGeometryChanged));

    public static readonly DependencyProperty DialogServiceProperty =
        DependencyProperty.Register(
            nameof(DialogService),
            typeof(IDialogService),
            typeof(GeometryDetailsDialogView),
            new PropertyMetadata(null, OnDialogServiceChanged));

    public static readonly DependencyProperty DialogTitleProperty =
        DependencyProperty.Register(
            nameof(DialogTitle),
            typeof(string),
            typeof(GeometryDetailsDialogView),
            new PropertyMetadata("Geometry Details"));

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

    public string DialogTitle
    {
        get => (string)GetValue(DialogTitleProperty);
        set => SetValue(DialogTitleProperty, value);
    }

    public Action<IRI.Maptor.Sta.Common.Primitives.Point>? RequestZoomToPoint { get; set; }

    public GeometryDetailsDialogView()
    {
        InitializeComponent();
        Loaded += GeometryDetailsDialogView_Loaded;
        LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;
        
        // Set initial title
        UpdateTitle();
    }

    private void UpdateTitle()
    {
        DialogTitle = LocalizationManager.Instance["dialog_geometryDetails_title"] ?? "Geometry Details";
        Title = DialogTitle;
    }

    private void OnLanguageChanged()
    {
        UpdateTitle();
    }

    private void GeometryDetailsDialogView_Loaded(object sender, RoutedEventArgs e)
    {
        // Ensure properties are set on the GeometryDetailsView after both are loaded
        UpdateGeometryDetailsViewProperties();
    }

    private void UpdateGeometryDetailsViewProperties()
    {
        if (GeometryDetailsViewControl != null && IsLoaded)
        {
            // Set RequestZoomToPoint handler first
            if (RequestZoomToPoint != null)
            {
                GeometryDetailsViewControl.RequestZoomToPoint = RequestZoomToPoint;
            }
            
            // Set DialogService (this will create the presenter and set DataContext)
            if (DialogService != null)
            {
                GeometryDetailsViewControl.DialogService = DialogService;
            }
            
            // Use Dispatcher to ensure the inner view's Loaded event has fired
            // before setting Geometry, so the presenter exists
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (GeometryDetailsViewControl != null && Geometry != null)
                {
                    GeometryDetailsViewControl.Geometry = Geometry;
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private static void OnGeometryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GeometryDetailsDialogView dialog)
        {
            if (dialog.GeometryDetailsViewControl != null && dialog.IsLoaded)
            {
                dialog.GeometryDetailsViewControl.Geometry = e.NewValue as IGeometry;
            }
        }
    }

    private static void OnDialogServiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GeometryDetailsDialogView dialog)
        {
            if (dialog.GeometryDetailsViewControl != null && dialog.IsLoaded)
            {
                dialog.GeometryDetailsViewControl.DialogService = e.NewValue as IDialogService;
                // After setting DialogService, update Geometry if it was set before
                if (dialog.Geometry != null)
                {
                    dialog.GeometryDetailsViewControl.Geometry = dialog.Geometry;
                }
            }
        }
    }

    private void GeometryDetailsView_RequestZoomToPoint(IRI.Maptor.Sta.Common.Primitives.Point point)
    {
        RequestZoomToPoint?.Invoke(point);
    }
}

