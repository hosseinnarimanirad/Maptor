using System.Windows;
using System.Windows.Controls;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// The graduation-cap button that opens a tool's guide page in a <see cref="GuideWindow"/>. Put one in the tool's
/// header (a <see cref="MapSidePanel"/> shows one by itself when its <see cref="MapSidePanel.GuideSource"/> is set).
/// </summary>
/// <remarks>
/// Two levels of help: an <see cref="InfoTip"/> (lightbulb) answers a short question
/// beside one input; the guide explains the whole tool. Use the guide only where a tool needs it (the analysis
/// panels), not on every view. Opening a window is a view concern, so this is code-behind, not a command.
/// </remarks>
public partial class GuideButton : UserControl
{
    public GuideButton()
    {
        InitializeComponent();
    }

    /// <summary>The guide's base path, e.g. <c>/MyApp;component/Guides/SpanAnalysis</c>; see <see cref="GuideWindow"/>.</summary>
    public string? Source
    {
        get => (string?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(nameof(Source), typeof(string), typeof(GuideButton), new PropertyMetadata(null));

    /// <summary>The guide window's title, usually the tool's name. Bind it to a key: it is read again after a language switch.</summary>
    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(GuideButton), new PropertyMetadata(null));

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Source))
            return;

        GuideWindow.Open(Source, () => Title, Window.GetWindow(this));
    }
}
