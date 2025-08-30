using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Models.Security;
using System;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.Presenters;

public class ChangePasswordDialogViewModel : Notifier
{
    public Action RequestClose;

    public Func<IHavePassword, bool> RequestAuthenticate;

    public Func<IHavePassword, Task<bool>> RequestAuthenticateAsync;

    public bool IsOk { get; private set; } = false;

    private string _title;

    public string Title
    {
        get { return _title; }
        set
        {
            _title = value;
            RaisePropertyChanged();
        }
    }


    private string _bannerText;

    public string BannerText
    {
        get { return _bannerText; }
        set
        {
            _bannerText = value;
            RaisePropertyChanged();
        }
    }


    private string _footerText;

    public string FooterText
    {
        get { return _footerText; }
        set
        {
            _footerText = value;
            RaisePropertyChanged();
        }
    }

    //public ChangePasswordDialogViewModel(Action requestClose)
    //{
    //    this.RequestClose = requestClose;

    //    //this.RequestAuthenticate = requestAuthenticate;
    //}

    public bool IsAsync { get; protected set; }

    public static ChangePasswordDialogViewModel Create(Action requestClose, Func<IHavePassword, bool> requestAuthenticate)
    {
        ChangePasswordDialogViewModel result = new ChangePasswordDialogViewModel();

        result.IsAsync = false;

        result.RequestClose = requestClose;

        result.RequestAuthenticate = requestAuthenticate;

        return result;
    }

    public static ChangePasswordDialogViewModel Create(Action requestClose, Func<IHavePassword, Task<bool>> requestAuthenticate)
    {
        ChangePasswordDialogViewModel result = new ChangePasswordDialogViewModel();

        result.IsAsync = true;

        result.RequestClose = requestClose;

        result.RequestAuthenticateAsync = requestAuthenticate;

        return result;
    }


    //private string _oldPassword;

    //public string OldPassword
    //{
    //    get { return _oldPassword; }
    //    set
    //    {
    //        _oldPassword = value;
    //        RaisePropertyChanged();
    //    }
    //}


    //private string _newPassword;

    //public string NewPassword
    //{
    //    get { return _newPassword; }
    //    set
    //    {
    //        _newPassword = value;
    //        RaisePropertyChanged();
    //    }
    //}

    private IChangePassword _model;

    public IChangePassword Model
    {
        get { return _model; }
        set
        {
            _model = value;
            RaisePropertyChanged();
        }
    }


    private RelayCommand _changePasswordCommand;

    public RelayCommand ChangePasswordCommand
    {
        get
        {
            if (_changePasswordCommand == null)
            {
                _changePasswordCommand = new RelayCommand(async param =>
                {
                    var model = param as IChangePassword;

                    if (model == null)
                    {
                        return;
                    }

                    //this.OldPassword = SecureStringHelper.GetString(model.Password);

                    if (model.IsNewPasswordValid() && await TryAuthenticate(model))
                    //if (model.IsNewPasswordValid())
                    {
                        //this.NewPassword = SecureStringHelper.GetString(model.NewPassword);

                        IsOk = true;

                        Model = model;

                        RequestClose?.Invoke();
                    }
                    else
                    {
                        model.ClearInputValues();

                        return;
                    }

                });
            }

            return _changePasswordCommand;
        }
    }

    private async Task<bool> TryAuthenticate(IHavePassword model)
    {
        if (IsAsync)
        {
            if (RequestAuthenticateAsync != null)
            {
                return await RequestAuthenticateAsync(model);
            }
        }
        else
        {
            return RequestAuthenticate(model);
        }

        return false;
    }

    private RelayCommand _cancelCommand;

    public RelayCommand CancelCommand
    {
        get
        {
            if (_cancelCommand == null)
            {
                _cancelCommand = new RelayCommand(param =>
                {
                    IsOk = false;

                    RequestClose?.Invoke();
                });
            }

            return _cancelCommand;
        }
    }


}
