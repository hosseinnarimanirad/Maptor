using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace IRI.Maptor.Jab.Maui.Controls;

/// <summary>
/// Base for the dark, SW-Maps-style panels that slide in over the <see cref="MapViewer"/>
/// from the right edge. Handles the open/close slide animation (<see cref="IsOpen"/>), the
/// optional expand-to-full-width toggle, and the shared visual palette. Subclasses build the
/// panel body and call <see cref="SetPanelContent"/>.
///
/// The control occupies only the right-edge panel strip, so the map to the left stays
/// interactive and the panel reliably receives touch input (no full-area InputTransparent
/// overlay, which swallows child touches on Android).
/// </summary>
public abstract class SlideOverSidebar : ContentView
{
    protected const double PanelWidth = 320;
    private const uint SlideDuration = 220;

    // Shared dark palette.
    protected static readonly Color PanelBackground = Color.FromArgb("#222A33");
    protected static readonly Color Accent = Color.FromArgb("#4DB6AC");
    protected static readonly Color PrimaryText = Colors.White;
    protected static readonly Color SecondaryText = Color.FromArgb("#A7B0BA");
    protected static readonly Color Divider = Color.FromArgb("#384450");

    private readonly Border _panel;
    private Button? _expandButton;
    private bool _expanded;

    protected SlideOverSidebar()
    {
        _panel = new Border
        {
            BackgroundColor = PanelBackground,
            StrokeThickness = 0,
            WidthRequest = PanelWidth,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Fill,
            Padding = new Thickness(12, 10),
        };

        HorizontalOptions = LayoutOptions.End;
        VerticalOptions = LayoutOptions.Fill;
        Content = _panel;

        // Start hidden (collapsed) off the right edge.
        IsVisible = false;
        _panel.TranslationX = PanelWidth;
    }

    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(
        nameof(IsOpen), typeof(bool), typeof(SlideOverSidebar), false, BindingMode.TwoWay, propertyChanged: OnIsOpenChanged);

    /// <summary>Whether the panel is slid in (open). Animate by toggling this.</summary>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Sets the body rendered inside the sliding panel.</summary>
    protected void SetPanelContent(View content) => _panel.Content = content;

    /// <summary>A flat, white-glyph icon button used for header actions.</summary>
    protected static Button IconButton(string glyph, EventHandler onClicked)
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

    /// <summary>A "✕" button that closes the panel.</summary>
    protected Button CreateCloseButton() => IconButton("✕", (_, _) => IsOpen = false);

    /// <summary>A "⤢" button that toggles the panel between the right strip and full width.</summary>
    protected Button CreateExpandButton()
    {
        _expandButton = IconButton("⤢", OnExpandClicked);
        return _expandButton;
    }

    private double CurrentPanelWidth
        => _panel.Width > 0 ? _panel.Width
            : _panel.WidthRequest > 0 ? _panel.WidthRequest
            : PanelWidth;

    private static async void OnIsOpenChanged(BindableObject bindable, object oldValue, object newValue)
    {
        await ((SlideOverSidebar)bindable).AnimateAsync((bool)newValue);
    }

    private async Task AnimateAsync(bool open)
    {
        if (open)
        {
            IsVisible = true;
            await _panel.TranslateTo(0, 0, SlideDuration, Easing.CubicOut);
        }
        else
        {
            await _panel.TranslateTo(CurrentPanelWidth, 0, SlideDuration, Easing.CubicIn);
            IsVisible = false;
        }
    }

    private void OnExpandClicked(object? sender, EventArgs e)
    {
        _expanded = !_expanded;

        if (_expanded)
        {
            // Grow to fill the whole map area.
            HorizontalOptions = LayoutOptions.Fill;
            _panel.HorizontalOptions = LayoutOptions.Fill;
            _panel.WidthRequest = -1;
        }
        else
        {
            // Back to the right-edge strip.
            HorizontalOptions = LayoutOptions.End;
            _panel.HorizontalOptions = LayoutOptions.End;
            _panel.WidthRequest = PanelWidth;
        }

        if (_expandButton is not null)
        {
            _expandButton.Text = _expanded ? "⤡" : "⤢";
        }

        // Keep it flush when open after a width change.
        if (IsOpen)
        {
            _panel.TranslationX = 0;
        }
    }
}
