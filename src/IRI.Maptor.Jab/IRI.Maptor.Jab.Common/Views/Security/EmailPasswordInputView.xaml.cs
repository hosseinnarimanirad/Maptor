using System.Security;
using System.Windows;

using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Jab.Common.Models.Security;
using IRI.Maptor.Jab.Common;


namespace IRI.Maptor.Jab.Controls.Security
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
