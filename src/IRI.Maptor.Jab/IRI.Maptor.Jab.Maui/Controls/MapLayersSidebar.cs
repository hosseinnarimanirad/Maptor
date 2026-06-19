using System.Collections;
using System.Windows.Input;

using IRI.Maptor.Jab.Maui.Layers;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace IRI.Maptor.Jab.Maui.Controls;

/// <summary>
/// A SW-Maps-style layer panel that slides in over the <see cref="MapViewer"/> from the
/// right edge. It shows a background-map (basemap) picker and the list of vector layers;
/// each row offers a visibility toggle, a color swatch (tap to cycle a preset palette),
/// the layer name + auto-generated description, and a delete button. A "+" button raises
/// <see cref="AddLayerCommand"/> so the host can import a layer (e.g. GeoJSON).
///
/// Bind <see cref="ItemsSource"/> to the map's <c>Layers</c> collection, <see cref="BaseMaps"/>
/// / <see cref="SelectedBaseMap"/> to the basemap options, set <see cref="Map"/> for
/// zoom-to-layer, and drive <see cref="IsOpen"/> from the toolbar's "layers" button.
/// </summary>
public class MapLayersSidebar : ContentView
{
    private const double PanelWidth = 320;
    private const uint SlideDuration = 220;

    // Dark SW-Maps-like palette.
    private static readonly Color PanelBackground = Color.FromArgb("#222A33");
    private static readonly Color Accent = Color.FromArgb("#4DB6AC");
    private static readonly Color PrimaryText = Colors.White;
    private static readonly Color SecondaryText = Color.FromArgb("#A7B0BA");
    private static readonly Color Divider = Color.FromArgb("#384450");

    private static readonly Color[] _swatchPalette =
    {
        Colors.Red, Colors.RoyalBlue, Colors.ForestGreen, Colors.Orange,
        Colors.MediumPurple, Colors.Brown, Colors.Teal, Colors.DeepPink, Colors.Black,
    };

    private readonly Grid _root;
    private readonly Border _panel;
    private readonly CollectionView _list;
    private readonly Button _expandButton;

    private bool _expanded;

    public MapLayersSidebar()
    {
        _list = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            ItemTemplate = new DataTemplate(CreateRow),
            EmptyView = new Label
            {
                Text = "No layers yet. Tap ＋ to import.",
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 18),
                TextColor = SecondaryText,
            },
        };

        _expandButton = IconButton("⤢", OnExpandClicked);

        _panel = new Border
        {
            BackgroundColor = PanelBackground,
            StrokeThickness = 0,
            WidthRequest = PanelWidth,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Fill,
            Padding = new Thickness(12, 10),
            Content = BuildPanelContent(),
        };

        _root = new Grid
        {
            // Let taps on the exposed map (outside the panel) pass through.
            InputTransparent = true,
            CascadeInputTransparent = false,
        };
        _root.Add(_panel);

        InputTransparent = true;
        CascadeInputTransparent = false;
        Content = _root;

        // Start hidden off the right edge.
        _panel.TranslationX = PanelWidth;
    }

    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(MapLayersSidebar), null, propertyChanged: OnItemsSourceChanged);

    /// <summary>The layer collection shown in the list (typically the map's <c>Layers</c>).</summary>
    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly BindableProperty BaseMapsProperty = BindableProperty.Create(
        nameof(BaseMaps), typeof(IEnumerable), typeof(MapLayersSidebar), null);

    /// <summary>The basemap options offered by the background-map picker.</summary>
    public IEnumerable? BaseMaps
    {
        get => (IEnumerable?)GetValue(BaseMapsProperty);
        set => SetValue(BaseMapsProperty, value);
    }

    public static readonly BindableProperty SelectedBaseMapProperty = BindableProperty.Create(
        nameof(SelectedBaseMap), typeof(object), typeof(MapLayersSidebar), null, BindingMode.TwoWay);

    /// <summary>The currently selected basemap (two-way).</summary>
    public object? SelectedBaseMap
    {
        get => GetValue(SelectedBaseMapProperty);
        set => SetValue(SelectedBaseMapProperty, value);
    }

    public static readonly BindableProperty MapProperty = BindableProperty.Create(
        nameof(Map), typeof(MapViewer), typeof(MapLayersSidebar), null);

    /// <summary>The map the sidebar controls (used for zoom-to-layer).</summary>
    public MapViewer? Map
    {
        get => (MapViewer?)GetValue(MapProperty);
        set => SetValue(MapProperty, value);
    }

    public static readonly BindableProperty AddLayerCommandProperty = BindableProperty.Create(
        nameof(AddLayerCommand), typeof(ICommand), typeof(MapLayersSidebar), null);

    /// <summary>Raised by the "+" button so the host can import a new layer.</summary>
    public ICommand? AddLayerCommand
    {
        get => (ICommand?)GetValue(AddLayerCommandProperty);
        set => SetValue(AddLayerCommandProperty, value);
    }

    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(
        nameof(IsOpen), typeof(bool), typeof(MapLayersSidebar), false, BindingMode.TwoWay, propertyChanged: OnIsOpenChanged);

    /// <summary>Whether the panel is slid in (open). Animate by toggling this.</summary>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    private double CurrentPanelWidth => _panel.WidthRequest > 0 ? _panel.WidthRequest : PanelWidth;

    private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((MapLayersSidebar)bindable)._list.ItemsSource = newValue as IEnumerable;
    }

    private static async void OnIsOpenChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var self = (MapLayersSidebar)bindable;
        await self.AnimateAsync((bool)newValue);
    }

    private async Task AnimateAsync(bool open)
    {
        var target = open ? 0 : CurrentPanelWidth;
        await _panel.TranslateTo(target, 0, SlideDuration, Easing.CubicOut);
    }

    private View BuildPanelContent()
    {
        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto }, // header
                new RowDefinition { Height = GridLength.Auto }, // background map
                new RowDefinition { Height = GridLength.Auto }, // divider
                new RowDefinition { Height = GridLength.Auto }, // layers header
                new RowDefinition { Height = GridLength.Star },  // layer list
            },
            RowSpacing = 10,
        };

        grid.Add(BuildHeader(), 0, 0);
        grid.Add(BuildBackgroundMapRow(), 0, 1);
        grid.Add(new BoxView { HeightRequest = 1, Color = Divider }, 0, 2);
        grid.Add(BuildLayersHeader(), 0, 3);
        grid.Add(_list, 0, 4);

        return grid;
    }

    private View BuildHeader()
    {
        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto },
            },
            ColumnSpacing = 4,
        };

        var title = new Label
        {
            Text = "Layers",
            TextColor = PrimaryText,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
        };

        header.Add(title, 0);
        header.Add(_expandButton, 1);
        header.Add(IconButton("✕", OnCloseClicked), 2);

        return header;
    }

    private View BuildBackgroundMapRow()
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
            },
            ColumnSpacing = 8,
        };

        var label = new Label
        {
            Text = "Background Map",
            TextColor = Accent,
            FontSize = 13,
            VerticalOptions = LayoutOptions.Center,
        };

        var picker = new Picker
        {
            TextColor = PrimaryText,
            TitleColor = SecondaryText,
            Title = "Select basemap",
            HorizontalOptions = LayoutOptions.Fill,
        };
        picker.SetBinding(Picker.ItemsSourceProperty, new Binding(nameof(BaseMaps), source: this));
        picker.SetBinding(Picker.SelectedItemProperty, new Binding(nameof(SelectedBaseMap), BindingMode.TwoWay, source: this));

        grid.Add(label, 0);
        grid.Add(picker, 1);

        return grid;
    }

    private View BuildLayersHeader()
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
            },
        };

        var label = new Label
        {
            Text = "Layers",
            TextColor = Accent,
            FontSize = 13,
            VerticalOptions = LayoutOptions.Center,
        };

        var add = new Button
        {
            Text = "＋",
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = PrimaryText,
            BackgroundColor = Accent,
            WidthRequest = 36,
            HeightRequest = 36,
            Padding = 0,
            CornerRadius = 6,
        };
        add.SetBinding(Button.CommandProperty, new Binding(nameof(AddLayerCommand), source: this));

        grid.Add(label, 0);
        grid.Add(add, 1);

        return grid;
    }

    private View CreateRow()
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto }, // visibility
                new ColumnDefinition { Width = GridLength.Auto }, // color swatch
                new ColumnDefinition { Width = GridLength.Star },  // name + description
                new ColumnDefinition { Width = GridLength.Auto }, // delete
            },
            ColumnSpacing = 8,
            Padding = new Thickness(0, 6),
        };

        var visibility = new CheckBox { VerticalOptions = LayoutOptions.Center, Color = Accent };
        visibility.SetBinding(CheckBox.IsCheckedProperty, nameof(MapLayer.IsVisible));
        grid.Add(visibility, 0);

        var colorSwatch = new Button
        {
            WidthRequest = 26,
            HeightRequest = 26,
            CornerRadius = 13,
            Padding = 0,
            BorderColor = Colors.White,
            BorderWidth = 1,
            VerticalOptions = LayoutOptions.Center,
        };
        colorSwatch.SetBinding(Button.BackgroundColorProperty, nameof(MapLayer.Color));
        colorSwatch.Clicked += OnColorClicked;
        grid.Add(colorSwatch, 1);

        var name = new Label
        {
            TextColor = PrimaryText,
            FontSize = 15,
            LineBreakMode = LineBreakMode.TailTruncation,
        };
        name.SetBinding(Label.TextProperty, nameof(MapLayer.Name));

        var description = new Label
        {
            TextColor = SecondaryText,
            FontSize = 12,
            LineBreakMode = LineBreakMode.TailTruncation,
        };
        description.SetBinding(Label.TextProperty, nameof(MapLayer.Description));

        var textStack = new VerticalStackLayout
        {
            Spacing = 0,
            VerticalOptions = LayoutOptions.Center,
            Children = { name, description },
        };

        // Tap the name/description to zoom to the layer.
        var tap = new TapGestureRecognizer();
        tap.Tapped += OnZoomTapped;
        textStack.GestureRecognizers.Add(tap);
        grid.Add(textStack, 2);

        var remove = new Button
        {
            Text = "🗑",
            FontSize = 16,
            BackgroundColor = Colors.Transparent,
            TextColor = SecondaryText,
            WidthRequest = 38,
            HeightRequest = 38,
            Padding = 0,
            VerticalOptions = LayoutOptions.Center,
        };
        remove.Clicked += OnRemoveClicked;
        grid.Add(remove, 3);

        return grid;
    }

    private static Button IconButton(string glyph, EventHandler onClicked)
    {
        var button = new Button
        {
            Text = glyph,
            FontSize = 18,
            BackgroundColor = Colors.Transparent,
            TextColor = PrimaryText,
            WidthRequest = 40,
            HeightRequest = 40,
            Padding = 0,
        };
        button.Clicked += onClicked;
        return button;
    }

    private void OnExpandClicked(object? sender, EventArgs e)
    {
        _expanded = !_expanded;
        _panel.WidthRequest = _expanded ? Math.Max(PanelWidth, _root.Width) : PanelWidth;
        _expandButton.Text = _expanded ? "⤡" : "⤢";

        // Keep it flush when open after a width change.
        if (IsOpen)
        {
            _panel.TranslationX = 0;
        }
    }

    private void OnCloseClicked(object? sender, EventArgs e) => IsOpen = false;

    private void OnColorClicked(object? sender, EventArgs e)
    {
        if (sender is not BindableObject b || b.BindingContext is not MapLayer layer)
        {
            return;
        }

        var currentHex = layer.Color?.ToArgbHex();
        var index = Array.FindIndex(_swatchPalette, c => c.ToArgbHex() == currentHex);
        layer.Color = _swatchPalette[(index + 1) % _swatchPalette.Length];
    }

    private void OnZoomTapped(object? sender, EventArgs e)
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
