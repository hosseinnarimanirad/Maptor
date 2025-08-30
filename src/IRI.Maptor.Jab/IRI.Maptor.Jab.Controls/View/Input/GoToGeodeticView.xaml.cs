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
        RaisePropertyChanged(nameof(PanToLabel));
        RaisePropertyChanged(nameof(ZoomToLabel));
    }

    public string XLabel => LocalizationManager.Instance[LocalizationResourceKeys.srs_defaultLongitude.ToString()];
     
    public string YLabel => LocalizationManager.Instance[LocalizationResourceKeys.srs_defaultLatitude.ToString()];
     
    public string PanToLabel => LocalizationManager.Instance[LocalizationResourceKeys.map_pan.ToString()];
     

    public string ZoomToLabel => LocalizationManager.Instance[LocalizationResourceKeys.map_zoomTo.ToString()];
     

    //private void UpdateUI()
    //{
    //    //bool isPersian = this.UILanguage == LanguageMode.Persian;

    //    //this.FlowDirection = isPersian ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    //    //this.XLabel = isPersian ? "طول جغرافیایی" : "Longitude";

    //    //this.YLabel = isPersian ? "عرض جغرافیایی" : "Latitude";

    //    //this.PanToLabel = isPersian ? "حرکت" : "Pan To";

    //    //this.ZoomToLabel = isPersian ? "بزرگ‌نمایی" : "Zoom To";
    //}
     
    //public IRI.Maptor.Jab.Common.Models.Language Language
    //{
    //    get { return (IRI.Maptor.Jab.Common.Models.Language)GetValue(LanguageProperty); }
    //    set { SetValue(LanguageProperty, value); }
    //}

    //// Using a DependencyProperty as the backing store for Language.  This enables animation, styling, binding, etc...
    //public static readonly DependencyProperty LanguageProperty =
    //    DependencyProperty.Register("Language", typeof(IRI.Maptor.Jab.Common.Models.Language), typeof(GoToGeodeticView), new PropertyMetadata(Common.Model.Language.Persian));


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
