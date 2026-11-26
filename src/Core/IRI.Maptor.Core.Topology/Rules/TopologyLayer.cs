using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Core.Topology.Rules;

/// <summary>A layer a rule set talks about, named as the server names it (its entity name).</summary>
public sealed class TopologyLayer
{
    /// <param name="entityName">The layer's name, e.g. its server entity or table name.</param>
    /// <param name="geometryType">The kind of geometry the layer holds.</param>
    /// <param name="rank">Trust order: when two layers disagree, a fix moves the lower rank to the higher.</param>
    /// <param name="displayNameKey">Resource key of the layer's name for the user, if any.</param>
    public TopologyLayer(string entityName, GeometryType geometryType, int rank = 0, string? displayNameKey = null)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            throw new ArgumentException("A layer needs a name.", nameof(entityName));

        EntityName = entityName;
        GeometryType = geometryType;
        Rank = rank;
        DisplayNameKey = displayNameKey;
    }

    public string EntityName { get; }

    public GeometryType GeometryType { get; }

    public int Rank { get; }

    public string? DisplayNameKey { get; }

    public override string ToString() => EntityName;
}
