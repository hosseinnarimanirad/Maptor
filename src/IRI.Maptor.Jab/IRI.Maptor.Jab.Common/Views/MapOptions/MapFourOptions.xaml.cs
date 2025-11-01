using IRI.Maptor.Extensions;
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

namespace IRI.Maptor.Jab.Common.Views.MapOptions
{
    /// <summary>
    /// Interaction logic for MapFourOptions.xaml
    /// </summary>
    public partial class MapFourOptions : UserControl
    {
        public MapFourOptions()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Storyboard storyboard = new Storyboard();

            storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.leftButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.X)"), -50));
            storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.leftButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.Y)"), -25));


            storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.upperLeftButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.X)"), -21));
            storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.upperLeftButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.Y)"), -55));


            storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.upperRightButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.X)"), 21));
            storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.upperRightButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.Y)"), -55));


            storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.rightButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.X)"), 50));
            storyboard.Children.Add(AnimationHelper.CreateElasticAnimation(this.rightButton, new PropertyPath("(Button.RenderTransform).(TranslateTransform.Y)"), -25));

            storyboard.Begin();
        }
    }
}
