using System;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using MahApps.Metro.Controls;
using IRI.Maptor.Jab.Core.Localization;
using IRI.Maptor.Jab.Wpf.ViewModels;

namespace IRI.Maptor.Jab.Controls;

/// <summary>
/// Interaction logic for GoToMetroWindow.xaml
/// </summary>
public partial class GoToMetroWindow : LocalizedMetroWindow
{
    public GoToMetroWindow() : base()
    {
        InitializeComponent();       
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        mainView.hamburgerMenuControl.IsPaneOpen = true;
        mainView.hamburgerMenuControl.IsPaneOpen = false;
        mainView.hamburgerMenuControl.UpdateLayout();
    }

    public GoToMetroWindow(GoToViewModel presenter) : this()
    {
        this.DataContext = presenter;
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

    //public string Ltxt_dialog_goto_title => LocalizationManager.Instance[nameof(IRI.Maptor.Jab.Core.Properties.Resources.dialog_goto_title)];

}
