using IRI.Maptor.Jab.Common.Models.Security;
using IRI.Maptor.Sta.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IRI.Maptor.Jab.Controls.View.Security
{
    /// <summary>
    /// Interaction logic for SetPasswordViewInputView.xaml
    /// </summary>
    public partial class SetPasswordViewInputView : SecurityInputUserControl, INewPassword
    {
        public SetPasswordViewInputView()
        {
            InitializeComponent();
        }

         
        public SecureString ConfirmPassword => this.confirmNewPassword.SecurePassword;

        public SecureString NewPassword => this.newPassword.SecurePassword;

         

        //same code exist in EmailSignUpView & ChangeUserPasswordView
        public bool IsNewPasswordValid()
        {
            return NewPassword != null && NewPassword.Length > 0 && SecureStringHelper.SecureStringEqual(this.NewPassword, this.ConfirmPassword);
        }

        public void ClearInputValues()
        {
            this.newPassword.Clear();
             
            this.confirmNewPassword.Clear();
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
}
