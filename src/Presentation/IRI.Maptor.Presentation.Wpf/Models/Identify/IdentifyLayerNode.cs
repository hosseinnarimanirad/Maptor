using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Presentation.Wpf.Models.Identify;

/// <summary>Parent of the identify tree: one layer with the features found under the click.</summary>
public class IdentifyLayerNode : Notifier, IIdentifyNode
{
    public IdentifyLayerNode(VectorLayer layer, FeatureSet<Point> hits)
    {
        Layer = layer ?? throw new ArgumentNullException(nameof(layer));

        if (hits is null)
            throw new ArgumentNullException(nameof(hits));

        LayerName = layer.LayerName;

        // IdentifyFeaturesAsync stamps the set with the layer's fields; fall back to the layer itself
        Fields = hits.Fields ?? layer.GetFields();

        Features = (hits.Features ?? Array.Empty<Feature<Point>>())
                        .Where(f => f is not null)
                        .Select(f => new IdentifyFeatureNode(this, f))
                        .ToList();

        GeometryType = hits.GeometryType != GeometryType.None
                            ? hits.GeometryType
                            : Features.FirstOrDefault()?.GeometryType ?? GeometryType.None;

        VisibleFeatures = new ObservableCollection<IdentifyFeatureNode>(Features);
    }

    public VectorLayer Layer { get; }

    public Guid LayerId => Layer.LayerId;

    public string LayerName { get; }

    public GeometryType GeometryType { get; }

    public IReadOnlyList<Field>? Fields { get; }

    /// <summary>Every hit, regardless of the filter.</summary>
    public IReadOnlyList<IdentifyFeatureNode> Features { get; }

    /// <summary>What the tree shows: <see cref="Features"/> narrowed by the current filter.</summary>
    public ObservableCollection<IdentifyFeatureNode> VisibleFeatures { get; }

    public int Count => Features.Count;

    public int VisibleCount => VisibleFeatures.Count;

    public bool HasVisibleFeatures => VisibleFeatures.Count > 0;

    /// <summary>Stroke width of the layer's own symbology; the highlight overlay is drawn relative to it.</summary>
    public double? StrokeThickness => Layer.DefaultSymbology?.StrokeThickness;

    /// <summary>Names the details pane lists for a layer node (displayable schema fields only).</summary>
    public IReadOnlyList<string> FieldNames =>
        Fields?.Where(FeatureTableHelper.IsDisplayableField)
               .Select(f => string.IsNullOrWhiteSpace(f.Alias) ? f.Name : f.Alias!)
               .ToList()
        ?? new List<string>();


    private bool _isExpanded = true;
    public bool IsExpanded
    {
        get { return _isExpanded; }
        set
        {
            if (_isExpanded == value)
                return;

            _isExpanded = value;
            RaisePropertyChanged();
        }
    }


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

    public void ApplyFilter(string? filter)
    {
        VisibleFeatures.Clear();

        foreach (var feature in Features)
        {
            if (feature.Matches(filter))
                VisibleFeatures.Add(feature);
        }

        RaisePropertyChanged(nameof(VisibleCount));
        RaisePropertyChanged(nameof(HasVisibleFeatures));
    }

    public override string ToString() => $"{LayerName} ({Count})";
}
