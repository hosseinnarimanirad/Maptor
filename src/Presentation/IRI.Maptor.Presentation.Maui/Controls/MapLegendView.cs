using System.Collections;

using IRI.Maptor.Presentation.Maui.Layers;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace IRI.Maptor.Presentation.Maui.Controls;

/// <summary>
/// A lightweight legend / layer list for the <see cref="MapViewer"/>. Each row shows a
/// visibility toggle, the layer name, a color swatch (tap to cycle a preset palette),
/// a zoom-to-layer button, and a remove button. Bind <see cref="ItemsSource"/> to the
/// map's <c>Layers</c> collection and set <see cref="Map"/> for zoom-to support.
/// </summary>
public class MapLegendView : ContentView
{
    private static readonly Color[] _palette =
    {
        Colors.Red, Colors.RoyalBlue, Colors.ForestGreen, Colors.Orange,
        Colors.MediumPurple, Colors.Brown, Colors.Teal, Colors.DeepPink, Colors.Black,
    };

    private readonly CollectionView _list;

    public MapLegendView()
    {
        _list = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            ItemTemplate = new DataTemplate(CreateRow),
            EmptyView = new Label
            {
                Text = "No layers loaded",
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 12),
                TextColor = Colors.Gray,
            },
        };

        Content = new Border
        {
            BackgroundColor = Color.FromArgb("#F2FFFFFF"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Padding = 6,
            Content = _list,
        };
    }

    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(MapLegendView), null, propertyChanged: OnItemsSourceChanged);

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly BindableProperty MapProperty = BindableProperty.Create(
        nameof(Map), typeof(MapViewer), typeof(MapLegendView), null);

    /// <summary>The map the legend controls (used for zoom-to-layer).</summary>
    public MapViewer? Map
    {
        get => (MapViewer?)GetValue(MapProperty);
        set => SetValue(MapProperty, value);
    }

    private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((MapLegendView)bindable)._list.ItemsSource = newValue as IEnumerable;
    }

    private View CreateRow()
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto },
            },
            ColumnSpacing = 6,
            Padding = new Thickness(2),
        };

        var visibility = new CheckBox { VerticalOptions = LayoutOptions.Center };
        visibility.SetBinding(CheckBox.IsCheckedProperty, nameof(MapLayer.IsVisible));
        grid.Add(visibility, 0);

        var name = new Label { VerticalTextAlignment = TextAlignment.Center, LineBreakMode = LineBreakMode.TailTruncation };
        name.SetBinding(Label.TextProperty, nameof(MapLayer.Name));
        grid.Add(name, 1);

        var colorSwatch = new Button
        {
            WidthRequest = 30,
            HeightRequest = 30,
            CornerRadius = 4,
            Padding = 0,
            BorderColor = Colors.Gray,
            BorderWidth = 1,
        };
        colorSwatch.SetBinding(Button.BackgroundColorProperty, nameof(MapLayer.Color));
        colorSwatch.Clicked += OnColorClicked;
        grid.Add(colorSwatch, 2);

        var zoom = new Button { Text = "⤢", WidthRequest = 34, HeightRequest = 30, Padding = 0, FontSize = 14 };
        zoom.Clicked += OnZoomClicked;
        grid.Add(zoom, 3);

        var remove = new Button { Text = "✕", WidthRequest = 34, HeightRequest = 30, Padding = 0, FontSize = 14 };
        remove.Clicked += OnRemoveClicked;
        grid.Add(remove, 4);

        return grid;
    }

    private void OnColorClicked(object? sender, EventArgs e)
    {
        if (sender is not BindableObject b || b.BindingContext is not MapLayer layer)
        {
            return;
        }

        var currentHex = layer.Color?.ToArgbHex();
        var index = Array.FindIndex(_palette, c => c.ToArgbHex() == currentHex);
        layer.Color = _palette[(index + 1) % _palette.Length];
    }

    private void OnZoomClicked(object? sender, EventArgs e)
    {
        if (sender is BindableObject b && b.BindingContext is MapLayer layer && layer.Extent is { } extent)
        {
            Map?.ZoomToExtent(extent);
        }
    }

    private void OnRemoveClicked(object? sender, EventArgs e)
    {
        if (sender is BindableObject b && b.BindingContext is MapLayer layer)
        {
            (ItemsSource as IList)?.Remove(layer);
        }
    }
}
