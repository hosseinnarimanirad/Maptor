using System.Globalization;
using System.Text.Json;
using IRI.Maptor.Sta.Common.Exceptions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace IRI.Maptor.Ket.VersioningPersistence.Services;

/// <summary>
/// Applies a winning proposal's serialized state to the live feature table, generically
/// via EF metadata — no per-entity code for ~100 tables. Unknown attribute keys are
/// dropped with a warning (tolerant mapping, doc 03 §5.3); a value that cannot be
/// converted to its column's CLR type blocks the commit (SchemaMismatch).
/// Audit stamps are written per R10/D31: the EDITOR's id + display name, not the
/// approver's. gis_id is deliberately NOT stamped from ClientKey — the column is 20
/// chars, a Guid string is 36 (doc 03 §5.2's open candidate, resolved: skip).
/// </summary>
public static class LiveEntityWriter
{
    private static readonly HashSet<string> _skippedAttributeKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "RowVersion", "Id", "OBJECTID", "ObjectId",
        "CreatedById", "CreatedByFullName", "CreatedAt",
        "LastUpdatedById", "LastUpdatedByFullName", "LastUpdatedAt",
    };

    /// <summary>Creates and tracks a new live entity from a create-proposal. Returns the tracked entry.</summary>
    public static EntityEntry ApplyCreate(
        DbContext context, IEntityType entityType, Proposal winner,
        int editorUserId, string editorDisplayName, DateTime now, List<string> warnings)
    {
        var entity = Activator.CreateInstance(entityType.ClrType)
            ?? throw new VersioningException("SchemaMismatch", $"cannot instantiate entity '{entityType.ClrType.Name}'.");

        var entry = context.Add(entity);

        ApplyState(entry, entityType, winner, warnings);

        SetIfExists(entry, entityType, "CreatedById", editorUserId);
        SetIfExists(entry, entityType, "CreatedByFullName", editorDisplayName);
        SetIfExists(entry, entityType, "CreatedAt", now);
        SetIfExists(entry, entityType, "LastUpdatedById", editorUserId);
        SetIfExists(entry, entityType, "LastUpdatedByFullName", editorDisplayName);
        SetIfExists(entry, entityType, "LastUpdatedAt", now);

        return entry;
    }

    /// <summary>Overwrites a tracked live entity with the winner's full state.</summary>
    public static void ApplyUpdate(
        EntityEntry entry, IEntityType entityType, Proposal winner,
        int editorUserId, string editorDisplayName, DateTime now, List<string> warnings)
    {
        ApplyState(entry, entityType, winner, warnings);

        SetIfExists(entry, entityType, "LastUpdatedById", editorUserId);
        SetIfExists(entry, entityType, "LastUpdatedByFullName", editorDisplayName);
        SetIfExists(entry, entityType, "LastUpdatedAt", now);
    }

    /// <summary>Loads the tracked live entity for an update/delete target; null when it no longer exists.</summary>
    public static EntityEntry? FindLiveEntry(DbContext context, IEntityType entityType, long featureId)
    {
        var keyProperty = entityType.FindPrimaryKey()?.Properties.FirstOrDefault()
            ?? throw new VersioningException("SchemaMismatch", $"entity '{entityType.ClrType.Name}' has no primary key.");

        var keyValue = ConvertValue(featureId, keyProperty.ClrType, keyProperty.Name, entityType.ClrType.Name);

        var entity = context.Find(entityType.ClrType, keyValue);

        return entity is null ? null : context.Entry(entity);
    }

    private static void ApplyState(EntityEntry entry, IEntityType entityType, Proposal winner, List<string> warnings)
    {
        var geometryProperty = entityType.GetProperties().FirstOrDefault(p => p.ClrType == typeof(Geometry<Point>));

        if (geometryProperty is not null)
            entry.Property(geometryProperty.Name).CurrentValue = winner.ProposedGeometry;

        var attributes = winner.ProposedAttributesJson is null
            ? new Dictionary<string, object?>()
            : CanonicalAttributeSerializer.Deserialize(winner.ProposedAttributesJson);

        var propertiesByName = entityType.GetProperties()
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in attributes)
        {
            if (_skippedAttributeKeys.Contains(key))
                continue;

            if (!propertiesByName.TryGetValue(key, out var property) || property.ClrType == typeof(Geometry<Point>))
            {
                // Tolerant mapping: a field removed since submission is dropped, loudly.
                warnings.Add($"proposal {winner.Id}: attribute '{key}' has no column on '{entityType.ClrType.Name}' — dropped");
                continue;
            }

            entry.Property(property.Name).CurrentValue =
                ConvertValue(value, property.ClrType, property.Name, entityType.ClrType.Name);
        }
    }

    private static void SetIfExists(EntityEntry entry, IEntityType entityType, string propertyName, object value)
    {
        var property = entityType.FindProperty(propertyName);

        if (property is not null)
            entry.Property(propertyName).CurrentValue = ConvertValue(value, property.ClrType, propertyName, entityType.ClrType.Name);
    }

    /// <summary>Canonical-JSON primitives → column CLR types; failure = SchemaMismatch (blocks the batch).</summary>
    private static object? ConvertValue(object? value, Type targetType, string propertyName, string entityName)
    {
        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            switch (value)
            {
                case null:
                    if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) is null)
                        throw new InvalidOperationException("null into non-nullable column");
                    return null;

                case JsonElement:
                    throw new InvalidOperationException("structured JSON value cannot map to a scalar column");
            }

            if (type.IsInstanceOfType(value))
                return value;

            if (type == typeof(string))
                return Convert.ToString(value, CultureInfo.InvariantCulture);

            if (type == typeof(DateTime))
                return DateTime.Parse((string)value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

            if (type == typeof(DateTimeOffset))
                return DateTimeOffset.Parse((string)value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

            if (type == typeof(Guid))
                return Guid.Parse((string)value);

            if (type == typeof(byte[]))
                return Convert.FromBase64String((string)value);

            if (type == typeof(bool))
                return Convert.ToBoolean(value, CultureInfo.InvariantCulture);

            return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            throw new VersioningException("SchemaMismatch", $"value for '{propertyName}' on '{entityName}' cannot be applied ({ex.Message}); the layer schema changed since submission (doc 03 §5.3).");
        }
    }
}
