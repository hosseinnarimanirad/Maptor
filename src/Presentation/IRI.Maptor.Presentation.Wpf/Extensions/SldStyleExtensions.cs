using System;
using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Core.Ogc.SLD;

namespace IRI.Maptor.Extensions;

/// <summary>One named style (an SLD <c>UserStyle</c>) of a layer's SLD.</summary>
/// <param name="Index">Position in the primary layer's style list.</param>
/// <param name="Name">The style's identity; null for a legacy unnamed style.</param>
/// <param name="Title">The human-readable title, when the style has one.</param>
/// <param name="IsDefault">True for the style marked <c>IsDefault</c>, or the first when none is marked.</param>
public sealed record SldStyleInfo(int Index, string? Name, string? Title, bool IsDefault)
{
    /// <summary>The title, else the name (what a style picker shows).</summary>
    public string DisplayTitle => Title ?? Name ?? string.Empty;
}

/// <summary>
/// A layer's SLD can hold several named styles (<c>UserStyle</c>s, e.g. "pipes by size" and "pipes by
/// material"), one of them the default. A layer is drawn with exactly one of them at a time: these
/// helpers list the styles and pick or cut out the one in use.
/// </summary>
public static class SldStyleExtensions
{
    /// <summary>
    /// The styles of the SLD's primary layer (the first NamedLayer with styles, else the first such
    /// UserLayer), in document order. Empty when there is none.
    /// </summary>
    public static IReadOnlyList<SldStyleInfo> GetStyles(this StyledLayerDescriptor? sld)
    {
        var styles = GetPrimaryStyles(sld);

        if (styles is null || styles.Count == 0)
            return [];

        var defaultIndex = FindStyleIndex(styles, null);

        return styles.Select((s, i) => new SldStyleInfo(i, NullIfBlank(s.Name), NullIfBlank(s.Title), i == defaultIndex)).ToList();
    }

    /// <summary>The name of the style drawn when none is chosen: the one marked IsDefault, else the first.</summary>
    public static string? GetDefaultStyleName(this StyledLayerDescriptor? sld)
    {
        var styles = GetPrimaryStyles(sld);

        var index = FindStyleIndex(styles, null);

        return index < 0 ? null : NullIfBlank(styles![index].Name);
    }

    /// <summary>True when the primary layer has a style with this exact name.</summary>
    public static bool HasStyle(this StyledLayerDescriptor? sld, string? styleName)
    {
        if (string.IsNullOrWhiteSpace(styleName))
            return false;

        return GetPrimaryStyles(sld)?.Any(s => string.Equals(s.Name, styleName, StringComparison.Ordinal)) == true;
    }

    /// <summary>
    /// The style to draw from <paramref name="styles"/>: the one named <paramref name="styleName"/>, else
    /// the one marked IsDefault, else the first; -1 when there are no styles.
    /// </summary>
    public static int FindStyleIndex(IList<UserStyle>? styles, string? styleName)
    {
        if (styles is null || styles.Count == 0)
            return -1;

        if (!string.IsNullOrWhiteSpace(styleName))
        {
            for (var i = 0; i < styles.Count; i++)
            {
                if (string.Equals(styles[i].Name, styleName, StringComparison.Ordinal))
                    return i;
            }
        }

        for (var i = 0; i < styles.Count; i++)
        {
            if (styles[i].IsDefault == true)
                return i;
        }

        return 0;
    }

    /// <summary>
    /// The UserStyle of one SLD layer (NamedLayer or UserLayer) to draw: see <see cref="FindStyleIndex"/>.
    /// </summary>
    public static UserStyle? SelectStyle(IList<UserStyle>? styles, string? styleName)
    {
        var index = FindStyleIndex(styles, styleName);

        return index < 0 ? null : styles![index];
    }

    /// <summary>
    /// A deep copy of the SLD in which every layer keeps only the style it is drawn with (see
    /// <see cref="FindStyleIndex"/>). The legend is built from this, so it shows the active style only.
    /// </summary>
    public static StyledLayerDescriptor ExtractStyle(this StyledLayerDescriptor sld, string? styleName)
    {
        var copy = SldHelper.Parse(SldHelper.Serialize(sld, indented: false))!;

        foreach (var namedLayer in copy.NamedLayers ?? [])
            KeepOnly(namedLayer.UserStyles, styleName);

        foreach (var userLayer in copy.UserLayers ?? [])
            KeepOnly(userLayer.UserStyles, styleName);

        return copy;
    }

    private static void KeepOnly(List<UserStyle>? styles, string? styleName)
    {
        var selected = SelectStyle(styles, styleName);

        if (selected is null)
            return;

        styles!.RemoveAll(s => !ReferenceEquals(s, selected));
    }

    #region Editing the style list (admin)

    // Style names are the permanent identity of a style (a user's project stores the active one), so the
    // admin edits titles only and names are generated: style-1, style-2, ...
    private const string GeneratedNamePrefix = "style-";

    /// <summary>The primary layer's styles, creating the NamedLayer when the SLD has none.</summary>
    public static List<UserStyle> GetOrCreatePrimaryStyles(this StyledLayerDescriptor sld)
    {
        var styles = GetPrimaryStyles(sld);

        if (styles is not null)
            return styles;

        var namedLayer = sld.NamedLayers.FirstOrDefault();

        if (namedLayer is null)
        {
            namedLayer = new NamedLayer();
            sld.NamedLayers.Add(namedLayer);
        }

        return namedLayer.UserStyles;
    }

    /// <summary>The primary layer's style at this position (see <see cref="SldStyleInfo.Index"/>), or null.</summary>
    public static UserStyle? FindStyleAt(this StyledLayerDescriptor? sld, int index)
    {
        var styles = GetPrimaryStyles(sld);

        return styles is not null && index >= 0 && index < styles.Count ? styles[index] : null;
    }

    /// <summary>The primary layer's style with this name, or null.</summary>
    public static UserStyle? FindStyle(this StyledLayerDescriptor? sld, string? styleName)
        => string.IsNullOrWhiteSpace(styleName) ? null : GetPrimaryStyles(sld)?.FirstOrDefault(s => string.Equals(s.Name, styleName, StringComparison.Ordinal));

    /// <summary>
    /// Gives every primary style a unique name (legacy SLDs often have none, or repeat one) and makes sure
    /// exactly one style is the default. Returns true when anything changed.
    /// </summary>
    public static bool EnsureStyleNames(this StyledLayerDescriptor sld)
    {
        var styles = GetPrimaryStyles(sld);

        if (styles is null || styles.Count == 0)
            return false;

        var changed = false;
        var used = new HashSet<string>(StringComparer.Ordinal);

        foreach (var style in styles)
        {
            if (string.IsNullOrWhiteSpace(style.Name) || !used.Add(style.Name))
            {
                style.Name = NewStyleName(styles, used);
                used.Add(style.Name);
                changed = true;
            }
        }

        var defaultIndex = FindStyleIndex(styles, null);

        for (var i = 0; i < styles.Count; i++)
        {
            bool? isDefault = i == defaultIndex ? true : null;

            if (styles[i].IsDefault != isDefault)
            {
                styles[i].IsDefault = isDefault;
                changed = true;
            }
        }

        return changed;
    }

    /// <summary>
    /// Appends <paramref name="style"/> to the primary layer under a new generated name and returns the
    /// name. The first style of a layer becomes its default.
    /// </summary>
    public static string AddStyle(this StyledLayerDescriptor sld, UserStyle style)
    {
        var styles = sld.GetOrCreatePrimaryStyles();

        style.Name = NewStyleName(styles, null);
        style.IsDefault = styles.Count == 0 ? true : null;

        styles.Add(style);

        return style.Name;
    }

    /// <summary>
    /// A copy of the style named <paramref name="styleName"/>, titled <paramref name="title"/>, inserted
    /// right after it; returns the copy's name, or null when there is no such style.
    /// </summary>
    public static string? CopyStyle(this StyledLayerDescriptor sld, string styleName, string title)
    {
        var styles = GetPrimaryStyles(sld);
        var source = sld.FindStyle(styleName);

        if (styles is null || source is null)
            return null;

        var copy = CloneStyle(source);
        copy.Name = NewStyleName(styles, null);
        copy.Title = title;
        copy.IsDefault = null;

        styles.Insert(styles.IndexOf(source) + 1, copy);

        return copy.Name;
    }

    /// <summary>
    /// Removes the style; when it was the default, the first remaining style becomes the default.
    /// Returns false when there is no such style.
    /// </summary>
    public static bool RemoveStyle(this StyledLayerDescriptor sld, string styleName)
    {
        var styles = GetPrimaryStyles(sld);
        var style = sld.FindStyle(styleName);

        if (styles is null || style is null)
            return false;

        var wasDefault = FindStyleIndex(styles, null) == styles.IndexOf(style);

        styles.Remove(style);

        if (wasDefault && styles.Count > 0)
            SetDefaultStyle(sld, styles[0].Name);

        return true;
    }

    /// <summary>Marks the style as the default and clears the mark on every other style.</summary>
    public static bool SetDefaultStyle(this StyledLayerDescriptor sld, string styleName)
    {
        var styles = GetPrimaryStyles(sld);

        if (styles is null || sld.FindStyle(styleName) is null)
            return false;

        foreach (var style in styles)
            style.IsDefault = string.Equals(style.Name, styleName, StringComparison.Ordinal) ? true : null;

        return true;
    }

    public static bool SetStyleTitle(this StyledLayerDescriptor sld, string styleName, string? title)
    {
        var style = sld.FindStyle(styleName);

        if (style is null)
            return false;

        style.Title = string.IsNullOrWhiteSpace(title) ? null! : title.Trim();

        return true;
    }

    /// <summary>Moves the style <paramref name="offset"/> places (negative: up); false when it cannot move.</summary>
    public static bool MoveStyle(this StyledLayerDescriptor sld, string styleName, int offset)
    {
        var styles = GetPrimaryStyles(sld);
        var style = sld.FindStyle(styleName);

        if (styles is null || style is null)
            return false;

        var index = styles.IndexOf(style);
        var target = index + offset;

        if (target < 0 || target >= styles.Count || target == index)
            return false;

        styles.RemoveAt(index);
        styles.Insert(target, style);

        return true;
    }

    private static string NewStyleName(List<UserStyle> styles, HashSet<string>? reserved)
    {
        for (var n = 1; ; n++)
        {
            var name = GeneratedNamePrefix + n.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (reserved?.Contains(name) != true && !styles.Any(s => string.Equals(s.Name, name, StringComparison.Ordinal)))
                return name;
        }
    }

    private static UserStyle CloneStyle(UserStyle style)
    {
        var holder = new StyledLayerDescriptor();
        var namedLayer = new NamedLayer();
        namedLayer.UserStyles.Add(style);
        holder.NamedLayers.Add(namedLayer);

        return SldHelper.Parse(SldHelper.Serialize(holder, indented: false))!.NamedLayers[0].UserStyles[0];
    }

    #endregion

    private static List<UserStyle>? GetPrimaryStyles(StyledLayerDescriptor? sld)
    {
        if (sld is null)
            return null;

        return sld.NamedLayers?.FirstOrDefault(n => n.UserStyles?.Count > 0)?.UserStyles
            ?? sld.UserLayers?.FirstOrDefault(u => u.UserStyles?.Count > 0)?.UserStyles;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
