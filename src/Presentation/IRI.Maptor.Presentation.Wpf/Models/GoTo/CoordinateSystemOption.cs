using System.Collections.Generic;

using IRI.Maptor.Presentation.Core.Localization;

namespace IRI.Maptor.Presentation.Wpf.Models.GoTo;

/// <summary>
/// One entry of the Go To dialog's coordinate-system picker. Geodetic appears twice (DMS and
/// decimal degrees) so the picker is the only mode control on the screen; UTM once; every
/// <see cref="ProjectionPreset"/> once. The datum is a separate row, not part of the entry.
/// </summary>
public sealed class CoordinateSystemOption
{
    private CoordinateSystemOption(string key, string title, string? subtitle, GoToMode mode, GeodeticFormat format, ProjectionPreset? projection)
    {
        Key = key;
        Title = title;
        Subtitle = subtitle;
        Mode = mode;
        Format = format;
        Projection = projection;
    }

    public string Key { get; }

    /// <summary>Localized display name.</summary>
    public string Title { get; }

    /// <summary>Qualifier shown after the title: the geodetic notation, an EPSG code, a datum, or "custom".</summary>
    public string? Subtitle { get; }

    public GoToMode Mode { get; }

    /// <summary>Meaningful for <see cref="GoToMode.Geodetic"/> entries only.</summary>
    public GeodeticFormat Format { get; }

    /// <summary>Set for <see cref="GoToMode.Projected"/> entries only.</summary>
    public ProjectionPreset? Projection { get; }

    public bool Matches(GoToMode mode, GeodeticFormat format, ProjectionPreset projection)
    {
        if (Mode != mode)
            return false;

        return mode switch
        {
            GoToMode.Geodetic => Format == format,
            GoToMode.Projected => ReferenceEquals(Projection, projection),
            _ => true,
        };
    }

    public override string ToString() => Subtitle is null ? Title : $"{Title} · {Subtitle}";

    /// <summary>
    /// The picker's catalogue, in display order: geodetic (two notations), UTM, then the
    /// projections in the order of <paramref name="projections"/>.
    /// </summary>
    public static List<CoordinateSystemOption> CreateDefaults(IReadOnlyList<ProjectionPreset> projections)
    {
        var l = LocalizationManager.Instance;

        string geodetic = l["dialog_goto_tabGeodetic"];

        var result = new List<CoordinateSystemOption>
        {
            new("geodeticDms", geodetic, l["dialog_goto_formatDms"], GoToMode.Geodetic, GeodeticFormat.DegreesMinutesSeconds, null),
            new("geodeticDecimal", geodetic, l["dialog_goto_formatDecimal"], GoToMode.Geodetic, GeodeticFormat.DecimalDegrees, null),
            new("utm", l["dialog_goto_tabUtm"], null, GoToMode.Utm, GeodeticFormat.DegreesMinutesSeconds, null),
        };

        foreach (var p in projections)
            result.Add(new(p.Key, p.Title, p.Subtitle, GoToMode.Projected, GeodeticFormat.DegreesMinutesSeconds, p));

        return result;
    }
}
