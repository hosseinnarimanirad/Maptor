using System.Security;
using System.Windows;

using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Jab.Common.Models.Security;

namespace IRI.Maptor.Jab.Controls.View.Security
{
    /// <summary>
    /// Interaction logic for EmailSignUpView.xaml
    /// </summary>
    public partial class EmailSignUpView : SecurityInputUserControl, INewUserEmailPassword
    {
        public EmailSignUpView()
        {
            InitializeComponent();
        }

        public SecureString NewPassword => this.key.SecurePassword;

        public SecureString ConfirmPassword => this.confirmPassword.SecurePassword;
         
        public bool IsValidEmail()
        {
            return NetHelper.IsValidEmail(UserNameOrEmail);
        }

        //same code exist in UserNameSignUpView & ChangeUserPasswordView
        public bool IsNewPasswordValid()
        {
            return NewPassword != null && NewPassword.Length > 0 && SecureStringHelper.SecureStringEqual(this.NewPassword, this.ConfirmPassword);
        }

        public void ClearInputValues()
        {
            this.key.Clear();

            this.confirmPassword.Clear();

            this.UserNameOrEmail = string.Empty;
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

        public string UserNameOrEmail
        {
            get { return (string)GetValue(UsreNameProperty); }
            set { SetValue(UsreNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UsreName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UsreNameProperty =
            DependencyProperty.Register(nameof(UserNameOrEmail), typeof(string), typeof(EmailSignUpView), new PropertyMetadata(string.Empty));
         

    }
}
