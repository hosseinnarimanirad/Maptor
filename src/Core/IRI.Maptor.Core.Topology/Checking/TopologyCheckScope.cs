using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Topology.Checking;

public enum CheckScopeKind
{
    /// <summary>Every enabled rule on all the data of its layers.</summary>
    WholeData = 0,

    /// <summary>The rules that involve one layer, on all the data of their layers.</summary>
    Layer = 1,

    /// <summary>Every enabled rule, for the issues located inside an area.</summary>
    Extent = 2,

    /// <summary>Every enabled rule, for the issues about chosen features of one layer.</summary>
    Features = 3,
}

/// <summary>What one check covers.</summary>
public sealed class TopologyCheckScope
{
    private TopologyCheckScope(CheckScopeKind kind, string? entityName, BoundingBox? extent, IReadOnlyCollection<long> featureIds)
    {
        Kind = kind;
        EntityName = entityName;
        Extent = extent;
        FeatureIds = featureIds;
    }

    public static TopologyCheckScope WholeData { get; } = new TopologyCheckScope(CheckScopeKind.WholeData, null, null, Array.Empty<long>());

    public static TopologyCheckScope ForLayer(string entityName)
        => new TopologyCheckScope(CheckScopeKind.Layer, Required(entityName), null, Array.Empty<long>());

    /// <param name="extent">The area, in the data's coordinates.</param>
    public static TopologyCheckScope ForExtent(BoundingBox extent)
    {
        if (extent.IsNaN() || extent.XMin > extent.XMax || extent.YMin > extent.YMax)
            throw new ArgumentException("The extent is not a valid box.", nameof(extent));

        return new TopologyCheckScope(CheckScopeKind.Extent, null, extent, Array.Empty<long>());
    }

    public static TopologyCheckScope ForFeatures(string entityName, IEnumerable<long> featureIds)
    {
        var ids = (featureIds ?? throw new ArgumentNullException(nameof(featureIds))).Distinct().ToList();

        if (ids.Count == 0)
            throw new ArgumentException("Choose at least one feature.", nameof(featureIds));

        return new TopologyCheckScope(CheckScopeKind.Features, Required(entityName), null, ids);
    }

    public CheckScopeKind Kind { get; }

    /// <summary>The layer of a <see cref="CheckScopeKind.Layer"/> or <see cref="CheckScopeKind.Features"/> scope.</summary>
    public string? EntityName { get; }

    /// <summary>The area of an <see cref="CheckScopeKind.Extent"/> scope.</summary>
    public BoundingBox? Extent { get; }

    /// <summary>The features of a <see cref="CheckScopeKind.Features"/> scope.</summary>
    public IReadOnlyCollection<long> FeatureIds { get; }

    public override string ToString() => Kind switch
    {
        CheckScopeKind.Layer => $"layer {EntityName}",
        CheckScopeKind.Extent => $"extent {Extent}",
        CheckScopeKind.Features => $"{FeatureIds.Count} features of {EntityName}",
        _ => "whole data",
    };

    private static string Required(string entityName)
        => string.IsNullOrWhiteSpace(entityName) ? throw new ArgumentException("Name the layer.", nameof(entityName)) : entityName;
}
