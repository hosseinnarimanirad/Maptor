using System.Windows;
using IRI.Maptor.Jab.Common.ViewModels.Symbology.Sld;

namespace IRI.Maptor.Jab.Controls.Views.Symbology.Sld;

/// <summary>
/// Interaction logic for SldEditorWindow.xaml
/// A standalone window for editing OGC SLD styles
/// </summary>
public partial class SldEditorWindow : Window
{
    public SldEditorWindow()
    {
        InitializeComponent();
        DataContext = new SldEditorViewModel();
    }

    public SldEditorWindow(SldEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

