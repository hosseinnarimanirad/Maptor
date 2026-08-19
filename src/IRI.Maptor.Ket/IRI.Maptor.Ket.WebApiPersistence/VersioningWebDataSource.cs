using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.Dtos;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Versioning;

namespace IRI.Maptor.Ket.WebApiPersistence;

/// <summary>
/// Data source for layers under spatial versioning: editing works exactly like
/// <see cref="WebApiDataSource"/>, but SaveChangesAsync submits the in-memory batch as a
/// version session (POST /Versioning/Sessions) instead of a direct sync. On success the
/// local edits are undone — the map always shows live truth; submitted work is pending,
/// not committed (D34). Subscribers get the submission result for the result dialog.
/// </summary>
public class VersioningWebDataSource : WebApiDataSource, IVersionedEditTarget
{
    private const string RowVersionAttributeName = "RowVersion";

    private readonly string _baseUrl;
    private readonly Guid _layerKey;

    /// <summary>Optional session title/comment the UI sets before saving; cleared after a successful submit.</summary>
    public string? NextSessionTitle { get; set; }

    public string? NextSessionComment { get; set; }

    public SessionSubmitResultDto? LastSubmissionResult { get; private set; }

    public event EventHandler<SessionSubmitResultDto>? SessionSubmitted;

    public VersioningWebDataSource(WebApiSourceParameter parameters, string baseUrl, Guid layerKey)
        : base(parameters)
    {
        _baseUrl = baseUrl;
        _layerKey = layerKey;
    }

    public Guid LayerKey => _layerKey;

    /// <summary>
    /// Same three buckets BuildSubmission draws from, without its orphan-delete guard: the
    /// caller only wants to know whether — and how much — Save would submit.
    /// </summary>
    public int CountPendingChanges()
        => _webMercatorFeatureSet.Features.Count(f => f.Status is FeatureStatus.New or FeatureStatus.Updated)
         + _webMercatorFeatureSet.GetAllFeatures().Count(f => f.Status == FeatureStatus.Removed);

    public override async Task SaveChangesAsync()
    {
        IsProcessing = true;

        try
        {
            HasError = false;

            var submission = BuildSubmission();

            if (submission.Proposals.Count == 0)
                return;

            var response = await VersioningWebApi.SubmitSessionAsync(
                _baseUrl,
                submission,
                _parameters.BearerToken,
                _parameters.Headers,
                _parameters.HttpClient);

            if (!response.IsSuccess || response.Result is null)
                throw VersioningApiErrors.ToException(response.Error?.Title, response.Error?.Detail)
                    ?? new Exception(response.Error?.Detail ?? response.Error?.Title ?? "Version session submission failed.");

            LastSubmissionResult = response.Result;
            NextSessionTitle = null;
            NextSessionComment = null;

            // D34: the map returns to live truth; the submitted state is pending, not live.
            UndoAllChanges();

            SessionSubmitted?.Invoke(this, response.Result);
        }
        catch
        {
            HasError = true;
            throw;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private SessionSubmitDto BuildSubmission()
    {
        var submission = new SessionSubmitDto
        {
            Title = NextSessionTitle,
            Comment = NextSessionComment,
        };

        foreach (var feature in _webMercatorFeatureSet.Features.Where(f => f.Status == FeatureStatus.New))
            submission.Proposals.Add(ToProposal(feature, ProposalChangeType.Create));

        foreach (var feature in _webMercatorFeatureSet.Features.Where(f => f.Status == FeatureStatus.Updated))
            submission.Proposals.Add(ToProposal(feature, ProposalChangeType.Update));

        foreach (var feature in _webMercatorFeatureSet.GetAllFeatures().Where(f => f.Status == FeatureStatus.Removed))
            submission.Proposals.Add(ToProposal(feature, ProposalChangeType.Delete));

        // Id-only deletions carry no RowVersion, so they cannot become delete proposals;
        // versioned layers must delete through tracked features (the normal edit tools do).
        var coveredIds = submission.Proposals
            .Where(p => p.ChangeType == ProposalChangeType.Delete && p.TargetFeatureId is not null)
            .Select(p => p.TargetFeatureId!.Value)
            .ToHashSet();

        var orphanDeleteId = _webMercatorFeatureSet.GetDeletedFeatureIds().FirstOrDefault(id => !coveredIds.Contains(id));

        if (orphanDeleteId != 0)
            throw new InvalidOperationException(
                $"Feature {orphanDeleteId} was deleted by id only (no RowVersion available); reload the layer and delete it through the editor.");

        return submission;
    }

    private ProposalSubmitDto ToProposal(Feature<Point> feature, ProposalChangeType changeType)
    {
        var proposal = new ProposalSubmitDto
        {
            LayerKey = _layerKey,
            ClientKey = feature.Key != Guid.Empty ? feature.Key : Guid.NewGuid(),
            ChangeType = changeType,
            TargetFeatureId = changeType == ProposalChangeType.Create ? null : feature.Id,
            BaseRowVersion = changeType == ProposalChangeType.Create ? null : ExtractRowVersion(feature),
        };

        if (changeType != ProposalChangeType.Delete)
        {
            var featureDto = FeatureDto.Parse(feature, SridHelper.GeodeticWGS84);

            proposal.GeometryBytes = featureDto.Shape;
            proposal.Srid = featureDto.Srid;

            proposal.Attributes = featureDto.Attributes
                .Where(pair => !string.Equals(pair.Key, RowVersionAttributeName, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(pair => pair.Key, pair => (object?)pair.Value);
        }

        return proposal;
    }

    /// <summary>
    /// The concurrency token rides in the attribute dictionary: byte[] after a sync
    /// round-trip, base64 string when freshly deserialized from the list endpoint.
    /// </summary>
    private static byte[] ExtractRowVersion(Feature<Point> feature)
    {
        if (feature.Attributes is not null && feature.Attributes.TryGetValue(RowVersionAttributeName, out var value))
        {
            switch (value)
            {
                case byte[] bytes when bytes.Length > 0:
                    return bytes;
                case string base64 when base64.Length > 0:
                    try { return Convert.FromBase64String(base64); }
                    catch (FormatException) { }
                    break;
            }
        }

        throw new InvalidOperationException(
            $"Feature {feature.Id} carries no usable RowVersion; reload the layer before editing a versioned layer.");
    }
}
