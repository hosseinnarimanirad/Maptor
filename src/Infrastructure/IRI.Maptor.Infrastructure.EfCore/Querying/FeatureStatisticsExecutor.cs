using System.Globalization;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Dtos;

using SpatialTable = IRI.Maptor.Infrastructure.EfCore.Querying.FeatureQueryExecutor.SpatialTable;

namespace IRI.Maptor.Infrastructure.EfCore.Querying;

/// <summary>
/// Whole-layer statistics computed by the database, for a Live layer whose features stay on the server: one
/// attribute (<see cref="FieldStatisticsAsync"/>), features grouped by an attribute (<see cref="GroupStatisticsAsync"/>)
/// and the geometries (<see cref="GeometryStatisticsAsync"/>), on SQL Server or PostgreSQL. They mirror the client's
/// in-memory calculators (the Statistics tab). Raw SQL per provider, like <see cref="FeatureQueryExecutor.ExtentAsync"/>:
/// identifiers come only from the EF model and every other piece of the SQL is the executor's own text or a number
/// it computed, never caller text. Lengths and areas are geodesic (geography; a geometry column is read as WGS84).
/// </summary>
public static class FeatureStatisticsExecutor
{
    private static readonly MethodInfo FieldCoreMethod = typeof(FeatureStatisticsExecutor).GetMethod(nameof(FieldCoreAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo GroupCoreMethod = typeof(FeatureStatisticsExecutor).GetMethod(nameof(GroupCoreAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// Counts, distinct values with their counts (most frequent first, up to <see cref="FeatureQueryLimits.MaxDistinctValues"/>),
    /// and for a numeric column min/max/sum/mean/median/standard deviation with a histogram, for a date column the
    /// earliest and latest. Throws <see cref="FeatureQueryException"/> for an unknown field.
    /// </summary>
    public static async Task<FieldStatisticsDto> FieldStatisticsAsync<TEntity>(DbContext context, string field, FeatureQueryLimits limits, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        var target = SpatialTable.For(context, typeof(TEntity));

        var property = FeatureQueryExecutor.Resolve(FeatureQueryExecutor.GetFilterableProperties(target.EntityType), field);

        return await (Task<FieldStatisticsDto>)FieldCoreMethod.MakeGenericMethod(property.PropertyType)
            .Invoke(null, new object[] { context, target, property, limits, cancellationToken })!;
    }

    /// <summary>
    /// Features grouped by the values of <paramref name="field"/>: per group the count, the total length or area
    /// (<paramref name="measureKind"/>, see <see cref="GeometryMeasureKinds"/>; null for none) and the total of the numeric
    /// <paramref name="sumField"/> (optional). Largest groups first, up to <see cref="FeatureQueryLimits.MaxDistinctValues"/>;
    /// the totals cover every feature. Throws <see cref="FeatureQueryException"/> for an unknown or non-numeric field.
    /// </summary>
    public static async Task<GroupStatisticsDto> GroupStatisticsAsync<TEntity>(
        DbContext context,
        string field,
        string? sumField,
        string? measureKind,
        FeatureQueryLimits limits,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        var target = SpatialTable.For(context, typeof(TEntity));

        var scalars = FeatureQueryExecutor.GetFilterableProperties(target.EntityType);

        var property = FeatureQueryExecutor.Resolve(scalars, field);

        PropertyInfo? sumProperty = null;

        if (!string.IsNullOrWhiteSpace(sumField))
        {
            sumProperty = FeatureQueryExecutor.Resolve(scalars, sumField);

            if (!IsNumeric(sumProperty.PropertyType))
                throw new FeatureQueryException($"The field '{sumField}' is not numeric.");
        }

        return await (Task<GroupStatisticsDto>)GroupCoreMethod.MakeGenericMethod(property.PropertyType)
            .Invoke(null, new object?[] { context, target, property, sumProperty, ParseMeasureKind(measureKind), limits, cancellationToken })!;
    }

    /// <summary>
    /// The feature count, the empty geometries and, for <paramref name="measureKind"/> (see <see cref="GeometryMeasureKinds"/>;
    /// null for none), the per-feature length or area summarised; geometries of another type are counted as unmeasured.
    /// </summary>
    public static async Task<GeometryStatisticsDto> GeometryStatisticsAsync<TEntity>(DbContext context, string? measureKind, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        var target = SpatialTable.For(context, typeof(TEntity));

        var kind = ParseMeasureKind(measureKind);

        var sql = new Sql(target);

        var measure = kind is null ? null : sql.Measure(kind);

        var unmeasured = measure is null
            ? "CAST(0 AS bigint)"
            : $"CAST(COUNT(CASE WHEN NOT {sql.EmptyGeometry} AND NOT ({measure.TypeCondition}) THEN 1 END) AS bigint)";

        var counts = await FirstAsync<GeometryCountsRow>(context,
            $@"SELECT CAST(COUNT(*) AS bigint) AS {sql.Q("Total")},
                      CAST(COUNT(CASE WHEN {sql.EmptyGeometry} THEN 1 END) AS bigint) AS {sql.Q("Empty")},
                      {unmeasured} AS {sql.Q("Unmeasured")}
               FROM {target.Table}", cancellationToken);

        var summary = measure is null
            ? null
            : await NumericSummaryAsync(context, sql, $"SELECT {measure.Expression} AS {sql.V} FROM {target.Table} WHERE {measure.Measurable}", cancellationToken);

        return new GeometryStatisticsDto
        {
            FeatureCount = (int)counts.Total,
            EmptyGeometryCount = (int)counts.Empty,
            MeasureKind = kind,
            UnmeasuredCount = (int)counts.Unmeasured,
            Measures = summary,
        };
    }

    private static async Task<FieldStatisticsDto> FieldCoreAsync<TValue>(
        DbContext context,
        SpatialTable target,
        PropertyInfo property,
        FeatureQueryLimits limits,
        CancellationToken cancellationToken)
    {
        var sql = new Sql(target);

        var column = target.Column(property);

        var notEmpty = sql.NotEmpty(column, property.PropertyType);

        var counts = await FirstAsync<FieldCountsRow>(context,
            $@"SELECT CAST(COUNT(*) AS bigint) AS {sql.Q("Total")},
                      CAST(COUNT(CASE WHEN {notEmpty} THEN 1 END) AS bigint) AS {sql.Q("NonEmpty")},
                      CAST(COUNT(DISTINCT CASE WHEN {notEmpty} THEN {column} END) AS bigint) AS {sql.Q("Distinct")}
               FROM {target.Table}", cancellationToken);

        var result = new FieldStatisticsDto
        {
            FeatureCount = (int)counts.Total,
            EmptyCount = (int)(counts.Total - counts.NonEmpty),
            DistinctCount = (int)counts.Distinct,
        };

        // nothing to read below: every query would come back empty (and a non-nullable MIN would fail on NULL)
        if (counts.NonEmpty == 0)
            return result;

        var values = await context.Database.SqlQueryRaw<ValueRow<TValue>>(sql.Top(limits.MaxDistinctValues + 1,
            $"{column} AS {sql.Q("Value")}, CAST(COUNT(*) AS bigint) AS {sql.Q("Count")}",
            $"FROM {target.Table} WHERE {notEmpty} GROUP BY {column} ORDER BY COUNT(*) DESC, {column}")).ToListAsync(cancellationToken);

        result.Truncated = values.Count > limits.MaxDistinctValues;

        if (result.Truncated)
            values.RemoveAt(values.Count - 1);

        result.Values = values.Select(v => (object?)v.Value).ToArray();
        result.Counts = values.Select(v => (int)v.Count).ToArray();

        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (IsNumeric(type))
        {
            result.Numbers = await NumericSummaryAsync(context, sql,
                $"SELECT {sql.Float(column)} AS {sql.V} FROM {target.Table} WHERE {column} IS NOT NULL", cancellationToken);
        }
        else if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            var range = await FirstAsync<RangeRow<TValue>>(context,
                $"SELECT MIN({column}) AS {sql.Q("Min")}, MAX({column}) AS {sql.Q("Max")} FROM {target.Table} WHERE {column} IS NOT NULL", cancellationToken);

            result.MinDate = ToDateTime(range.Min);
            result.MaxDate = ToDateTime(range.Max);
        }

        return result;
    }

    private static async Task<GroupStatisticsDto> GroupCoreAsync<TValue>(
        DbContext context,
        SpatialTable target,
        PropertyInfo property,
        PropertyInfo? sumProperty,
        string? measureKind,
        FeatureQueryLimits limits,
        CancellationToken cancellationToken)
    {
        var sql = new Sql(target);

        var column = target.Column(property);

        var empty = sql.Empty(column, property.PropertyType);

        var measure = measureKind is null ? null : sql.Measure(measureKind);

        var sumColumn = sumProperty is null ? null : sql.Float(target.Column(sumProperty));

        string MeasureTotal(string? condition) => measure is null
            ? sql.NullFloat
            : $"SUM(CASE WHEN {(condition is null ? "" : $"{condition} AND ")}{measure.Measurable} THEN {measure.Expression} END)";

        string SumTotal(string? condition) => sumColumn is null
            ? sql.NullFloat
            : condition is null ? $"SUM({sumColumn})" : $"SUM(CASE WHEN {condition} THEN {sumColumn} END)";

        var rows = await context.Database.SqlQueryRaw<GroupRow<TValue>>(sql.Top(limits.MaxDistinctValues + 1,
            $@"{column} AS {sql.Q("Value")}, CAST(COUNT(*) AS bigint) AS {sql.Q("Count")},
               {MeasureTotal(null)} AS {sql.Q("Measure")}, {SumTotal(null)} AS {sql.Q("Sum")}",
            $"FROM {target.Table} WHERE NOT {empty} GROUP BY {column} ORDER BY COUNT(*) DESC, {column}")).ToListAsync(cancellationToken);

        var totals = await FirstAsync<GroupTotalsRow>(context,
            $@"SELECT CAST(COUNT(*) AS bigint) AS {sql.Q("Total")},
                      CAST(COUNT(CASE WHEN {empty} THEN 1 END) AS bigint) AS {sql.Q("EmptyCount")},
                      {MeasureTotal(empty)} AS {sql.Q("EmptyMeasure")},
                      {SumTotal(empty)} AS {sql.Q("EmptySum")},
                      {MeasureTotal(null)} AS {sql.Q("TotalMeasure")},
                      {SumTotal(null)} AS {sql.Q("TotalSum")},
                      CAST(COUNT(DISTINCT CASE WHEN NOT {empty} THEN {column} END) AS bigint) AS {sql.Q("Distinct")}
               FROM {target.Table}", cancellationToken);

        var truncated = rows.Count > limits.MaxDistinctValues;

        if (truncated)
            rows.RemoveAt(rows.Count - 1);

        return new GroupStatisticsDto
        {
            FeatureCount = (int)totals.Total,
            MeasureKind = measureKind,
            Keys = rows.Select(r => (object?)r.Value).ToArray(),
            Counts = rows.Select(r => (int)r.Count).ToArray(),
            Measures = rows.Select(r => r.Measure).ToArray(),
            Sums = rows.Select(r => r.Sum).ToArray(),
            EmptyCount = (int)totals.EmptyCount,
            EmptyMeasure = totals.EmptyMeasure,
            EmptySum = totals.EmptySum,
            TotalMeasure = totals.TotalMeasure,
            TotalSum = totals.TotalSum,
            GroupCount = (int)totals.Distinct + (totals.EmptyCount > 0 ? 1 : 0),
            Truncated = truncated,
        };
    }

    /// <summary>The summary of the column <see cref="Sql.V"/> of <paramref name="source"/> (a SELECT); null when it has no row.</summary>
    private static async Task<NumericSummaryDto?> NumericSummaryAsync(DbContext context, Sql sql, string source, CancellationToken cancellationToken)
    {
        var v = sql.V;

        var row = await FirstAsync<SummaryRow>(context,
            $@"SELECT CAST(COUNT(*) AS bigint) AS {sql.Q("Count")}, MIN({v}) AS {sql.Q("Min")}, MAX({v}) AS {sql.Q("Max")},
                      SUM({v}) AS {sql.Q("Sum")}, AVG({v}) AS {sql.Q("Mean")}, {sql.StandardDeviation}({v}) AS {sql.Q("StandardDeviation")}
               FROM ({source}) AS x", cancellationToken);

        if (row.Count == 0)
            return null;

        // exact median: PERCENTILE_CONT sorts the whole column (a few seconds on millions of rows); the user asked for it
        var median = await FirstAsync<ScalarRow>(context, sql.IsPostgreSql
            ? $"SELECT percentile_cont(0.5) WITHIN GROUP (ORDER BY {v}) AS {sql.Q("Value")} FROM ({source}) AS x"
            : $"SELECT TOP (1) PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY {v}) OVER () AS {sql.Q("Value")} FROM ({source}) AS x", cancellationToken);

        var count = (int)row.Count;
        double min = row.Min!.Value, max = row.Max!.Value;

        var (start, width, binCount) = HistogramLayout.Calculate(count, min, max);

        var bins = new int[binCount];

        if (binCount == 1)
        {
            bins[0] = count;
        }
        else
        {
            // the bin index before clamping; the few out-of-range ones (a value on the last edge) are folded in here
            var bin = $"FLOOR(({v} - {sql.Number(start)}) / {sql.Number(width)})";

            var counts = await context.Database.SqlQueryRaw<BinRow>(
                $"SELECT {bin} AS {sql.Q("Bin")}, CAST(COUNT(*) AS bigint) AS {sql.Q("Count")} FROM ({source}) AS x GROUP BY {bin}").ToListAsync(cancellationToken);

            foreach (var item in counts)
                bins[Math.Clamp((int)item.Bin, 0, binCount - 1)] += (int)item.Count;
        }

        return new NumericSummaryDto
        {
            Count = count,
            Min = min,
            Max = max,
            Sum = row.Sum ?? 0,
            Mean = row.Mean ?? 0,
            Median = median.Value ?? 0,
            StandardDeviation = row.StandardDeviation,
            Histogram = new HistogramDto { Start = start, BinWidth = width, Counts = bins },
        };
    }

    private static async Task<T> FirstAsync<T>(DbContext context, string sql, CancellationToken cancellationToken)
        => (await context.Database.SqlQueryRaw<T>(sql).ToListAsync(cancellationToken)).First();

    private static string? ParseMeasureKind(string? measureKind)
    {
        if (string.IsNullOrWhiteSpace(measureKind))
            return null;

        return GeometryMeasureKinds.Normalize(measureKind)
            ?? throw new FeatureQueryException($"Unknown measure '{measureKind}' (expected {GeometryMeasureKinds.Length} or {GeometryMeasureKinds.Area}).");
    }

    private static bool IsNumeric(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
            || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong)
            || type == typeof(float) || type == typeof(double) || type == typeof(decimal);
    }

    private static DateTime? ToDateTime(object? value) => value switch
    {
        DateTime date => date,
        DateTimeOffset offset => offset.DateTime,
        _ => null,
    };

    /// <summary>The SQL text pieces of one provider for one layer table.</summary>
    private sealed class Sql
    {
        private readonly SpatialTable _target;

        public Sql(SpatialTable target)
        {
            _target = target;
        }

        public bool IsPostgreSql => _target.IsPostgreSql;

        public string Q(string alias) => _target.Quote(alias);

        /// <summary>The value column of a summary source.</summary>
        public string V => Q("v");

        public string FloatType => IsPostgreSql ? "double precision" : "float";

        public string NullFloat => $"CAST(NULL AS {FloatType})";

        public string StandardDeviation => IsPostgreSql ? "stddev_samp" : "STDEV";

        public string Float(string expression) => $"CAST({expression} AS {FloatType})";

        /// <summary>A number the executor computed, as a typed literal.</summary>
        public string Number(double value) => Float(value.ToString("R", CultureInfo.InvariantCulture));

        /// <summary>SELECT with a row limit: TOP on SQL Server, LIMIT on PostgreSQL.</summary>
        public string Top(int count, string selectList, string rest) => IsPostgreSql
            ? $"SELECT {selectList} {rest} LIMIT {count}"
            : $"SELECT TOP ({count}) {selectList} {rest}";

        /// <summary>Null, or for text also blank (as the client counts empty values).</summary>
        public string Empty(string column, Type type) => type == typeof(string)
            ? $"({column} IS NULL OR {(IsPostgreSql ? $"btrim({column})" : $"LTRIM(RTRIM({column}))")} = '')"
            : $"({column} IS NULL)";

        public string NotEmpty(string column, Type type) => $"NOT {Empty(column, type)}";

        public string EmptyGeometry => IsPostgreSql
            ? $"({_target.Shape} IS NULL OR ST_IsEmpty({_target.Shape}::geometry))"
            : $"({_target.Shape} IS NULL OR {_target.Shape}.STIsEmpty() = 1)";

        /// <summary>The geodesic measure of <paramref name="kind"/> and which geometries it applies to.</summary>
        public MeasureSql Measure(string kind)
        {
            var shape = _target.Shape;
            var isLength = kind == GeometryMeasureKinds.Length;

            string typeCondition, expression;

            if (IsPostgreSql)
            {
                var geography = _target.IsGeography ? shape : $"{shape}::geography";

                typeCondition = isLength
                    ? $"GeometryType({shape}::geometry) IN ('LINESTRING', 'MULTILINESTRING')"
                    : $"GeometryType({shape}::geometry) IN ('POLYGON', 'MULTIPOLYGON')";

                expression = isLength ? $"ST_Length({geography})" : $"ST_Area({geography})";
            }
            else
            {
                var geography = _target.IsGeography ? shape : $"geography::STGeomFromWKB({shape}.STAsBinary(), 4326)";

                typeCondition = isLength
                    ? $"{shape}.STGeometryType() IN ('LineString', 'MultiLineString')"
                    : $"{shape}.STGeometryType() IN ('Polygon', 'MultiPolygon')";

                expression = isLength ? $"{geography}.STLength()" : $"{geography}.STArea()";
            }

            return new MeasureSql(typeCondition, $"NOT {EmptyGeometry} AND {typeCondition}", expression);
        }
    }

    private sealed record MeasureSql(string TypeCondition, string Measurable, string Expression);

    #region Rows read with SqlQueryRaw (property names = column aliases; internal, as EF builds them like PointExtentScan)

    internal sealed class FieldCountsRow
    {
        public long Total { get; set; }

        public long NonEmpty { get; set; }

        public long Distinct { get; set; }
    }

    internal sealed class ValueRow<TValue>
    {
        public TValue Value { get; set; } = default!;

        public long Count { get; set; }
    }

    internal sealed class RangeRow<TValue>
    {
        public TValue Min { get; set; } = default!;

        public TValue Max { get; set; } = default!;
    }

    internal sealed class GroupRow<TValue>
    {
        public TValue Value { get; set; } = default!;

        public long Count { get; set; }

        public double? Measure { get; set; }

        public double? Sum { get; set; }
    }

    internal sealed class GroupTotalsRow
    {
        public long Total { get; set; }

        public long EmptyCount { get; set; }

        public double? EmptyMeasure { get; set; }

        public double? EmptySum { get; set; }

        public double? TotalMeasure { get; set; }

        public double? TotalSum { get; set; }

        public long Distinct { get; set; }
    }

    internal sealed class GeometryCountsRow
    {
        public long Total { get; set; }

        public long Empty { get; set; }

        public long Unmeasured { get; set; }
    }

    internal sealed class SummaryRow
    {
        public long Count { get; set; }

        public double? Min { get; set; }

        public double? Max { get; set; }

        public double? Sum { get; set; }

        public double? Mean { get; set; }

        public double? StandardDeviation { get; set; }
    }

    internal sealed class ScalarRow
    {
        public double? Value { get; set; }
    }

    internal sealed class BinRow
    {
        public double Bin { get; set; }

        public long Count { get; set; }
    }

    #endregion
}
