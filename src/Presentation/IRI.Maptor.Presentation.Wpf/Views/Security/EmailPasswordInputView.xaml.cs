using System.Security;
using System.Windows;

using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Core.Models.Security;


namespace IRI.Maptor.Presentation.Wpf.Controls.Security
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
            return NetworkUtilities.IsValidEmail(UserNameOrEmail);
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
            get { return (string)GetValue(UserNameProperty); }
            set { SetValue(UserNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UserName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UserNameProperty =
            DependencyProperty.Register(nameof(UserNameOrEmail), typeof(string), typeof(EmailPasswordInputView), new PropertyMetadata(string.Empty));

         


        public RelayCommand EnterCommand
        {
            get { return (RelayCommand)GetValue(EnterCommandProperty); }
            set { SetValue(EnterCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EnterCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnterCommandProperty =
            DependencyProperty.Register(nameof(EnterCommand), typeof(RelayCommand), typeof(EmailPasswordInputView), new PropertyMetadata(null));




        public object EnterCommandParameter
        {
            get { return (object)GetValue(EnterCommandParameterProperty); }
            set { SetValue(EnterCommandParameterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EnterCommandParameter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnterCommandParameterProperty =
            DependencyProperty.Register(nameof(EnterCommandParameter), typeof(object), typeof(EmailPasswordInputView), new PropertyMetadata(null));

        private void root_Loaded(object sender, RoutedEventArgs e)
        {
            this.email.Focus();
        }
    }
}
