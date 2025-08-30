using System.Security;
using System.Windows;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Jab.Common.Models.Security;

namespace IRI.Maptor.Jab.Controls.View.Security
{
    /// <summary>
    /// Interaction logic for UserPasswordInputView.xaml
    /// </summary>
    public partial class UserPasswordInputView : SecurityInputUserControl, IUserEmailPassword
    {
        public UserPasswordInputView()
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
            DependencyProperty.Register(nameof(UserNameWatermark), typeof(string), typeof(UserPasswordInputView), new PropertyMetadata("نام کاربری"));




        public string UserNameOrEmail
        {
            get { return (string)GetValue(UsreNameProperty); }
            set { SetValue(UsreNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UsreName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UsreNameProperty =
            DependencyProperty.Register(nameof(UserNameOrEmail), typeof(string), typeof(UserPasswordInputView), new PropertyMetadata(string.Empty));



        //public Brush InputBorderBrush
        //{
        //    get { return (Brush)GetValue(InputBorderBrushProperty); }
        //    set { SetValue(InputBorderBrushProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for InputBorderBrush.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty InputBorderBrushProperty =
        //    DependencyProperty.Register(nameof(InputBorderBrush), typeof(Brush), typeof(UserPasswordInputView), new PropertyMetadata(null));


        //public Thickness InputBorderThickness
        //{
        //    get { return (Thickness)GetValue(InputBorderThicknessProperty); }
        //    set { SetValue(InputBorderThicknessProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for InputBorderThickness.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty InputBorderThicknessProperty =
        //    DependencyProperty.Register(nameof(InputBorderThickness), typeof(Thickness), typeof(UserPasswordInputView), new PropertyMetadata(new Thickness()));

    }
}
