namespace IRI.Maptor.Core.Versioning;

/// <summary>
/// Marks a data source whose Save submits a review session instead of writing to live
/// (D34). The UI tier needs this without referencing the persistence tier: it decides
/// whether to badge the layer as versioned and whether Save must announce itself as
/// "submit for review" before it runs.
/// </summary>
public interface IVersionedEditTarget
{
    /// <summary>Registry key of the versioned layer behind this source.</summary>
    Guid LayerKey { get; }

    /// <summary>Optional session title/comment applied to the next submission.</summary>
    string? NextSessionTitle { get; set; }

    string? NextSessionComment { get; set; }

    /// <summary>
    /// How many edits the next Save would submit. Zero means Save has nothing to send —
    /// the confirmation must not appear.
    /// </summary>
    int CountPendingChanges();
}
