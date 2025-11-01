using System;
using System.ComponentModel;
using System.Windows;
using System.Runtime.CompilerServices;

using MahApps.Metro.Controls;
using IRI.Maptor.Jab.Common.Localization;

namespace IRI.Maptor.Jab.Controls.Views.Symbology;

/// <summary>
/// Interaction logic for SymbologyView.xaml
/// </summary>
public partial class SymbologyView : LocalizedMetroWindow
{
    public SymbologyView():base()
    {
        InitializeComponent(); 
    }
     
    public string Ltxt_dialog_symbology_title => LocalizationManager.Instance[nameof(IRI.Maptor.Jab.Common.Properties.Resources.dialog_symbology_title)];
}
