using System.Windows;

using IRI.Maptor.Jab.Controls;

namespace IRI.Maptor.Jab.Controls;

/// <summary>
/// Interaction logic for MapDrawingLegendView.xaml
/// </summary>
public partial class MapDrawingLegendView : NotifiableUserControl
{
    public MapDrawingLegendView() 
    {
        InitializeComponent();
    }
     
    public string GroupName
    {
        get { return (string)GetValue(GroupNameProperty); }
        set
        {
            SetValue(GroupNameProperty, value);
            RaisePropertyChanged(nameof(ShowTools));
        }
    }
    public static readonly DependencyProperty GroupNameProperty =
        DependencyProperty.Register(nameof(GroupName), typeof(string), typeof(MapDrawingLegendView), new PropertyMetadata("D"));


    public double TitleFontSize
    {
        get { return (double)GetValue(TitleFontSizeProperty); }
        set { SetValue(TitleFontSizeProperty, value); }
    }
    public static readonly DependencyProperty TitleFontSizeProperty =
        DependencyProperty.Register(nameof(TitleFontSize), typeof(double), typeof(MapDrawingLegendView), new PropertyMetadata(13.0));


    public bool ShowTools
    {
        get { return (bool)GetValue(ShowToolsProperty); }
        set
        {
            SetValue(ShowToolsProperty, value);
            RaisePropertyChanged(nameof(ShowTools));
        }
    }
    public static readonly DependencyProperty ShowToolsProperty =
        DependencyProperty.Register(nameof(ShowTools), typeof(bool), typeof(MapDrawingLegendView), new PropertyMetadata(true));


    public bool ShowLayerColors
    {
        get { return (bool)GetValue(ShowLayerColorsProperty); }
        set { SetValue(ShowLayerColorsProperty, value); }
    }
    public static readonly DependencyProperty ShowLayerColorsProperty =
        DependencyProperty.Register(nameof(ShowLayerColors), typeof(bool), typeof(MapDrawingLegendView), new PropertyMetadata(true));


    #region Expander configs

    private int _selectedExpanderIndex = 2;
    public int SelectedExpanderIndex
    {
        get => _selectedExpanderIndex;
        set
        {
            if (_selectedExpanderIndex != value)
            {
                _selectedExpanderIndex = value;
                RaisePropertyChanged();
            }
        }
    }
     


    #endregion

     
}
