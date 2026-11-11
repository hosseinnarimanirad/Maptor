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

    // No Unloaded clean-up (removed 2026-11-11): the view model is MapViewModelBase.MapExtentPanel, owned by the map
    // and bound in by the host. Disposing it here and setting DataContext = null broke the panel for good the first
    // time the view was unloaded and loaded again (e.g. a theme change re-templating the side panel): every value
    // showed NaN or nothing. ClearViewModel stays for the commented-out MapPresenter path, which created its own.
    public MapExtentPanelView()
    {
        InitializeComponent();
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
