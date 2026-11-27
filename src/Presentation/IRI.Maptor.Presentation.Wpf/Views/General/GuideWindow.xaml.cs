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
/// <para>A page with two or more main headings (<c>IRI.Maptor.Styles.Guide.Heading</c>) is read one section at a
/// time: the contents list beside the page names the sections (<see cref="GuideSections"/>), previous/next buttons
/// under the page step through them, and "Show all" shows the whole page, the list then jumping to a heading. A page
/// with fewer headings is shown whole, without the list. Guide pages need nothing for this beyond their headings.</para>
/// <para>One window per guide: opening the same guide again brings its window to the front. A language switch
/// reloads the page and the title in the new language, on the same section.</para>
/// </remarks>
public partial class GuideWindow : MetroWindow
{
    private static readonly Dictionary<string, GuideWindow> _openWindows = new(StringComparer.OrdinalIgnoreCase);

    private readonly string _source;

    private readonly Func<string?> _title;

    // the whole page, shown by "Show all"; the headings of _sections are its paragraphs, the jump targets
    private FlowDocument? _wholePage;

    private IReadOnlyList<GuideSection> _sections = Array.Empty<GuideSection>();

    private int _index;

    private bool _fillingList;

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

    // document: a freshly loaded page, kept as the whole page
    private void SetDocument(FlowDocument document)
    {
        Title = _title() ?? string.Empty;

        Prepare(document);

        _wholePage = document;
        _sections = GuideSections.Find(document);
        _index = Math.Clamp(_index, 0, _sections.Count - 1);

        var hasContents = _sections.Count > 1;

        contentsColumn.Width = hasContents ? new GridLength(240) : new GridLength(0);
        contentsPane.Visibility = hasContents ? Visibility.Visible : Visibility.Collapsed;

        _fillingList = true;
        contentsList.ItemsSource = hasContents ? _sections : null;
        contentsList.SelectedIndex = hasContents ? _index : -1;
        _fillingList = false;

        ShowCurrent();
    }

    // The page's own look stays in Guide.xaml; what depends on the window is set here: the UI font (Persian face in a
    // Persian UI, through the window style), the theme colours, and a reading width.
    private void Prepare(FlowDocument document)
    {
        document.SetBinding(FlowDocument.FontFamilyProperty, new Binding(nameof(FontFamily)) { Source = this });
        document.SetResourceReference(FlowDocument.ForegroundProperty, "MahApps.Brushes.ThemeForeground");
        document.SetResourceReference(FlowDocument.BackgroundProperty, "MahApps.Brushes.ThemeBackground");
        document.FontSize = 14;
        document.PagePadding = new Thickness(36, 28, 36, 36);
        document.MaxPageWidth = 860;
        document.TextAlignment = TextAlignment.Left;
        document.FlowDirection = LocalizationManager.Instance.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }

    private void ShowCurrent()
    {
        if (_wholePage is null)
            return;

        var whole = _sections.Count < 2 || showAllCheck.IsChecked == true;

        footer.Visibility = whole ? Visibility.Collapsed : Visibility.Visible;

        if (whole)
        {
            if (!ReferenceEquals(viewer.Document, _wholePage))
                viewer.Document = _wholePage;

            if (_sections.Count > 1)
                JumpTo(_index == 0 ? _wholePage.Blocks.FirstBlock : _sections[_index].Heading);

            return;
        }

        // a section page is cut from its own copy, so the whole page stays intact for "Show all"
        var copy = LoadDocument(_source, LocalizationManager.Instance.CurrentCulture);

        if (copy is null || GuideSections.Find(copy).Count != _sections.Count)
            return;

        var page = GuideSections.TakeSection(copy, _index);

        Prepare(page);

        viewer.Document = page;

        previousButton.Visibility = _index > 0 ? Visibility.Visible : Visibility.Hidden;
        nextButton.Visibility = _index < _sections.Count - 1 ? Visibility.Visible : Visibility.Hidden;

        previousText.Text = _index > 0 ? _sections[_index - 1].Title : string.Empty;
        nextText.Text = _index < _sections.Count - 1 ? _sections[_index + 1].Title : string.Empty;
    }

    // once the page is laid out, so the target has a position
    private void JumpTo(Block? block)
    {
        if (block is null)
            return;

        Dispatcher.BeginInvoke(() => block.BringIntoView(), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void ContentsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_fillingList || contentsList.SelectedIndex < 0)
            return;

        _index = contentsList.SelectedIndex;

        ShowCurrent();
    }

    private void ShowAll_Changed(object sender, RoutedEventArgs e) => ShowCurrent();

    private void PreviousButton_Click(object sender, RoutedEventArgs e) => Step(-1);

    private void NextButton_Click(object sender, RoutedEventArgs e) => Step(+1);

    private void Step(int delta)
    {
        var target = _index + delta;

        if (target < 0 || target >= _sections.Count)
            return;

        contentsList.SelectedIndex = target;

        contentsList.ScrollIntoView(contentsList.SelectedItem);
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
