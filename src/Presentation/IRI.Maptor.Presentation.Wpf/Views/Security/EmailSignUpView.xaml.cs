using System.Security;
using System.Windows;
using System.Windows.Input;

using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Presentation.Core.Models.Security;


namespace IRI.Maptor.Presentation.Wpf.Controls.Security
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
            return NetworkUtilities.IsValidEmail(UserNameOrEmail);
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
            get { return (string)GetValue(UserNameProperty); }
            set { SetValue(UserNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UserName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UserNameProperty =
            DependencyProperty.Register(nameof(UserNameOrEmail), typeof(string), typeof(EmailSignUpView), new PropertyMetadata(string.Empty));


        /// <summary>Run by Enter in the confirm-password field (the host's sign-up command); null: Enter only moves on.</summary>
        public ICommand EnterCommand
        {
            get { return (ICommand)GetValue(EnterCommandProperty); }
            set { SetValue(EnterCommandProperty, value); }
        }

        public static readonly DependencyProperty EnterCommandProperty =
            DependencyProperty.Register(nameof(EnterCommand), typeof(ICommand), typeof(EmailSignUpView), new PropertyMetadata(null));

        /// <summary>The command's parameter; this view when not set (the sign-up command takes the view).</summary>
        public object EnterCommandParameter
        {
            get { return GetValue(EnterCommandParameterProperty); }
            set { SetValue(EnterCommandParameterProperty, value); }
        }

        public static readonly DependencyProperty EnterCommandParameterProperty =
            DependencyProperty.Register(nameof(EnterCommandParameter), typeof(object), typeof(EmailSignUpView), new PropertyMetadata(null));

        // Enter walks e-mail → password → confirm, then signs up, as the login form does
        private void email_PreviewKeyDown(object sender, KeyEventArgs e) => MoveOnEnter(e, this.key);

        private void key_PreviewKeyDown(object sender, KeyEventArgs e) => MoveOnEnter(e, this.confirmPassword);

        private void confirmPassword_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;

            var parameter = EnterCommandParameter ?? this;

            if (EnterCommand?.CanExecute(parameter) == true)
                EnterCommand.Execute(parameter);
        }

        private static void MoveOnEnter(KeyEventArgs e, UIElement next)
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;

            next.Focus();
        }
    }
}
