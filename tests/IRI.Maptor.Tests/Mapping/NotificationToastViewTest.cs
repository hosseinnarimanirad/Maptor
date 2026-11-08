using System;
using System.Windows;
using System.Windows.Controls;

using IRI.Maptor.Presentation.Wpf.Controls.Dialogs;
using IRI.Maptor.Tests.Common;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// The notification window opened by IDialogService.ShowNotification: it must build (every StaticResource it uses
/// resolves) and must not take focus or appear in the taskbar. Constructed only, never shown.
/// </summary>
[Collection(WpfCollection.Name)]
public class NotificationToastViewTest
{
    private static void EnsureMahAppsResources()
    {
        var resources = Application.Current.Resources;

        if (resources.Contains("MahApps.Styles.Button"))
            return;

        foreach (var source in new[]
                 {
                     "pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml",
                     "pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml",
                     "pack://application:,,,/MahApps.Metro;component/Styles/Themes/Light.Blue.xaml",
                 })
        {
            resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(source, UriKind.Absolute) });
        }
    }

    [Fact]
    public void Builds_ShowsTheMessage_AndTakesNoFocus()
    {
        WpfTestHost.Run(() =>
        {
            EnsureMahAppsResources();

            var view = new NotificationToastView("3 features have geometry problems.");

            Assert.False(view.ShowActivated);
            Assert.False(view.ShowInTaskbar);
            Assert.Equal(WindowStyle.None, view.WindowStyle);

            var message = (TextBlock)view.FindName("messageText");

            Assert.Equal("3 features have geometry problems.", message.Text);
        });
    }
}
