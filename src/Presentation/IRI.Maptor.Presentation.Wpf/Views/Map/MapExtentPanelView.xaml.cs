using System.Windows;

using IRI.Maptor.Presentation.Wpf;
using IRI.Maptor.Presentation.Wpf.ViewModels;
using IRI.Maptor.Presentation.Wpf.ViewModels.Map;
using IRI.Maptor.Presentation.Wpf.Controls;

namespace IRI.Maptor.Presentation.Wpf.Controls;

public partial class MapExtentPanelView : NotifiableUserControl
{
    //public static readonly DependencyProperty MapPresenterProperty = DependencyProperty.Register(
    //    nameof(MapPresenter),
    //    typeof(MapViewModelBase),
    //    typeof(MapExtentPanelView),
    //    new PropertyMetadata(null, OnMapPresenterChanged));

    public MapExtentPanelView()
    {
        InitializeComponent();
        Unloaded += (_, _) => ClearViewModel();
    }

    //public MapViewModelBase? MapPresenter
    //{
    //    get => (MapViewModelBase?)GetValue(MapPresenterProperty);
    //    set => SetValue(MapPresenterProperty, value);
    //}

    //private static void OnMapPresenterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //{
    //    var panel = (MapExtentPanelView)d;
    //    panel.ClearViewModel();

    //    if (e.NewValue is MapViewModelBase map)
    //        panel.DataContext = new MapExtentPanelViewModel(map);
    //}

    private void ClearViewModel()
    {
        if (DataContext is MapExtentPanelViewModel vm)
        {
            vm.Dispose();
            DataContext = null;
        }
    }
}
