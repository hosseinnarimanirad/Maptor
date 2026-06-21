using IRI.Maptor.Jab.Core.Localization;

using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace IRI.Maptor.Jab.Maui.Localization;

/// <summary>
/// Applies the current language's reading direction to a visual tree and keeps it in sync.
/// Set it on a top-level element (e.g. the Shell or main page) — MAUI propagates
/// <see cref="FlowDirection"/> to descendants, so the whole UI mirrors for RTL languages.
/// </summary>
public static class LocalizationFlow
{
    /// <summary>Sets <paramref name="root"/>.FlowDirection now and on every language change.</summary>
    public static void Apply(VisualElement root)
    {
        if (root is null)
        {
            return;
        }

        UpdateFlow(root);
        LocalizationManager.Instance.FlowDirectionChanged += () => UpdateFlow(root);
    }

    private static void UpdateFlow(VisualElement root)
    {
        var direction = LocalizationManager.Instance.IsRightToLeft
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;

        if (MainThread.IsMainThread)
        {
            root.FlowDirection = direction;
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() => root.FlowDirection = direction);
        }
    }
}
