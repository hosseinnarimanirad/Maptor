using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using IRI.Maptor.Tests.Common;

using MahApps.Metro.IconPacks;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// Expander.Card and Expander.CardSection (Controls.Expander.xaml) must build, i.e. every StaticResource in their
/// templates resolves, and the header chevron must point the way the panel opens or folds. Built and laid out
/// off-screen, never shown. Flow direction is the default (left-to-right).
/// </summary>
[Collection(WpfCollection.Name)]
public class CardExpanderStyleTest
{
    [Theory]
    [InlineData(ExpandDirection.Down, false, PackIconMaterialKind.ChevronDown)]
    [InlineData(ExpandDirection.Down, true, PackIconMaterialKind.ChevronUp)]
    [InlineData(ExpandDirection.Up, false, PackIconMaterialKind.ChevronUp)]
    [InlineData(ExpandDirection.Up, true, PackIconMaterialKind.ChevronDown)]
    [InlineData(ExpandDirection.Right, true, PackIconMaterialKind.ChevronDoubleLeft)]
    [InlineData(ExpandDirection.Right, false, PackIconMaterialKind.ChevronDoubleRight)]
    public void Card_ChevronFollowsDirectionAndState(ExpandDirection direction, bool isExpanded, PackIconMaterialKind expected)
    {
        WpfTestHost.Run(() =>
        {
            var expander = BuildCard("IRI.Maptor.Styles.Expander.Card", direction, isExpanded);

            Assert.Equal(expected, FindChevron(expander).Kind);

            var body = (ContentPresenter)expander.Template.FindName("body", expander);
            Assert.Equal(isExpanded ? Visibility.Visible : Visibility.Collapsed, body.Visibility);
        });
    }

    [Fact]
    public void Card_CollapsedRight_IsARailOverTheWholeHeight()
    {
        WpfTestHost.Run(() =>
        {
            var expander = BuildCard("IRI.Maptor.Styles.Expander.Card", ExpandDirection.Right, isExpanded: false);

            var toggle = (ToggleButton)expander.Template.FindName("headerToggle", expander);
            Assert.Equal(3, Grid.GetRowSpan(toggle));

            var chevron = FindChevron(expander);
            Assert.Equal(Dock.Top, DockPanel.GetDock(chevron));
        });
    }

    [Theory]
    [InlineData(false, PackIconMaterialKind.ChevronDown)]
    [InlineData(true, PackIconMaterialKind.ChevronUp)]
    public void CardSection_Builds(bool isExpanded, PackIconMaterialKind expected)
    {
        WpfTestHost.Run(() =>
        {
            var expander = BuildCard("IRI.Maptor.Styles.Expander.CardSection", ExpandDirection.Down, isExpanded);

            Assert.Equal(expected, FindChevron(expander).Kind);
        });
    }

    private static Expander BuildCard(string styleKey, ExpandDirection direction, bool isExpanded)
    {
        var cache = typeof(IRI.Maptor.Presentation.Wpf.Helpers.SharedResourceDictionary)
            .GetField("_cache", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null) as System.Collections.IDictionary;

        lock (cache!)
            cache.Clear();

        WpfTestHost.EnsureMahAppsResources();

        var styles = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/Styles/Controls.All.xaml", UriKind.Absolute)
        };

        var header = new StackPanel { Orientation = Orientation.Horizontal };
        header.Children.Add(new PackIconPhosphorIcons { Style = (Style)styles["IRI.Maptor.Styles.PackIconPhosphorIcons.CardHeader"] });
        header.Children.Add(new TextBlock { Text = "Layers", Style = (Style)styles["IRI.Maptor.Styles.TextBlock.CardHeaderTitle"] });
        header.Children.Add(new TextBlock { Text = "(3)", Style = (Style)styles["IRI.Maptor.Styles.TextBlock.CardHeaderCount"] });

        var expander = new Expander
        {
            Style = (Style)styles[styleKey],
            ExpandDirection = direction,
            IsExpanded = isExpanded,
            Header = header,
            Content = new TextBlock { Text = "content" },
        };

        // a host for layout, so templates apply and the template triggers run
        var host = new Grid { Width = 300, Height = 400 };
        host.Resources.MergedDictionaries.Add(styles);
        host.Children.Add(expander);
        host.Measure(new Size(300, 400));
        host.Arrange(new Rect(0, 0, 300, 400));
        host.UpdateLayout();

        return expander;
    }

    private static PackIconMaterial FindChevron(Expander expander)
    {
        var toggle = (ToggleButton)expander.Template.FindName("headerToggle", expander);
        toggle.ApplyTemplate();
        return (PackIconMaterial)toggle.Template.FindName("chevron", toggle);
    }
}
