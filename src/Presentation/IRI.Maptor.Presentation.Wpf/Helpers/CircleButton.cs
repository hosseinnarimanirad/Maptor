using System.Windows;

namespace IRI.Maptor.Presentation.Wpf.Helpers;

/// <summary>
/// Options for the solid round buttons (<c>IRI.Maptor.Styles.Button.CircleLight</c> and
/// <c>IRI.Maptor.Styles.ToggleButton.CircleLight</c>).
/// </summary>
/// <remarks>
/// <see cref="IsFlatProperty"/>: no drop shadow, not even on hover. The shadow lifts a button off map imagery (the
/// sketch bar, the geometry editor, text markers); inside a white panel or dialog it only adds weight, so the
/// <c>CircleLight.Flat</c> styles turn it on (compact review, Step 4, 2026-11-11).
/// </remarks>
public static class CircleButton
{
    public static readonly DependencyProperty IsFlatProperty = DependencyProperty.RegisterAttached(
        "IsFlat", typeof(bool), typeof(CircleButton), new PropertyMetadata(false));

    public static bool GetIsFlat(DependencyObject element) => (bool)element.GetValue(IsFlatProperty);

    public static void SetIsFlat(DependencyObject element, bool value) => element.SetValue(IsFlatProperty, value);
}
