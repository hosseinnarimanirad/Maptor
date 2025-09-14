using System;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Localization;

namespace IRI.Maptor.Jab.Controls.View;

/// <summary>
/// Interaction logic for GoToMapProjectView.xaml
/// </summary>
public partial class GoToMapProjectView : LocalizedUserControl /*NotifiableUserControl, IDisposable*/
{
    public GoToMapProjectView() : base()
    {
        InitializeComponent();

        //LocalizationManager.Instance.LanguageChanged += Instance_LanguageChanged;
    }

    protected override void Instance_LanguageChanged()
    {
        RaisePropertyChanged(nameof(Ltxt_srs_utmZone));
    }

    public string Ltxt_srs_utmZone => LocalizationManager.Instance[LocalizationResourceKeys.srs_utmZone.ToString()];

    //#region IDispose

    //private bool _disposed = false;

    //protected virtual void Dispose(bool disposing)
    //{
    //    if (!_disposed)
    //    {
    //        if (disposing)
    //        {
    //            // Dispose managed resources
    //            LocalizationManager.Instance.LanguageChanged -= Instance_LanguageChanged;
    //        }

    //        // Dispose unmanaged resources here if any
    //        _disposed = true;
    //    }
    //}

    //public void Dispose()
    //{
    //    Dispose(true);
    //    GC.SuppressFinalize(this);
    //}

    //#endregion
}
