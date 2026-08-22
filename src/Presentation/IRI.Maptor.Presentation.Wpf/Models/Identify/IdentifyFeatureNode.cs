using System;
using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Wpf.Helpers;
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

        Title = IdentifyAttributeHelper.ResolveTitle(feature, layer.Fields);
    }

    public IdentifyLayerNode Layer { get; }

    public Feature<Point> Feature { get; }

    public int Id => Feature.Id;

    /// <summary>Label → first text attribute → #Id. Computed once at construction.</summary>
    public string Title { get; }

    public GeometryType GeometryType => Feature.GeometryType;

    /// <summary>Built lazily; identify result sets are small, so this is cheap and keeps construction light.</summary>
    public IReadOnlyList<IdentifyAttributeRow> Attributes => _attributes ??= IdentifyAttributeHelper.BuildRows(Feature, Layer.Fields);


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

        return Attributes.Any(r => r.DisplayText.Contains(filter, comparison) || r.DisplayName.Contains(filter, comparison));
    }

    public override string ToString() => $"{Layer.LayerName} / {Title}";
}
