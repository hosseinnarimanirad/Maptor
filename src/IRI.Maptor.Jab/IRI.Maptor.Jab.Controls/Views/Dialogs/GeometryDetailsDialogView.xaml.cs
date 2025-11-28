using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

using MahApps.Metro.Controls;

using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Common.Localization;
using IRI.Maptor.Jab.Controls.ViewModels;
using IRI.Maptor.Jab.Common.Models.CoordinateEditor;

namespace IRI.Maptor.Jab.Controls.Views.Dialogs;

/// <summary>
/// Interaction logic for GeometryDetailsDialogView.xaml
/// </summary>
public partial class GeometryDetailsDialogView : MetroWindow
{

    public EditableFeatureLayer EditableFeatureLayer { get; }

    private IDialogService DialogService { get; }

    public static readonly DependencyProperty DialogTitleProperty =
        DependencyProperty.Register(
            nameof(DialogTitle),
            typeof(string),
            typeof(GeometryDetailsDialogView),
            new PropertyMetadata("Geometry Details"));

    public string DialogTitle
    {
        get => (string)GetValue(DialogTitleProperty);
        set => SetValue(DialogTitleProperty, value);
    }

    private GeometryDetailsViewModel? ViewModel { get; set; }




    private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridRow row && row.DataContext is PointInfo pointInfo)
        {
            var point = new IRI.Maptor.Sta.Common.Primitives.Point(pointInfo.X, pointInfo.Y);
            //RequestZoomToPoint?.Invoke(point);
        }
    }


    public GeometryDetailsDialogView(EditableFeatureLayer editableFeatureLayer, IDialogService dialogService)
    {
        InitializeComponent();
        LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;

        // Set initial title
        UpdateTitle();

        this.EditableFeatureLayer = editableFeatureLayer;

        this.DialogService = dialogService;

        this.ViewModel = new GeometryDetailsViewModel(editableFeatureLayer, dialogService);

        this.DataContext = ViewModel;
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
}

