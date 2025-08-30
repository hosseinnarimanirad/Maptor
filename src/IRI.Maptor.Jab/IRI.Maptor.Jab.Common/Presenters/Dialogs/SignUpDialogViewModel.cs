using System;

using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Models.Security;

namespace IRI.Maptor.Jab.Common.Presenters;

public class SignUpDialogViewModel : DialogViewModelBase
{
    public Action RequestClose;

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

    private SimpleUserEmailPasswordModel _model;

    public SimpleUserEmailPasswordModel Model
    {
        get { return _model; }
        set
        {
            _model = value;
            RaisePropertyChanged();
        }
    }

    public SignUpDialogViewModel(Action requestClose)
    {
        //this.Model = new SimpleUserEmailPasswordModel();

        RequestClose = requestClose;
    }

    private RelayCommand _signUpCommand;

    public RelayCommand SignUpCommand
    {
        get
        {
            if (_signUpCommand == null)
            {
                _signUpCommand = new RelayCommand(param =>
                {
                    var model = param as INewUserEmailPassword;

                    if (model == null)
                    {
                        return;
                    }

                    if (!model.IsNewPasswordValid())
                    {
                        model.ClearInputValues();

                        return;
                    }

                    Model = new SimpleUserEmailPasswordModel(model.NewPassword) { UserNameOrEmail = model.UserNameOrEmail };

                    //this.Model.UserName = model.UserNameOrEmail;

                    //this.Model.Password = SecureStringHelper.GetString(model.NewPassword);

                    IsOk = true;

                    RequestClose?.Invoke();
                });
            }

            return _signUpCommand;
        }
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
