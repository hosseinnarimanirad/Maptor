using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

using IRI.Maptor.Sta.Versioning;

namespace IRI.Maptor.Jab.Wpf.Converters;

/// <summary>
/// Picks the status-pill style for a proposal's editor-facing status.
/// <para>
/// Every one of the six statuses used to render in the same Accent pill, so a Rejected
/// proposal was pixel-identical to a Committed one and only the Persian caption distinguished
/// them. The mapping below gives the outcome states their own colour and leaves the
/// still-in-flight states advisory.
/// </para>
/// <para>
/// Pair with <see cref="EditorFacingStatusToPillTextStyleConverter"/>: a Border cannot restyle
/// its own child, so the caption needs the matching text style from the same mapping.
/// </para>
/// </summary>
public class EditorFacingStatusToPillStyleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => Lookup(value, suffix: string.Empty);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();

    /// <summary>Shared mapping so the border and its caption can never disagree.</summary>
    internal static object Lookup(object value, string suffix)
    {
        // ".Text" sits between "Pill" and the variant: IRI.Maptor.Styles.Pill.Text.Valid
        var key = value is EditorFacingStatus status
            ? status switch
            {
                EditorFacingStatus.Committed => $"IRI.Maptor.Styles.Pill{suffix}.Valid",
                EditorFacingStatus.Rejected => $"IRI.Maptor.Styles.Pill{suffix}.Invalid",
                EditorFacingStatus.PendingReview => $"IRI.Maptor.Styles.Pill{suffix}.Warning",
                EditorFacingStatus.UnderReview => $"IRI.Maptor.Styles.Pill{suffix}.Warning",
                EditorFacingStatus.InCompetition => $"IRI.Maptor.Styles.Pill{suffix}.Accent",

                // Withdrawn: the editor took it back, so it is neither a failure nor advisory
                _ => $"IRI.Maptor.Styles.Pill{suffix}",
            }
            : $"IRI.Maptor.Styles.Pill{suffix}";

        // UnsetValue rather than null: it tells WPF to fall back to the property's default,
        // which is what we want in the designer where there is no Application to resolve against.
        return Application.Current?.TryFindResource(key) as Style ?? (object)DependencyProperty.UnsetValue;
    }
}

/// <summary>
/// Caption style matching <see cref="EditorFacingStatusToPillStyleConverter"/>.
/// </summary>
public class EditorFacingStatusToPillTextStyleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => EditorFacingStatusToPillStyleConverter.Lookup(value, suffix: ".Text");

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
