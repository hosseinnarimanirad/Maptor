using System;
using System.Windows.Input;

using IRI.Maptor.Presentation.Wpf.ViewModels;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// Interaction logic for GoToMetroWindow.xaml
/// </summary>
public partial class GoToMetroWindow : LocalizedMetroWindow
{
    public GoToMetroWindow() : base()
    {
        InitializeComponent();
    }

    public GoToMetroWindow(GoToViewModel presenter) : this()
    {
        this.DataContext = presenter;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        // the most common first move is to paste something, so start in the quick-entry box
        mainView.FocusQuickEntry();
    }

    // Close-only footer: no Cancel button to carry IsCancel, and this window is opened
    // with Show() rather than ShowDialog(), so Esc is handled here.
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
