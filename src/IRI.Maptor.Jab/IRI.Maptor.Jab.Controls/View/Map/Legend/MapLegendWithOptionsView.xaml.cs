using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;

using IRI.Maptor.Jab.Common;

namespace IRI.Maptor.Jab.Controls.View;

/// <summary>
/// Interaction logic for MapLegendWithOptions.xaml
/// </summary>
public partial class MapLegendWithOptionsView : UserControl
{
    public MapLegendWithOptionsView()
    {
        InitializeComponent();
    }



    public string GroupName
    {
        get { return (string)GetValue(GroupNameProperty); }
        set { SetValue(GroupNameProperty, value); }
    }

    // Using a DependencyProperty as the backing store for GroupName.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty GroupNameProperty =
        DependencyProperty.Register(nameof(GroupName), typeof(string), typeof(MapLegendWithOptionsView), new PropertyMetadata("A"));


    public bool EnableFilterMode
    {
        get { return (bool)GetValue(EnableFilterModeProperty); }
        set { SetValue(EnableFilterModeProperty, value); }
    }

    // Using a DependencyProperty as the backing store for EnableFilterMode.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty EnableFilterModeProperty =
        DependencyProperty.Register(nameof(EnableFilterMode), typeof(bool), typeof(MapLegendWithOptionsView), new PropertyMetadata(true));


    public bool ShowVectorLayers
    {
        get { return (bool)GetValue(ShowVectorLayersProperty); }
        set { SetValue(ShowVectorLayersProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ShowVectorLayers.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ShowVectorLayersProperty =
        DependencyProperty.Register(nameof(ShowVectorLayers), typeof(bool), typeof(MapLegendWithOptionsView), new PropertyMetadata(true));



    public bool ShowRasterLayers
    {
        get { return (bool)GetValue(ShowRasterLayersProperty); }
        set { SetValue(ShowRasterLayersProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ShowRasterLayers.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ShowRasterLayersProperty =
        DependencyProperty.Register(nameof(ShowRasterLayers), typeof(bool), typeof(MapLegendWithOptionsView), new PropertyMetadata(true));



    public double TitleFontSize
    {
        get { return (double)GetValue(TitleFontSizeProperty); }
        set { SetValue(TitleFontSizeProperty, value); }
    }

    // Using a DependencyProperty as the backing store for FontSize.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TitleFontSizeProperty =
        DependencyProperty.Register(nameof(TitleFontSize), typeof(double), typeof(MapLegendWithOptionsView), new PropertyMetadata(13.0));

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
               (ShowVectorLayers && item.Type.HasFlag(LayerType.VectorLayer)) ||
               (ShowRasterLayers && item.Type.HasFlag(LayerType.Raster)) ||
               (ShowRasterLayers && item.Type.HasFlag(LayerType.ImagePyramid)) ||
               item.Type.HasFlag(LayerType.GroupLayer));

            //e.Accepted =
            //    item.Layer.ShowInToc && (
            //    (ShowVectorLayers && item.Layer.Type.HasFlag(LayerType.VectorLayer)) ||
            //    (ShowRasterLayers && item.Layer.Type.HasFlag(LayerType.Raster)) ||
            //    (ShowRasterLayers && item.Layer.Type.HasFlag(LayerType.ImagePyramid)));
        }

    }
}
