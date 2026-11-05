using System;
using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Presentation.Wpf.Models.Identify;

/// <summary>Leaf of the identify tree: one feature hit, under its layer node.</summary>
public class IdentifyFeatureNode : Notifier, IIdentifyNode
{
    private IReadOnlyList<IdentifyAttributeRow>? _attributes;

    public IdentifyFeatureNode(IdentifyLayerNode layer, Feature<Point> feature)
    {
        Layer = layer ?? throw new ArgumentNullException(nameof(layer));

        Feature = feature ?? throw new ArgumentNullException(nameof(feature));

        _fields = layer.Fields;

        Title = IdentifyAttributeHelper.ResolveTitle(feature, _fields, layer.LabelColumn);

        // two features can share a label (two busbars "BUS82"): the id tells them apart
        IdText = $"[{feature.Id}]";
    }

    /// <summary>A drawing item under the drawings group: titled with its legend name, its own fields.</summary>
    public IdentifyFeatureNode(IdentifyLayerNode group, DrawingItemLayer drawing)
    {
        Layer = group ?? throw new ArgumentNullException(nameof(group));

        Drawing = drawing ?? throw new ArgumentNullException(nameof(drawing));

        Feature = drawing.Feature ?? throw new ArgumentException("A text drawing has no feature to identify.", nameof(drawing));

        _fields = drawing.GetFields();

        Title = string.IsNullOrWhiteSpace(drawing.LayerName)
                    ? IdentifyAttributeHelper.ResolveTitle(Feature, _fields)
                    : drawing.LayerName;

        // a drawing is named by its legend title; only a nameless one falls back to its id
        if (string.IsNullOrWhiteSpace(Title))
            IdText = $"[{Feature.Id}]";
    }

    private readonly IReadOnlyList<Field>? _fields;

    public IdentifyLayerNode Layer { get; }

    /// <summary>The drawing item this row stands for; null for a data-layer feature.</summary>
    public DrawingItemLayer? Drawing { get; }

    public Feature<Point> Feature { get; }

    public int Id => Feature.Id;

    /// <summary>
    /// The feature's label (<see cref="IdentifyAttributeHelper.ResolveTitle"/>), or a drawing's legend name;
    /// may be empty. Computed once at construction.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// "[Id]", shown in front of <see cref="Title"/> so features with the same label stay distinguishable.
    /// Null for a drawing item, which is named by its legend title and has no meaningful id.
    /// Kept apart from the title so the view can keep the Latin digits left-to-right in an RTL list.
    /// </summary>
    public string? IdText { get; }

    public bool HasTitle => !string.IsNullOrWhiteSpace(Title);

    public GeometryType GeometryType => Feature.GeometryType;

    /// <summary>Built lazily; identify result sets are small, so this is cheap and keeps construction light.</summary>
    public IReadOnlyList<IdentifyAttributeRow> Attributes => _attributes ??= IdentifyAttributeHelper.BuildRows(Feature, _fields);


    private bool _isSelected;
    public bool IsSelected
    {
        get { return _isSelected; }
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            RaisePropertyChanged();

            IsSelectedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? IsSelectedChanged;

    /// <summary>A leaf has nothing to expand; exists so the shared container style binds cleanly.</summary>
    public bool IsExpanded { get; set; }

    /// <summary>Case-insensitive match against the title, any display name or any display value.</summary>
    public bool Matches(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return true;

        var comparison = StringComparison.OrdinalIgnoreCase;

        if (Title.Contains(filter, comparison))
            return true;

        if (IdText is not null && IdText.Contains(filter, comparison))
            return true;

        return Attributes.Any(r => r.DisplayText.Contains(filter, comparison) || r.DisplayName.Contains(filter, comparison));
    }

    public override string ToString() => $"{Layer.LayerName} / {IdText} {Title}".Trim();
}
