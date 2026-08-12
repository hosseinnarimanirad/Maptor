using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Jab.Blazor.FeatureTable;

/// <summary>
/// One layer's attribute table, as the tab strip holds it.
///
/// <para>Carries its own loading and error state because a table can be opened for a layer whose
/// features have never been fetched — a layer outside its zoom band is never downloaded, yet the
/// user can still legitimately ask to see its attributes.</para>
///
/// <para>Host-agnostic: it knows nothing about where the features came from. The host fills
/// <see cref="Features"/> however it must and calls back into the panel to redraw.</para>
/// </summary>
public sealed class FeatureTableTab
{
    /// <summary>Matches the TOC node and vector-layer id, so reopening the same layer reuses the
    /// existing tab instead of stacking duplicates.</summary>
    public required string LayerId { get; init; }

    public required string Title { get; init; }

    public FeatureSet<Point>? Features { get; set; }

    public bool IsLoading { get; set; }

    public string? ErrorMessage { get; set; }

    /// <summary>Columns shown for this tab, derived once when the features arrive.</summary>
    public IReadOnlyList<FeatureTableColumn> Columns { get; private set; } = [];

    /// <summary>Free text matched against every column. Null or empty shows everything.</summary>
    public string? Filter { get; set; }

    /// <summary>Column name currently sorted on, or null for the layer's own order.</summary>
    public string? SortColumn { get; set; }

    public bool SortDescending { get; set; }

    /// <summary>
    /// Sort and filter live on the tab, not on the grid, so each open table keeps its own view —
    /// switching tabs and coming back must not silently reset what the user set up.
    /// </summary>
    public void ToggleSort(string columnName)
    {
        // Ascending, then descending, then back to the layer's own order. The third state matters:
        // the original order is the feature order the service returned, and there is otherwise no
        // way back to it short of closing the tab.
        if (SortColumn != columnName)
        {
            SortColumn = columnName;
            SortDescending = false;
        }
        else if (!SortDescending)
        {
            SortDescending = true;
        }
        else
        {
            SortColumn = null;
            SortDescending = false;
        }
    }

    public void SetFeatures(FeatureSet<Point>? features)
    {
        Features = features;
        Columns = FeatureTableColumn.From(features);

        // A view built against the previous feature set means nothing against a new one.
        Filter = null;
        SortColumn = null;
        SortDescending = false;
    }
}

/// <summary>One attribute column: the key to read from a feature, and the label to show.</summary>
public sealed record FeatureTableColumn(string Name, string Header)
{
    /// <summary>
    /// Marker in a Field's type name for the geometry column. The WPF FeatureTable hides it the
    /// same way — the shape is already on the map, and its serialised form is unreadable in a cell.
    /// </summary>
    private const string GeometryTypeMarker = "NetTopologySuite";

    /// <summary>
    /// Columns for a feature set. Prefers the set's declared <c>Fields</c>, which carry the Alias
    /// the schema wants shown; falls back to the union of the features' own attribute keys, since
    /// some endpoints return attributes without a field list.
    /// </summary>
    public static IReadOnlyList<FeatureTableColumn> From(FeatureSet<Point>? featureSet)
    {
        if (featureSet is null)
            return [];

        if (featureSet.Fields is { Count: > 0 })
        {
            return featureSet.Fields
                .Where(field => !string.IsNullOrWhiteSpace(field.Name) && !IsGeometryField(field))
                .Select(field => new FeatureTableColumn(
                    field.Name,
                    string.IsNullOrWhiteSpace(field.Alias) ? field.Name : field.Alias!))
                .ToList();
        }

        // Union rather than the first feature's keys: attribute dictionaries are per-feature, and a
        // sparse one would otherwise decide the whole table's shape.
        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var feature in featureSet.Features)
        {
            if (feature.Attributes is null)
                continue;

            foreach (var key in feature.Attributes.Keys)
            {
                if (seen.Add(key))
                    names.Add(key);
            }
        }

        return names.Select(name => new FeatureTableColumn(name, name)).ToList();
    }

    private static bool IsGeometryField(Field field) =>
        field.TypeFullName?.Contains(GeometryTypeMarker, StringComparison.OrdinalIgnoreCase) == true;
}
