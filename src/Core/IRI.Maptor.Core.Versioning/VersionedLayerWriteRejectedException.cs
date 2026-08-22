using IRI.Maptor.Core.Common.Exceptions;

namespace IRI.Maptor.Core.Versioning;

/// <summary>
/// Thrown when a direct sync write targets a layer that is under versioning (D26).
/// A distinct type so API error envelopes carry a distinct name/resource key the client
/// can match — the same pattern the concurrency exception uses.
/// </summary>
public class VersionedLayerWriteRejectedException : DomainException
{
    public VersionedLayerWriteRejectedException(string layerEntityName)
        : base($"VersionedLayerWriteRejected: layer '{layerEntityName}' is under versioning; submit changes as a version session instead of direct sync.")
    {
        LayerEntityName = layerEntityName;
    }

    public string LayerEntityName { get; }

    public override string MessageResourceKey => "message_error_versionedLayerWriteRejected";
}
