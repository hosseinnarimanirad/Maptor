using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using System.Collections.ObjectModel;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common.ViewModels.Map;
using IRI.Maptor.Jab.Core;
using IRI.Maptor.Jab.Core.Layers;

namespace IRI.Maptor.Jab.Controls;

/// <summary>
/// Interaction logic for MapLegendWithOptions.xaml
/// </summary>
public partial class MapLegendView : NotifiableUserControl
{
    public MapLegendView()
    {
        InitializeComponent();
    }


    public LegendViewModel LegendViewModel
    {
        get { return (LegendViewModel)GetValue(LegendViewModelProperty); }
        set { SetValue(LegendViewModelProperty, value); }
    }

    public static readonly DependencyProperty LegendViewModelProperty =
        DependencyProperty.Register(nameof(LegendViewModel),
                                    typeof(LegendViewModel),
                                    typeof(MapLegendView),
                                    new PropertyMetadata(new PropertyChangedCallback((dp, dpE) =>
                                    {
                                        var control = dp as MapLegendView;

                                        var obj = dpE.NewValue as LegendViewModel;

                                        if (obj is null || control is null) return;

                                        obj.RequestRefreshView = async () =>
                                        {
                                            var cvs = control.Resources["collectionViewSource"] as CollectionViewSource;

                                            if (cvs?.View != null)
                                            {
                                                await control.Dispatcher.InvokeAsync(() => cvs.View.Refresh(), DispatcherPriority.Normal);
                                            }
                                        };
                                    })));


    //public string GroupName
    //{
    //    get { return (string)GetValue(GroupNameProperty); }
    //    set { SetValue(GroupNameProperty, value); }
    //}
    //public static readonly DependencyProperty GroupNameProperty =
    //    DependencyProperty.Register(nameof(GroupName), typeof(string), typeof(MapLegendView), new PropertyMetadata("A"));


    //public bool EnableFilterMode
    //{
    //    get { return (bool)GetValue(EnableFilterModeProperty); }
    //    set { SetValue(EnableFilterModeProperty, value); }
    //}
    //public static readonly DependencyProperty EnableFilterModeProperty =
    //    DependencyProperty.Register(nameof(EnableFilterMode), typeof(bool), typeof(MapLegendView), new PropertyMetadata(true));


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

    public static readonly DependencyProperty ShowToolsProperty =
        DependencyProperty.Register(nameof(ShowTools), typeof(bool), typeof(MapLegendView), new PropertyMetadata(true));


    public bool ShowLayerColors
    {
        get { return (bool)GetValue(ShowLayerColorsProperty); }
        set { SetValue(ShowLayerColorsProperty, value); }
    }
    public static readonly DependencyProperty ShowLayerColorsProperty =
        DependencyProperty.Register(nameof(ShowLayerColors), typeof(bool), typeof(MapLegendView), new PropertyMetadata(true));



    public string TocGroup
    {
        get { return (string)GetValue(TocGroupProperty); }
        set { SetValue(TocGroupProperty, value); }
    }

    public static readonly DependencyProperty TocGroupProperty =
        DependencyProperty.Register(nameof(TocGroup), typeof(string), typeof(MapLegendView), new PropertyMetadata(LegendViewModel.DefaultTocGroup));


    //public ObservableCollection<ILayer> Layers
    //{
    //    get { return (ObservableCollection<ILayer>)GetValue(LayersProperty); }
    //    set { SetValue(LayersProperty, value); }
    //}
    //public static readonly DependencyProperty LayersProperty =
    //    DependencyProperty.Register(nameof(Layers), typeof(ObservableCollection<ILayer>), typeof(MapLegendView), new PropertyMetadata(null));


    private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
    {
        var item = e.Item as ILayer;

        if (item is null)
        {
            e.Accepted = false;
            return;
        }
         
        if (string.IsNullOrWhiteSpace(this.TocGroup) ||
            !this.TocGroup.EqualsIgnoreCase(item.TocGroup))
        {
            e.Accepted = false;
            return;
        }

        var passesTypeFilter = /*item.ShowInToc && (*/
            (ShowVectorLayers && item.Type == LayerType.VectorLayer) ||
            (ShowRasterLayers && item.Type == LayerType.Raster) ||
            (ShowRasterLayers && item.Type == LayerType.ImagePyramid) ||
            item.Type == LayerType.GroupLayer
            //)
            ;

        if (!passesTypeFilter)
        {
            e.Accepted = false;
            return;
        }

        if (LegendViewModel is null)
        {
            e.Accepted = false;
            return;
        }

        e.Accepted = LegendViewModel.IsFilterPassedCached(item);
         
    }

    //private static bool IsDataSourceKindAllowed(ILayer layer, List<DataSourceKind> allowedKinds)
    //{
    //    var kind = layer.DataSource?.DataSourceKind ?? DataSourceKind.Other;
    //    return allowedKinds.Contains(kind);
    //}
}
