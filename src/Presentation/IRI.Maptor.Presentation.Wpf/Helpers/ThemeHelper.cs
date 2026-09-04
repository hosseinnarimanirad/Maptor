using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using ControlzEx.Theming;
using IRI.Maptor.Presentation.Core.Models;

namespace IRI.Maptor.Presentation.Wpf.Helpers;

/// <summary>
/// Applies a MahApps accent + light/dark mode to the running application, remembers which one is
/// on, and keeps the Maptor status palette in step with it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Read this before adding a call.</b> Until 2026-08-31 the only entry point was
/// <c>ApplyTheme(colour, mode = null)</c> with <c>mode ??= ThemeMode.Light</c> inside it, so a
/// caller that knew only the accent silently reset the whole application to light. That bug was
/// found and fixed once in the host application, then found again in four more call sites. The cause was
/// structural: nothing could answer "which mode is on right now", so a partial caller had no way
/// to preserve the other half. Now <see cref="Current"/> always holds both halves, an omitted
/// argument means <i>keep what is applied</i>, and <see cref="SetAccent"/> / <see cref="SetMode"/>
/// exist so a caller that only cares about one half never has to mention the other.
/// </para>
/// <para>
/// <b>The dictionary swapping is not ours.</b> This used to hand-build
/// <c>pack://…/Styles/Themes/{mode}.{accent}.xaml</c>, remove the old entry by URL substring and
/// append the new one, once for MahApps and again for Fluent.Ribbon. That is what
/// <see cref="ThemeManager"/> already does, better: one
/// <see cref="ThemeManager.ChangeTheme(Application, string, bool)"/> themes MahApps <i>and</i>
/// Fluent (measured: Fluent's <c>LabelTextBrush</c> follows), it removes the host's statically
/// merged theme dictionaries rather than shadowing them, and it raises
/// <see cref="ThemeManager.ThemeChanged"/>. What is left here is the part ControlzEx cannot know
/// about — the Maptor semantic status palette — plus the state and events the callers need.
/// </para>
/// <para>
/// Because the status palette is driven from <see cref="ThemeManager.ThemeChanged"/> rather than
/// from our own entry point, it now follows a theme change made by <i>anyone</i>: this class, a
/// host calling <see cref="ThemeManager"/> directly (that application does), or the Windows theme
/// changing under <see cref="FollowWindowsMode"/>.
/// </para>
/// </remarks>
public static class ThemeHelper
{
    private static AppliedTheme _current = AppliedTheme.Default;

    /// <summary>
    /// Available MahApps themes with their display names and accent colors.
    /// Colors are ordered in rainbow order.
    /// </summary>
    public static readonly List<ThemeInfoModel> AvailableThemes;

    /// <summary>
    /// The accent and mode currently applied. Before the first successful apply this is
    /// <see cref="AppliedTheme.Default"/> (Light.Amber), which is what a host that never calls in
    /// actually renders as.
    /// </summary>
    public static AppliedTheme Current => _current;

    /// <summary>
    /// True once a theme has actually been applied, as opposed to <see cref="Current"/> merely
    /// reporting the default.
    /// </summary>
    public static bool IsApplied { get; private set; }

    /// <summary>
    /// Raised after a theme has been applied, with the new value of <see cref="Current"/>.
    /// Not raised when the requested theme is already the applied one.
    /// </summary>
    /// <remarks>
    /// Anything that derives a colour in C# rather than through <c>DynamicResource</c> —
    /// <c>Helpers/StatusBrushes.cs</c>, converters that pick a brush by status — can subscribe
    /// instead of re-reading settings or probing resources.
    /// </remarks>
    public static event Action<AppliedTheme>? ThemeChanged;

    /// <summary>
    /// Where theme failures are reported. Replace it to route them into the host's logger; the
    /// default writes to <see cref="Debug"/> and <see cref="Trace"/>.
    /// </summary>
    public static Action<string, Exception?> Log { get; set; } = DefaultLog;

    static ThemeHelper()
    {
        AvailableThemes = Enum.GetValues<MahAppsThemeColor>()
                            .Where(i => i != MahAppsThemeColor.Brown)
                            .Select(i => new ThemeInfoModel(i, _current.Mode))
                            .ToList();

        // the single place the status palette and our own state are kept in step, whoever changed
        // the theme
        ThemeManager.Current.ThemeChanged += OnThemeManagerThemeChanged;
    }

    /// <summary>
    /// Follow the Windows "app mode" light/dark setting, and keep following it while the user
    /// changes it in Settings. Off by default: turning it on changes what an existing application
    /// looks like on first run, which is the host's decision, not the library's.
    /// </summary>
    /// <remarks>
    /// Only the light/dark half is synced, never the Windows accent colour — the accent here is a
    /// user choice from <see cref="AvailableThemes"/> and syncing would silently discard it.
    /// <see cref="SetMode"/> switches this off, because an explicit choice has to outlive the next
    /// Windows preference change; re-set it to true for a "System" option.
    /// </remarks>
    public static bool FollowWindowsMode
    {
        get => ThemeManager.Current.ThemeSyncMode == ThemeSyncMode.SyncWithAppMode;
        set
        {
            ThemeManager.Current.ThemeSyncMode = value ? ThemeSyncMode.SyncWithAppMode : ThemeSyncMode.DoNotSync;

            if (!value)
                return;

            try
            {
                // adopt the Windows setting now; ControlzEx keeps it in step from here
                ThemeManager.Current.SyncTheme();
            }
            catch (Exception ex)
            {
                Log("could not sync with the Windows app mode.", ex);
            }
        }
    }

    /// <summary>
    /// What Windows itself is set to, independent of what this application is showing.
    /// </summary>
    public static ThemeMode WindowsMode =>
        WindowsThemeHelper.AppsUseLightTheme() ? ThemeMode.Light : ThemeMode.Dark;

    /// <summary>
    /// Changes the accent and keeps the current <see cref="ThemeMode"/>.
    /// </summary>
    public static void SetAccent(MahAppsThemeColor color) => ApplyTheme(color, null);

    /// <summary>
    /// Changes light/dark and keeps the current accent. Switches <see cref="FollowWindowsMode"/>
    /// off: an explicit choice must survive the next Windows preference change.
    /// </summary>
    public static void SetMode(ThemeMode mode)
    {
        if (FollowWindowsMode)
            FollowWindowsMode = false;

        ApplyTheme(null, mode);
    }

    /// <summary>
    /// Applies a theme. <b>A null argument means "keep whatever is applied"</b>, not "use the
    /// default" — pass both when you mean both, or use <see cref="SetAccent"/> /
    /// <see cref="SetMode"/> when you mean one.
    /// </summary>
    public static void ApplyTheme(MahAppsThemeColor? color, ThemeMode? mode = null)
    {
        Apply(new AppliedTheme(color ?? _current.Color, mode ?? _current.Mode), allowFallback: true);
    }

    private static void Apply(AppliedTheme target, bool allowFallback)
    {
        var app = Application.Current;

        if (app?.Resources is null)
        {
            Log($"cannot apply {target}: Application.Current or its Resources is null.", null);
            return;
        }

        if (IsApplied && target == _current)
            return;

        Theme? applied;

        try
        {
            // themes MahApps and Fluent.Ribbon together, and drops the host's statically merged
            // theme dictionaries instead of leaving them to shadow anything
            applied = ThemeManager.Current.ChangeTheme(app, target.ToString());
        }
        catch (Exception ex)
        {
            Log($"could not apply {target}; the previous theme is left in place.", ex);
            FallBack(target, allowFallback);
            return;
        }

        if (applied is null)
        {
            Log($"ThemeManager does not know a theme named {target}; the previous theme is left in place.", null);
            FallBack(target, allowFallback);
            return;
        }

        // ChangeTheme raises ThemeChanged, so Adopt has normally already run. Call it anyway:
        // ControlzEx does not raise the event when the requested theme is already applied, and
        // this class may still be out of step with it (a host that called ThemeManager directly).
        Adopt(applied);
    }

    private static void FallBack(AppliedTheme failed, bool allowFallback)
    {
        if (allowFallback && failed != AppliedTheme.Default)
            Apply(AppliedTheme.Default, allowFallback: false);
    }

    private static void OnThemeManagerThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        // per-window theming is a different thing and must not move the application's palette
        if (e.Target is not Application)
            return;

        Adopt(e.NewTheme);
    }

    /// <summary>
    /// Brings this class, the status palette and subscribers in line with a theme that has already
    /// been applied — by us, by a host calling <see cref="ThemeManager"/> directly, or by the
    /// Windows theme changing. Idempotent, so the double call from
    /// <see cref="Apply"/> costs nothing.
    /// </summary>
    private static void Adopt(Theme theme)
    {
        var target = Translate(theme);

        if (IsApplied && target == _current)
            return;

        ApplyStatusPalette(target);

        _current = target;
        IsApplied = true;

        // the picker paints its tiles from these
        foreach (var info in AvailableThemes)
            info.Mode = target.Mode;

        try
        {
            ThemeChanged?.Invoke(target);
        }
        catch (Exception ex)
        {
            // a badly behaved subscriber must not leave the theme half applied
            Log($"a {nameof(ThemeChanged)} subscriber threw after {target} was applied.", ex);
        }
    }

    /// <summary>
    /// Maps a ControlzEx theme onto our own pair. A runtime-generated accent (see
    /// <c>RuntimeThemeGenerator</c>) has no <see cref="MahAppsThemeColor"/> member, so the mode is
    /// taken and the recorded accent is left alone rather than guessed at.
    /// </summary>
    private static AppliedTheme Translate(Theme theme)
    {
        var mode = string.Equals(theme.BaseColorScheme, "Dark", StringComparison.OrdinalIgnoreCase)
            ? ThemeMode.Dark
            : ThemeMode.Light;

        return Enum.TryParse<MahAppsThemeColor>(theme.ColorScheme, ignoreCase: true, out var color)
            ? new AppliedTheme(color, mode)
            : new AppliedTheme(_current.Color, mode);
    }

    /// <summary>
    /// Our own semantic status palette (valid / invalid / warning / muted). MahApps has no such
    /// colours, so ControlzEx cannot swap them and this is the one piece of theming still done by
    /// hand.
    /// </summary>
    /// <remarks>
    /// The entry ends up at the TOP level of <c>Application.Resources.MergedDictionaries</c>: WPF
    /// searches a merge list last-to-first, so the appended entry is the one that wins, and it
    /// survives a <see cref="ThemeManager"/> change (measured — ControlzEx only removes the theme
    /// dictionaries it recognises). A copy nested inside <c>Assets/Maptor.All.xaml</c> is
    /// deliberate and harmless: it is the fallback for a host that never applies a theme. See the
    /// design-system README section 4.4c.
    /// </remarks>
    private static void ApplyStatusPalette(AppliedTheme target)
    {
        var app = Application.Current;

        if (app?.Resources is null)
            return;

        ResourceDictionary status;

        try
        {
            status = LoadDictionary(
                $"pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/Styles/Status.{target.Mode}.xaml");
        }
        catch (Exception ex)
        {
            // not fatal: every status key is consumed with DynamicResource, so a missing palette
            // leaves those properties at their defaults rather than throwing. It IS a bug though.
            Log($"the {target.Mode} status palette could not be loaded; valid/invalid colours will "
                + "not follow the theme.", ex);
            return;
        }

        var merged = app.Resources.MergedDictionaries;

        var stale = merged
            .Where(rd => rd.Source is not null && rd.Source.ToString().Contains("/Assets/Styles/Status."))
            .ToList();

        foreach (var dictionary in stale)
            merged.Remove(dictionary);

        merged.Add(status);
    }

    private static ResourceDictionary LoadDictionary(string packUri)
    {
        var dictionary = new ResourceDictionary { Source = new Uri(packUri, UriKind.Absolute) };

        // touching Count forces the deferred load, so a broken URI throws here rather than later
        _ = dictionary.Count;

        return dictionary;
    }

    private static void DefaultLog(string message, Exception? ex)
    {
        var text = "ThemeHelper: " + message + (ex is null ? string.Empty : " " + ex);

        Debug.WriteLine(text);
        Trace.TraceWarning(text);
    }
}
