using System;
using System.Windows;
using System.Security.Principal;

using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Common.Localization;

//using static IRI.Maptor.Jab.Common.Localization.LocalizationResourceKeys;

namespace IRI.Maptor.Jab.Common.ViewModels;

//TO DO: consider replacing Action methods with "IDialogService" 
public class BaseViewModel : Notifier
{
    public IDialogService DialogService { get; set; }

    private string _userName;

    public string UserName
    {
        get { return _userName; }
        set
        {
            _userName = value;
            RaisePropertyChanged();
        }
    }

    public GenericPrincipal CurrentGenericPrincipal
    {
        get { return System.Threading.Thread.CurrentPrincipal as GenericPrincipal; }
        set
        {
            System.Threading.Thread.CurrentPrincipal = value;

            UserName = value.Identity.Name;

            RaisePropertyChanged(nameof(UserName));

            RaisePropertyChanged();

            UserChanged?.Invoke(this, UserName);
        }
    }

    public event EventHandler<string> UserChanged;


    public Action RequestClose;

    public Action RequestActivateWindow;
     

    public BaseViewModel()
    {
        //LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;
        //LocalizationManager.Instance.FlowDirectionChanged += Instance_FlowDirectionChanged;
    }

    public void RedirectRequestesTo(BaseViewModel presenter)
    {
        if (presenter == this)
        {
            return;
        }

        DialogService = presenter.DialogService; 
    }        
}
 

