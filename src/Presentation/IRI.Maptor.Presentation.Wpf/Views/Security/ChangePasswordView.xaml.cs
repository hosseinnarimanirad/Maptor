using IRI.Maptor.Presentation.Core.Models.Security;
using IRI.Maptor.Core.Common.Helpers;
using System.Security;

namespace IRI.Maptor.Presentation.Wpf.Controls.Security;

/// <summary>
/// Interaction logic for ChangePasswordView.xaml
/// </summary>
public partial class ChangePasswordView : SecurityInputUserControl, IChangePassword
{
    public ChangePasswordView()
    {
        InitializeComponent();
    }

    public SecureString NewPassword => this.newPassword.SecurePassword;

    public SecureString ConfirmPassword => this.confirmNewPassword.SecurePassword;

    public SecureString Password => this.key.SecurePassword;
     

    //same code exist in EmailSignUpView & ChangeUserPasswordView
    public bool IsNewPasswordValid()
    {
        return NewPassword != null && NewPassword.Length > 0 && SecureStringHelper.SecureStringEqual(this.NewPassword, this.ConfirmPassword);
    }

    public void ClearInputValues()
    {
        this.key.Clear();

        this.newPassword.Clear();

        this.confirmNewPassword.Clear();
    }

    public string GetPasswordText()
    {
        return SecureStringHelper.GetString(Password);
    }

    public string GetNewPasswordText()
    {
        if (IsNewPasswordValid())
        {
            return SecureStringHelper.GetString(NewPassword);
        }
        else
        {
            return null;
        }
    }

}
