using System;
using System.Windows;
using System.Windows.Input;
using IRI.Maptor.Extensions;
using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Presenters;

namespace IRI.Maptor.Jab.Controls.View;

/// <summary>
/// Interaction logic for CoordinatePanelView.xaml
/// </summary>
public partial class CoordinatePanelView : NotifiableUserControl
{
    public CoordinatePanelPresenter Presenter { get { return this.DataContext as CoordinatePanelPresenter; } }
   
    //#region INotifyPropertyChanged

    //public event PropertyChangedEventHandler PropertyChanged;

    //protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    //{
    //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    //}

    //#endregion

    public CoordinatePanelView()
    {
        InitializeComponent();
    }
     

    private void options_MouseDown(object sender, MouseButtonEventArgs e)
    {
        this.optionsRow.Height = new GridLength(1, GridUnitType.Auto);

        this.optionsRow2.Height = new GridLength(1, GridUnitType.Auto);

        this.Opacity = 1;
    }



    /// <summary>
    /// Sets the x,y coordinates from elipsoidal mercatar. elipsoid: WGS84
    /// </summary>
    /// <param name="mecatorX"></param>
    /// <param name="mercatorY"></param>
    public void SetCoordinates(Point geodeticPoint)
    {
        if (Presenter is null)
            return;
         
        Presenter.SelectedItem?.Update(geodeticPoint.AsPoint());
    }


    private void UserControl_MouseLeave(object sender, MouseEventArgs e)
    {
        this.optionsRow.Height = new GridLength(0, GridUnitType.Pixel);

        this.optionsRow2.Height = new GridLength(0, GridUnitType.Pixel);

        this.Opacity = .8;
    }

    public Point Position
    {
        get { return (Point)GetValue(PositionProperty); }
        set { SetValue(PositionProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Position.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty PositionProperty =
        DependencyProperty.Register(nameof(Position), typeof(Point), typeof(CoordinatePanelView), new PropertyMetadata(new PropertyChangedCallback((d, dp) =>
        {
            try
            {
                ((CoordinatePanelView)d).SetCoordinates((Point)dp.NewValue);
            }
            catch (Exception ex)
            {
                return;
            }
        })));


    //public LanguageMode UILanguage
    //{
    //    get { return (LanguageMode)GetValue(UILanguageProperty); }
    //    set
    //    {
    //        SetValue(UILanguageProperty, value);
    //        SetLanguage(value);
    //    }
    //}

    //// Using a DependencyProperty as the backing store for UILanguage.  This enables animation, styling, binding, etc...
    //public static readonly DependencyProperty UILanguageProperty =
    //    DependencyProperty.Register(
    //        nameof(UILanguage),
    //        typeof(LanguageMode),
    //        typeof(CoordinatePanelView),
    //        new PropertyMetadata(LanguageMode.Persian, (d, dp) =>
    //        {
    //            try
    //            {
    //                ((CoordinatePanelView)d).SetLanguage((LanguageMode)dp.NewValue);
    //            }
    //            catch (Exception ex)
    //            {
    //                return;
    //            }
    //        }));

    //private void SetLanguage(LanguageMode value)
    //{
    //    this.FlowDirection = (value == LanguageMode.Persian) ?
    //                            FlowDirection.RightToLeft :
    //                            FlowDirection.LeftToRight;

    //    //if (this.Presenter != null)
    //    //{
    //    //    this.Presenter.SetLanguage(value);
    //    //}
    //}



}
