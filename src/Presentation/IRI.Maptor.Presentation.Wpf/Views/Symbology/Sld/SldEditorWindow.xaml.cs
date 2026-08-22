using System.Windows;
using IRI.Maptor.Presentation.Wpf.ViewModels.Symbology;

namespace IRI.Maptor.Presentation.Wpf.Controls.Symbology.Sld;

/// <summary>
/// Interaction logic for SldEditorWindow.xaml
/// A dialog for editing OGC SLD styles. The host wires
/// <see cref="SldEditorViewModel.RequestApplyAction"/> to push the edited SLD
/// back onto the layer; close/message handling is defaulted here.
/// </summary>
public partial class SldEditorWindow : LocalizedMetroWindow
{
    public SldEditorWindow()
        : this(new SldEditorViewModel())
    {
    }

    public SldEditorWindow(SldEditorViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

        viewModel.RequestCloseAction ??= Close;

        viewModel.RequestShowWarning ??= (message, title) =>
            MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

        viewModel.RequestShowError ??= (message, title) =>
            MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
