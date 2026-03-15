using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;

using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Models.Legend;

namespace IRI.Maptor.Jab.Controls.Views;

/// <summary>
/// Interaction logic for MapLegendItemWithOptions.xaml
/// </summary>
public partial class MapLegendItemView : UserControl//, IDisposable, INotifyPropertyChanged
{
    //private readonly System.Windows.Threading.DispatcherTimer _popupCloseTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };

    public MapLegendItemView()
    {
        InitializeComponent();
        //_popupCloseTimer.Tick += (_, _) =>
        //{
        //    _popupCloseTimer.Stop();
        //    if (pendingChangesPopup != null)
        //        pendingChangesPopup.IsOpen = false;
        //};
        //LocalizationManager.Instance.LanguageChanged -= Instance_LanguageChanged;
        //LocalizationManager.Instance.LanguageChanged += Instance_LanguageChanged;
    }

    //private void OnPencilIconMouseEnter(object sender, MouseEventArgs e)
    //{
    //    _popupCloseTimer.Stop();
    //    if (pendingChangesPopup != null)
    //        pendingChangesPopup.IsOpen = true;
    //}

    //private void OnPencilIconMouseLeave(object sender, MouseEventArgs e)
    //{
    //    _popupCloseTimer.Stop();
    //    _popupCloseTimer.Start();
    //}

    //private void OnPendingChangesPopupMouseEnter(object sender, MouseEventArgs e)
    //{
    //    _popupCloseTimer.Stop();
    //}

    //private void OnPendingChangesPopupMouseLeave(object sender, MouseEventArgs e)
    //{
    //    _popupCloseTimer.Stop();
    //    _popupCloseTimer.Start();
    //}

    #region DependencyProperties

    public string Title
    {
        get { return (string)GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }

    // Using a DependencyProperty as the backing store for LayerName.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(MapLegendItemView), new PropertyMetadata(new PropertyChangedCallback((d, dp) =>
        {
            try
            {
                ((MapLegendItemView)d).UpdateTitle((string)dp.NewValue);
            }
            catch (Exception)
            {
                return;
            }
        })));

    public double TitleFontSize
    {
        get { return (double)GetValue(TitleFontSizeProperty); }
        set { SetValue(TitleFontSizeProperty, value); }
    }

    // Using a DependencyProperty as the backing store for FontSize.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TitleFontSizeProperty =
        DependencyProperty.Register(nameof(TitleFontSize), typeof(double), typeof(MapLegendItemView), new PropertyMetadata(13.0));


    public bool IsEditable
    {
        get { return (bool)GetValue(IsEditableProperty); }
        set { SetValue(IsEditableProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsEditable.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsEditableProperty =
        DependencyProperty.Register(nameof(IsEditable), typeof(bool), typeof(MapLegendItemView), new PropertyMetadata(false));


    public VisualParameters Symbology
    {
        get { return (VisualParameters)GetValue(SymbologyProperty); }
        set { SetValue(SymbologyProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Symbology.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SymbologyProperty =
        DependencyProperty.Register(nameof(Symbology), typeof(VisualParameters), typeof(MapLegendItemView), new PropertyMetadata(null));



    public bool IsChecked
    {
        get { return (bool)GetValue(IsCheckedProperty); }
        set { SetValue(IsCheckedProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsChecked.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(MapLegendItemView), new PropertyMetadata(false));



    public IEnumerable<ILegendCommand> Commands
    {
        get { return (IEnumerable<ILegendCommand>)GetValue(CommandsProperty); }
        set { SetValue(CommandsProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Commands.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CommandsProperty =
        DependencyProperty.Register(nameof(Commands), typeof(IEnumerable<ILegendCommand>), typeof(MapLegendItemView), new PropertyMetadata(null));

    #endregion

    //private bool _isInScaleRange;

    //public bool IsInScaleRange
    //{
    //    get { return _isInScaleRange; }
    //    set
    //    {
    //        _isInScaleRange = value;
    //        //RaisePropertyChanged();
    //    }
    //}
     
    //public string SymbologyExpanderHeaderText => LocalizationManager.Instance[LocalizationResourceKeys.legend_symbologyExpanderHeaderText.ToString()];


    private void UpdateTitle(string newValue)
    {
        if (this.DataContext is DrawingItemLayer layer)
        {
            layer.LayerName = newValue;
        }
        //var layer = (this.DataContext as DrawingItemLayer);

        //if (layer != null)
        //{
        //    layer.LayerName = newValue;
        //} 
    }

    private void pendingChangesPopup_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void pendingChangesPopup_Opened(object sender, EventArgs e)
    {
        pendingChangesPopupBorder.DataContext = DataContext;
    }
}
