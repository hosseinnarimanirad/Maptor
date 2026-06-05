using System.Security;
using System.Windows;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Jab.Common.Models.Security;
using IRI.Maptor.Jab.Common.Localization;


namespace IRI.Maptor.Jab.Controls.Security
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
            get { return (string)GetValue(UsreNameProperty); }
            set { SetValue(UsreNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UsreName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UsreNameProperty =
            DependencyProperty.Register(nameof(UserNameOrEmail), typeof(string), typeof(UserPasswordInputView), new PropertyMetadata(string.Empty));
         
    }
}
