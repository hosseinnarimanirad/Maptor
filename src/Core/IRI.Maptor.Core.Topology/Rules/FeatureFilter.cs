using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.Topology.Rules;

/// <summary>
/// Which features of a layer a rule checks, e.g. "overhead routes only". A rule set is declared in code, so the
/// filter is a predicate; its <see cref="Name"/> identifies it (work shared between rules is keyed by it) and its
/// <see cref="Fields"/> are the attributes it reads, so they are loaded.
/// </summary>
public sealed class FeatureFilter
{
    private readonly Func<Feature<Point>, bool> _predicate;

    /// <param name="name">A short stable name, e.g. "overhead"; two filters with one name must select the same features.</param>
    /// <param name="predicate">True for the features the rule checks.</param>
    /// <param name="fields">The attributes the predicate reads.</param>
    public FeatureFilter(string name, Func<Feature<Point>, bool> predicate, params string[] fields)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A filter needs a name.", nameof(name));

        Name = name;
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        Fields = fields ?? Array.Empty<string>();
    }

    public string Name { get; }

    public IReadOnlyCollection<string> Fields { get; }

    public bool Matches(Feature<Point> feature) => feature is not null && _predicate(feature);

    /// <summary>The value of an attribute as text, or null when the feature does not have it.</summary>
    public static string? TextOf(Feature<Point> feature, string field)
        => feature.Attributes is not null && feature.Attributes.TryGetValue(field, out var value) ? value?.ToString() : null;

    public override string ToString() => Name;
}
