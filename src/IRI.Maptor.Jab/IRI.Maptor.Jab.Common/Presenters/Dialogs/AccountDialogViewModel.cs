using System;
using System.Threading.Tasks;
using IRI.Maptor.Sta.Common.Contracts.Google;
using IRI.Maptor.Sta.Common.Services;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Models.Security;
using IRI.Maptor.Sta.Spatial.Services.Google;
using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Common.Events;

namespace IRI.Maptor.Jab.Common.Presenters;

public class AccountDialogViewModel : DialogViewModelBase
{
    public AccountDialogViewModel(AuthenticationType type, IDialogService dialogService)
    {
        Type = type;

        DialogService = dialogService;
    }

    #region Actions & Funcs

    //login
    public Func<IUserEmailPassword, Task<bool>> RequestAuthenticateAsync;
    public Func<IUserEmailPassword, bool> RequestAuthenticate;

    public Action<Response<GoogleOAuthUserInfoResult>> RequestLoginWithGoogleOAuth;

    public Action RequestLoginAsGuest;

    //sign out/sign up
    public Action<INewUserEmailPassword> RequestSignUp;

    public Action RequestSignOut;

    //password
    public Action<IHaveEmail> RequestHandleForgetPassword;

    public Action RequestResetPassword;

    public Action<IChangePassword> RequestChangePassword;

    //other
    public Action<IHaveEmail> RequestVerifyEmailAddress;

    public Action RequestShowTermsOfUse;

    //dialog
    public Action<SignUpDialogViewModel> RequestShowSignUpDialogView;

    //public Action<ChangePasswordDialogViewModel> RequestShowChangePasswordDialogView;

    #endregion

    public delegate void SignInChangeHandler(CustomEventArgs<bool> args);

    public event SignInChangeHandler SignInChanged;

    #region Properties

    private bool _isBusy;

    public bool IsBusy
    {
        get { return _isBusy; }
        set
        {
            if (value == true)
            {
                SignUpMessage = string.Empty;
                LoginMessage = string.Empty;
            }

            _isBusy = value;
            RaisePropertyChanged();
        }
    }

    public AuthenticationType Type { get; private set; }

    //private string _termsOfUseWebPageUrl;

    //public string TermsOfUseWebPageUrl
    //{
    //    get { return _termsOfUseWebPageUrl; }
    //    set
    //    {
    //        _termsOfUseWebPageUrl = value;
    //        RaisePropertyChanged();
    //    }
    //}

    private bool _isSignedIn;

    public bool IsSignedIn
    {
        get { return _isSignedIn; }
        set
        {
            _isSignedIn = value;
            RaisePropertyChanged();

            SignInChanged?.Invoke(new CustomEventArgs<bool>(value));
        }
    }


    private bool _confirmTermOfUser;

    public bool ConfirmTermOfUser
    {
        get { return _confirmTermOfUser; }
        set
        {
            _confirmTermOfUser = value;
            RaisePropertyChanged();
        }
    }

    private string _signUpMessage;

    public string SignUpMessage
    {
        get { return _signUpMessage; }
        set
        {
            _signUpMessage = value?.Trim(Environment.NewLine.ToCharArray());
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasSignUpMessage));
        }
    }

    public bool HasSignUpMessage
    {
        get { return SignUpMessage?.Length > 0; }
    }

    private string _loginMessage;

    public string LoginMessage
    {
        get { return _loginMessage; }
        set
        {
            _loginMessage = value?.Trim(Environment.NewLine.ToCharArray());
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasLoginMessage));
        }
    }

    public bool HasLoginMessage
    {
        get { return LoginMessage?.Length > 0; }
    }


    #endregion

    #region Properties (Mode)

    private bool _isForgetPasswordMode;

    public bool IsForgetPasswordMode
    {
        get { return _isForgetPasswordMode; }
        set
        {
            if (_isForgetPasswordMode == value)
                return;

            _isForgetPasswordMode = value;
            RaisePropertyChanged();

            if (value)
            {
                SetMode(nameof(IsForgetPasswordMode));
            }
        }
    }


    private bool _isRegisterMode;

    public bool IsRegisterMode
    {
        get { return _isRegisterMode; }
        set
        {
            if (_isRegisterMode == value)
                return;

            _isRegisterMode = value;
            RaisePropertyChanged();

            if (value)
            {
                SetMode(nameof(IsRegisterMode));
            }
        }
    }


    private bool _isNormalMode = true;

    public bool IsLoginMode
    {
        get { return _isNormalMode; }
        set
        {
            if (_isNormalMode == value)
                return;

            _isNormalMode = value;
            RaisePropertyChanged();

            if (value)
            {
                SetMode(nameof(IsLoginMode));
            }
        }
    }

    private bool _isEmailVerificationMode;

    public bool IsEmailVerificationMode
    {
        get { return _isEmailVerificationMode; }
        set
        {
            if (_isEmailVerificationMode == value)
                return;

            _isEmailVerificationMode = value;
            RaisePropertyChanged();

            if (value)
            {
                SetMode(nameof(IsEmailVerificationMode));
            }
        }
    }

    private bool _isSetPasswordMode;

    public bool IsSetPasswordMode
    {
        get { return _isSetPasswordMode; }
        set
        {
            if (_isSetPasswordMode == value)
                return;

            _isSetPasswordMode = value;
            RaisePropertyChanged();

            if (value)
            {
                SetMode(nameof(IsSetPasswordMode));
            }
        }
    }

    private bool _isChangePasswordMode;

    public bool IsChangePasswordMode
    {
        get { return _isChangePasswordMode; }
        set
        {
            if (value == _isChangePasswordMode)
                return;

            _isChangePasswordMode = value;
            RaisePropertyChanged();

            if (value)
            {
                SetMode(nameof(IsChangePasswordMode));
            }
        }
    }

    #endregion

    #region Private Methods

    private void SetMode(string modeTitle)
    {
        IsForgetPasswordMode = nameof(IsForgetPasswordMode) == modeTitle;
        IsRegisterMode = nameof(IsRegisterMode) == modeTitle;
        IsLoginMode = nameof(IsLoginMode) == modeTitle;
        IsEmailVerificationMode = nameof(IsEmailVerificationMode) == modeTitle;
        IsSetPasswordMode = nameof(IsSetPasswordMode) == modeTitle;
        IsChangePasswordMode = nameof(IsChangePasswordMode) == modeTitle;
    }

    #endregion

    #region Commands

    //login
    private RelayCommand _loginAsyncCommand;

    public RelayCommand LoginAsyncCommand
    {
        get
        {
            if (_loginAsyncCommand == null)
            {
                _loginAsyncCommand = new RelayCommand(param => LoginAsync(param));
            }

            return _loginAsyncCommand;
        }
    }

    //****************************************************   Login  *****************************************************
    private RelayCommand _loginCommand;

    public RelayCommand LoginCommand
    {
        get
        {
            if (_loginCommand == null)
            {
                _loginCommand = new RelayCommand(param => Login(param));
            }

            return _loginCommand;
        }
    }

    //****************************************************   Login Guest  ***********************************************
    private RelayCommand _loginGuestCommand;

    public RelayCommand LoginGuestCommand
    {
        get
        {
            if (_loginGuestCommand == null)
            {
                _loginGuestCommand = new RelayCommand(param =>
                {
                    RequestLoginAsGuest?.Invoke();

                    DialogResult = true;
                });
            }

            return _loginGuestCommand;
        }
    }

    //****************************************************   Login Google OAuth  ****************************************
    private RelayCommand _loginWithGoogleOAuthCommand;

    public RelayCommand LoginWithGoogleOAuthCommand
    {
        get
        {
            if (_loginWithGoogleOAuthCommand == null)
            {
                _loginWithGoogleOAuthCommand = new RelayCommand(async param =>
                {
                    var result = await GoogleOAuthService.RunOAuth2();

                    RequestLoginWithGoogleOAuth?.Invoke(result);

                });
            }

            return _loginWithGoogleOAuthCommand;
        }
    }



    //sign out/sign up

    //****************************************************   Sign Up  ***************************************************
    private RelayCommand _signUpCommand;

    public RelayCommand SignUpCommand
    {
        get
        {
            if (_signUpCommand == null)
            {
                _signUpCommand = new RelayCommand(param => RequestSignUp?.Invoke(param as INewUserEmailPassword));
            }

            return _signUpCommand;
        }
    }


    //****************************************************   Sign Out  **************************************************
    private RelayCommand _signOutCommand;

    public RelayCommand SignOutCommand
    {
        get
        {
            if (_signOutCommand == null)
            {
                _signOutCommand = new RelayCommand(param => RequestSignOut?.Invoke());
            }

            return _signOutCommand;
        }
    }



    //password

    //****************************************************   Reset Password  ********************************************
    private RelayCommand _resetPasswordCommand;

    public RelayCommand ResetPasswordCommand
    {
        get
        {
            if (_resetPasswordCommand == null)
            {
                _resetPasswordCommand = new RelayCommand(param =>
                {
                    RequestResetPassword?.Invoke();
                });
            }

            return _resetPasswordCommand;
        }
    }


    //****************************************************   Forget Password  *******************************************
    private RelayCommand _handleForgetPasswordCommand;

    public RelayCommand HandleForgetPasswordCommand
    {
        get
        {
            if (_handleForgetPasswordCommand == null)
            {
                _handleForgetPasswordCommand = new RelayCommand(param =>
                {
                    RequestHandleForgetPassword?.Invoke(param as IHaveEmail);
                });
            }

            return _handleForgetPasswordCommand;
        }
    }


    //****************************************************   Change Password  *******************************************
    private RelayCommand _changePasswordCommand;

    public RelayCommand ChangePasswordCommand
    {
        get
        {
            if (_changePasswordCommand == null)
            {
                _changePasswordCommand = new RelayCommand(param =>
                {
                    RequestChangePassword?.Invoke(param as IChangePassword);
                });
            }

            return _changePasswordCommand;
        }
    }


    //****************************************************   Show Dialog Change Password  *******************************
    private RelayCommand _showChangePasswordDialogViewCommand;

    public RelayCommand ShowChangePasswordDialogViewCommand
    {
        get
        {
            if (_showChangePasswordDialogViewCommand == null)
            {
                _showChangePasswordDialogViewCommand = new RelayCommand(async param =>
                {

                    var viewModel = await DialogService?.ShowChangePasswordDialog(param, ihp =>
                    {
                        var parameter = new SimpleUserEmailPasswordModel(ihp.Password) { UserNameOrEmail = UserName };

                        return RequestAuthenticate?.Invoke(parameter) == true;
                    });

                    await ChangePassword(viewModel, param);

                    //if (model == null)
                    //{
                    //    return;
                    //}

                    //try
                    //{
                    //    this.RequestChangePassword?.Invoke(model.Model);

                    //    //await DialogService?.ShowMessage(param, null, "رمز عبور با موفقیت تغییر یافت", "پیغام");
                    //}
                    //catch (Exception ex)
                    //{
                    //    await DialogService?.ShowMessage(param, null, ex.Message, "خطا");
                    //}
                });
            }

            return _showChangePasswordDialogViewCommand;
        }
    }


    //****************************************************   Show Dialog Sign Up  ***************************************
    private RelayCommand _showSignUpDialogViewCommand;

    public RelayCommand ShowSignUpDialogViewCommand
    {
        get
        {
            if (_showSignUpDialogViewCommand == null)
            {
                //owner window should be passed via param
                //sample: CommandParameter="{Binding RelativeSource={RelativeSource Mode=FindAncestor, AncestorType={x:Type esiDb:Shell}}}"
                _showSignUpDialogViewCommand = new RelayCommand(async param =>
                {
                    var model = await DialogService.ShowUserNameSignUpDialogAsync(param);

                    if (model == null)
                    {
                        return;
                    }

                    try
                    {
                        RequestShowSignUpDialogView?.Invoke(model);

                        await DialogService.ShowMessageAsync("کاربر جدید با موفقیت اضافه شد", "پیغام", param);
                    }
                    catch (Exception ex)
                    {
                        await DialogService.ShowMessageAsync(ex.Message, "خطا", param);
                    }

                });
            }

            return _showSignUpDialogViewCommand;
        }
    }


    //other
    private RelayCommand _verifyEmailAddressCommand;

    public RelayCommand VerifyEmailAddressCommand
    {
        get
        {
            if (_verifyEmailAddressCommand == null)
            {
                _verifyEmailAddressCommand = new RelayCommand(param =>
                {
                    RequestVerifyEmailAddress?.Invoke(param as IHaveEmail);
                });
            }

            return _verifyEmailAddressCommand;
        }
    }


    private RelayCommand _goToTermsOfUserWebPage;

    public RelayCommand GoToTermsOfUserWebPage
    {
        get
        {
            if (_goToTermsOfUserWebPage == null)
            {
                _goToTermsOfUserWebPage = new RelayCommand(param =>
                {
                    //System.Diagnostics.Process.Start(TermsOfUseWebPageUrl);
                    RequestShowTermsOfUse?.Invoke();
                });
            }

            return _goToTermsOfUserWebPage;
        }
    }


    private RelayCommand _clearInputValuesCommand;

    public RelayCommand ClearInputValuesCommand
    {
        get
        {
            if (_clearInputValuesCommand == null)
            {
                _clearInputValuesCommand = new RelayCommand(param =>
                {
                    var view = param as ISecurityBase;

                    if (view != null)
                    {
                        view.ClearInputValues();
                    }
                });
            }

            return _clearInputValuesCommand;
        }
    }


    #endregion


    #region Async Commands


    private RelayCommand _showChangePasswordDialogViewAsyncCommand;

    public RelayCommand ShowChangePasswordDialogViewAsyncCommand
    {
        get
        {
            if (_showChangePasswordDialogViewAsyncCommand == null)
            {
                _showChangePasswordDialogViewAsyncCommand = new RelayCommand(async param =>
                {
                    var viewModel = await DialogService?.ShowChangePasswordDialog(param, ihp =>
                    {
                        var parameter = new SimpleUserEmailPasswordModel(ihp.Password) { UserNameOrEmail = UserName };

                        return RequestAuthenticateAsync(parameter);
                    });

                    await ChangePassword(viewModel, param);
                });
            }

            return _showChangePasswordDialogViewAsyncCommand;
        }
    }

    #endregion

    #region Private Methods
    private async Task ChangePassword(ChangePasswordDialogViewModel viewModel, object ownerWindow)
    {
        if (viewModel == null)
        {
            return;
        }

        try
        {
            RequestChangePassword?.Invoke(viewModel.Model);

            await DialogService?.ShowMessageAsync("رمز عبور با موفقیت تغییر یافت", "پیغام", ownerWindow, null);
        }
        catch (Exception ex)
        {
            await DialogService?.ShowMessageAsync(ex.Message, "خطا", ownerWindow, null);
        }
    }

    #endregion


    #region Commands (Go To Mode)

    private RelayCommand _goToLoginModeCommand;

    public RelayCommand GoToLoginModeCommand
    {
        get
        {
            if (_goToLoginModeCommand == null)
            {
                _goToLoginModeCommand = new RelayCommand(param =>
                {
                    IsLoginMode = true;
                });
            }

            return _goToLoginModeCommand;
        }
    }


    private RelayCommand _goToRegisterModeCommand;

    public RelayCommand GoToRegisterModeCommand
    {
        get
        {
            if (_goToRegisterModeCommand == null)
            {
                _goToRegisterModeCommand = new RelayCommand(param =>
                {
                    IsRegisterMode = true;
                });
            }

            return _goToRegisterModeCommand;
        }
    }


    private RelayCommand _gotoForgetPasswordModeCommand;

    public RelayCommand GoToForgetPasswordModeCommand
    {
        get
        {
            if (_gotoForgetPasswordModeCommand == null)
            {
                _gotoForgetPasswordModeCommand = new RelayCommand(param =>
                {
                    IsForgetPasswordMode = true;
                });
            }

            return _gotoForgetPasswordModeCommand;
        }
    }


    private RelayCommand _gotoEmailVerificationModeCommand;

    public RelayCommand GotoEmailVerificationModeCommand
    {
        get
        {
            if (_gotoEmailVerificationModeCommand == null)
            {
                _gotoEmailVerificationModeCommand = new RelayCommand(param =>
                {
                    IsEmailVerificationMode = true;
                });
            }

            return _gotoEmailVerificationModeCommand;
        }
    }


    private RelayCommand _goToChangePasswordModeCommand;

    public RelayCommand GoToChangePasswordModeCommand
    {
        get
        {
            if (_goToChangePasswordModeCommand == null)
            {
                _goToChangePasswordModeCommand = new RelayCommand(param =>
                {
                    IsChangePasswordMode = true;
                });
            }

            return _goToChangePasswordModeCommand;
        }
    }


    private RelayCommand _goToSetPasswordModeCommand;

    public RelayCommand GoToSetPasswordModeCommand
    {
        get
        {
            if (_goToSetPasswordModeCommand == null)
            {
                _goToSetPasswordModeCommand = new RelayCommand(param =>
                {
                    IsSetPasswordMode = true;
                });
            }

            return _goToSetPasswordModeCommand;
        }
    }

    #endregion


    protected async void LoginAsync(object parameter)
    {
        try
        {
            IsBusy = true;

            var passContainer = parameter as IUserEmailPassword;

            if (passContainer != null)
            {
                //this.Password = passContainer.Password;                

                if (RequestAuthenticateAsync != null)
                {
                    if (await RequestAuthenticateAsync(passContainer) != true)
                    {
                        IsSignedIn = false;

                        passContainer.ClearInputValues();
                        passContainer.Password.Clear();
                    }
                    else
                    {
                        IsSignedIn = true;

                        DialogResult = true;
                    }
                }
            }
            else
            {
                passContainer.ClearInputValues();
            }
        }
        catch (Exception ex)
        {

        }
        finally
        {
            IsBusy = false;
        }
    }

    protected void Login(object parameter)
    {
        try
        {
            IsBusy = true;

            var passContainer = parameter as IUserEmailPassword;

            if (passContainer != null)
            {
                //this.Password = passContainer.Password;                

                if (RequestAuthenticate != null)
                {
                    if (RequestAuthenticate(passContainer) != true)
                    {
                        IsSignedIn = false;

                        passContainer.ClearInputValues();
                    }
                    else
                    {
                        IsSignedIn = true;

                        DialogResult = true;
                    }
                }
            }
            else
            {
                passContainer.ClearInputValues();
            }
        }
        catch (Exception ex)
        {

        }
        finally
        {
            IsBusy = false;
        }
    }
}
