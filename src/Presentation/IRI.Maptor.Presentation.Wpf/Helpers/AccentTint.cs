using System;
using System.Windows.Media;

namespace IRI.Maptor.Presentation.Wpf.Helpers;

/// <summary>
/// Light tints of the theme accent for the header bands of cards, panels and dialogs (2026-11-10, the main-layout
/// polish): a solid accent bar was too saturated, a neutral band too colourless. <see cref="ThemeHelper"/> computes
/// the brushes on every theme or accent change and puts them in the application resources under these keys;
/// consume them with <c>DynamicResource</c>. Status.Light/Dark.xaml hold neutral fallbacks for a host that never
/// applies a theme.
/// </summary>
/// <remarks>
/// Kept apart from <see cref="ThemeHelper"/> on purpose: touching that class starts ControlzEx's theme manager, which
/// is bound to the thread it starts on, so the arithmetic here can be used (and tested) without it.
/// </remarks>
public static class AccentTint
{
    /// <summary>Header band: the accent at a tenth (light) or a sixth (dark) over the theme background.</summary>
    public const string Key = "IRI.Maptor.Brushes.Accent.Tint";

    /// <summary>The header band under the mouse.</summary>
    public const string HoverKey = "IRI.Maptor.Brushes.Accent.Tint.Hover";

    /// <summary>The rule under a tinted header band.</summary>
    public const string BorderKey = "IRI.Maptor.Brushes.Accent.Tint.Border";

    // how much accent each brush takes; the dark theme needs more for the same visible step
    public const double LightAmount = 0.10, LightHoverAmount = 0.17, LightBorderAmount = 0.30;

    public const double DarkAmount = 0.16, DarkHoverAmount = 0.24, DarkBorderAmount = 0.40;

    /// <summary><paramref name="amount"/> (0 to 1) of <paramref name="accent"/> over <paramref name="background"/>, opaque.</summary>
    public static Color Blend(Color background, Color accent, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);

        byte Mix(byte b, byte a) => (byte)Math.Round(b + (a - b) * amount);

        return Color.FromRgb(Mix(background.R, accent.R), Mix(background.G, accent.G), Mix(background.B, accent.B));
    }

    /// <summary>A frozen brush of <see cref="Blend"/>.</summary>
    public static SolidColorBrush BlendBrush(Color background, Color accent, double amount)
    {
        var brush = new SolidColorBrush(Blend(background, accent, amount));
        brush.Freeze();
        return brush;
    }
}
