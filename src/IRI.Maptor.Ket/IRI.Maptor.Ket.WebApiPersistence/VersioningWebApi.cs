using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Services;
using IRI.Maptor.Sta.Versioning;

namespace IRI.Maptor.Ket.WebApiPersistence;

/// <summary>
/// Client for the /Versioning API (docs/features/spatial-versioning, doc 04 §3).
/// Mirrors <see cref="WebApiInfrastructure"/>: static, bearer/headers optional, and a
/// caller-provided shared HttpClient (with its default Authorization) is preferred.
/// </summary>
public static class VersioningWebApi
{
    /// <summary>GET {baseUrl}/Versioning/Layers — which layers are versioned; clients route saves by this.</summary>
    public static Task<Response<List<VersionedLayerInfoDto>>> GetLayersAsync(
        string baseUrl,
        string? bearerToken = null,
        Dictionary<string, string>? headers = null,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/Versioning/Layers";

        return httpClient is not null
            ? HttpTransport.GetAsync<List<VersionedLayerInfoDto>>(httpClient, url, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken)
            : HttpTransport.GetAsync<List<VersionedLayerInfoDto>>(url, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken);
    }

    /// <summary>POST {baseUrl}/Versioning/Sessions — submit a version session.</summary>
    public static Task<Response<SessionSubmitResultDto>> SubmitSessionAsync(
        string baseUrl,
        SessionSubmitDto submission,
        string? bearerToken = null,
        Dictionary<string, string>? headers = null,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/Versioning/Sessions";

        return httpClient is not null
            ? HttpTransport.PostAsync<SessionSubmitResultDto>(httpClient, url, submission, cancellationToken)
            : HttpTransport.PostAsync<SessionSubmitResultDto>(url, submission, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken);
    }

    /// <summary>GET {baseUrl}/Versioning/MyProposals — own proposals with collapsed statuses.</summary>
    public static Task<Response<List<MyProposalDto>>> GetMyProposalsAsync(
        string baseUrl,
        string? bearerToken = null,
        Dictionary<string, string>? headers = null,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/Versioning/MyProposals";

        return httpClient is not null
            ? HttpTransport.GetAsync<List<MyProposalDto>>(httpClient, url, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken)
            : HttpTransport.GetAsync<List<MyProposalDto>>(url, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken);
    }

    /// <summary>GET {baseUrl}/Versioning/MyLayerPending — own active proposals with geometry (overlay).</summary>
    public static Task<Response<List<MyLayerPendingFeatureDto>>> GetMyLayerPendingAsync(
        string baseUrl,
        Guid layerKey,
        string? bearerToken = null,
        Dictionary<string, string>? headers = null,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/Versioning/MyLayerPending?layerKey={layerKey}";

        return httpClient is not null
            ? HttpTransport.GetAsync<List<MyLayerPendingFeatureDto>>(httpClient, url, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken)
            : HttpTransport.GetAsync<List<MyLayerPendingFeatureDto>>(url, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken);
    }

    /// <summary>GET {baseUrl}/Versioning/PendingStatus — on-demand per-feature check (count + authors).</summary>
    public static Task<Response<PendingStatusDto>> GetPendingStatusAsync(
        string baseUrl,
        Guid layerKey,
        long featureId,
        string? bearerToken = null,
        Dictionary<string, string>? headers = null,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/Versioning/PendingStatus?layerKey={layerKey}&featureId={featureId}";

        return httpClient is not null
            ? HttpTransport.GetAsync<PendingStatusDto>(httpClient, url, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken)
            : HttpTransport.GetAsync<PendingStatusDto>(url, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken);
    }

    // ------------------------------------------------------------- review (M3)

    public static Task<Response<List<ReviewQueueItemDto>>> GetReviewQueueAsync(
        string baseUrl, Guid? layerKey = null,
        string? bearerToken = null, Dictionary<string, string>? headers = null, HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/Versioning/Review/Queue" + (layerKey is null ? string.Empty : $"?layerKey={layerKey}");

        return httpClient is not null
            ? HttpTransport.GetAsync<List<ReviewQueueItemDto>>(httpClient, url, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken)
            : HttpTransport.GetAsync<List<ReviewQueueItemDto>>(url, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken);
    }

    public static Task<Response<CompetitionCompareDto>> GetCompetitionCompareAsync(
        string baseUrl, long competitionId,
        string? bearerToken = null, Dictionary<string, string>? headers = null, HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/Versioning/Review/Competitions/{competitionId}";

        return httpClient is not null
            ? HttpTransport.GetAsync<CompetitionCompareDto>(httpClient, url, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken)
            : HttpTransport.GetAsync<CompetitionCompareDto>(url, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken);
    }

    public static Task<Response<object>> SelectWinnerAsync(
        string baseUrl, SelectWinnerRequestDto request,
        string? bearerToken = null, Dictionary<string, string>? headers = null, HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/Versioning/Review/Competitions/{request.CompetitionId}/Select";

        return httpClient is not null
            ? HttpTransport.PostAsync<object>(httpClient, url, request, cancellationToken)
            : HttpTransport.PostAsync<object>(url, request, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken);
    }

    public static Task<Response<object>> CloseNoWinnerAsync(
        string baseUrl, CloseNoWinnerRequestDto request,
        string? bearerToken = null, Dictionary<string, string>? headers = null, HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/Versioning/Review/Competitions/{request.CompetitionId}/CloseNoWinner";

        return httpClient is not null
            ? HttpTransport.PostAsync<object>(httpClient, url, request, cancellationToken)
            : HttpTransport.PostAsync<object>(url, request, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken);
    }

    public static Task<Response<GroupResultDto>> GroupProposalsAsync(
        string baseUrl, GroupProposalsRequestDto request,
        string? bearerToken = null, Dictionary<string, string>? headers = null, HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/Versioning/Review/Group";

        return httpClient is not null
            ? HttpTransport.PostAsync<GroupResultDto>(httpClient, url, request, cancellationToken)
            : HttpTransport.PostAsync<GroupResultDto>(url, request, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken);
    }

    public static Task<Response<object>> DismissSuggestionAsync(
        string baseUrl, long suggestionId,
        string? bearerToken = null, Dictionary<string, string>? headers = null, HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/Versioning/Review/Suggestions/{suggestionId}/Dismiss";

        return httpClient is not null
            ? HttpTransport.PostAsync<object>(httpClient, url, null, cancellationToken)
            : HttpTransport.PostAsync<object>(url, null, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken);
    }

    public static Task<Response<List<BulkAcceptResultItemDto>>> BulkAcceptAsync(
        string baseUrl, BulkAcceptRequestDto request,
        string? bearerToken = null, Dictionary<string, string>? headers = null, HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/Versioning/Review/BulkAccept";

        return httpClient is not null
            ? HttpTransport.PostAsync<List<BulkAcceptResultItemDto>>(httpClient, url, request, cancellationToken)
            : HttpTransport.PostAsync<List<BulkAcceptResultItemDto>>(url, request, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken);
    }

    // ----------------------------------------------------- approval + history (M4)

    public static Task<Response<List<ApprovalQueueItemDto>>> GetApprovalQueueAsync(
        string baseUrl, Guid? layerKey = null,
        string? bearerToken = null, Dictionary<string, string>? headers = null, HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/Versioning/Approval/Queue" + (layerKey is null ? string.Empty : $"?layerKey={layerKey}");

        return httpClient is not null
            ? HttpTransport.GetAsync<List<ApprovalQueueItemDto>>(httpClient, url, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken)
            : HttpTransport.GetAsync<List<ApprovalQueueItemDto>>(url, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken);
    }

    public static Task<Response<CommitResultDto>> CommitAsync(
        string baseUrl, CommitRequestDto request,
        string? bearerToken = null, Dictionary<string, string>? headers = null, HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/Versioning/Approval/Commit";

        return httpClient is not null
            ? HttpTransport.PostAsync<CommitResultDto>(httpClient, url, request, cancellationToken)
            : HttpTransport.PostAsync<CommitResultDto>(url, request, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken);
    }

    public static Task<Response<object>> ReturnCompetitionAsync(
        string baseUrl, ReturnRequestDto request,
        string? bearerToken = null, Dictionary<string, string>? headers = null, HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/Versioning/Approval/Competitions/{request.CompetitionId}/Return";

        return httpClient is not null
            ? HttpTransport.PostAsync<object>(httpClient, url, request, cancellationToken)
            : HttpTransport.PostAsync<object>(url, request, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken);
    }

    public static Task<Response<FeatureTimelineDto>> GetFeatureTimelineAsync(
        string baseUrl, Guid layerKey, long featureId,
        string? bearerToken = null, Dictionary<string, string>? headers = null, HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/Versioning/History/{layerKey}/{featureId}";

        return httpClient is not null
            ? HttpTransport.GetAsync<FeatureTimelineDto>(httpClient, url, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken)
            : HttpTransport.GetAsync<FeatureTimelineDto>(url, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken);
    }

    // ------------------------------------------------------------------ inbox (M5)

    public static Task<Response<List<InboxItemDto>>> GetInboxAsync(
        string baseUrl, bool unreadOnly = false,
        string? bearerToken = null, Dictionary<string, string>? headers = null, HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/Versioning/Inbox" + (unreadOnly ? "?unreadOnly=true" : string.Empty);

        return httpClient is not null
            ? HttpTransport.GetAsync<List<InboxItemDto>>(httpClient, url, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken)
            : HttpTransport.GetAsync<List<InboxItemDto>>(url, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken);
    }

    public static Task<Response<InboxMarkReadResultDto>> MarkInboxReadAsync(
        string baseUrl, InboxMarkReadRequestDto request,
        string? bearerToken = null, Dictionary<string, string>? headers = null, HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/Versioning/Inbox/MarkRead";

        return httpClient is not null
            ? HttpTransport.PostAsync<InboxMarkReadResultDto>(httpClient, url, request, cancellationToken)
            : HttpTransport.PostAsync<InboxMarkReadResultDto>(url, request, bearer: bearerToken, headers: headers, cancellationToken: cancellationToken);
    }
}
