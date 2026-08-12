using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Wpf.ViewModels;
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
    /// Interaction logic for MapFiveOptions.xaml
    /// </summary>
    public partial class MapFiveOptions : UserControl
    {
        public MapFiveOptions()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var viewModel = this.DataContext as MapOptionsViewModel;

            if (viewModel != null && viewModel.PrepareAction != null)
            {
                viewModel.PrepareAction?.Invoke();
            }

            Storyboard storyboard = new Storyboard();

            storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.leftButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.X)"), -50));
            storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.leftButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.Y)"), 5));


            storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.upperLeftButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.X)"), -40));
            storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.upperLeftButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.Y)"), -35));


            //storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.middleButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.X)"), -21));
            storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.middleButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.Y)"), -50));


            storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.upperRightButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.X)"), 40));
            storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.upperRightButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.Y)"), -35));


            storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.rightButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.X)"), 50));
            storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.rightButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.Y)"), 5));

            storyboard.Begin();
        }
    }
}
