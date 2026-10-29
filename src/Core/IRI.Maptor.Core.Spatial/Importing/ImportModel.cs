using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Common.Common.JsonConverters;

namespace IRI.Maptor.Core.Spatial.Importing;

/// <summary>
/// An application's data model as an importer needs it (the "import model" file): for every layer its table and
/// columns with types, lengths, required-ness, defaults and domains, and which columns the database or the importer
/// fills. Exported by the app's server from its compiled model (<c>GET LayerSetting/Admin/ImportModel</c>), so an
/// import that writes to the database directly follows the same rules as the app.
/// </summary>
public class ImportModelDto
{
    public const int CurrentFormatVersion = 1;

    public int FormatVersion { get; set; } = CurrentFormatVersion;

    /// <summary>"Gas", "Saba", ...</summary>
    public string Application { get; set; } = string.Empty;

    /// <summary>The data model the app implements, e.g. "NIGC.DataModel 1.10"; null when it has no name.</summary>
    public string? Model { get; set; }

    /// <summary>"SqlServer" or "PostgreSql": the database the file was exported from (column names are the same on both).</summary>
    public string? DatabaseProvider { get; set; }

    public DateTime ExportedAt { get; set; }

    public List<ImportModelLayerDto> Layers { get; set; } = new List<ImportModelLayerDto>();

    public List<ImportModelDomainDto> Domains { get; set; } = new List<ImportModelDomainDto>();

    private static readonly System.Text.Json.JsonSerializerOptions FileOptions = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        // Persian aliases and domain values stay readable in the file
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>The file's text: indented JSON, camelCase, Persian kept as is.</summary>
    public string ToJson() => System.Text.Json.JsonSerializer.Serialize(this, FileOptions);

    /// <summary>Reads a file written by <see cref="ToJson"/>; throws for a format version this code does not know.</summary>
    public static ImportModelDto FromJson(string json)
    {
        var model = System.Text.Json.JsonSerializer.Deserialize<ImportModelDto>(json, FileOptions)
            ?? throw new FormatException("The import model file is empty.");

        if (model.FormatVersion > CurrentFormatVersion)
            throw new FormatException($"The import model file has format version {model.FormatVersion}; this program reads up to {CurrentFormatVersion}.");

        return model;
    }

    public FieldDomain? FindDomain(string? name)
    {
        var domain = name is null ? null : Domains.FirstOrDefault(d => string.Equals(d.Name, name, StringComparison.Ordinal));

        return domain is null ? null : new FieldDomain { Name = domain.Name, Description = domain.Description, Values = domain.Values };
    }
}

public class ImportModelLayerDto
{
    /// <summary>The LayerSettings id.</summary>
    public int LayerId { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>The entity's alias (e.g. «علمک»).</summary>
    public string? Alias { get; set; }

    /// <summary>The layer's name in the data model (Gas: "layer_1256"); null when the app has none.</summary>
    public string? DataModelName { get; set; }

    public string? Schema { get; set; }

    public string Table { get; set; } = string.Empty;

    /// <summary>"Point", "Polyline" or "Polygon" (multi-part included); null when the model does not say (Saba): the source's kind is then taken.</summary>
    public string? Geometry { get; set; }

    /// <summary>WGS84 for both apps' geography columns.</summary>
    public int Srid { get; set; } = 4326;

    public List<ImportModelColumnDto> Columns { get; set; } = new List<ImportModelColumnDto>();

    /// <summary><see cref="Geometry"/> as the converter's option; <see cref="GeometryCategory.None"/> when unknown.</summary>
    public GeometryCategory GetGeometryCategory() => Geometry switch
    {
        "Point" => GeometryCategory.Point,
        "Polyline" => GeometryCategory.Polyline,
        "Polygon" => GeometryCategory.Polygon,
        _ => GeometryCategory.None,
    };

    /// <summary>The columns an import maps from the source (<see cref="ImportColumnRole.Data"/>), as the converter's fields.</summary>
    public List<ImportField> GetImportFields(ImportModelDto model) => Columns
        .Where(c => c.Role == ImportColumnRole.Data)
        .Select(c => new ImportField
        {
            Name = c.Column,
            Alias = c.Alias,
            DataType = c.DataType,
            Length = c.Length,
            IsRequired = c.IsRequired,
            DefaultValue = c.DefaultValue,
            Domain = model.FindDomain(c.Domain),
        })
        .ToList();
}

/// <summary>What a column is for, so the importer knows what it maps, what it fills and what it must not write.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ImportColumnRole
{
    /// <summary>Mapped from the source (this includes OBJECTID, GlobalID, Nid, gis_id).</summary>
    Data,

    /// <summary>The key the database generates; never written.</summary>
    Key,

    /// <summary>The geometry column.</summary>
    Shape,

    /// <summary>RowVersion / xmin, maintained by the database; never written.</summary>
    Concurrency,

    /// <summary>Creation time: the import time.</summary>
    CreatedAt,

    /// <summary>Creator's name: the name the operator gives the import.</summary>
    CreatedBy,

    /// <summary>Creator's user id: left empty (an import has no user).</summary>
    CreatedById,

    /// <summary>Last update time: the import time.</summary>
    UpdatedAt,

    /// <summary>Last updater's name: the name the operator gives the import.</summary>
    UpdatedBy,

    /// <summary>Last updater's user id: left empty.</summary>
    UpdatedById,
}

public class ImportModelColumnDto
{
    /// <summary>The column name in the database.</summary>
    public string Column { get; set; } = string.Empty;

    /// <summary>The entity property (differs from <see cref="Column"/> e.g. ObjectId → OBJECTID).</summary>
    public string Property { get; set; } = string.Empty;

    public string? Alias { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FieldDataType DataType { get; set; }

    /// <summary>The database type, e.g. "nvarchar(100)", "integer", "geography".</summary>
    public string? StoreType { get; set; }

    /// <summary>Most characters of a text column; 0 for no limit.</summary>
    public int Length { get; set; }

    public bool IsRequired { get; set; }

    /// <summary>The model's default for an empty value (e.g. «نا مشخص»); null for none.</summary>
    [JsonConverter(typeof(ObjectValueConverter))]
    public object? DefaultValue { get; set; }

    /// <summary>The name of the column's domain in <see cref="ImportModelDto.Domains"/>; null for a free column.</summary>
    public string? Domain { get; set; }

    public ImportColumnRole Role { get; set; }
}

public class ImportModelDomainDto
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>The stored values, with the data model's code where it has one (Gas); Saba's lists have values only.</summary>
    public List<FieldDomainValue> Values { get; set; } = new List<FieldDomainValue>();
}
