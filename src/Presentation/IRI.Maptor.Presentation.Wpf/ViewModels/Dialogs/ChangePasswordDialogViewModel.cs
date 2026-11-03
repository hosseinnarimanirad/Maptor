using System;
using System.Threading.Tasks;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Core.Models.Security;
using IRI.Maptor.Presentation.Core.Properties;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Dialogs;

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

    /// <summary>
    /// A dialog that performs the change itself: it checks the input locally, calls
    /// <paramref name="requestChangePassword"/> and closes only when that succeeds. A failure (the
    /// callback throws) is shown inside the dialog, which stays open. The old password is verified by
    /// the server as part of the change, never by signing in again: a second sign-in would open a new
    /// server session (ending the current one) and count a mistyped password as a failed login.
    /// </summary>
    public static ChangePasswordDialogViewModel CreateForChange(Func<IChangePassword, Task> requestChangePassword)
    {
        return new ChangePasswordDialogViewModel
        {
            IsAsync = true,
            RequestChangePasswordAsync = requestChangePassword ?? throw new ArgumentNullException(nameof(requestChangePassword))
        };
    }

    /// <summary>Set by <see cref="CreateForChange"/>; throws with a user-facing message on failure.</summary>
    public Func<IChangePassword, Task>? RequestChangePasswordAsync { get; private set; }


    private bool _isBusy;

    public bool IsBusy
    {
        get { return _isBusy; }
        private set
        {
            _isBusy = value;
            RaisePropertyChanged();
        }
    }


    private string? _errorMessage;

    public string? ErrorMessage
    {
        get { return _errorMessage; }
        private set
        {
            _errorMessage = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasErrorMessage));
        }
    }

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);


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

                    if (RequestChangePasswordAsync != null)
                    {
                        await SubmitChangeAsync(model);

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

    private async Task SubmitChangeAsync(IChangePassword model)
    {
        if (IsBusy)
            return;

        ErrorMessage = null;

        if (model.Password == null || model.Password.Length == 0)
        {
            ErrorMessage = LocalizationManager.Instance[nameof(Resources.dialog_changePassword_currentRequired)];
            return;
        }

        if (model.NewPassword == null || model.NewPassword.Length == 0)
        {
            ErrorMessage = LocalizationManager.Instance[nameof(Resources.dialog_changePassword_enterPassword)];
            return;
        }

        if (!model.IsNewPasswordValid())
        {
            ErrorMessage = LocalizationManager.Instance[nameof(Resources.dialog_changePassword_newPasswordsDiffer)];
            return;
        }

        try
        {
            IsBusy = true;

            await RequestChangePasswordAsync!(model);

            IsOk = true;

            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
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
                // Not while a change is in flight: closing then would hide whether it succeeded.
                _cancelCommand = new RelayCommand(param =>
                {
                    IsOk = false;

                    RequestClose?.Invoke();
                }, _ => !IsBusy);
            }

            return _cancelCommand;
        }
    }


}
