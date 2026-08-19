using System.Windows;
using System.Windows.Media;

namespace IRI.Maptor.Jab.Wpf.Helpers;

/// <summary>
/// Resolves the semantic status palette (Status.Light.xaml / Status.Dark.xaml) from code.
/// <para>
/// Converters that hand a Brush back to a binding cannot use DynamicResource, so they have to
/// look the brush up themselves. Before this existed they returned hard-coded literals such as
/// <c>Brushes.Black</c>, which never took part in ThemeHelper's light/dark swap and left the
/// feature table's status column frozen to the light palette.
/// </para>
/// <para>
/// Limitation worth knowing: the brush is resolved when the binding runs, so a theme change
/// after that does not repaint already-realised rows. They pick up the new palette when the
/// binding next re-evaluates (scroll, refresh, reopen). That is a large improvement over a
/// literal, which was wrong in dark theme permanently, but it is not full live theming.
/// </para>
/// </summary>
public static class StatusBrushes
{
    public const string ValidKey = "IRI.Maptor.Brushes.Valid";
    public const string ValidFillKey = "IRI.Maptor.Brushes.Valid.Fill";
    public const string InvalidKey = "IRI.Maptor.Brushes.Invalid";
    public const string InvalidFillKey = "IRI.Maptor.Brushes.Invalid.Fill";
    public const string WarningKey = "IRI.Maptor.Brushes.Warning";
    public const string WarningFillKey = "IRI.Maptor.Brushes.Warning.Fill";
    public const string MutedKey = "IRI.Maptor.Brushes.Muted";
    public const string MutedFillKey = "IRI.Maptor.Brushes.Muted.Fill";

    /// <summary>
    /// Fallbacks mirror Status.Light.xaml. They are only reached when there is no Application
    /// (the XAML designer, a unit test) or before the dictionaries have been merged.
    /// </summary>
    private static readonly Brush FallbackValid = Frozen("#1B7F4B");
    private static readonly Brush FallbackValidFill = Frozen("#E4F2EA");
    private static readonly Brush FallbackInvalid = Frozen("#B3261E");
    private static readonly Brush FallbackInvalidFill = Frozen("#FAE9E8");
    private static readonly Brush FallbackWarning = Frozen("#8A6100");
    private static readonly Brush FallbackWarningFill = Frozen("#FBF0D3");
    private static readonly Brush FallbackMuted = Frozen("#5F6368");
    private static readonly Brush FallbackMutedFill = Frozen("#ECECEE");

    public static Brush Valid => Resolve(ValidKey, FallbackValid);
    public static Brush ValidFill => Resolve(ValidFillKey, FallbackValidFill);
    public static Brush Invalid => Resolve(InvalidKey, FallbackInvalid);
    public static Brush InvalidFill => Resolve(InvalidFillKey, FallbackInvalidFill);
    public static Brush Warning => Resolve(WarningKey, FallbackWarning);
    public static Brush WarningFill => Resolve(WarningFillKey, FallbackWarningFill);
    public static Brush Muted => Resolve(MutedKey, FallbackMuted);
    public static Brush MutedFill => Resolve(MutedFillKey, FallbackMutedFill);

    /// <summary>Body text / glyph colour that follows the theme, for a neutral "no status" row.</summary>
    public static Brush ThemeForeground =>
        Resolve("MahApps.Brushes.ThemeForeground", SystemColors.ControlTextBrush);

    public static Brush Resolve(string key, Brush fallback)
    {
        // Application.Current is null in the designer and in tests.
        var found = Application.Current?.TryFindResource(key) as Brush;

        return found ?? fallback;
    }

    private static Brush Frozen(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

        brush.Freeze();

        return brush;
    }
}
