using System.Windows.Input;

using MahApps.Metro.Controls;

using IRI.Maptor.Presentation.Wpf.ViewModels.Identify;

namespace IRI.Maptor.Presentation.Wpf.Controls.Identify;

/// <summary>
/// Modeless window around <see cref="IdentifyResultsView"/>. The host (MapViewer) keeps one
/// instance per map, sets <c>Owner</c> and <c>DataContext</c>, and calls
/// <see cref="IdentifyResultsViewModel.Update"/> on every identify click.
/// </summary>
public partial class IdentifyResultsWindow : MetroWindow
{
    public IdentifyResultsWindow()
    {
        InitializeComponent();

        // Title and FlowDirection are bound in XAML / by the shared window style.
    }

    public IdentifyResultsViewModel? ViewModel => DataContext as IdentifyResultsViewModel;

    /// <summary>
    /// Close-only footer, so no Cancel button carries IsCancel; Esc is handled here.
    /// A first Esc clears an active filter, a second one closes the window.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled || e.Key != Key.Escape)
            return;

        e.Handled = true;

        if (resultsView.TryClearFilter())
            return;

        Close();
    }
}
