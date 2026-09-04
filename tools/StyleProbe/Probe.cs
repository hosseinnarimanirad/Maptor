using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ControlzEx.Theming;
using IRI.Maptor.Presentation.Core.Models;
using IRI.Maptor.Presentation.Wpf.Helpers;

// The Maptor design-system style probe.
//
// WHY THIS EXISTS. Building proves nothing about XAML: a StaticResource resolves at RUNTIME, so a
// missing, misspelled or unreachable key compiles cleanly and throws when the view is first
// realised. Every step of the design-system remediation was verified by constructing the affected
// views in a real Application, and every step re-authored a throwaway probe to do it. This is that
// probe, kept.
//
// It asserts the contracts that were expensive to establish, each of which has been broken at
// least once:
//
//   1  a host that merges the MahApps baseline + Assets/Maptor.All.xaml gets a working application
//   2  ThemeHelper's API contract: a null argument keeps what is applied, it never resets it
//   3  ThemeHelper drives ControlzEx correctly and the status palette follows anyone's change
//   4  the views and styles actually realise
//
// Run:   dotnet run --project tools/StyleProbe
// Exit:  0 = PASS, 1 = FAIL
static class Probe
{
    static int _fail;
    static Application _app;

    static void Check(string what, bool ok, string detail = null)
    {
        Console.WriteLine((ok ? "  ok   " : "  FAIL ") + what + (detail == null ? "" : "   [" + detail + "]"));
        if (!ok) _fail++;
    }

    static string Hex(object o)
    {
        var b = o as SolidColorBrush;
        return b == null ? "<" + (o == null ? "null" : o.GetType().Name) + ">" : b.Color.ToString();
    }

    static ResourceDictionary Load(string uri)
    {
        return new ResourceDictionary { Source = new Uri(uri, UriKind.Absolute) };
    }

    // Status.Light.xaml / Status.Dark.xaml. If these change, this probe should fail and be updated
    // deliberately -- they are the whole point of the semantic palette.
    const string ValidLight = "#FF1B7F4B";
    const string ValidDark = "#FF4CC38A";

    static string Mah() => Hex(_app.TryFindResource("MahApps.Brushes.ThemeBackground"));
    static string Fluent() => Hex(_app.TryFindResource("Fluent.Ribbon.Brushes.LabelTextBrush"));
    static string Status() => Hex(_app.TryFindResource("IRI.Maptor.Brushes.Valid"));
    static string Accent() => Hex(_app.TryFindResource("MahApps.Brushes.Accent"));

    static int Dicts(string marker) => _app.Resources.MergedDictionaries
        .Count(d => d.Source != null && d.Source.ToString().Contains(marker));

    [STAThread]
    static int Main()
    {
        _app = new Application { Resources = new ResourceDictionary() };

        BeforeAnythingIsApplied();
        MergeLikeAHost();
        HostWiring();
        Typography();
        ThemeApiContract();
        ThemeManagerIntegration();
        ViewsRealise();

        Console.WriteLine();
        Console.WriteLine(_fail == 0 ? "RESULT: PASS" : "RESULT: FAIL (" + _fail + ")");
        return _fail == 0 ? 0 : 1;
    }

    static void BeforeAnythingIsApplied()
    {
        Console.WriteLine();
        Console.WriteLine("0. ThemeHelper before a theme is applied");
        Check("Current is the documented default Light.Amber",
              ThemeHelper.Current == AppliedTheme.Default, ThemeHelper.Current.ToString());
        Check("IsApplied is false", !ThemeHelper.IsApplied);
        Check("FollowWindowsMode is off by default", !ThemeHelper.FollowWindowsMode);
        Console.WriteLine("       (this machine's Windows mode is " + ThemeHelper.WindowsMode + ")");
    }

    /// <summary>
    /// Exactly what samples/IRI.Maptor.Samples.Wpf.HelloMap/App.xaml declares, plus the Fluent
    /// dictionaries the three ribbon applications carry.
    /// </summary>
    static void MergeLikeAHost()
    {
        _app.Resources.MergedDictionaries.Add(Load("pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml"));
        _app.Resources.MergedDictionaries.Add(Load("pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml"));
        _app.Resources.MergedDictionaries.Add(Load("pack://application:,,,/MahApps.Metro;component/Styles/Themes/Light.Amber.xaml"));
        _app.Resources.MergedDictionaries.Add(Load("pack://application:,,,/Fluent;component/Themes/Generic.xaml"));
        _app.Resources.MergedDictionaries.Add(Load("pack://application:,,,/Fluent;component/Themes/Themes/Light.Amber.xaml"));
        _app.Resources.MergedDictionaries.Add(Load("pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/Maptor.All.xaml"));
    }

    static void HostWiring()
    {
        Console.WriteLine();
        Console.WriteLine("1. one Maptor.All.xaml gives a host everything it used to wire by hand");

        Check("the Localization provider resolves", _app.TryFindResource("Localization") != null);
        Check("MahApps.Brushes.Accent", _app.TryFindResource("MahApps.Brushes.Accent") != null);
        Check("MahApps.Styles.Button.Flat", _app.TryFindResource("MahApps.Styles.Button.Flat") != null);
        foreach (var k in new[] { "MahApps.Colors.Accent", "MahApps.Colors.Highlight", "MahApps.Brushes.Gray3" })
            Check(k, _app.TryFindResource(k) != null, "" + _app.TryFindResource(k));

        // the five styles that are BasedOn a MahApps key. These are why the MahApps merges cannot
        // move inside Maptor.All.xaml: a StaticResource cannot reach a nested sibling.
        foreach (var key in new[]
        {
            "IRI.Maptor.Styles.ButtonBase",
            "IRI.Maptor.Styles.Button.Primary.Large",
            "IRI.Maptor.Styles.PasswordBoxBase",
            "IRI.Maptor.Styles.TextBox.Normal",
            "IRI.Maptor.Styles.Border.Section",
            "IRI.Maptor.Styles.TextBlock.SectionHeader",
            "IRI.Maptor.Styles.Thickness.DialogContent",
            "IRI.Maptor.Styles.CornerRadius.Control",
            "IRI.Maptor.Styles.GridLength.FieldLabelColumn",
            "IRI.Maptor.Brushes.OnMap.Text",
        })
        {
            try { Check(key, _app.TryFindResource(key) != null); }
            catch (Exception ex) { Check(key, false, (ex.InnerException ?? ex).Message); }
        }

        // Controls.Textbox.xaml was renamed to Controls.TextBox.xaml on 2026-08-31 to match the nine
        // references that already spelled it that way. Assert the styles it defines still resolve:
        // the rename changes a pack-URI resource name, which the compiler cannot check.
        foreach (var key in new[] { "IRI.Maptor.Styles.TextBoxBase", "IRI.Maptor.Styles.TextBox.Large" })
            Check(key + " (from the renamed dictionary)", _app.TryFindResource(key) != null);

        // and the fonts: FontAwesome.ttf and its FontFamily key were deleted as dead. A rename or a
        // deletion is only half-verified by checking what should exist; assert the NEGATIVE too.
        Check("bYekan still resolves", _app.TryFindResource("bYekan") != null);
        Check("iranSans still resolves", _app.TryFindResource("iranSans") != null);
        Check("FontAwesome is GONE", _app.TryFindResource("FontAwesome") == null,
              "" + _app.TryFindResource("FontAwesome"));

        Console.WriteLine();
        Console.WriteLine("   the status palette falls back to LIGHT when no theme is applied");
        Check("Valid = " + ValidLight, Status() == ValidLight, Status());
        Check("Invalid = #FFB3261E", Hex(_app.TryFindResource("IRI.Maptor.Brushes.Invalid")) == "#FFB3261E",
              Hex(_app.TryFindResource("IRI.Maptor.Brushes.Invalid")));
        Check("Muted = #FF5F6368", Hex(_app.TryFindResource("IRI.Maptor.Brushes.Muted")) == "#FF5F6368",
              Hex(_app.TryFindResource("IRI.Maptor.Brushes.Muted")));
    }


    /// <summary>
    /// The type scale. Every FontSize in the library's own styles resolves through
    /// IRI.Maptor.Styles.Size.Text.*; this asserts the tokens exist with the right values and that
    /// all 42 setters actually resolved.
    /// </summary>
    /// <remarks>
    /// A StaticResource that cannot be resolved does not fall back, it throws when the dictionary
    /// loads -- and six of these dictionaries had to gain a Common.Metrics.xaml merge to see the
    /// tokens at all. That is exactly the failure a build cannot see.
    /// </remarks>
    static void Typography()
    {
        Console.WriteLine();
        Console.WriteLine("1b. the type scale");

        var scale = new Dictionary<string, double>
        {
            { "Micro", 10 }, { "Caption", 11 }, { "Small", 12 }, { "Body", 13 },
            { "BodyLarge", 14 }, { "Input", 15 }, { "Action", 16 }, { "Title", 20 },
        };

        foreach (var step in scale)
        {
            var key = "IRI.Maptor.Styles.Size.Text." + step.Key;
            var value = _app.TryFindResource(key);
            Check(key + " = " + step.Value, value is double && (double)value == step.Value,
                  value == null ? "missing" : value.ToString());
        }

        // Every FontSize setter in the LIBRARY's own styles must be one of those steps.
        //
        // Only IRI.Maptor.* keys: MahApps sets FontSize through DynamicResource and Binding on its
        // own styles, which is correct for it and would otherwise read as 56 failures here.
        // Deduplicated by resource key, because Controls.Pill and Controls.Section both merge
        // Controls.TextBlock (they are BasedOn its styles), so a naive walk counts it three times.
        var dictionaries = new List<ResourceDictionary>();
        try
        {
            dictionaries.Add(Load("pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/Styles/Controls.All.xaml"));
            // not in Controls.All by design (FieldGroup binds to an ancestor named "root")
            dictionaries.Add(Load("pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/Styles/Controls.SecurityInputs.xaml"));
        }
        catch (Exception ex)
        {
            Check("the style dictionaries load", false, (ex.InnerException ?? ex).Message);
            return;
        }

        var found = new Dictionary<string, object>();
        var seen = new HashSet<ResourceDictionary>();
        foreach (var d in dictionaries) CollectFontSizes(d, found, seen);

        var unresolved = found.Where(kv => !(kv.Value is double))
                              .Select(kv => kv.Key + " -> " + (kv.Value == null ? "null" : kv.Value.GetType().Name))
                              .ToList();
        Check("no Maptor FontSize setter failed to resolve", unresolved.Count == 0, string.Join(", ", unresolved));

        var sizes = found.Values.OfType<double>().ToList();

        var offScale = sizes.Where(v => !scale.Values.Contains(v)).Distinct().ToList();
        Check("every Maptor FontSize setter is on the scale", offScale.Count == 0,
              string.Join(", ", offScale.Select(v => v.ToString())));

        Check("all 42 setters are accounted for", found.Count == 42, "found " + found.Count);

        Console.WriteLine("       distribution: " + string.Join("  ", sizes.GroupBy(v => v)
            .OrderBy(g => g.Key).Select(g => g.Key + "x" + g.Count())));
    }

    /// <summary>
    /// Walks a dictionary and its merges, recording the FontSize setter of every
    /// IRI.Maptor.* style by key. Keyed rather than listed so a dictionary merged by two parents
    /// is counted once.
    /// </summary>
    static void CollectFontSizes(ResourceDictionary d, Dictionary<string, object> found,
                                 HashSet<ResourceDictionary> seen)
    {
        if (!seen.Add(d)) return;

        foreach (var rawKey in d.Keys)
        {
            var key = rawKey as string;
            if (key == null || !key.StartsWith("IRI.Maptor.")) continue;

            var style = d[rawKey] as Style;
            if (style == null) continue;

            foreach (var setterBase in style.Setters)
            {
                var setter = setterBase as Setter;
                if (setter == null || setter.Property == null) continue;
                if (setter.Property.Name != "FontSize") continue;

                found[key] = setter.Value;
            }
        }

        foreach (var m in d.MergedDictionaries) CollectFontSizes(m, found, seen);
    }

    static readonly List<AppliedTheme> Raised = new List<AppliedTheme>();

    static void ThemeApiContract()
    {
        Console.WriteLine();
        Console.WriteLine("2. ThemeHelper: a null argument KEEPS what is applied, it never resets it");

        ThemeHelper.ThemeChanged += t => Raised.Add(t);

        ThemeHelper.ApplyTheme(MahAppsThemeColor.Cobalt, ThemeMode.Dark);
        Check("explicit apply sets both halves", ThemeHelper.Current.ToString() == "Dark.Cobalt", ThemeHelper.Current.ToString());
        Check("IsApplied is true", ThemeHelper.IsApplied);
        Check("ThemeChanged raised exactly once", Raised.Count == 1, "count=" + Raised.Count);
        Check("MahApps went dark", Mah() == "#FF252525", Mah());
        Check("the status palette went dark", Status() == ValidDark, Status());
        Check("AvailableThemes followed the mode", ThemeHelper.AvailableThemes.All(t => t.Mode == ThemeMode.Dark));

        // the regression that cost five call sites: an accent change must not silently go light.
        // Scored on ThemeBackground and the status palette, NOT on the accent -- the MahApps accent
        // is byte-identical in light and dark, so it proves nothing here.
        ThemeHelper.SetAccent(MahAppsThemeColor.Emerald);
        Check("SetAccent keeps the mode", ThemeHelper.Current.ToString() == "Dark.Emerald", ThemeHelper.Current.ToString());
        Check("  and the screen is still dark", Mah() == "#FF252525" && Status() == ValidDark, Mah() + " / " + Status());

        ThemeHelper.ApplyTheme(MahAppsThemeColor.Crimson);
        Check("a bare ApplyTheme(colour) keeps the mode too", ThemeHelper.Current.ToString() == "Dark.Crimson",
              ThemeHelper.Current.ToString());

        var accentBefore = Accent();
        ThemeHelper.SetMode(ThemeMode.Light);
        Check("SetMode keeps the accent", ThemeHelper.Current.ToString() == "Light.Crimson", ThemeHelper.Current.ToString());
        Check("  background went light", Mah() == "#FFFFFFFF", Mah());
        Check("  the status palette went light", Status() == ValidLight, Status());
        Check("  the accent is unchanged across the flip", Accent() == accentBefore, accentBefore + " -> " + Accent());

        var before = Raised.Count;
        ThemeHelper.ApplyTheme(MahAppsThemeColor.Crimson, ThemeMode.Light);
        ThemeHelper.SetAccent(MahAppsThemeColor.Crimson);
        Check("re-applying the same theme raises nothing", Raised.Count == before,
              "raised " + (Raised.Count - before) + " extra");
    }

    static void ThemeManagerIntegration()
    {
        Console.WriteLine();
        Console.WriteLine("3. ThemeHelper drives ControlzEx, and the palette follows anyone's change");

        ThemeHelper.ApplyTheme(MahAppsThemeColor.Cobalt, ThemeMode.Dark);
        Check("one call themes Fluent.Ribbon too", Fluent() == "#FFFFFFFF", Fluent());
        ThemeHelper.SetMode(ThemeMode.Light);
        Check("  and back", Fluent() == "#FF000000", Fluent());

        var before = Raised.Count;
        ThemeManager.Current.ChangeTheme(_app, "Dark.Teal");
        Check("an EXTERNAL ThemeManager change moves Current", ThemeHelper.Current.ToString() == "Dark.Teal",
              ThemeHelper.Current.ToString());
        Check("  the status palette follows it", Status() == ValidDark, Status());
        Check("  our ThemeChanged fires once for it", Raised.Count == before + 1, "raised " + (Raised.Count - before));

        Console.WriteLine();
        ThemeHelper.ApplyTheme(MahAppsThemeColor.Cobalt, ThemeMode.Dark);
        var windows = ThemeHelper.WindowsMode;
        ThemeHelper.FollowWindowsMode = true;
        Check("FollowWindowsMode reports itself on", ThemeHelper.FollowWindowsMode);
        Check("  it adopted the Windows mode (" + windows + ")", ThemeHelper.Current.Mode == windows,
              ThemeHelper.Current.ToString());
        Check("  it kept the chosen accent", ThemeHelper.Current.Color == MahAppsThemeColor.Cobalt,
              ThemeHelper.Current.ToString());
        Check("  the status palette matches the Windows mode",
              Status() == (windows == ThemeMode.Light ? ValidLight : ValidDark), Status());

        var opposite = windows == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light;
        ThemeHelper.SetMode(opposite);
        Check("an explicit SetMode turns the sync off", !ThemeHelper.FollowWindowsMode);
        Check("  and the explicit mode wins", ThemeHelper.Current.Mode == opposite, ThemeHelper.Current.ToString());

        Console.WriteLine();
        var accentEnumBefore = ThemeHelper.Current.Color;
        try
        {
            var moss = (Color)ColorConverter.ConvertFromString("#2E5236");
            var generated = RuntimeThemeGenerator.Current.GenerateRuntimeTheme("Dark", moss);
            ThemeManager.Current.ChangeTheme(_app, generated);
            Check("a runtime-generated accent still yields its mode", ThemeHelper.Current.Mode == ThemeMode.Dark,
                  ThemeHelper.Current.ToString());
            Check("  and leaves Current.Color at the last known enum value",
                  ThemeHelper.Current.Color == accentEnumBefore, ThemeHelper.Current.ToString());
            Check("  and the status palette still follows", Status() == ValidDark, Status());
        }
        catch (Exception ex) { Check("a runtime-generated accent is handled", false, ex.Message); }

        Console.WriteLine();
        for (var i = 0; i < 10; i++)
            ThemeHelper.ApplyTheme(i % 2 == 0 ? MahAppsThemeColor.Amber : MahAppsThemeColor.Cobalt,
                                   i % 2 == 0 ? ThemeMode.Dark : ThemeMode.Light);

        Check("10 swaps leave exactly one status dictionary", Dicts("/Assets/Styles/Status.") == 1,
              "count=" + Dicts("/Assets/Styles/Status."));
        Check("  no statically merged MahApps theme left behind",
              Dicts("/MahApps.Metro;component/Styles/Themes/") == 0,
              "count=" + Dicts("/MahApps.Metro;component/Styles/Themes/"));
        Check("  no statically merged Fluent theme left behind",
              Dicts("/Fluent;component/Themes/Themes/") == 0,
              "count=" + Dicts("/Fluent;component/Themes/Themes/"));

        Console.WriteLine();
        var logged = new List<string>();
        var previousLog = ThemeHelper.Log;
        ThemeHelper.Log = (m, e) => logged.Add(m);
        Action<AppliedTheme> bad = _ => throw new InvalidOperationException("subscriber blew up");
        ThemeHelper.ThemeChanged += bad;
        try
        {
            ThemeHelper.ApplyTheme(MahAppsThemeColor.Lime, ThemeMode.Dark);
            Check("a throwing subscriber does not propagate", true);
        }
        catch (Exception ex) { Check("a throwing subscriber does not propagate", false, ex.Message); }
        finally { ThemeHelper.ThemeChanged -= bad; }

        Check("  it is logged instead", logged.Count >= 1, string.Join(" | ", logged));
        Check("  and the theme still landed", ThemeHelper.Current.ToString() == "Dark.Lime", ThemeHelper.Current.ToString());
        ThemeHelper.Log = previousLog;
    }

    static void ViewsRealise()
    {
        Console.WriteLine();
        Console.WriteLine("4. views construct and styles realise");

        // the six library views that bind {StaticResource Localization} WITHOUT declaring the
        // provider themselves. In a host that does not declare it these throw on load, because an
        // unresolvable StaticResource is a hard failure, not a silent default.
        var views = new List<Tuple<string, Func<FrameworkElement>>>
        {
            Tuple.Create<string, Func<FrameworkElement>>("MessageBoxView",            () => new IRI.Maptor.Presentation.Wpf.Controls.Dialogs.MessageBoxView()),
            Tuple.Create<string, Func<FrameworkElement>>("YesNoDialogView",           () => new IRI.Maptor.Presentation.Wpf.Controls.Dialogs.YesNoDialogView()),
            Tuple.Create<string, Func<FrameworkElement>>("MapExtentPanelView",        () => new IRI.Maptor.Presentation.Wpf.Controls.MapExtentPanelView()),
            Tuple.Create<string, Func<FrameworkElement>>("LayerSettings_GeneralView", () => new IRI.Maptor.Presentation.Wpf.Controls.LayerSettings.LayerSettings_GeneralView()),
            Tuple.Create<string, Func<FrameworkElement>>("ScaleRangeEditorView",      () => new IRI.Maptor.Presentation.Wpf.Controls.Symbology.Sld.ScaleRangeEditorView()),
            Tuple.Create<string, Func<FrameworkElement>>("SldEditorView",             () => new IRI.Maptor.Presentation.Wpf.Controls.Symbology.Sld.SldEditorView()),

            // the views swept onto IRI.Maptor.Styles.Size.Text.* -- a StaticResource that cannot
            // be reached from the view's own scope throws here and nowhere earlier
            Tuple.Create<string, Func<FrameworkElement>>("CsvTsvOpenDialogView",       () => new IRI.Maptor.Presentation.Wpf.Controls.Dialogs.CsvTsvOpenDialogView()),
            Tuple.Create<string, Func<FrameworkElement>>("DxfOpenDialogView",          () => new IRI.Maptor.Presentation.Wpf.Controls.Dialogs.DxfOpenDialogView()),
            Tuple.Create<string, Func<FrameworkElement>>("GeoJsonTopoJsonOpenDialogView", () => new IRI.Maptor.Presentation.Wpf.Controls.Dialogs.GeoJsonTopoJsonOpenDialogView()),
            Tuple.Create<string, Func<FrameworkElement>>("GeometryDetailsDialogView",  () => new IRI.Maptor.Presentation.Wpf.Controls.Dialogs.GeometryDetailsDialogView()),
            Tuple.Create<string, Func<FrameworkElement>>("ThemeSelectionDialog",       () => new IRI.Maptor.Presentation.Wpf.Controls.Dialogs.ThemeSelectionDialog()),
            Tuple.Create<string, Func<FrameworkElement>>("FeatureChangesView",         () => new IRI.Maptor.Presentation.Wpf.Controls.FeatureChangesView()),
            Tuple.Create<string, Func<FrameworkElement>>("GeometryEditorView",         () => new IRI.Maptor.Presentation.Wpf.Controls.GeometryEditorView()),
            Tuple.Create<string, Func<FrameworkElement>>("MapLegendItemView",          () => new IRI.Maptor.Presentation.Wpf.Controls.MapLegendItemView()),

            // the six views that had no style dictionary in scope at all
            Tuple.Create<string, Func<FrameworkElement>>("MultiSelectItem",            () => new IRI.Maptor.Presentation.Wpf.Controls.MultiSelectItem()),
            Tuple.Create<string, Func<FrameworkElement>>("SelectedItem",               () => new IRI.Maptor.Presentation.Wpf.Controls.SelectedItem()),
            Tuple.Create<string, Func<FrameworkElement>>("ImageViewer",                () => new IRI.Maptor.Presentation.Wpf.Controls.ImageViewer()),
            Tuple.Create<string, Func<FrameworkElement>>("ActiveExtentView",           () => new IRI.Maptor.Presentation.Wpf.Controls.Controls.ActiveExtentView()),
            Tuple.Create<string, Func<FrameworkElement>>("DottedBusyIndicatorView",    () => new IRI.Maptor.Presentation.Wpf.Controls.DottedBusyIndicatorView()),
            Tuple.Create<string, Func<FrameworkElement>>("LanguageSelectorView",       () => new IRI.Maptor.Presentation.Wpf.Controls.LanguageSelectorView()),

            // these seven merge Assets/Styles/Controls.TextBox.xaml by pack URI, so they are what
            // actually proves the 2026-08-31 case rename of that file
            Tuple.Create<string, Func<FrameworkElement>>("ChangePasswordView",         () => new IRI.Maptor.Presentation.Wpf.Controls.Security.ChangePasswordView()),
            Tuple.Create<string, Func<FrameworkElement>>("EmailPasswordInputView",     () => new IRI.Maptor.Presentation.Wpf.Controls.Security.EmailPasswordInputView()),
            Tuple.Create<string, Func<FrameworkElement>>("EmailSignUpView",            () => new IRI.Maptor.Presentation.Wpf.Controls.Security.EmailSignUpView()),
            Tuple.Create<string, Func<FrameworkElement>>("ForgetPasswordView",         () => new IRI.Maptor.Presentation.Wpf.Controls.Security.ForgetPasswordView()),
            Tuple.Create<string, Func<FrameworkElement>>("SetPasswordViewInputView",   () => new IRI.Maptor.Presentation.Wpf.Controls.Security.SetPasswordViewInputView()),
            Tuple.Create<string, Func<FrameworkElement>>("UserNameSignUpView",         () => new IRI.Maptor.Presentation.Wpf.Controls.Security.UserNameSignUpView()),
            Tuple.Create<string, Func<FrameworkElement>>("UserPasswordInputView",      () => new IRI.Maptor.Presentation.Wpf.Controls.Security.UserPasswordInputView()),
        };

        FrameworkElement hosted = null;

        foreach (var v in views)
        {
            try
            {
                var e = v.Item2();
                // a Window cannot be nested, so keep a UserControl for the element-tree check
                if (hosted == null && e is UserControl) hosted = e;
                Check(v.Item1 + " loads", true);
            }
            catch (Exception ex)
            {
                var real = ex.InnerException ?? ex;
                Check(v.Item1 + " loads", false, real.GetType().Name + ": " + real.Message);
            }
        }

        Window window = null;

        try
        {
            var button = new Button { Content = "x", Style = (Style)_app.FindResource("IRI.Maptor.Styles.Button.Primary.Large") };
            var panel = new StackPanel();
            panel.Children.Add(button);
            if (hosted != null) panel.Children.Add(hosted);

            window = new Window
            {
                Content = panel, Width = 500, Height = 400,
                ShowInTaskbar = false, WindowStyle = WindowStyle.None,
                Left = -3000, Top = -3000
            };
            window.Show();
            panel.UpdateLayout();

            Check("a styled Button realises", button.ActualWidth > 0, "width=" + button.ActualWidth);

            if (hosted != null)
            {
                Check("a hosted view realises", hosted.ActualWidth > 0, "width=" + hosted.ActualWidth);

                // README section 4.4c: the element tree and application scope must agree on the
                // status palette. They disagreed for months, silently.
                Check("the element tree agrees with app scope on the status palette",
                      Hex(hosted.TryFindResource("IRI.Maptor.Brushes.Valid")) == Status(),
                      Hex(hosted.TryFindResource("IRI.Maptor.Brushes.Valid")) + " vs " + Status());
            }
        }
        catch (Exception ex)
        {
            Check("styles realise on live controls", false, (ex.InnerException ?? ex).Message);
        }
        finally
        {
            if (window != null) window.Close();
        }
    }
}
