using System;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using MahApps.Metro.Controls;
using IRI.Maptor.Jab.Common.Localization;
using IRI.Maptor.Jab.Common.ViewModels;

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

    //public string Ltxt_dialog_goto_title => LocalizationManager.Instance[nameof(IRI.Maptor.Jab.Common.Properties.Resources.dialog_goto_title)];

}
