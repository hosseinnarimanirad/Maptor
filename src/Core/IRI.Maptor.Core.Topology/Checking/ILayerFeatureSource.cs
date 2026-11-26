namespace IRI.Maptor.Core.Topology.Checking;

/// <summary>
/// A feature source over a layer an app keeps open (a map layer): besides its features, an identity that changes
/// when the layer is replaced, whether it holds only part of the data, and a way to hear about edits and reloads.
/// Callers that keep results built from a layer use these to know when the results are out of date.
/// </summary>
public interface ILayerFeatureSource : ITopologyFeatureSource
{
    /// <summary>The layer itself; a different object means a different (reloaded) layer.</summary>
    object Identity { get; }

    /// <summary>True when the layer holds only what is in view (Live mode), so a whole-layer result cannot be built from it.</summary>
    bool IsLive { get; }

    /// <summary>Calls <paramref name="onChanged"/> whenever the layer's data is reloaded or edited, until disposed.</summary>
    IDisposable WatchChanges(Action onChanged);
}
