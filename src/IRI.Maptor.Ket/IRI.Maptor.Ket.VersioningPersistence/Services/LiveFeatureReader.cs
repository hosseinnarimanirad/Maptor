using System.Data;
using System.Data.Common;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace IRI.Maptor.Ket.VersioningPersistence.Services;

/// <summary>
/// Generic reads against live feature tables, resolved from EF metadata (never hardcode
/// schema/table — live tables sit in their own schemas, e.g. <c>sub</c>/<c>tl</c>).
/// Used for stale/orphan flags and the compare view's live side; geometry comes back as
/// OGC WKB via STAsBinary so clients parse it exactly like proposal geometry.
/// </summary>
public static class LiveFeatureReader
{
    public static IEntityType? FindEntityType(DbContext context, string entityName)
        => context.Model.GetEntityTypes()
            .FirstOrDefault(t => string.Equals(t.ClrType.Name, entityName, StringComparison.OrdinalIgnoreCase));

    /// <summary>Live RowVersions for the given ids; a missing id means the feature is orphaned.</summary>
    public static async Task<Dictionary<long, byte[]>> GetRowVersionsAsync(
        DbContext context, IEntityType entityType, IReadOnlyCollection<long> featureIds, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<long, byte[]>();

        if (featureIds.Count == 0 || !TryGetIdentity(entityType, out var schema, out var table, out var keyColumn))
            return result;

        var idList = string.Join(",", featureIds.Distinct());

        await using var command = CreateCommand(
            context,
            $"SELECT [{keyColumn}], [RowVersion] FROM [{schema}].[{table}] WHERE [{keyColumn}] IN ({idList})");

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var id = Convert.ToInt64(reader.GetValue(0));
            result[id] = (byte[])reader.GetValue(1);
        }

        return result;
    }

    /// <summary>Full current state of one live feature; null when it no longer exists.</summary>
    public static async Task<LiveFeatureStateDto?> GetSnapshotAsync(
        DbContext context, IEntityType entityType, long featureId, CancellationToken cancellationToken = default)
    {
        if (!TryGetIdentity(entityType, out var schema, out var table, out var keyColumn))
            return null;

        var geometryColumn = entityType.GetProperties()
            .FirstOrDefault(p => p.ClrType == typeof(Geometry<Point>))?
            .GetColumnName();

        var scalarColumns = entityType.GetProperties()
            .Where(p => p.ClrType != typeof(Geometry<Point>))
            .Select(p => p.GetColumnName() ?? p.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var selectList = string.Join(", ", scalarColumns.Select(c => $"[{c}]"));

        if (geometryColumn is not null)
            selectList += $", [{geometryColumn}].STAsBinary() AS [__wkb], [{geometryColumn}].STSrid AS [__srid]";

        await using var command = CreateCommand(
            context,
            $"SELECT {selectList} FROM [{schema}].[{table}] WHERE [{keyColumn}] = {featureId}");

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        var snapshot = new LiveFeatureStateDto { FeatureId = featureId };

        for (int i = 0; i < reader.FieldCount; i++)
        {
            var name = reader.GetName(i);
            var value = reader.IsDBNull(i) ? null : reader.GetValue(i);

            switch (name)
            {
                case "__wkb":
                    snapshot.GeometryWkb = value as byte[];
                    break;
                case "__srid":
                    snapshot.Srid = value is null ? 0 : Convert.ToInt32(value);
                    break;
                case "RowVersion":
                    snapshot.RowVersion = value as byte[];
                    break;
                default:
                    snapshot.Attributes[name] = value;
                    break;
            }
        }

        return snapshot;
    }

    private static bool TryGetIdentity(IEntityType entityType, out string schema, out string table, out string keyColumn)
    {
        schema = entityType.GetSchema() ?? "dbo";
        table = entityType.GetTableName() ?? string.Empty;
        keyColumn = entityType.FindPrimaryKey()?.Properties.FirstOrDefault()?.GetColumnName() ?? string.Empty;

        return table.Length > 0 && keyColumn.Length > 0;
    }

    private static DbCommand CreateCommand(DbContext context, string sql)
    {
        var connection = context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
            connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = sql;

        // Joins the ambient transaction when one is active (harnesses roll everything back).
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();

        return command;
    }
}
