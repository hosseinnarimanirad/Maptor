using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using IRI.Maptor.Jab.Core.Models;
using IRI.Maptor.Sta.Common.Helpers;
//using static IRI.Maptor.Jab.Wpf.Models.Themes.MahAppsThemeColor;

namespace IRI.Maptor.Jab.Wpf.Helpers;

public static class ThemeHelper
{
    /// <summary>
    /// Available MahApps themes with their display names and accent colors
    /// Colors are ordered in rainbow order
    /// </summary>
    public static readonly List<ThemeInfoModel> AvailableThemes;//=
    //[
    //    // Green family
    //    new ThemeInfoModel(/*01,*/ Lime),//, /*nameof(Properties.Resources.theme_color_lime),*/ "#FFB8D135"),
    //    new ThemeInfoModel(/*01,*/ gree),//, /*nameof(Properties.Resources.theme_color_lime),*/ "#FFB8D135"),
    //    new ThemeInfoModel(/*02,*/ Emerald),//,/* nameof(Properties.Resources.theme_color_emerald),*/ "#FF34A134"),

    //    // Blue family (includil/cyan)
    //    new ThemeInfoModel(/*03,*/ Teal),//, /*nameof(Properties.Resources.theme_color_teal),*/ "#FF34BCBA"),
    //    new ThemeInfoModel(/*04,*/ Cyan),//, /*nameof(Properties.Resources.theme_color_cyan),*/ "#FF4AB4E8"),
    //    new ThemeInfoModel(/*05,*/ Blue),//, /*nameof(Properties.Resources.theme_color_blue),*/ "#FF41B1E1"),
    //    new ThemeInfoModel(/*06,*/ Cobalt),//, /*nameof(Properties.Resources.theme_color_cobalt),*/ "#FF3474F2"),

    //    // Pink/Magenta family
    //    new ThemeInfoModel(/*07,*/ Purple),//, /*nameof(Properties.Resources.theme_color_purple),*/ "#FF837AE5"),
    //    new ThemeInfoModel(/*08,*/ Indigo),//, /*nameof(Properties.Resources.theme_color_indigo),*/ "#FF8D3BFF"),
    //    new ThemeInfoModel(/*09,*/ Violet),//, /*nameof(Properties.Resources.theme_color_violet),*/ "#FFB733F9"),
    //    new ThemeInfoModel(/*10,*/ Pink),//, /*nameof(Properties.Resources.theme_color_pink),*/ "#FFF68ED9"),

    //    // Red family
    //    new ThemeInfoModel(/*11,*/ Magenta),//, /*nameof(Properties.Resources.theme_color_magenta),*/ "#FFE0338F"),
    //    new ThemeInfoModel(/*12,*/ Crimson),//, /*nameof(Properties.Resources.theme_color_crimson),*/ "#FFB53251"),
    //    new ThemeInfoModel(/*13,*/ Red),//, /*nameof(Properties.Resources.theme_color_red),*/ "#FFEA4333"),

    //    // Orange/Yellow family
    //    new ThemeInfoModel(/*14,*/ Orange),//, /*nameof(Properties.Resources.theme_color_orange),*/ "#FFFF8130"),
    //    new ThemeInfoModel(/*15,*/ Amber),//,  /*nameof(Properties.Resources.theme_color_amber), */"#FFEEB739"),
    //    new ThemeInfoModel(/*16,*/ Yellow),//, /*nameof(Properties.Resources.theme_color_yellow),*/ "#FFFFE736"),

    //    //new ThemeInfoModel(17, "Brown", /*nameof(Properties.Resources.theme_color_brown),*/ "#FF9E794F"),
    //    new ThemeInfoModel(/*17,*/ Sienna),//, /*nameof(Properties.Resources.theme_color_sienna),*/ "#FFB27759"),
    //    new ThemeInfoModel(/*18,*/ Taupe ),//, /*nameof(Properties.Resources.theme_color_taupe), */"#FFA09575"),
    //    new ThemeInfoModel(/*19,*/ Olive ),//, /*nameof(Properties.Resources.theme_color_olive), */"#FF8A9E82"),
    //    new ThemeInfoModel(/*20,*/ Steel ),//, /*nameof(Properties.Resources.theme_color_steel), */"#FF83919E"),
    //    new ThemeInfoModel(/*21,*/ Mauve ),//, /*nameof(Properties.Resources.theme_color_mauve), */"#FF9180A2"),
    //];
    static ThemeHelper()
    {
        AvailableThemes = Enum.GetValues<MahAppsThemeColor>()
                            .Where(i => i != MahAppsThemeColor.Brown)
                            .Select(i => new ThemeInfoModel(i, ThemeMode.Light))
                            .ToList();
    }


    ///// <summary>
    ///// Gets theme info by theme name
    ///// </summary>
    //public static ThemeInfoModel? GetThemeInfo(MahAppsThemeColor color) => AvailableThemes.FirstOrDefault(t => t.Color == color);

    /// <summary>
    /// Applies a MahApps theme to the application
    /// </summary>
    /// <param name="themeName">Theme name in format "Light.Amber" or "Dark.Cobalt"</param>
    public static void ApplyTheme(MahAppsThemeColor? color, ThemeMode? mode = null)
    {
        color ??= MahAppsThemeColor.Amber;
        mode ??= ThemeMode.Light;

        try
        {
            //// Parse theme name (e.g., "Light.Amber" -> baseTheme="Light", accent="Amber")
            //var parts = themeName.Split('.');
            //if (parts.Length != 2)
            //{
            //    System.Diagnostics.Debug.WriteLine($"Invalid theme format: {themeName}. Expected format: BaseTheme.Accent");
            //    return;
            //}

            // "Light" or "Dark". MahApps and Fluent both ship a dictionary per
            // mode x accent, generated from one template, so the brush key set is
            // identical either way and styles built on MahApps tokens follow along.
            var baseTheme = mode.ToString();
            var accent = color.ToString();//parts[1]; // "Amber", "Cobalt", etc.

            // Build theme resource dictionary path
            var themePath = $"pack://application:,,,/MahApps.Metro;component/Styles/Themes/{baseTheme}.{accent}.xaml";

            var app = Application.Current;
            if (app?.Resources == null)
            {
                System.Diagnostics.Debug.WriteLine("Application.Current or Resources is null");
                return;
            }

            // Remove existing MahApps theme dictionaries
            var mergedDictionaries = app.Resources.MergedDictionaries;
            var themesToRemove = mergedDictionaries
                .OfType<ResourceDictionary>()
                .Where(rd => rd.Source != null &&
                            rd.Source.ToString().Contains("/MahApps.Metro;component/Styles/Themes/"))
                .ToList();

            foreach (var dict in themesToRemove)
            {
                mergedDictionaries.Remove(dict);
            }

            // Add new theme dictionary
            var newTheme = new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Absolute)
            };
            mergedDictionaries.Add(newTheme);

            // Also update Fluent.Ribbon theme if available
            try
            {
                var fluentThemesToRemove = mergedDictionaries
                    .OfType<ResourceDictionary>()
                    .Where(rd => rd.Source != null &&
                                rd.Source.ToString().Contains("/Fluent;component/Themes/Themes/"))
                    .ToList();

                foreach (var dict in fluentThemesToRemove)
                {
                    mergedDictionaries.Remove(dict);
                }

                var fluentThemePath = $"pack://application:,,,/Fluent;component/Themes/Themes/{baseTheme}.{accent}.xaml";
                var fluentTheme = new ResourceDictionary
                {
                    Source = new Uri(fluentThemePath, UriKind.Absolute)
                };
                mergedDictionaries.Add(fluentTheme);
            }
            catch
            {
                // Fluent theme is optional, continue if it fails
            }

            // Our own semantic status palette (valid / invalid / muted). MahApps has no such
            // colours, so it cannot follow the theme on its own and is swapped here.
            try
            {
                var statusToRemove = mergedDictionaries
                    .OfType<ResourceDictionary>()
                    .Where(rd => rd.Source != null &&
                                rd.Source.ToString().Contains("/Assets/Styles/Status."))
                    .ToList();

                foreach (var dict in statusToRemove)
                {
                    mergedDictionaries.Remove(dict);
                }

                var statusPath = $"pack://application:,,,/IRI.Maptor.Jab.Wpf;component/Assets/Styles/Status.{baseTheme}.xaml";
                mergedDictionaries.Add(new ResourceDictionary { Source = new Uri(statusPath, UriKind.Absolute) });
            }
            catch
            {
                // status palette is optional, continue if it fails
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error applying theme {color}: {ex.Message}");
            // Try fallback to default theme
            try
            {
                ApplyTheme(MahAppsThemeColor.Amber, ThemeMode.Light);
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Failed to apply fallback theme");
            }
        }
    }

}
