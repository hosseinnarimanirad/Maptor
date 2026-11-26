using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.Topology.Checking;

/// <summary>
/// Where a check reads one layer's features from. Its only job is loading; the engine never sees a data
/// source, a map layer or a database. Boxes are in the source's own coordinates (<see cref="Srid"/>).
/// </summary>
public interface ITopologyFeatureSource
{
    /// <summary>The layer's name, as the rule set names it.</summary>
    string EntityName { get; }

    /// <summary>
    /// The SRS of the features: geodetic WGS84 (4326) or an SRS in metres. Web Mercator is refused by the
    /// check, because its units are not ground metres; convert to WGS84 first.
    /// </summary>
    int Srid { get; }

    /// <summary>The features whose geometry may intersect <paramref name="box"/>; all of them when it is null.</summary>
    /// <param name="box">The area to load, or null for the whole layer.</param>
    /// <param name="fields">Attributes the rules read; a source may return more.</param>
    /// <param name="cancellationToken">Cancels the load.</param>
    Task<IReadOnlyList<Feature<Point>>> GetFeaturesAsync(BoundingBox? box, IReadOnlyCollection<string> fields, CancellationToken cancellationToken);

    /// <summary>The features with these ids; ids that do not exist are skipped.</summary>
    /// <param name="ids">The feature ids.</param>
    /// <param name="fields">Attributes the rules read; a source may return more.</param>
    /// <param name="cancellationToken">Cancels the load.</param>
    Task<IReadOnlyList<Feature<Point>>> GetFeaturesByIdsAsync(IReadOnlyCollection<long> ids, IReadOnlyCollection<string> fields, CancellationToken cancellationToken);
}
