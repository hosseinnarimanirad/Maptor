using System;
using System.ComponentModel;
using System.Windows;
using System.Runtime.CompilerServices;

using MahApps.Metro.Controls;
using IRI.Maptor.Jab.Common.Localization;
using IRI.Maptor.Jab.Controls;

namespace IRI.Maptor.Jab.Controls.Symbology;

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
