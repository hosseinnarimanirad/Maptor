using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Wpf.Topology;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// One case of a topology guide: figure, chip, title, text and, optionally, the fix the Issues panel offers.
/// The chip's words and colours are the panel's own (<see cref="TopologyMessages"/>, <see cref="TopologyOverlay"/>).
/// </summary>
public partial class TopologyCaseCard : UserControl
{
    public TopologyCaseCard()
    {
        InitializeComponent();

        UpdateChip();
    }

    public static readonly DependencyProperty CaseProperty = DependencyProperty.Register(
        nameof(Case), typeof(TopologyGuideCase), typeof(TopologyCaseCard), new PropertyMetadata(TopologyGuideCase.Legend));

    public TopologyGuideCase Case
    {
        get => (TopologyGuideCase)GetValue(CaseProperty);
        set => SetValue(CaseProperty, value);
    }

    public static readonly DependencyProperty BadgeProperty = DependencyProperty.Register(
        nameof(Badge), typeof(TopologyGuideBadge), typeof(TopologyCaseCard),
        new PropertyMetadata(TopologyGuideBadge.None, (d, _) => ((TopologyCaseCard)d).UpdateChip()));

    /// <summary>How the case is reported by default (a rule set may change it), or that the card shows a fix or correct data.</summary>
    public TopologyGuideBadge Badge
    {
        get => (TopologyGuideBadge)GetValue(BadgeProperty);
        set => SetValue(BadgeProperty, value);
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(TopologyCaseCard), new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(TopologyCaseCard), new PropertyMetadata(string.Empty));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty FixProperty = DependencyProperty.Register(
        nameof(Fix), typeof(string), typeof(TopologyCaseCard),
        new PropertyMetadata(string.Empty, (d, _) => ((TopologyCaseCard)d).UpdateFix()));

    /// <summary>The fix offered for this case, in the guide's words; shown after the word "Fix". Empty = no line.</summary>
    public string Fix
    {
        get => (string)GetValue(FixProperty);
        set => SetValue(FixProperty, value);
    }

    private static string Word(string key, string fallback)
    {
        var text = LocalizationManager.Instance[key];

        return string.IsNullOrEmpty(text) || text.StartsWith('#') ? fallback : text;
    }

    private void UpdateChip()
    {
        var (brush, text) = Badge switch
        {
            TopologyGuideBadge.Error => (TopologyOverlay.ErrorBrush, TopologyMessages.SeverityText(IssueSeverity.Error)),
            TopologyGuideBadge.Warning => (TopologyOverlay.WarningBrush, TopologyMessages.SeverityText(IssueSeverity.Warning)),
            TopologyGuideBadge.Notice => (TopologyOverlay.InfoBrush, TopologyMessages.SeverityText(IssueSeverity.Info)),
            TopologyGuideBadge.Fix => (TopologyOverlay.PreviewAfterBrush, Word("topology_fix_title", "Fix")),
            TopologyGuideBadge.Correct => (TopologyOverlay.PreviewAfterBrush, Word("topology_guide_correct", "Correct data")),
            _ => (null, string.Empty),
        };

        if (brush is null)
        {
            chip.Visibility = Visibility.Collapsed;
            return;
        }

        chip.Visibility = Visibility.Visible;
        chip.Background = new SolidColorBrush(Color.FromArgb(0x2E, brush.Color.R, brush.Color.G, brush.Color.B));
        chipDot.Fill = brush;
        chipText.Text = text;
        chipText.SetResourceReference(TextBlock.ForegroundProperty, "MahApps.Brushes.ThemeForeground");
    }

    private void UpdateFix()
    {
        fixLine.Inlines.Clear();

        if (string.IsNullOrWhiteSpace(Fix))
        {
            fixLine.Visibility = Visibility.Collapsed;
            return;
        }

        fixLine.Inlines.Add(new Run(Word("topology_fix_title", "Fix") + ": ") { FontWeight = FontWeights.SemiBold, Foreground = TopologyOverlay.PreviewAfterBrush });
        fixLine.Inlines.Add(new Run(Fix));
        fixLine.Visibility = Visibility.Visible;
    }
}
