using System;
using System.Linq;
using System.Windows;

using IRI.Maptor.Presentation.Wpf.Controls;
using IRI.Maptor.Presentation.Wpf.ViewModels;
using IRI.Maptor.Presentation.Wpf.ViewModels.Map;
using IRI.Maptor.Tests.Common;

using Xunit;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// The map extent panel's view model belongs to the map (<see cref="MapViewModelBase.MapExtentPanel"/>). Until
/// 2026-11-11 the view disposed it and set its own DataContext to null on Unloaded, so the first time it was
/// unloaded and loaded again (a theme change re-templating the side panel) every value showed NaN or nothing.
/// </summary>
[Collection(WpfCollection.Name)]
public class MapExtentPanelViewTest
{
    private sealed class TestMap : MapViewModelBase
    {
    }

    [Fact]
    public void Unloaded_KeepsTheMapsViewModel()
    {
        WpfTestHost.Run(() =>
        {
            // SharedResourceDictionary caches its dictionaries per process; one built on another test's thread
            // throws "a different thread owns it" here, so start from an empty cache (as CardExpanderStyleTest does)
            var cache = typeof(IRI.Maptor.Presentation.Wpf.Helpers.SharedResourceDictionary)
                .GetField("_cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .GetValue(null) as System.Collections.IDictionary;
            lock (cache!)
                cache.Clear();

            // the view's StaticResources (localizedNumberConverter, …) live in the application's Maptor.All.xaml
            var merged = Application.Current.Resources.MergedDictionaries;
            if (!merged.Any(d => d.Source?.OriginalString.EndsWith("Maptor.All.xaml") == true))
            {
                WpfTestHost.EnsureMahAppsResources();
                merged.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/Maptor.All.xaml") });
            }

            var map = new TestMap();
            var viewModel = map.MapExtentPanel;
            Assert.NotNull(viewModel);

            var view = new MapExtentPanelView { DataContext = viewModel };

            view.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));

            Assert.Same(viewModel, view.DataContext);
            Assert.Same(viewModel, map.MapExtentPanel);
        });
    }
}
