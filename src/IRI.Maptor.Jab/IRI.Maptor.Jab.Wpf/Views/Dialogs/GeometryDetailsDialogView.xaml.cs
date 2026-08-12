using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

using MahApps.Metro.Controls;

using IRI.Maptor.Jab.Wpf;

using IRI.Maptor.Jab.Core.Localization;
using IRI.Maptor.Jab.Wpf.ViewModels;
using IRI.Maptor.Jab.Wpf.Models.CoordinateEditor;
using IRI.Maptor.Jab.Wpf.ViewModels.CoordinateEditor;
using IRI.Maptor.Jab.Core.Models;

namespace IRI.Maptor.Jab.Controls.Dialogs;

/// <summary>
/// Interaction logic for GeometryDetailsDialogView.xaml
/// </summary>
public partial class GeometryDetailsDialogView : MetroWindow
{
    //public EditableFeatureLayer EditableFeatureLayer { get; }

    //private IDialogService DialogService { get; }

    //private GeometryDetailsViewModel? ViewModel { get; set; }




    public GeometryDetailsDialogView(/*EditableFeatureLayer editableFeatureLayer, IDialogService dialogService*/)
    {
        InitializeComponent();

        // Title is bound in XAML like every other dialog; no manual language handling here.

        //this.EditableFeatureLayer = editableFeatureLayer;

        //this.DialogService = dialogService;

        //this.ViewModel = new GeometryDetailsViewModel(editableFeatureLayer, dialogService);

        //this.DataContext = ViewModel;

        // Subscribe to DataContext changes to wire up RequestClose event
        this.DataContextChanged += GeometryDetailsDialogView_DataContextChanged;
    }

    private void GeometryDetailsDialogView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is GeometryDetailsViewModel oldViewModel)
        {
            oldViewModel.RequestClose -= ViewModel_RequestClose;
        }

        if (e.NewValue is GeometryDetailsViewModel viewModel)
        {
            viewModel.RequestClose += ViewModel_RequestClose;
        }
    }

    private void ViewModel_RequestClose()
    {
        this.Close();
    }
}

