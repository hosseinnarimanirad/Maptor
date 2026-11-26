using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Topology.Checking;

namespace IRI.Maptor.Core.Topology.Fixes;

/// <summary>
/// A feature source as it would read after a fix: the edited features with their new geometry, removed ones left
/// out, everything else as the source has it. Nothing is written.
/// </summary>
internal sealed class EditedFeatureSource : ITopologyFeatureSource
{
    private readonly ITopologyFeatureSource _source;
    private readonly Dictionary<long, FeatureEdit> _edits;

    public EditedFeatureSource(ITopologyFeatureSource source, IEnumerable<FeatureEdit> edits)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _edits = edits.Where(e => e.EntityName == source.EntityName).ToDictionary(e => e.FeatureId);
    }

    public string EntityName => _source.EntityName;

    public int Srid => _source.Srid;

    public async Task<IReadOnlyList<Feature<Point>>> GetFeaturesAsync(BoundingBox? box, IReadOnlyCollection<string> fields, CancellationToken cancellationToken)
    {
        var features = await _source.GetFeaturesAsync(box, fields, cancellationToken).ConfigureAwait(false);

        if (_edits.Count == 0)
            return features;

        var result = features.Where(f => !_edits.ContainsKey(f.Id)).ToList();

        // An edited feature counts where it is after the edit, whether or not it was in the box before.
        var originals = await _source.GetFeaturesByIdsAsync(_edits.Keys.ToList(), fields, cancellationToken).ConfigureAwait(false);

        foreach (var original in originals)
        {
            var after = _edits[original.Id].After;

            if (after is not null && (box is null || after.GetBoundingBox().Intersects(box.Value)))
                result.Add(Edited(original, after));
        }

        return result;
    }

    public async Task<IReadOnlyList<Feature<Point>>> GetFeaturesByIdsAsync(IReadOnlyCollection<long> ids, IReadOnlyCollection<string> fields, CancellationToken cancellationToken)
    {
        var features = await _source.GetFeaturesByIdsAsync(ids, fields, cancellationToken).ConfigureAwait(false);

        return features
            .Where(f => !_edits.TryGetValue(f.Id, out var edit) || edit.After is not null)
            .Select(f => _edits.TryGetValue(f.Id, out var edit) ? Edited(f, edit.After!) : f)
            .ToList();
    }

    private static Feature<Point> Edited(Feature<Point> original, Geometry<Point> after)
        => new(after, original.Attributes ?? new Dictionary<string, object>()) { Id = original.Id };
}
