using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using IRI.Maptor.Presentation.Wpf.Controls;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Tests.Common;

using MahApps.Metro.IconPacks;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// MapSidePanel as a card (2026-11-10), the panel-close button, the panel splitters and the double-click reset.
/// Built and laid out off-screen, never shown.
/// </summary>
[Collection(WpfCollection.Name)]
public class MapSidePanelTest
{
    [Fact]
    public void Panel_Builds_WithGhostCloseAndIconOnlyWhenSet()
    {
        WpfTestHost.Run(() =>
        {
            var panel = BuildPanel(out _);

            var icon = Assert.IsType<PackIconMaterial>(panel.FindName("headerIcon"));
            Assert.Equal(Visibility.Collapsed, icon.Visibility);

            panel.IconKind = PackIconMaterialKind.TransmissionTower;
            Assert.Equal(Visibility.Visible, icon.Visibility);
            Assert.Equal(PackIconMaterialKind.TransmissionTower, icon.Kind);

            panel.IconKind = PackIconMaterialKind.None;
            Assert.Equal(Visibility.Collapsed, icon.Visibility);
        });
    }

    [Fact]
    public void Panel_DoubleClickOnEdge_GoesBackToPreferredWidth()
    {
        WpfTestHost.Run(() =>
        {
            var panel = BuildPanel(out var host);

            Assert.Equal(400, panel.Width);

            var thumb = Assert.IsType<Thumb>(panel.FindName("resizeThumb"));

            // dragging towards the map (negative in the panel's own coordinates) widens it
            thumb.RaiseEvent(new DragDeltaEventArgs(-60, 0) { RoutedEvent = Thumb.DragDeltaEvent });
            Assert.Equal(460, panel.Width);

            thumb.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Control.MouseDoubleClickEvent });
            Assert.Equal(400, panel.Width);

            // closing and reopening still keeps a dragged width
            thumb.RaiseEvent(new DragDeltaEventArgs(-30, 0) { RoutedEvent = Thumb.DragDeltaEvent });
            panel.IsOpen = false;
            panel.IsOpen = true;
            Assert.Equal(430, panel.Width);
        });
    }

    [Fact]
    public void PanelStyles_Build()
    {
        WpfTestHost.Run(() =>
        {
            var styles = LoadStyles();

            var host = new StackPanel();
            host.Resources.MergedDictionaries.Add(styles);

            var close = new Button { Style = (Style)styles["IRI.Maptor.Styles.Button.PanelClose"] };
            var vertical = new GridSplitter { Style = (Style)styles["IRI.Maptor.Styles.GridSplitter.Panel.Vertical"], Height = 100 };
            var horizontal = new GridSplitter { Style = (Style)styles["IRI.Maptor.Styles.GridSplitter.Panel.Horizontal"] };

            host.Children.Add(close);
            host.Children.Add(vertical);
            host.Children.Add(horizontal);
            Layout(host, 300, 300);

            Assert.True(close.ApplyTemplate() || close.Template is not null);
            Assert.NotNull(vertical.Template.FindName("handle", vertical));
            Assert.NotNull(horizontal.Template.FindName("handle", horizontal));
            Assert.Equal(GridResizeDirection.Columns, vertical.ResizeDirection);
            Assert.Equal(GridResizeDirection.Rows, horizontal.ResizeDirection);
            Assert.NotNull(close.ToolTip);
        });
    }

    [Fact]
    public void GridSplitterReset_ResetsPreviousColumnAndNextRow_WithinLimits()
    {
        WpfTestHost.Run(() =>
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250), MinWidth = 300, MaxWidth = 500 });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(90) });

            var columns = new GridSplitter { ResizeDirection = GridResizeDirection.Columns };
            Grid.SetColumn(columns, 1);
            GridSplitterReset.SetLength(columns, 400);

            var rows = new GridSplitter { ResizeDirection = GridResizeDirection.Rows };
            Grid.SetRow(rows, 1);
            GridSplitterReset.SetLength(rows, 260);
            GridSplitterReset.SetSide(rows, GridSplitterResetSide.Next);

            var unset = new GridSplitter { ResizeDirection = GridResizeDirection.Columns };
            Grid.SetColumn(unset, 1);

            grid.Children.Add(columns);
            grid.Children.Add(rows);
            grid.Children.Add(unset);

            Assert.True(GridSplitterReset.Reset(columns));
            Assert.Equal(400, grid.ColumnDefinitions[0].Width.Value);

            Assert.True(GridSplitterReset.Reset(rows));
            Assert.Equal(260, grid.RowDefinitions[2].Height.Value);

            // clamped to the column's MaxWidth
            GridSplitterReset.SetLength(columns, 900);
            Assert.True(GridSplitterReset.Reset(columns));
            Assert.Equal(500, grid.ColumnDefinitions[0].Width.Value);

            Assert.False(GridSplitterReset.Reset(unset));

            // the double-click itself
            grid.ColumnDefinitions[0].Width = new GridLength(320);
            GridSplitterReset.SetLength(columns, 400);
            columns.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Control.MouseDoubleClickEvent });
            Assert.Equal(400, grid.ColumnDefinitions[0].Width.Value);
        });
    }

    private static MapSidePanel BuildPanel(out Grid host)
    {
        var styles = LoadStyles();

        host = new Grid { Width = 1200, Height = 700 };
        host.Resources.MergedDictionaries.Add(styles);

        var panel = new MapSidePanel { Title = "Buffer zone", PreferredWidth = 400, IsOpen = true, PanelContent = new TextBlock { Text = "content" } };
        host.Children.Add(panel);

        Layout(host, 1200, 700);

        // Loaded does not fire off-screen; the panel measures its host from there
        typeof(MapSidePanel).GetMethod("AttachHost", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(panel, null);

        return panel;
    }

    private static ResourceDictionary LoadStyles()
    {
        var cache = typeof(SharedResourceDictionary)
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

    private static void Layout(FrameworkElement element, double width, double height)
    {
        element.Measure(new Size(width, height));
        element.Arrange(new Rect(0, 0, width, height));
        element.UpdateLayout();
    }
}
