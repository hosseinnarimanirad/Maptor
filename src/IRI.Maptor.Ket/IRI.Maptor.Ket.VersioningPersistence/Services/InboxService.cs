using System.Text.Json;

using IRI.Maptor.Sta.Versioning;
using Microsoft.EntityFrameworkCore;

namespace IRI.Maptor.Ket.VersioningPersistence.Services;

/// <summary>
/// The notification inbox (doc 02 §8): per-session digests written by the other services
/// (N1 competition joined, N2 committed, N3 rejected, N4 closed no winner, N5 returned).
/// Reads are scoped to the recipient; payloads are normalized here so clients never see
/// the stored JSON. Manual refresh only (D37).
/// </summary>
public static class InboxService
{
    public static async Task<List<InboxItemDto>> GetInboxAsync(
        DbContext context, int userId, bool unreadOnly = false, int take = 200,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<VersionNotification>().AsNoTracking()
            .Where(n => n.RecipientUserId == userId);

        if (unreadOnly)
            query = query.Where(n => n.ReadAt == null);

        var rows = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return rows.Select(ToDto).ToList();
    }

    public static async Task<InboxMarkReadResultDto> MarkReadAsync(
        DbContext context, int userId, InboxMarkReadRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<VersionNotification>()
            .Where(n => n.RecipientUserId == userId && n.ReadAt == null);

        if (!request.All)
        {
            var ids = request.Ids ?? new List<long>();
            query = query.Where(n => ids.Contains(n.Id));
        }

        var rows = await query.ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        foreach (var row in rows)
            row.ReadAt = now;

        await context.SaveChangesAsync(cancellationToken);

        return new InboxMarkReadResultDto { MarkedCount = rows.Count };
    }

    /// <summary>
    /// Payload shapes are the writers' anonymous objects:
    /// N1 {competitions:[{competitionId,targetFeatureId}]}; N2/N3/N4
    /// {proposals:[{proposalId,targetFeatureId,reason}]}; N5 {competitionId,reason}.
    /// A malformed payload never hides the row — it just carries no detail.
    /// </summary>
    private static InboxItemDto ToDto(VersionNotification notification)
    {
        var dto = new InboxItemDto
        {
            Id = notification.Id,
            Type = notification.Type,
            SessionId = notification.SessionId,
            CompetitionId = notification.CompetitionId,
            CreatedAt = notification.CreatedAt,
            IsRead = notification.ReadAt is not null,
        };

        try
        {
            using var document = JsonDocument.Parse(notification.PayloadJson);
            var root = document.RootElement;

            if (root.TryGetProperty("proposals", out var proposals) && proposals.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in proposals.EnumerateArray())
                {
                    dto.ItemCount++;
                    CollectFeature(dto, item);
                    CollectReason(dto, item);
                }
            }

            if (root.TryGetProperty("competitions", out var competitions) && competitions.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in competitions.EnumerateArray())
                {
                    dto.ItemCount++;
                    CollectFeature(dto, item);
                }
            }

            CollectReason(dto, root);
        }
        catch (JsonException)
        {
        }

        return dto;
    }

    private static void CollectFeature(InboxItemDto dto, JsonElement item)
    {
        if (item.ValueKind == JsonValueKind.Object
            && item.TryGetProperty("targetFeatureId", out var feature)
            && feature.ValueKind == JsonValueKind.Number
            && !dto.TargetFeatureIds.Contains(feature.GetInt64()))
        {
            dto.TargetFeatureIds.Add(feature.GetInt64());
        }
    }

    private static void CollectReason(InboxItemDto dto, JsonElement item)
    {
        if (item.ValueKind == JsonValueKind.Object
            && item.TryGetProperty("reason", out var reason)
            && reason.ValueKind == JsonValueKind.String)
        {
            var text = reason.GetString();

            if (!string.IsNullOrWhiteSpace(text) && !dto.Reasons.Contains(text!))
                dto.Reasons.Add(text!);
        }
    }
}
