using System;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using MahApps.Metro.Controls;
using IRI.Maptor.Jab.Common.Localization;

namespace IRI.Maptor.Jab.Controls.Views;

/// <summary>
/// Interaction logic for GoToMetroWindow.xaml
/// </summary>
public partial class GoToMetroWindow : LocalizedMetroWindow
{
    public GoToMetroWindow() : base()
    {
        InitializeComponent();
    }

    public GoToMetroWindow(Presenters.GoToPresenter presenter) : this()
    {
        this.DataContext = presenter;
    } 

    public string Ltxt_dialog_goto_title => LocalizationManager.Instance[nameof(IRI.Maptor.Jab.Common.Properties.Resources.dialog_goto_title)];

}
