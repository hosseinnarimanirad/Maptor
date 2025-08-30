using System.Windows;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Jab.Common.Models.Security;

namespace IRI.Maptor.Jab.Controls.View.Security
{
    /// <summary>
    /// Interaction logic for ForgetPasswordView.xaml
    /// </summary>
    public partial class ForgetPasswordView : SecurityInputUserControl, IHaveEmail
    {
        public ForgetPasswordView()
        {
            InitializeComponent();
        }

        public string Email { get => EmailAddress; set => EmailAddress = value; }

        public void ClearInputValues()
        {
            this.EmailAddress = string.Empty;
        }

        public bool IsValidEmail( )
        {
            return NetHelper.IsValidEmail(Email);
        }


        public string EmailAddress
        {
            get { return (string)GetValue(EmailAddressProperty); }
            set { SetValue(EmailAddressProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EmailAddress.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EmailAddressProperty =
            DependencyProperty.Register(nameof(EmailAddress), typeof(string), typeof(ForgetPasswordView), new PropertyMetadata(string.Empty));


        //public Brush InputBorderBrush
        //{
        //    get { return (Brush)GetValue(InputBorderBrushProperty); }
        //    set { SetValue(InputBorderBrushProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for InputBorderBrush.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty InputBorderBrushProperty =
        //    DependencyProperty.Register(nameof(InputBorderBrush), typeof(Brush), typeof(ForgetPasswordView), new PropertyMetadata(null));


        //public Thickness InputBorderThickness
        //{
        //    get { return (Thickness)GetValue(InputBorderThicknessProperty); }
        //    set { SetValue(InputBorderThicknessProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for InputBorderThickness.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty InputBorderThicknessProperty =
        //    DependencyProperty.Register(nameof(InputBorderThickness), typeof(Thickness), typeof(ForgetPasswordView), new PropertyMetadata(new Thickness()));

    }
}
