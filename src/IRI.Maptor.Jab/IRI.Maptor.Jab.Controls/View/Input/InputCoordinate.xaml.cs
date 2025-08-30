using System;
using System.Windows;
using System.Windows.Controls;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Jab.Controls.View;

/// <summary>
/// Interaction logic for InputCoordinate.xaml
/// </summary>
public partial class InputCoordinate : UserControl
{
    Presenter.InputCoordinatePresenter Presenter { get { return this.DataContext as Presenter.InputCoordinatePresenter; } }

    public InputCoordinate()
    {
        InitializeComponent();
    }

    private void save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(this.x.Text) || string.IsNullOrEmpty(this.y.Text))
        {
            return;
        }

        double x, y;

        if (double.TryParse(this.x.Text, out x) && double.TryParse(this.y.Text, out y))
        {
            this.Presenter.PointCollection.Add(new IRI.Maptor.Sta.Common.Primitives.Point(x, y));
        }

        this.y.Text = string.Empty;

        this.x.Text = string.Empty;

        this.x.Focus();
    }

    private void zone_LostFocus(object sender, RoutedEventArgs e)
    {
        int zone;

        if (!int.TryParse(this.zone.Text, out zone))
        {
            this.zone.Text = "39";
        }
    }

    private void remove_Click(object sender, RoutedEventArgs e)
    {
        Button button = e.OriginalSource as Button;

        this.Presenter.PointCollection.Remove(button.DataContext as IRI.Maptor.Sta.Common.Primitives.Point);
    }

    private void coordinateType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            //this.zone.Visibility =
            //coordinateType.SelectedIndex == 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;

            //this.zoneLabel.Visibility =
            //    coordinateType.SelectedIndex == 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;

            if (coordinateType.SelectedIndex == 0)
            {
                note.Content = "توجه: مختصات ها بایستی بر اساس طول و عرض جغرافیایی باشند.";
                this.Presenter.InputType = SpatialReferenceType.Geodetic;
            }
            else
            {

                this.Presenter.InputType = SpatialReferenceType.UTM;
                note.Content = "توجه: مختصات ها، UTM می باشند.";
            }
        }
        catch (Exception ex)
        {
            //MessageBox.Show(ex.Message);
        }

    }
}
