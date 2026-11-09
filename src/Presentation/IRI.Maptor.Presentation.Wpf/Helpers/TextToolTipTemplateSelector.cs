using System.Windows;
using System.Windows.Controls;

namespace IRI.Maptor.Presentation.Wpf.Helpers;

/// <summary>
/// Shows a plain-text tooltip through <see cref="TextTemplate"/> (a wrapping TextBlock) and leaves
/// any other tooltip content to the default presentation. Used by the implicit ToolTip style in
/// Controls.ToolTip.xaml.
/// </summary>
/// <remarks>
/// A selector is needed because the simpler routes do not reach the text: ContentPresenter does not
/// pick up a DataTemplate keyed by <see cref="string"/> from the ToolTip style's resources, and a
/// plain ContentTemplate setter would turn non-text content (a StackPanel, an image) into its
/// ToString().
/// </remarks>
public class TextToolTipTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is string && TextTemplate is not null)
            return TextTemplate;

        return base.SelectTemplate(item, container);
    }
}
