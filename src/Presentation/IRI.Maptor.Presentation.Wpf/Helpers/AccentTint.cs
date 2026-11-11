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
    /// <summary>Header band: the accent at about a sixth (light) or a fifth (dark) over the theme background.</summary>
    public const string Key = "IRI.Maptor.Brushes.Accent.Tint";

    /// <summary>The header band under the mouse.</summary>
    public const string HoverKey = "IRI.Maptor.Brushes.Accent.Tint.Hover";

    /// <summary>The rule under a tinted header band.</summary>
    public const string BorderKey = "IRI.Maptor.Brushes.Accent.Tint.Border";

    // how much accent each brush takes; the dark theme needs more for the same visible step.
    // 2026-11-11: raised from 10/17/30 % (light) and 16/24/40 % (dark), which the user found too pale.
    public const double LightAmount = 0.18, LightHoverAmount = 0.26, LightBorderAmount = 0.40;

    public const double DarkAmount = 0.22, DarkHoverAmount = 0.30, DarkBorderAmount = 0.48;

    /// <summary>
    /// Text on an accent pill (<c>Pill.Text.Accent</c>, on the <c>MahApps.Brushes.Accent4</c> fill). Light: the accent
    /// itself. Dark: the theme foreground, because accent text on the dark accent fill measured about 2.8:1 contrast
    /// (2026-11-12, user choice: dark only, light keeps its accent text).
    /// </summary>
    public const string TextKey = "IRI.Maptor.Brushes.Accent.Text";

    /// <summary>The <see cref="TextKey"/> brush: <paramref name="accent"/> in light, <paramref name="foreground"/> in dark. Frozen.</summary>
    public static SolidColorBrush TextOnAccentFill(Color accent, Color foreground, bool dark)
    {
        var brush = new SolidColorBrush(dark ? foreground : accent);
        brush.Freeze();
        return brush;
    }

    /// <summary>
    /// MahApps' key for the veil laid over a disabled input (TextBox, ComboBox, NumericUpDown, …). MahApps uses 65 %
    /// white (#A5FFFFFF) in both themes, which turns a disabled input into a light grey slab on a dark window
    /// (2026-11-11). In the dark theme <see cref="ThemeHelper"/> replaces it with <see cref="DisabledOverlay"/>.
    /// </summary>
    public const string DisabledOverlayKey = "MahApps.Brushes.Control.Disabled";

    /// <summary>How much of the theme background the dark disabled veil keeps (about 60 %).</summary>
    public const byte DisabledOverlayAlpha = 0x99;

    /// <summary>
    /// The disabled veil for a dark theme: the theme background, semi-transparent, so a disabled input fades into
    /// the surface instead of lighting up. Frozen.
    /// </summary>
    public static SolidColorBrush DisabledOverlay(Color background)
    {
        var brush = new SolidColorBrush(Color.FromArgb(DisabledOverlayAlpha, background.R, background.G, background.B));
        brush.Freeze();
        return brush;
    }

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
