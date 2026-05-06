using System;
using System.Windows.Controls;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Localization;

namespace IRI.Maptor.Jab.Controls;

/// <summary>
/// Interaction logic for GoToGeodetic.xaml
/// </summary>
public partial class GoToGeodeticView : UserControl
{

    public GoToGeodeticView() //: base()
    {
        InitializeComponent();

        //this.UILanguage = LanguageMode.Persian;
        //LocalizationManager.Instance.LanguageChanged += Instance_LanguageChanged;
    }

    //protected override void Instance_LanguageChanged()
    //{
    //    RaisePropertyChanged(nameof(XLabel));
    //    RaisePropertyChanged(nameof(YLabel));
    //}

    //public string XLabel => LocalizationManager.Instance[LocalizationResourceKeys.srs_defaultLongitude.ToString()];

    //public string YLabel => LocalizationManager.Instance[LocalizationResourceKeys.srs_defaultLatitude.ToString()];      
}
