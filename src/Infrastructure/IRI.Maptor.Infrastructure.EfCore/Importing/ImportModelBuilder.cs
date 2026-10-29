using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using IRI.Maptor.Core.Common.Attributes;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Importing;

namespace IRI.Maptor.Infrastructure.EfCore.Importing;

/// <summary>A layer to put in the import model: its LayerSettings row, its entity and what only the app knows.</summary>
public class ImportModelLayerSource
{
    public int LayerId { get; set; }

    public string Title { get; set; } = string.Empty;

    public Type EntityType { get; set; } = null!;

    /// <summary>The layer's name in the data model (Gas "layer_1256"); null when the app has none.</summary>
    public string? DataModelName { get; set; }

    /// <summary>"Point", "Polyline" or "Polygon"; null when the app does not record it.</summary>
    public string? Geometry { get; set; }
}

/// <summary>
/// Builds an app's <see cref="ImportModelDto"/> from its EF model and its entities' attributes: table and column
/// names, store types, lengths and required-ness from EF (which reads nullable reference types, so a non-nullable
/// <c>string</c> is required), aliases and domains from <see cref="FieldAttribute"/> / <see cref="FieldDomainAttribute"/>,
/// defaults from the values a new entity starts with (text only: <c>string.Empty</c> and number defaults are not real
/// defaults), and each column's role (key, shape, concurrency token, audit, data). Shared by Gas and Saba.
/// </summary>
public static class ImportModelBuilder
{
    private const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";

    private const string PostgreSqlProvider = "Npgsql.EntityFrameworkCore.PostgreSQL";

    // the audit columns of both apps' FeatureBaseEntity (IHasCreatedByOptional, IHasCreatedAtOptional, IHasLastUpdatedByOptional)
    private static readonly Dictionary<string, ImportColumnRole> AuditRoles = new(StringComparer.Ordinal)
    {
        ["CreatedAt"] = ImportColumnRole.CreatedAt,
        ["CreatedByFullName"] = ImportColumnRole.CreatedBy,
        ["CreatedById"] = ImportColumnRole.CreatedById,
        ["LastUpdatedAt"] = ImportColumnRole.UpdatedAt,
        ["LastUpdatedByFullName"] = ImportColumnRole.UpdatedBy,
        ["LastUpdatedById"] = ImportColumnRole.UpdatedById,
    };

    public static ImportModelDto Build(DbContext context, string application, string? model, IEnumerable<ImportModelLayerSource> layers, DateTime? exportedAt = null)
    {
        var result = new ImportModelDto
        {
            Application = application,
            Model = model,
            DatabaseProvider = context.Database.ProviderName switch
            {
                SqlServerProvider => "SqlServer",
                PostgreSqlProvider => "PostgreSql",
                var other => other,
            },
            ExportedAt = exportedAt ?? DateTime.UtcNow,
        };

        var domains = new Dictionary<string, ImportModelDomainDto>(StringComparer.Ordinal);

        foreach (var layer in layers.OrderBy(l => l.LayerId))
        {
            var entityType = context.Model.FindEntityType(layer.EntityType)
                ?? throw new InvalidOperationException($"{layer.EntityType.Name} (layer {layer.LayerId}) is not part of the model.");

            result.Layers.Add(BuildLayer(layer, entityType, domains));
        }

        result.Domains = domains.Values.OrderBy(d => d.Name, StringComparer.Ordinal).ToList();

        return result;
    }

    private static ImportModelLayerDto BuildLayer(ImportModelLayerSource layer, IEntityType entityType, Dictionary<string, ImportModelDomainDto> domains)
    {
        var table = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());

        var defaults = CreateDefaults(layer.EntityType);

        var result = new ImportModelLayerDto
        {
            LayerId = layer.LayerId,
            Title = layer.Title,
            Alias = layer.EntityType.GetCustomAttribute<EntityAttribute>()?.Alias,
            DataModelName = layer.DataModelName,
            Schema = table.Schema,
            Table = table.Name,
            Geometry = layer.Geometry,
        };

        foreach (var property in entityType.GetProperties())
        {
            var column = property.GetColumnName(table);

            if (column is null)
                continue;

            var info = property.PropertyInfo;
            var fieldAttribute = info?.GetCustomAttribute<FieldAttribute>();
            var storeType = property.GetColumnType();
            var role = GetRole(property, storeType);

            var columnDto = new ImportModelColumnDto
            {
                Column = column,
                Property = property.Name,
                Alias = fieldAttribute?.Alias,
                DataType = FieldDataTypes.FromClrType(property.ClrType),
                StoreType = storeType,
                Length = property.GetMaxLength() ?? 0,
                IsRequired = !property.IsColumnNullable(table),
                Role = role,
            };

            if (role == ImportColumnRole.Data && info is not null)
            {
                columnDto.DefaultValue = defaults(info);

                // an application's domain (Gas coded values) wins; a plain AllowedValues list is a domain too (Saba)
                var domain = info.GetCustomAttribute<FieldDomainAttribute>()?.GetDomain() ?? FieldDomain.FromValues(fieldAttribute?.AllowedValues);

                if (domain is not null)
                    columnDto.Domain = AddDomain(domains, domain, $"{table.Name}.{column}");
            }

            result.Columns.Add(columnDto);
        }

        return result;
    }

    private static ImportColumnRole GetRole(IProperty property, string storeType)
    {
        if (property.IsPrimaryKey())
            return ImportColumnRole.Key;

        if (storeType.StartsWith("geography", StringComparison.OrdinalIgnoreCase) || storeType.StartsWith("geometry", StringComparison.OrdinalIgnoreCase))
            return ImportColumnRole.Shape;

        if (property.IsConcurrencyToken || property.ValueGenerated == ValueGenerated.OnAddOrUpdate)
            return ImportColumnRole.Concurrency;

        return AuditRoles.TryGetValue(property.Name, out var audit) ? audit : ImportColumnRole.Data;
    }

    // a named domain once (Gas's coded domains are shared by many columns); a plain list under "<table>.<column>"
    private static string AddDomain(Dictionary<string, ImportModelDomainDto> domains, FieldDomain domain, string fallbackName)
    {
        var name = string.IsNullOrWhiteSpace(domain.Name) ? fallbackName : domain.Name!;

        if (!domains.ContainsKey(name))
        {
            domains[name] = new ImportModelDomainDto
            {
                Name = name,
                Description = domain.Description,
                Values = domain.Values.Select(v => new FieldDomainValue { Value = v.Value, Code = v.Code, Label = v.Label }).ToList(),
            };
        }

        return name;
    }

    // the text a new entity starts with (e.g. «نا مشخص»); string.Empty, null and value types are no default
    private static Func<PropertyInfo, object?> CreateDefaults(Type entityType)
    {
        object? instance;

        try
        {
            instance = Activator.CreateInstance(entityType);
        }
        catch
        {
            instance = null;
        }

        return property =>
        {
            if (instance is null || property.PropertyType != typeof(string) || property.GetMethod is null)
                return null;

            return property.GetValue(instance) is string text && text.Length > 0 ? text : null;
        };
    }
}
