using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using IRI.Maptor.Presentation.Core.Localization;

namespace IRI.Maptor.Presentation.Wpf.Behaviors;

/// <summary>
/// Lets the watermark of a left-to-right text box follow the UI language's direction. Used by
/// <c>IRI.Maptor.Styles.TextBox.Normal.En.UiWatermark</c> (Controls.Textbox.xaml) for boxes whose
/// content is always Latin (coordinates, links, numbers) but whose hint is a sentence in the UI
/// language: in a Persian UI the box stays LTR while «مختصات یا پیوند نقشه را اینجا بچسبانید…»
/// reads right to left from the right edge.
/// </summary>
/// <remarks>
/// MahApps draws the watermark with the template part <c>PART_Message</c>, which inherits the box's
/// LTR direction. This binds that part's <see cref="FrameworkElement.FlowDirection"/> to
/// <see cref="LocalizationManager.CurrentFlowDirection"/> (so it also follows a language switch) and
/// stretches it, so an RTL hint starts at the right edge. The rest of MahApps' template (clear button,
/// trimming, focus fade) is untouched. Before this existed, the CSV/TSV and GeoJSON open dialogs drew
/// their own TextBlock over the box for the same reason.
/// </remarks>
public static class WatermarkAssist
{
    public const string MessagePartName = "PART_Message";

    /// <summary>Turns the behaviour on; set by the style.</summary>
    public static readonly DependencyProperty FollowsUiDirectionProperty =
        DependencyProperty.RegisterAttached("FollowsUiDirection", typeof(bool), typeof(WatermarkAssist), new PropertyMetadata(false, OnChanged));

    public static bool GetFollowsUiDirection(DependencyObject element) => (bool)element.GetValue(FollowsUiDirectionProperty);

    public static void SetFollowsUiDirection(DependencyObject element, bool value) => element.SetValue(FollowsUiDirectionProperty, value);

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox box)
            return;

        // detach first so a style re-applied with the same value never doubles the handler
        box.Loaded -= OnLoaded;

        if (e.NewValue is not true)
            return;

        box.Loaded += OnLoaded;

        if (box.IsLoaded)
            Apply(box);
    }

    // Loaded, not OnApplyTemplate: the template is applied by then, and a box that is unloaded and
    // loaded again (a tab switch) gets the binding re-checked at no cost
    private static void OnLoaded(object sender, RoutedEventArgs e) => Apply((TextBox)sender);

    private static void Apply(TextBox box)
    {
        box.ApplyTemplate();

        if (box.Template?.FindName(MessagePartName, box) is not TextBlock message)
            return;

        // the binding holds the manager weakly (PropertyChanged), so nothing leaks with the box
        BindingOperations.SetBinding(message, FrameworkElement.FlowDirectionProperty,
            new Binding(nameof(LocalizationManager.CurrentFlowDirection)) { Source = LocalizationManager.Instance });

        // MahApps aligns the part by the box's HorizontalContentAlignment (Left in an LTR box); stretched,
        // the text aligns by its own direction instead: from the left in English, from the right in Persian
        message.HorizontalAlignment = HorizontalAlignment.Stretch;
    }
}
