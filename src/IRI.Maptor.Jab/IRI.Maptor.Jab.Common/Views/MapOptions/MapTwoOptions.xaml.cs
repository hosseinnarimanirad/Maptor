using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IRI.Maptor.Jab.Controls.MapOptions
{
    /// <summary>
    /// Interaction logic for MapTwoOptions.xaml
    /// </summary>
    public partial class MapTwoOptions : UserControl
    {
        public MapTwoOptions()
        {
            InitializeComponent();
        }

        public MapTwoOptions(bool isPointEllipseVisible = true)
        {
            InitializeComponent();

            this.pointEllipse.Visibility = isPointEllipseVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var viewModel = this.DataContext as MapOptionsViewModel;

            if (viewModel != null && viewModel.PrepareAction != null)
            {
                viewModel.PrepareAction?.Invoke();
            }

            Storyboard storyboard = new Storyboard();

            storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.leftButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.X)"), -35));
            storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.leftButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.Y)"), -30));


            storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.rightButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.X)"), 35));
            storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.rightButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.Y)"), -30));

            storyboard.Begin();
        }
    }
}
