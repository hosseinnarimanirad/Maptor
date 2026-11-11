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

    [Fact]
    public void Card_Collapsed_ShrinksToItsHeader_ButTheRailKeepsTheHeight()
    {
        WpfTestHost.Run(() =>
        {
            Assert.Equal(VerticalAlignment.Top, BuildCard("IRI.Maptor.Styles.Expander.Card", ExpandDirection.Down, isExpanded: false).VerticalAlignment);
            Assert.Equal(VerticalAlignment.Stretch, BuildCard("IRI.Maptor.Styles.Expander.Card", ExpandDirection.Down, isExpanded: true).VerticalAlignment);
            Assert.Equal(VerticalAlignment.Stretch, BuildCard("IRI.Maptor.Styles.Expander.Card", ExpandDirection.Right, isExpanded: false).VerticalAlignment);
        });
    }

    [Fact]
    public void Card_PassesHorizontalContentAlignmentToItsHeader()
    {
        WpfTestHost.Run(() =>
        {
            var expander = BuildCard("IRI.Maptor.Styles.Expander.Card", ExpandDirection.Down, isExpanded: true, HorizontalAlignment.Stretch);

            var toggle = (ToggleButton)expander.Template.FindName("headerToggle", expander);
            Assert.Equal(HorizontalAlignment.Stretch, toggle.HorizontalContentAlignment);
        });
    }

    /// <summary>
    /// ToggleButton.PanelEdgeTab, the legend's show/hide tab on the map edge: shown (checked), the chevron points to the
    /// reading start (left in this left-to-right test); hidden, to the map.
    /// </summary>
    [Theory]
    [InlineData(true, PackIconMaterialKind.ChevronLeft)]
    [InlineData(false, PackIconMaterialKind.ChevronRight)]
    public void PanelEdgeTab_ChevronFollowsTheState(bool isChecked, PackIconMaterialKind expected)
    {
        WpfTestHost.Run(() =>
        {
            var styles = LoadStyles();

            var tab = new ToggleButton { Style = (Style)styles["IRI.Maptor.Styles.ToggleButton.PanelEdgeTab"], IsChecked = isChecked };

            var host = new Grid { Width = 300, Height = 300 };
            host.Resources.MergedDictionaries.Add(styles);
            host.Children.Add(tab);
            host.Measure(new Size(300, 300));
            host.Arrange(new Rect(0, 0, 300, 300));
            host.UpdateLayout();

            var icon = (PackIconMaterial)tab.Template.FindName("icon", tab);
            Assert.Equal(expected, icon.Kind);
        });
    }

    /// <summary>RadioButton.Segment and Border.SegmentTrack (compact segmented choice, 2026-11-11) build and lay out.</summary>
    [Fact]
    public void Segment_Builds_Compact()
    {
        WpfTestHost.Run(() =>
        {
            var styles = LoadStyles();

            var a = new RadioButton { Style = (Style)styles["IRI.Maptor.Styles.RadioButton.Segment"], Content = "A", GroupName = "g", IsChecked = true };
            var b = new RadioButton { Style = (Style)styles["IRI.Maptor.Styles.RadioButton.Segment"], Content = "B", GroupName = "g" };
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(a);
            row.Children.Add(b);
            // VerticalAlignment Top: in a view the track sits in a StackPanel at its own height, not stretched
            var track = new Border { Style = (Style)styles["IRI.Maptor.Styles.Border.SegmentTrack"], Child = row, VerticalAlignment = VerticalAlignment.Top };

            var host = new Grid { Width = 300, Height = 100 };
            host.Resources.MergedDictionaries.Add(styles);
            host.Children.Add(track);
            host.Measure(new Size(300, 100));
            host.Arrange(new Rect(0, 0, 300, 100));
            host.UpdateLayout();

            Assert.NotNull(a.Template.FindName("pill", a));
            // compact: a 24 px row plus the track's 2 px padding on each side
            Assert.True(track.ActualHeight <= 30, $"track is {track.ActualHeight} px high");
        });
    }

    /// <summary>
    /// The layout gap tokens (compact review, Step 1, 2026-11-11) agree with each other, and the panel splitters
    /// fill exactly that gap, so a splitter never makes the gap between two cards wider than a plain gap.
    /// </summary>
    [Fact]
    public void LayoutGapTokens_AgreeWithThePanelSplitters()
    {
        WpfTestHost.Run(() =>
        {
            var styles = LoadStyles();

            var gap = (double)styles["IRI.Maptor.Styles.Size.PanelGap"];
            Assert.Equal(new GridLength(gap), (GridLength)styles["IRI.Maptor.Styles.GridLength.PanelGap"]);
            Assert.Equal(new Thickness(gap), (Thickness)styles["IRI.Maptor.Styles.Thickness.CanvasEdge"]);
            Assert.Equal(new Thickness(0, 0, 0, gap), (Thickness)styles["IRI.Maptor.Styles.Thickness.StackGap"]);

            var vertical = new GridSplitter { Style = (Style)styles["IRI.Maptor.Styles.GridSplitter.Panel.Vertical"] };
            var horizontal = new GridSplitter { Style = (Style)styles["IRI.Maptor.Styles.GridSplitter.Panel.Horizontal"] };
            Assert.Equal(gap, vertical.Width);
            Assert.Equal(gap, horizontal.Height);
        });
    }

    private static ResourceDictionary LoadStyles()
    {
        var cache = typeof(IRI.Maptor.Presentation.Wpf.Helpers.SharedResourceDictionary)
            .GetField("_cache", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null) as System.Collections.IDictionary;

        lock (cache!)
            cache.Clear();

        WpfTestHost.EnsureMahAppsResources();

        return new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/Styles/Controls.All.xaml", UriKind.Absolute)
        };
    }

    private static Expander BuildCard(string styleKey, ExpandDirection direction, bool isExpanded,
        HorizontalAlignment headerAlignment = HorizontalAlignment.Left)
    {
        var styles = LoadStyles();

        var header = new StackPanel { Orientation = Orientation.Horizontal };
        header.Children.Add(new PackIconPhosphorIcons { Style = (Style)styles["IRI.Maptor.Styles.PackIconPhosphorIcons.CardHeader"] });
        header.Children.Add(new TextBlock { Text = "Layers", Style = (Style)styles["IRI.Maptor.Styles.TextBlock.CardHeaderTitle"] });
        header.Children.Add(new TextBlock { Text = "(3)", Style = (Style)styles["IRI.Maptor.Styles.TextBlock.CardHeaderCount"] });

        var expander = new Expander
        {
            Style = (Style)styles[styleKey],
            ExpandDirection = direction,
            IsExpanded = isExpanded,
            HorizontalContentAlignment = headerAlignment,
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
