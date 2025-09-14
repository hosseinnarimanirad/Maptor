using System;
using System.ComponentModel;
using System.Windows;
using System.Runtime.CompilerServices;

using MahApps.Metro.Controls;
using IRI.Maptor.Jab.Common.Localization;

namespace IRI.Maptor.Jab.Controls.View.Symbology;

/// <summary>
/// Interaction logic for SymbologyView.xaml
/// </summary>
public partial class SymbologyView : MetroWindow, IDisposable, INotifyPropertyChanged
{
    private bool _disposed = false;

    public SymbologyView()
    {
        InitializeComponent();
        LocalizationManager.Instance.LanguageChanged += Instance_LanguageChanged;
    }

    public FlowDirection CurrentFlowDirection => LocalizationManager.Instance.CurrentFlowDirection;

    public string Ltxt_dialog_symbology_fill => LocalizationManager.Instance[LocalizationResourceKeys.dialog_symbology_fill.ToString()];

    public string Ltxt_dialog_symbology_stroke => LocalizationManager.Instance[LocalizationResourceKeys.dialog_symbology_stroke.ToString()];

    public string Ltxt_dialog_symbology_strokeWidth => LocalizationManager.Instance[LocalizationResourceKeys.dialog_symbology_strokeWidth.ToString()];

    public string Ltxt_dialog_symbology_title => LocalizationManager.Instance[LocalizationResourceKeys.dialog_symbology_title.ToString()];

    private void Instance_LanguageChanged()
    {
        RaisePropertyChanged(nameof(Ltxt_dialog_symbology_fill));
        RaisePropertyChanged(nameof(Ltxt_dialog_symbology_stroke));
        RaisePropertyChanged(nameof(Ltxt_dialog_symbology_strokeWidth));
        RaisePropertyChanged(nameof(Ltxt_dialog_symbology_title));
        RaisePropertyChanged(nameof(CurrentFlowDirection));
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion


    #region IDispose

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Dispose managed resources
            LocalizationManager.Instance.LanguageChanged -= Instance_LanguageChanged;
        }

        // Dispose unmanaged resources here if any
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
