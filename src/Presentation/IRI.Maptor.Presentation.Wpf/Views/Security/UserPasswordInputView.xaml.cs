using System.Security;
using System.Windows;
using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Core.Models.Security;


namespace IRI.Maptor.Presentation.Wpf.Controls.Security
{
    /// <summary>
    /// Interaction logic for UserPasswordInputView.xaml
    /// </summary>
    public partial class UserPasswordInputView : SecurityInputUserControl, IUserEmailPassword
    {
        public UserPasswordInputView()
        {
            InitializeComponent();
            if (string.IsNullOrEmpty(UserNameWatermark))
                UserNameWatermark = LocalizationManager.Instance["common_username"];
        }

        public SecureString Password => this.key.SecurePassword;
         
        public bool IsValidEmail()
        {
            return NetworkUtilities.IsValidEmail(UserNameOrEmail);
        }
 
        public void ClearInputValues()
        {
            this.key.Clear();

            this.UserNameOrEmail = string.Empty;
        }

        public string GetPasswordText()
        {
            return SecureStringHelper.GetString(Password);
        }

        public string UserNameWatermark
        {
            get { return (string)GetValue(UserNameWatermarkProperty); }
            set { SetValue(UserNameWatermarkProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UserNameWatermark.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UserNameWatermarkProperty =
            DependencyProperty.Register(nameof(UserNameWatermark), typeof(string), typeof(UserPasswordInputView), new PropertyMetadata(string.Empty));




        public string UserNameOrEmail
        {
            get { return (string)GetValue(UserNameProperty); }
            set { SetValue(UserNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UserName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UserNameProperty =
            DependencyProperty.Register(nameof(UserNameOrEmail), typeof(string), typeof(UserPasswordInputView), new PropertyMetadata(string.Empty));
         
    }
}
