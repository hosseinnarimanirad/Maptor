using System;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Localization;

namespace IRI.Maptor.Jab.Controls.View;

/// <summary>
/// Interaction logic for GoToGeodetic.xaml
/// </summary>
public partial class GoToGeodeticView : NotifiableUserControl, IDisposable
{

    public GoToGeodeticView()
    {
        InitializeComponent();

        //this.UILanguage = LanguageMode.Persian;
        LocalizationManager.Instance.LanguageChanged += Instance_LanguageChanged;
    }

    private void Instance_LanguageChanged()
    { 
        RaisePropertyChanged(nameof(XLabel));
        RaisePropertyChanged(nameof(YLabel));
        //RaisePropertyChanged(nameof(PanToLabel));
        //RaisePropertyChanged(nameof(ZoomToLabel));
    }

    public string XLabel => LocalizationManager.Instance[LocalizationResourceKeys.srs_defaultLongitude.ToString()];
     
    public string YLabel => LocalizationManager.Instance[LocalizationResourceKeys.srs_defaultLatitude.ToString()];
     
    //public string PanToLabel => LocalizationManager.Instance[LocalizationResourceKeys.cmd_general_pan.ToString()];
     

    //public string ZoomToLabel => LocalizationManager.Instance[LocalizationResourceKeys.cmd_general_zoomTo.ToString()];
     
     

    #region IDispose

    private bool _disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                LocalizationManager.Instance.LanguageChanged -= Instance_LanguageChanged;
            }

            // Dispose unmanaged resources here if any
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
