using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Spatial.Importing;

/// <summary>
/// One column of the layer data is imported into, as the data model describes it: what a value must be to be
/// written there. Built from the app's model (Gas and Saba entities), so the import follows the same rules as the
/// app instead of guessing from the destination table.
/// </summary>
public class ImportField
{
    /// <summary>The column / property name.</summary>
    public string Name { get; set; } = string.Empty;

    public string? Alias { get; set; }

    public FieldDataType DataType { get; set; }

    /// <summary>Most characters of a text value; 0 for no limit.</summary>
    public int Length { get; set; }

    /// <summary>An empty value takes <see cref="DefaultValue"/>, or the row does not fit.</summary>
    public bool IsRequired { get; set; }

    /// <summary>The model's default (e.g. Gas's «نا مشخص» of a required coded field); null for none.</summary>
    public object? DefaultValue { get; set; }

    /// <summary>The values the model allows (with their codes, when the data model has codes); null for a free field.</summary>
    public FieldDomain? Domain { get; set; }

    public override string ToString() => $"{Name} ({DataType}{(Length > 0 ? $", {Length}" : "")}{(IsRequired ? ", required" : "")}{(Domain is null ? "" : $", domain {Domain.Name}")})";
}

/// <summary>Where one layer field takes its values from.</summary>
public class ImportFieldMapping
{
    public ImportFieldMapping(ImportField target, string? sourceName, FieldDomain? domain = null)
    {
        Target = target;
        SourceName = sourceName;
        Domain = domain;
    }

    public ImportField Target { get; }

    /// <summary>The source column; null leaves the field to its default (or empty).</summary>
    public string? SourceName { get; }

    /// <summary>
    /// The domain the user chose for this column; null keeps the source value as it is (converted to the field's type).
    /// With a domain, a value that is one of its values is kept (in the domain's spelling) and a whole number that is one
    /// of its codes becomes that code's value; anything else does not fit.
    /// </summary>
    public FieldDomain? Domain { get; }

    /// <summary>
    /// A text longer than the field is cut to the field's length (and counted) instead of making the row not fit. The
    /// user's choice per column, for values whose extra characters are noise (e.g. the delivery's postal codes written as
    /// "1839796765.0000").
    /// </summary>
    public bool TruncateTooLong { get; set; }

    /// <summary>
    /// Runs on each non-empty source value before the type conversion and the domain (a constant runs without a source
    /// column); null converts the value as it is.
    /// </summary>
    public ImportValueFunction? Function { get; set; }
}

public class FeatureImportOptions
{
    /// <summary>Arabic «ي ى ك» become Persian «ی ک» in every text value (the user's choice for all imports).</summary>
    public bool NormalizePersianLetters { get; set; } = true;

    /// <summary>The kind of shape the layer holds (single or multi part); <see cref="GeometryCategory.None"/> for a table without shapes.</summary>
    public GeometryCategory GeometryCategory { get; set; } = GeometryCategory.None;

    /// <summary>The SRID of source shapes that carry none (0); null refuses them.</summary>
    public int? AssumedSourceSrid { get; set; }

    /// <summary>
    /// A value outside the column's domain is treated as empty (the field's default, else empty; counted) instead of making
    /// the row not fit — the user's choice for the provider's deliveries. A required field without a default still does
    /// not fit, since nothing valid can be written.
    /// </summary>
    public bool EmptyOutOfDomainValues { get; set; }
}
