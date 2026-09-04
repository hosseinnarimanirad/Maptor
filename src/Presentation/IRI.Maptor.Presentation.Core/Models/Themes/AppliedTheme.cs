namespace IRI.Maptor.Presentation.Core.Models;

/// <summary>
/// The accent and mode that are actually applied to the running application.
/// </summary>
/// <remarks>
/// Before this existed nothing could answer "which theme is on right now": callers had to
/// re-read whatever they had persisted, and a caller that only knew half of it silently reset
/// the other half. <see cref="MahAppsThemeColor"/> and <see cref="ThemeMode"/> are always both
/// known here, so <c>ThemeHelper.SetAccent</c> and <c>ThemeHelper.SetMode</c> can each change
/// one without touching the other.
/// </remarks>
public readonly record struct AppliedTheme(MahAppsThemeColor Color, ThemeMode Mode)
{
    /// <summary>What a host gets before it applies anything, and the fallback after a failure.</summary>
    public static AppliedTheme Default => new(MahAppsThemeColor.Amber, ThemeMode.Light);

    /// <summary>The "Light.Amber" form MahApps and Fluent use to name their dictionaries.</summary>
    public override string ToString() => $"{Mode}.{Color}";
}
