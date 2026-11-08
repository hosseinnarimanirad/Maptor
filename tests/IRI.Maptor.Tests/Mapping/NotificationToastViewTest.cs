using System;
using System.Windows;
using System.Windows.Controls;

using IRI.Maptor.Presentation.Wpf.Controls.Dialogs;
using IRI.Maptor.Presentation.Wpf.Controls.LayerSettings;
using IRI.Maptor.Tests.Common;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// The views added for invalid geometries must build, i.e. every StaticResource they use resolves: the notification
/// window (IDialogService.ShowNotification; it must also take no focus and stay out of the taskbar) and
/// Layer Settings › Geometry. Constructed only, never shown.
/// </summary>
/// <remarks>
/// SharedResourceDictionary caches its dictionaries process-wide, and each <see cref="WpfTestHost.Run"/> runs on a
/// new thread, so a view built after another test's view would use styles owned by that test's (finished) thread.
/// That is a test-host artefact (the app has one UI thread); the cache is emptied first so this run builds its own.
/// </remarks>
[Collection(WpfCollection.Name)]
public class NotificationToastViewTest
{
    private static void ClearSharedResourceDictionaryCache()
    {
        var cache = typeof(IRI.Maptor.Presentation.Wpf.Helpers.SharedResourceDictionary)
            .GetField("_cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .GetValue(null) as System.Collections.IDictionary;

        lock (cache!)
            cache.Clear();
    }

    [Fact]
    public void GeometryViews_Build()
    {
        WpfTestHost.Run(() =>
        {
            ClearSharedResourceDictionaryCache();

            WpfTestHost.EnsureMahAppsResources();

            var toast = new NotificationToastView("3 features have geometry problems.");

            Assert.False(toast.ShowActivated);
            Assert.False(toast.ShowInTaskbar);
            Assert.Equal(WindowStyle.None, toast.WindowStyle);
            Assert.Equal("3 features have geometry problems.", ((TextBlock)toast.FindName("messageText")).Text);
            Assert.True(toast.ClosesAutomatically);
            Assert.Equal(Visibility.Collapsed, ((TextBlock)toast.FindName("titleText")).Visibility);

            // with a title, and staying until closed (e.g. an access history reporting failed sign-ins)
            var sticky = new NotificationToastView("Last sign-in: 1405/08/17 09:12 (10.0.0.5)", "Access history", closesAutomatically: false);

            Assert.False(sticky.ClosesAutomatically);
            Assert.Equal("Access history", ((TextBlock)sticky.FindName("titleText")).Text);
            Assert.Equal(Visibility.Visible, ((TextBlock)sticky.FindName("titleText")).Visibility);

            var geometryTab = new LayerSettings_GeometryView();

            Assert.NotNull(geometryTab.Content);
        });
    }
}
