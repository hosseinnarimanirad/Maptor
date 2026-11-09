using System;
using System.Reflection;
using System.Windows;

using IRI.Maptor.Presentation.Wpf.Controls.Dialogs;
using IRI.Maptor.Presentation.Wpf.Services;
using IRI.Maptor.Presentation.Wpf.ViewModels.Dialogs;
using IRI.Maptor.Tests.Common;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// The import options window must build, i.e. its XAML parses and every StaticResource it uses resolves.
/// Constructed only, never shown. See <see cref="NotificationToastViewTest"/> for why the shared dictionary
/// cache is emptied first.
/// </summary>
[Collection(WpfCollection.Name)]
public class ImportOptionsDialogViewTest
{
    [Fact]
    public void ImportOptionsDialogView_Builds()
    {
        WpfTestHost.Run(() =>
        {
            var cache = typeof(IRI.Maptor.Presentation.Wpf.Helpers.SharedResourceDictionary)
                .GetField("_cache", BindingFlags.NonPublic | BindingFlags.Static)!
                .GetValue(null) as System.Collections.IDictionary;

            lock (cache!)
                cache.Clear();

            WpfTestHost.EnsureMahAppsResources();

            // The window's own Style is looked up before its Resources exist, so it comes from the application,
            // as in the apps.
            var styles = new Uri("pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/Styles/Controls.All.xaml", UriKind.Absolute);
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = styles });

            var vm = new ImportOptionsDialogViewModel(
                DispatchProxy.Create<IDialogService, ImportOptionsDialogViewModelTest.NullDialogService>(), null, null)
            {
                RawText = "51.3,35.7\n51.4,35.8\n51.5,35.9",
            };

            var view = new ImportOptionsDialogView { DataContext = vm };

            Assert.NotNull(view.Content);
            Assert.NotNull(view.FindName("dataTextBox"));
        });
    }
}
