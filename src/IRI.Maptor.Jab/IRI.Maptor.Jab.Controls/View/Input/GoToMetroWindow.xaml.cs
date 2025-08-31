using System;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using MahApps.Metro.Controls;
using IRI.Maptor.Jab.Common.Localization;

namespace IRI.Maptor.Jab.Controls.View;

/// <summary>
/// Interaction logic for GoToMetroWindow.xaml
/// </summary>
public partial class GoToMetroWindow : LocalizedMetroWindow
{ 
    public GoToMetroWindow():base()
    {
        InitializeComponent();
        //LocalizationManager.Instance.LanguageChanged -= Instance_LanguageChanged;
        //LocalizationManager.Instance.LanguageChanged += Instance_LanguageChanged;
    }

    public GoToMetroWindow(Presenter.GoToPresenter presenter) : this()
    {
        this.DataContext = presenter;
    }

    //public FlowDirection CurrentFlowDirection => LocalizationManager.Instance.CurrentFlowDirection;

    //private void Instance_LanguageChanged()
    //{
    //    RaisePropertyChanged(nameof(Ltxt_dialog_goto_title));
    //    RaisePropertyChanged(nameof(CurrentFlowDirection));
    //}

    public string Ltxt_dialog_goto_title => LocalizationManager.Instance[LocalizationResourceKeys.dialog_goto_title.ToString()];

}
