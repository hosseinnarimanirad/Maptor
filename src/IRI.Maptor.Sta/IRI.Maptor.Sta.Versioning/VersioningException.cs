using IRI.Maptor.Sta.Common.Exceptions;

namespace IRI.Maptor.Sta.Versioning;

/// <summary>
/// Domain error of the versioning workflow. The stable code (doc 04 §8: InvalidProposal,
/// ConcurrentSubmission, StaleBaseRequiresOverride, …) prefixes the technical message —
/// same wire format the plain DomainException throws used — and derives the resource key,
/// so every code localizes client-side without a class per code.
/// </summary>
public class VersioningException : DomainException
{
    public const string ResourceKeyPrefix = "message_error_versioning";

    public VersioningException(string code, string technicalMessage)
        : base($"{code}: {technicalMessage}")
    {
        Code = code;
    }

    /// <summary>Stable error code (doc 04 §8) — also the API envelope's resource-key suffix.</summary>
    public string Code { get; }

    public override string MessageResourceKey => $"{ResourceKeyPrefix}{Code}";
}

/// <summary>
/// Client-side reconstruction of versioning errors from the API error envelope
/// (code = exception type name, resourceKey = MessageResourceKey — the middleware
/// contract). Rethrowing the typed exception lets the standard DomainException dialog
/// path localize it, the same way the concurrency exception is handled.
/// </summary>
public static class VersioningApiErrors
{
    public static DomainException? ToException(string? typeName, string? resourceKey)
    {
        if (typeName == nameof(VersionedLayerWriteRejectedException))
            return new VersionedLayerWriteRejectedException("(reported by the server)");

        if (typeName == nameof(VersioningException)
            && resourceKey is not null
            && resourceKey.StartsWith(VersioningException.ResourceKeyPrefix, StringComparison.Ordinal))
        {
            return new VersioningException(
                resourceKey.Substring(VersioningException.ResourceKeyPrefix.Length),
                "reported by the server.");
        }

        return null;
    }
}
