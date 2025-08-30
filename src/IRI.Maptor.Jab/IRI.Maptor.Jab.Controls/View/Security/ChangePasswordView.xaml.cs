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


        //public Brush InputBorderBrush
        //{
        //    get { return (Brush)GetValue(InputBorderBrushProperty); }
        //    set { SetValue(InputBorderBrushProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for InputBorderBrush.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty InputBorderBrushProperty =
        //    DependencyProperty.Register(nameof(InputBorderBrush), typeof(Brush), typeof(ChangePasswordView), new PropertyMetadata(null));


        //public Thickness InputBorderThickness
        //{
        //    get { return (Thickness)GetValue(InputBorderThicknessProperty); }
        //    set { SetValue(InputBorderThicknessProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for InputBorderThickness.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty InputBorderThicknessProperty =
        //    DependencyProperty.Register(nameof(InputBorderThickness), typeof(Thickness), typeof(ChangePasswordView), new PropertyMetadata(new Thickness()));


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
}
