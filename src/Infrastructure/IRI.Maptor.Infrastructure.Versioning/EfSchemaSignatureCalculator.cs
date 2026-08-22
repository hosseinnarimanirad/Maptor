using IRI.Maptor.Core.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace IRI.Maptor.Infrastructure.Versioning;

/// <summary>
/// Computes a layer's schema signature from EF model metadata. Call at API startup for
/// every versioned entity and stamp the result into VersionedLayer — drift is detected,
/// never declared by hand.
/// </summary>
public static class EfSchemaSignatureCalculator
{
    /// <summary>
    /// Audit stamps and concurrency columns are excluded: they are assigned at commit,
    /// not proposed by editors, so changing them must not invalidate pending proposals.
    /// </summary>
    private static readonly HashSet<string> _defaultExcludedColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "RowVersion",
        "OBJECTID",
        "CreatedById", "CreatedByFullName", "CreatedAt",
        "LastUpdatedById", "LastUpdatedByFullName", "LastUpdatedAt",
    };

    public static string Calculate(IEntityType entityType, IEnumerable<string>? excludedColumns = null)
    {
        var excluded = excludedColumns is null
            ? _defaultExcludedColumns
            : new HashSet<string>(excludedColumns, StringComparer.OrdinalIgnoreCase);

        var primaryKeyProperties = entityType.FindPrimaryKey()?.Properties ?? (IReadOnlyList<IProperty>)Array.Empty<IProperty>();

        var fields = new List<FieldSignature>();

        foreach (var property in entityType.GetProperties())
        {
            var columnName = property.GetColumnName() ?? property.Name;

            if (excluded.Contains(columnName))
                continue;

            if (primaryKeyProperties.Contains(property) && property.ValueGenerated == ValueGenerated.OnAdd)
                continue;

            var storeType = property.GetColumnType() ?? property.ClrType.Name;

            fields.Add(new FieldSignature(columnName, storeType, property.IsNullable));
        }

        return SchemaSignatureCalculator.Calculate(fields);
    }
}
