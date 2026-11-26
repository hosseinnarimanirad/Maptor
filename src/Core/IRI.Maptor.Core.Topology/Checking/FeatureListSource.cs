using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Core.Topology.Checking;

/// <summary>
/// A feature source over features already in memory: an imported file, a test, a console tool. Features
/// without geometry are returned only when the whole layer is asked for, so that a geometry check can report
/// them.
/// </summary>
public sealed class FeatureListSource : ITopologyFeatureSource
{
    private readonly IReadOnlyList<Feature<Point>> _features;

    public FeatureListSource(string entityName, int srid, IEnumerable<Feature<Point>> features)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            throw new ArgumentException("A source needs its layer's name.", nameof(entityName));

        EntityName = entityName;
        Srid = srid;
        _features = (features ?? throw new ArgumentNullException(nameof(features))).ToList();
    }

    public string EntityName { get; }

    public int Srid { get; }

    public Task<IReadOnlyList<Feature<Point>>> GetFeaturesAsync(BoundingBox? box, IReadOnlyCollection<string> fields, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (box is null)
            return Task.FromResult(_features);

        var area = box.Value;

        IReadOnlyList<Feature<Point>> result = _features
            .Where(f => f.TheGeometry is not null && !f.TheGeometry.IsNullOrEmpty() && f.TheGeometry.GetBoundingBox().Intersects(area))
            .ToList();

        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Feature<Point>>> GetFeaturesByIdsAsync(IReadOnlyCollection<long> ids, IReadOnlyCollection<string> fields, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var wanted = new HashSet<long>(ids ?? throw new ArgumentNullException(nameof(ids)));

        IReadOnlyList<Feature<Point>> result = _features.Where(f => wanted.Contains(f.Id)).ToList();

        return Task.FromResult(result);
    }
}
