using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;

using IRI.Maptor.Jab.Common;
using System.Collections.ObjectModel;

namespace IRI.Maptor.Jab.Controls.Views;

/// <summary>
/// Interaction logic for MapLegendWithOptions.xaml
/// </summary>
public partial class MapLegendView : NotifiableUserControl
{
    public MapLegendView()
    {
        InitializeComponent();
    }

    public string GroupName
    {
        get { return (string)GetValue(GroupNameProperty); }
        set { SetValue(GroupNameProperty, value); }
    }
    public static readonly DependencyProperty GroupNameProperty =
        DependencyProperty.Register(nameof(GroupName), typeof(string), typeof(MapLegendView), new PropertyMetadata("A"));


    public bool EnableFilterMode
    {
        get { return (bool)GetValue(EnableFilterModeProperty); }
        set { SetValue(EnableFilterModeProperty, value); }
    }
    public static readonly DependencyProperty EnableFilterModeProperty =
        DependencyProperty.Register(nameof(EnableFilterMode), typeof(bool), typeof(MapLegendView), new PropertyMetadata(true));


    public bool ShowVectorLayers
    {
        get { return (bool)GetValue(ShowVectorLayersProperty); }
        set { SetValue(ShowVectorLayersProperty, value); }
    }
    public static readonly DependencyProperty ShowVectorLayersProperty =
        DependencyProperty.Register(nameof(ShowVectorLayers), typeof(bool), typeof(MapLegendView), new PropertyMetadata(true));



    public bool ShowRasterLayers
    {
        get { return (bool)GetValue(ShowRasterLayersProperty); }
        set { SetValue(ShowRasterLayersProperty, value); }
    }
    public static readonly DependencyProperty ShowRasterLayersProperty =
        DependencyProperty.Register(nameof(ShowRasterLayers), typeof(bool), typeof(MapLegendView), new PropertyMetadata(true));


    public double TitleFontSize
    {
        get { return (double)GetValue(TitleFontSizeProperty); }
        set { SetValue(TitleFontSizeProperty, value); }
    }
    public static readonly DependencyProperty TitleFontSizeProperty =
        DependencyProperty.Register(nameof(TitleFontSize), typeof(double), typeof(MapLegendView), new PropertyMetadata(13.0));


    public bool ShowTools
    {
        get { return (bool)GetValue(ShowToolsProperty); }
        set
        {
            SetValue(ShowToolsProperty, value);
            RaisePropertyChanged(nameof(ShowTools));
        }
    }

    // Using a DependencyProperty as the backing store for ShowTools.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ShowToolsProperty =
        DependencyProperty.Register(nameof(ShowTools), typeof(bool), typeof(MapLegendView), new PropertyMetadata(true));



    public ObservableCollection<ILayer> Layers
    {
        get { return (ObservableCollection<ILayer>)GetValue(LayersProperty); }
        set { SetValue(LayersProperty, value); }
    }
    public static readonly DependencyProperty LayersProperty =
        DependencyProperty.Register("Layers", typeof(ObservableCollection<ILayer>), typeof(MapLegendView), new PropertyMetadata(null));



    private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
    {
        //var item = e.Item as MapLegendItemWithOptionsModel;
        var item = e.Item as ILayer;

        if (item is null)
            e.Accepted = false;

        else if (!EnableFilterMode)
            e.Accepted = true;

        else
        {
            e.Accepted =
               item.ShowInToc && (
               (ShowVectorLayers && item.Type == LayerType.VectorLayer) ||
               (ShowRasterLayers && item.Type == LayerType.Raster) ||
               (ShowRasterLayers && item.Type == LayerType.ImagePyramid) ||
               item.Type == LayerType.GroupLayer); 
        }

    }
}
