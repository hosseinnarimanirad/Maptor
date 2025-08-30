using System.Security;
using System.Windows;

using IRI.Maptor.Sta.Common.Helpers; 
using IRI.Maptor.Jab.Common.Models.Security;

namespace IRI.Maptor.Jab.Controls.View.Security
{
    /// <summary>
    /// Interaction logic for EmailPasswordInputView.xaml
    /// </summary>
    public partial class EmailPasswordInputView : SecurityInputUserControl, IUserEmailPassword
    {
        public EmailPasswordInputView()
        {
            InitializeComponent();
        }

        public SecureString Password => this.key.SecurePassword;
         
        public bool IsValidEmail()
        {
            return NetHelper.IsValidEmail(UserNameOrEmail);
        }

        public void ClearInputValues()
        {
            this.key.Clear();

            //this.UserNameOrEmail = string.Empty;
        }

        public string GetPasswordText()
        {
            return SecureStringHelper.GetString(Password);
        }

        public string UserNameOrEmail
        {
            get { return (string)GetValue(UsreNameProperty); }
            set { SetValue(UsreNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UsreName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UsreNameProperty =
            DependencyProperty.Register(nameof(UserNameOrEmail), typeof(string), typeof(EmailPasswordInputView), new PropertyMetadata(string.Empty));

    }
}
