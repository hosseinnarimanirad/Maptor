using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Core.Spatial.Dtos;

/// <summary>
/// A query on one layer's features, sent to <c>POST /LayerQuery/{layerId}</c>. Serves both the map and
/// the attribute table of a layer in Live mode:
/// <list type="bullet">
/// <item><b>Page mode</b> (<see cref="PageIndex"/> set): one page of at most the server's maximum page
/// size (1,000), for the attribute table; ask for <see cref="IncludeCount"/> when the filters change.</item>
/// <item><b>Extent mode</b> (<see cref="PageIndex"/> null): every matching feature, up to the server's
/// extent cap; above it the response is <see cref="FeaturePageDto.Truncated"/>.</item>
/// </list>
/// All conditions are combined with AND. Field names are the attribute names of the layer's features.
/// </summary>
public class FeatureQueryDto
{
    /// <summary>Spatial filter, WGS84 degrees: the features intersecting the box. All four or none.</summary>
    public double? MinX { get; set; }

    public double? MinY { get; set; }

    public double? MaxX { get; set; }

    public double? MaxY { get; set; }

    public List<FeatureFilterDto>? Filters { get; set; }

    /// <summary>
    /// Map search: features with any text attribute containing this text (case-insensitive). Combined with
    /// the other conditions by AND. Numbers and dates are not searched as text.
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>Sort column; the feature id is always the last sort key, so pages are stable.</summary>
    public string? SortField { get; set; }

    public bool SortDescending { get; set; }

    /// <summary>Zero-based page; null asks for every matching feature (extent mode).</summary>
    public int? PageIndex { get; set; }

    /// <summary>Rows per page in page mode; the server lowers it to its maximum.</summary>
    public int PageSize { get; set; } = 100;

    /// <summary>Also count all matching features (one more query on the server: ask only when the filters change).</summary>
    public bool IncludeCount { get; set; }

    public bool HasBoundingBox => MinX.HasValue && MinY.HasValue && MaxX.HasValue && MaxY.HasValue;
}

/// <summary>
/// One condition on an attribute. <see cref="Value"/> / <see cref="ValueTo"/> / <see cref="Values"/> hold
/// strings, numbers, booleans or ISO dates; the server converts them to the field's type.
/// </summary>
public class FeatureFilterDto
{
    public string Field { get; set; } = string.Empty;

    public FeatureFilterOperator Operator { get; set; }

    /// <summary>The operand of Eq, Ne, Lt, Le, Gt, Ge, and the lower bound of Between.</summary>
    public object? Value { get; set; }

    /// <summary>The upper bound of Between. Either bound of Between may be null (an open range).</summary>
    public object? ValueTo { get; set; }

    /// <summary>The set of In; a null item matches empty (null) values.</summary>
    public List<object?>? Values { get; set; }
}

/// <summary>
/// The distinct values of one attribute over the whole layer (<c>GET /LayerQuery/{layerId}/Distinct/{field}</c>),
/// ascending, null first when present: the checklist of the attribute table's value filter in Live mode.
/// </summary>
public class DistinctValuesDto
{
    /// <summary>Real CLR values (string, int, long, double, bool, null), as the attribute dictionaries hold them.</summary>
    [JsonConverter(typeof(IRI.Maptor.Core.Common.Common.JsonConverters.ObjectArrayConverter))]
    public object?[] Values { get; set; } = System.Array.Empty<object?>();

    /// <summary>More distinct values exist than the server returns (its limit, 1,000 by default).</summary>
    public bool Truncated { get; set; }
}

/// <summary>
/// The extent of all of a layer's features (<c>GET /LayerQuery/{layerId}/Extent</c>), WGS84 degrees; all null
/// for an empty layer. A Live layer does not hold all its features, so "zoom to layer" needs it from the server.
/// </summary>
public class LayerExtentDto
{
    public double? MinX { get; set; }

    public double? MinY { get; set; }

    public double? MaxX { get; set; }

    public double? MaxY { get; set; }
}

/// <summary>An invalid <see cref="FeatureQueryDto"/> (unknown field, wrong value type, bad box): the API answers 400 with the message.</summary>
public class FeatureQueryException : System.Exception
{
    public FeatureQueryException(string message) : base(message)
    {
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FeatureFilterOperator
{
    Eq,
    Ne,
    Lt,
    Le,
    Gt,
    Ge,
    /// <summary>Value &lt;= field &lt;= ValueTo; a missing bound is open.</summary>
    Between,
    In,
    IsNull,
    NotNull,
}

/// <summary>
/// The answer to a <see cref="FeatureQueryDto"/>: the features in compact form (all attributes, as the
/// layer's List endpoint returns them) plus the paging facts.
/// </summary>
public class FeaturePageDto : CompactFeatureSetDto
{
    /// <summary>All matching features; set when the query asked for <see cref="FeatureQueryDto.IncludeCount"/>.</summary>
    public int? TotalCount { get; set; }

    /// <summary>The page returned; null in extent mode.</summary>
    public int? PageIndex { get; set; }

    /// <summary>The page size actually used (the server may lower the requested one).</summary>
    public int? PageSize { get; set; }

    /// <summary>Extent mode only: more features match than the server's cap, and only the first ones (by id) are returned.</summary>
    public bool Truncated { get; set; }

    public static FeaturePageDto Create(FeatureSetDto featureSet, int? totalCount, int? pageIndex, int? pageSize, bool truncated)
    {
        var result = new FeaturePageDto
        {
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            Truncated = truncated,
        };

        result.Fill(featureSet);

        return result;
    }
}
