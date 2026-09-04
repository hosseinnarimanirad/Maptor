using System;
using System.Windows.Input;

using IRI.Maptor.Presentation.Wpf.ViewModels;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// Interaction logic for MgrsGoToMetroWindow.xaml
/// </summary>
public partial class MgrsGoToMetroWindow : LocalizedMetroWindow
{
    public MgrsGoToMetroWindow() : base()
    {
        InitializeComponent();
    }

    public MgrsGoToMetroWindow(MgrsGoToViewModel presenter) : this()
    {
        this.DataContext = presenter;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        mainView.FocusReference();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // the view model listens to LanguageChanged for as long as the window is open
        (this.DataContext as MgrsGoToViewModel)?.Dispose();
    }

    // Opened with Show() rather than ShowDialog() and the footer carries no Cancel button, so
    // Esc is handled here, exactly as in GoToMetroWindow.
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!e.Handled && e.Key == Key.Escape)
        {
            e.Handled = true;

            Close();
        }
    }
}
