using System.Windows.Controls;
using System.Windows.Input;

using IRI.Maptor.Presentation.Wpf.ViewModels.Identify;

namespace IRI.Maptor.Presentation.Wpf.Controls.Identify;

/// <summary>
/// Master–detail presentation of identify results. All state lives in
/// <see cref="IdentifyResultsViewModel"/>; the code-behind only adds mouse gestures.
/// </summary>
public partial class IdentifyResultsView : UserControl
{
    public IdentifyResultsView()
    {
        InitializeComponent();
    }

    public IdentifyResultsViewModel? ViewModel => DataContext as IdentifyResultsViewModel;

    /// <summary>Clears the filter box; returns false when there was nothing to clear.</summary>
    public bool TryClearFilter()
    {
        if (ViewModel is null || string.IsNullOrEmpty(ViewModel.FilterText))
            return false;

        ViewModel.FilterText = string.Empty;

        return true;
    }

    /// <summary>Double-clicking a feature row zooms to it; layer rows keep the default expand/collapse.</summary>
    private void OnTreeMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel is not { IsFeatureSelected: true } vm)
            return;

        if (vm.ZoomToCommand.CanExecute(null))
        {
            vm.ZoomToCommand.Execute(null);

            e.Handled = true;
        }
    }
}
