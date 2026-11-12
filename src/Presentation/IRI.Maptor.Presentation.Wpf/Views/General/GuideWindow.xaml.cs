using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;

using MahApps.Metro.Controls;

using IRI.Maptor.Presentation.Core.Localization;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// A modeless window that shows a guide page: a FlowDocument compiled into an application, explaining one tool at
/// length (what it computes, how, with figures and a worked example). The short how-to beside an input is an
/// <see cref="InfoTip"/>; the guide is what <see cref="GuideButton"/> (the graduation cap) opens.
/// </summary>
/// <remarks>
/// <para>A guide is named by a base path without extension, e.g.
/// <c>/MyApp;component/Guides/SpanAnalysis</c>. The page for the UI language is looked up the way resx
/// satellites are: <c>SpanAnalysis.fa-IR.xaml</c>, then <c>SpanAnalysis.fa.xaml</c>, then <c>SpanAnalysis.xaml</c>
/// (English, the neutral page). So a language without its own page falls back to English. Each page is a
/// <c>Page</c> build item whose root is a <see cref="FlowDocument"/> (no x:Class); it merges
/// <c>Assets/Styles/Guide.xaml</c> for its styles.</para>
/// <para>One window per guide: opening the same guide again brings its window to the front. A language switch
/// reloads the page and the title in the new language.</para>
/// </remarks>
public partial class GuideWindow : MetroWindow
{
    private static readonly Dictionary<string, GuideWindow> _openWindows = new(StringComparer.OrdinalIgnoreCase);

    private readonly string _source;

    private readonly Func<string?> _title;

    private GuideWindow(string source, Func<string?> title)
    {
        InitializeComponent();

        _source = source;
        _title = title;

        LocalizationManager.Instance.LanguageChanged += Localization_LanguageChanged;
    }

    /// <summary>
    /// Opens the guide <paramref name="source"/> (a base path, see the remarks) or brings its window to the front.
    /// </summary>
    /// <param name="source">The guide's base path, without culture or extension.</param>
    /// <param name="title">Read for the window title now and after every language switch.</param>
    /// <param name="owner">The window the guide belongs to; it opens centred on it.</param>
    /// <returns>False when no page of the guide could be loaded; nothing is shown then.</returns>
    public static bool Open(string source, Func<string?> title, Window? owner)
    {
        if (string.IsNullOrWhiteSpace(source))
            return false;

        if (_openWindows.TryGetValue(source, out var existing))
        {
            if (existing.WindowState == WindowState.Minimized)
                existing.WindowState = WindowState.Normal;

            existing.Activate();

            return true;
        }

        var document = LoadDocument(source, LocalizationManager.Instance.CurrentCulture);

        if (document is null)
            return false;

        var window = new GuideWindow(source, title ?? (() => null));

        if (owner is not null && owner.IsVisible)
            window.Owner = owner;
        else
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        window.Show(document);

        _openWindows[source] = window;

        return true;
    }

    /// <summary>
    /// The paths tried for <paramref name="culture"/>, most specific first: the culture and each parent culture,
    /// then the neutral page.
    /// </summary>
    public static IReadOnlyList<string> GetCandidatePaths(string source, CultureInfo culture)
    {
        var paths = new List<string>();

        for (var current = culture; current is not null && !string.IsNullOrEmpty(current.Name); current = current.Parent)
            paths.Add($"{source}.{current.Name}.xaml");

        paths.Add($"{source}.xaml");

        return paths;
    }

    /// <summary>The page for <paramref name="culture"/>, or null when no candidate path exists.</summary>
    public static FlowDocument? LoadDocument(string source, CultureInfo culture)
    {
        foreach (var path in GetCandidatePaths(source, culture))
        {
            try
            {
                if (Application.LoadComponent(new Uri(path, UriKind.Relative)) is FlowDocument document)
                    return document;
            }
            catch (IOException)
            {
                // no page for this culture; try the next one
            }
        }

        Debug.WriteLine($"GuideWindow: no page found for '{source}'.");

        return null;
    }

    private void Show(FlowDocument document)
    {
        SetDocument(document);

        Show();
    }

    private void SetDocument(FlowDocument document)
    {
        Title = _title() ?? string.Empty;

        // The page's own look stays in Guide.xaml; what depends on the window is set here: the UI font (Persian
        // face in a Persian UI, through the window style), the theme colours, and a reading width.
        document.SetBinding(FlowDocument.FontFamilyProperty, new Binding(nameof(FontFamily)) { Source = this });
        document.SetResourceReference(FlowDocument.ForegroundProperty, "MahApps.Brushes.ThemeForeground");
        document.SetResourceReference(FlowDocument.BackgroundProperty, "MahApps.Brushes.ThemeBackground");
        document.FontSize = 14;
        document.PagePadding = new Thickness(36, 28, 36, 36);
        document.MaxPageWidth = 860;
        document.TextAlignment = TextAlignment.Left;
        document.FlowDirection = LocalizationManager.Instance.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        viewer.Document = document;
    }

    // LanguageChanged is raised before the localized bindings refresh, so the title (usually bound to a key) is read
    // once the dispatcher has run those updates
    private void Localization_LanguageChanged()
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!IsLoaded)
                return;

            var document = LoadDocument(_source, LocalizationManager.Instance.CurrentCulture);

            if (document is not null)
                SetDocument(document);
        });
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled || e.Key != Key.Escape)
            return;

        e.Handled = true;

        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        LocalizationManager.Instance.LanguageChanged -= Localization_LanguageChanged;

        _openWindows.Remove(_source);

        base.OnClosed(e);
    }
}
