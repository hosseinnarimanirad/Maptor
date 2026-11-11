using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

using IRI.Maptor.Presentation.Wpf.Controls.Dialogs;
using IRI.Maptor.Tests.Common;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// DialogFooterView.IsDismissOnly (2026-11-10): a footer whose only button closes the dialog draws it as a secondary
/// button; by default it stays primary. Built off-screen, never shown.
/// </summary>
[Collection(WpfCollection.Name)]
public class DialogFooterViewTest
{
    [Fact]
    public void PrimaryButton_IsSecondaryOnlyWhenDismissOnly()
    {
        WpfTestHost.Run(() =>
        {
            var cache = typeof(IRI.Maptor.Presentation.Wpf.Helpers.SharedResourceDictionary)
                .GetField("_cache", BindingFlags.NonPublic | BindingFlags.Static)!
                .GetValue(null) as System.Collections.IDictionary;

            lock (cache!)
                cache.Clear();

            WpfTestHost.EnsureMahAppsResources();

            var footer = new DialogFooterView { ShowSecondary = false };

            var button = Assert.IsType<Button>(footer.FindName("primaryButton"));

            var primary = (Style)footer.FindResource("IRI.Maptor.Styles.Button.Dialogs.Primary");
            var secondary = (Style)footer.FindResource("IRI.Maptor.Styles.Button.Dialogs.Secondary");

            Assert.Same(primary, button.Style);

            footer.IsDismissOnly = true;
            Assert.Same(secondary, button.Style);

            footer.IsDismissOnly = false;
            Assert.Same(primary, button.Style);
        });
    }
}
