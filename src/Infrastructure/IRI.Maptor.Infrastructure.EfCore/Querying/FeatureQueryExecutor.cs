using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Dtos;

namespace IRI.Maptor.Infrastructure.EfCore.Querying;

/// <summary>Server limits of <see cref="FeatureQueryExecutor"/>; bound from configuration ("LayerQuery") by the apps.</summary>
public class FeatureQueryLimits
{
    public const string SectionName = "LayerQuery";

    /// <summary>Largest page in page mode; a bigger requested page size is lowered to it.</summary>
    public int MaxPageSize { get; set; } = 1000;

    /// <summary>Most features of one extent-mode answer; beyond it the answer is truncated.</summary>
    public int MaxExtentFeatures { get; set; } = 100_000;

    /// <summary>Most items of one In filter.</summary>
    public int MaxInValues { get; set; } = 1000;

    /// <summary>Most values of one distinct-values answer.</summary>
    public int MaxDistinctValues { get; set; } = 1000;
}

public class FeatureQueryResult<TEntity>
{
    public List<TEntity> Items { get; init; } = new();

    public int? TotalCount { get; init; }

    public int? PageIndex { get; init; }

    public int? PageSize { get; init; }

    public bool Truncated { get; init; }
}

/// <summary>
/// Runs a <see cref="FeatureQueryDto"/> on an entity set of any app, on SQL Server or PostgreSQL. The
/// spatial condition is raw SQL per provider (EF does not translate Maptor geometries); filters, sort,
/// count and paging are LINQ composed on top. Safe by construction: field names must be scalar properties
/// of the entity, every value is a SQL parameter, and identifiers come only from the EF model.
/// </summary>
public static class FeatureQueryExecutor
{
    internal const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";

    internal const string PostgreSqlProvider = "Npgsql.EntityFrameworkCore.PostgreSQL";

    public static async Task<FeatureQueryResult<TEntity>> ExecuteAsync<TEntity>(
        DbContext context,
        FeatureQueryDto query,
        FeatureQueryLimits limits,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is not part of the model.");

        var keyProperty = entityType.FindPrimaryKey()?.Properties.SingleOrDefault()?.PropertyInfo
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} needs a single-column key with a CLR property.");

        var source = query.HasBoundingBox
            ? FromBoundingBox<TEntity>(context, entityType, query)
            : context.Set<TEntity>();

        var filtered = source.AsNoTracking();

        var scalars = GetFilterableProperties(entityType);

        foreach (var filter in query.Filters ?? Enumerable.Empty<FeatureFilterDto>())
            filtered = filtered.Where(BuildPredicate<TEntity>(filter, scalars, limits));

        if (!string.IsNullOrWhiteSpace(query.SearchText))
            filtered = filtered.Where(BuildSearch<TEntity>(query.SearchText.Trim(), scalars));

        int? totalCount = query.IncludeCount ? await filtered.CountAsync(cancellationToken) : null;

        var ordered = Order(filtered, query, scalars, keyProperty);

        if (query.PageIndex is int pageIndex)
        {
            if (pageIndex < 0)
                throw new FeatureQueryException("PageIndex must not be negative.");

            var pageSize = Math.Clamp(query.PageSize, 1, limits.MaxPageSize);

            var items = await ordered.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync(cancellationToken);

            return new FeatureQueryResult<TEntity> { Items = items, TotalCount = totalCount, PageIndex = pageIndex, PageSize = pageSize };
        }

        // Extent mode: one more than the cap tells whether the answer is complete.
        var all = await ordered.Take(limits.MaxExtentFeatures + 1).ToListAsync(cancellationToken);

        var truncated = all.Count > limits.MaxExtentFeatures;

        if (truncated)
            all.RemoveAt(all.Count - 1);

        return new FeatureQueryResult<TEntity> { Items = all, TotalCount = totalCount, Truncated = truncated };
    }

    /// <summary>
    /// The distinct values of one scalar property over the whole set, ascending (null first), up to the limit.
    /// Async so that an unknown field faults the task (a 400 for the caller) instead of throwing through reflection.
    /// </summary>
    public static async Task<DistinctValuesDto> DistinctAsync<TEntity>(
        DbContext context,
        string field,
        FeatureQueryLimits limits,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is not part of the model.");

        var property = Resolve(GetFilterableProperties(entityType), field);

        var method = typeof(FeatureQueryExecutor).GetMethod(nameof(DistinctCoreAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(TEntity), property.PropertyType);

        return await (Task<DistinctValuesDto>)method.Invoke(null, new object[] { context, property, limits, cancellationToken })!;
    }

    private static async Task<DistinctValuesDto> DistinctCoreAsync<TEntity, TValue>(
        DbContext context,
        PropertyInfo property,
        FeatureQueryLimits limits,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var entity = Expression.Parameter(typeof(TEntity), "e");

        var selector = Expression.Lambda<Func<TEntity, TValue>>(Expression.Property(entity, property), entity);

        var values = await context.Set<TEntity>().AsNoTracking()
            .Select(selector)
            .Distinct()
            .OrderBy(v => v)
            .Take(limits.MaxDistinctValues + 1)
            .ToListAsync(cancellationToken);

        var truncated = values.Count > limits.MaxDistinctValues;

        if (truncated)
            values.RemoveAt(values.Count - 1);

        return new DistinctValuesDto { Values = values.Select(v => (object?)v).ToArray(), Truncated = truncated };
    }

    /// <summary>The extent of all features (WGS84), computed by the database; all null for an empty set.</summary>
    public static async Task<LayerExtentDto> ExtentAsync<TEntity>(DbContext context, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        var target = SpatialTable.For(context, typeof(TEntity));

        if (target.Provider == SqlServerProvider)
        {
            // One cheap pass first: the point coordinates (Long/Lat, or STX/STY, are NULL on other shapes).
            // A layer of points only (the large ones) is done here: 0.25 s for 500k rows, against 14 s for
            // the envelope aggregate below, which converts every geography to geometry.
            var (x, y) = target.IsGeography ? ("Long", "Lat") : ("STX", "STY");

            var scan = (await context.Database.SqlQueryRaw<PointExtentScan>(
                $@"SELECT MIN({target.Shape}.{x}) AS MinX, MIN({target.Shape}.{y}) AS MinY,
                          MAX({target.Shape}.{x}) AS MaxX, MAX({target.Shape}.{y}) AS MaxY,
                          COUNT({target.Shape}.{x}) AS Points, COUNT(*) AS Total
                   FROM {target.Table}").ToListAsync(cancellationToken)).First();

            if (scan.Points == scan.Total)
                return new LayerExtentDto { MinX = scan.MinX, MinY = scan.MinY, MaxX = scan.MaxX, MaxY = scan.MaxY };
        }

        var sql = target.Provider switch
        {
            // EnvelopeAggregate needs geometry; a geography column goes through its WKB (lon/lat, as WGS84 x/y).
            // The envelope is a point, a line or a rectangle; its first point is the minimum, and the point
            // at NumPoints / 2 + 1 the maximum (1, 2 or 3 of 1, 2 or 5 points).
            SqlServerProvider => $@"SELECT e.STPointN(1).STX AS MinX, e.STPointN(1).STY AS MinY,
                                           e.STPointN(e.STNumPoints() / 2 + 1).STX AS MaxX, e.STPointN(e.STNumPoints() / 2 + 1).STY AS MaxY
                                    FROM (SELECT geometry::EnvelopeAggregate({(target.IsGeography ? $"geometry::STGeomFromWKB({target.Shape}.STAsBinary(), 4326)" : target.Shape)}).STEnvelope() AS e
                                          FROM {target.Table}) AS x",
            PostgreSqlProvider => $@"SELECT ST_XMin(e) AS ""MinX"", ST_YMin(e) AS ""MinY"", ST_XMax(e) AS ""MaxX"", ST_YMax(e) AS ""MaxY""
                                     FROM (SELECT ST_Extent({target.Shape}::geometry) AS e FROM {target.Table}) AS x",
            _ => throw new NotSupportedException($"Spatial queries are not supported on provider {target.Provider}."),
        };

        var rows = await context.Database.SqlQueryRaw<LayerExtentDto>(sql).ToListAsync(cancellationToken);

        return rows.FirstOrDefault() ?? new LayerExtentDto();
    }

    /// <summary>The first pass of <see cref="ExtentAsync"/> on SQL Server.</summary>
    internal sealed class PointExtentScan
    {
        public double? MinX { get; set; }

        public double? MinY { get; set; }

        public double? MaxX { get; set; }

        public double? MaxY { get; set; }

        public int Points { get; set; }

        public int Total { get; set; }
    }

    /// <summary>A layer's table and spatial column as SQL identifiers of the current provider, from the EF model only.</summary>
    internal sealed record SpatialTable(string Provider, string Table, string Shape, string Columns, bool IsGeography)
    {
        /// <summary>The entity's model, for <see cref="Column"/>.</summary>
        public IEntityType EntityType { get; private init; } = null!;

        public StoreObjectIdentifier StoreObject { get; private init; }

        public bool IsPostgreSql => Provider == PostgreSqlProvider;

        /// <summary>A SQL identifier of the current provider (an alias, or a name from the EF model).</summary>
        public string Quote(string name) => IsPostgreSql
            ? $"\"{name.Replace("\"", "\"\"")}\""
            : $"[{name.Replace("]", "]]")}]";

        /// <summary>The quoted column of a scalar property of the entity.</summary>
        public string Column(PropertyInfo property)
        {
            var column = EntityType.FindProperty(property)?.GetColumnName(StoreObject)
                ?? throw new FeatureQueryException($"The field '{property.Name}' has no column.");

            return Quote(column);
        }

        public static SpatialTable For(DbContext context, Type entity)
        {
            var entityType = context.Model.FindEntityType(entity)
                ?? throw new InvalidOperationException($"{entity.Name} is not part of the model.");

            var spatial = entityType.GetProperties().FirstOrDefault(IsSpatial)
                ?? throw new FeatureQueryException("The layer has no spatial column.");

            var table = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());

            var provider = context.Database.ProviderName ?? string.Empty;

            string Quote(string name) => provider == PostgreSqlProvider
                ? $"\"{name.Replace("\"", "\"\"")}\""
                : $"[{name.Replace("]", "]]")}]";

            // Every mapped column, by name: "SELECT *" would miss PostgreSQL's system column xmin (RowVersion).
            var columns = string.Join(", ", entityType.GetProperties()
                .Select(p => p.GetColumnName(table))
                .Where(c => c is not null)
                .Distinct()
                .Select(c => Quote(c!)));

            return new SpatialTable(
                provider,
                table.Schema is null ? Quote(table.Name) : $"{Quote(table.Schema)}.{Quote(table.Name)}",
                Quote(spatial.GetColumnName(table)!),
                columns,
                spatial.GetColumnType().StartsWith("geography", StringComparison.OrdinalIgnoreCase))
            {
                EntityType = entityType,
                StoreObject = table,
            };
        }
    }

    #region Spatial

    private static IQueryable<TEntity> FromBoundingBox<TEntity>(DbContext context, IEntityType entityType, FeatureQueryDto query)
        where TEntity : class
    {
        var box = new BoundingBox(
            Math.Max(query.MinX!.Value, -180), Math.Max(query.MinY!.Value, -90),
            Math.Min(query.MaxX!.Value, 180), Math.Min(query.MaxY!.Value, 90));

        if (box.IsNaN() || !box.IsValid() || box.XMin > box.XMax || box.YMin > box.YMax)
            throw new FeatureQueryException("The bounding box is not valid (WGS84 degrees, Min <= Max).");

        var target = SpatialTable.For(context, typeof(TEntity));

        var sql = target.Provider switch
        {
            SqlServerProvider => $"SELECT {target.Columns} FROM {target.Table} WHERE {target.Shape}.STIntersects({(target.IsGeography ? "geography" : "geometry")}::STGeomFromText({{0}}, 4326)) = 1",
            PostgreSqlProvider => $"SELECT {target.Columns} FROM {target.Table} WHERE ST_Intersects({target.Shape}, {(target.IsGeography ? "ST_GeogFromText({0})" : "ST_GeomFromText({0}, 4326)")})",
            _ => throw new NotSupportedException($"Spatial queries are not supported on provider {target.Provider}."),
        };

        return context.Set<TEntity>().FromSqlRaw(sql, target.IsGeography ? DensifiedWkt(box) : box.AsWkt());
    }

    /// <summary>
    /// The box for a geography column. Geography edges are great-circle arcs, which bow poleward of the box's
    /// top and bottom latitude lines (about 18 m on a 0.4° wide box at 35°N): without extra vertices the query
    /// misses points just inside the bottom edge and adds some just outside the top one, against the straight
    /// edges the map draws. A vertex every 0.01° of longitude brings the bow under 2 cm; the side edges are
    /// meridians, already exact. Same counter-clockwise order as <see cref="BoundingBox.AsWkt"/>.
    /// </summary>
    internal static string DensifiedWkt(BoundingBox box)
    {
        const double step = 0.01;

        const int maxSegments = 4096;

        var segments = Math.Clamp((int)Math.Ceiling((box.XMax - box.XMin) / step), 1, maxSegments);

        var points = new List<string>(2 * segments + 3);

        string P(double x, double y) => string.Format(CultureInfo.InvariantCulture, "{0} {1}", x, y);

        // bottom edge west → east, top edge east → west
        for (int i = 0; i <= segments; i++)
            points.Add(P(box.XMin + (box.XMax - box.XMin) * i / segments, box.YMin));

        for (int i = segments; i >= 0; i--)
            points.Add(P(box.XMin + (box.XMax - box.XMin) * i / segments, box.YMax));

        points.Add(points[0]);

        return $"POLYGON(({string.Join(", ", points)}))";
    }

    private static bool IsSpatial(IProperty property)
    {
        var type = property.GetColumnType();

        return type.StartsWith("geography", StringComparison.OrdinalIgnoreCase)
            || type.StartsWith("geometry", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Filters

    /// <summary>The scalar CLR properties a query may filter or sort on, by name (case-insensitive).</summary>
    internal static Dictionary<string, PropertyInfo> GetFilterableProperties(IEntityType entityType)
    {
        return entityType.GetProperties()
            .Where(p => p.PropertyInfo is not null && !IsSpatial(p) && p.ClrType != typeof(byte[]))
            .ToDictionary(p => p.PropertyInfo!.Name, p => p.PropertyInfo!, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Any text property containing <paramref name="text"/>: LOWER(column) LIKE '%text%' on both providers
    /// (PostgreSQL's LIKE is case-sensitive), the text as a parameter. No text property: nothing matches.
    /// </summary>
    private static Expression<Func<TEntity, bool>> BuildSearch<TEntity>(string text, Dictionary<string, PropertyInfo> scalars)
    {
        var entity = Expression.Parameter(typeof(TEntity), "e");

        var needle = Parameter(text.ToLowerInvariant(), typeof(string));

        var toLower = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
        var contains = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;

        Expression? body = null;

        foreach (var property in scalars.Values.Where(p => p.PropertyType == typeof(string)).Distinct())
        {
            var member = Expression.Property(entity, property);

            var test = Expression.AndAlso(
                Expression.NotEqual(member, Expression.Constant(null, typeof(string))),
                Expression.Call(Expression.Call(member, toLower), contains, needle));

            body = body is null ? test : Expression.OrElse(body, test);
        }

        return Expression.Lambda<Func<TEntity, bool>>(body ?? Expression.Constant(false), entity);
    }

    internal static PropertyInfo Resolve(Dictionary<string, PropertyInfo> scalars, string? field)
    {
        if (string.IsNullOrWhiteSpace(field) || !scalars.TryGetValue(field, out var property))
            throw new FeatureQueryException($"Unknown or not filterable field '{field}'.");

        return property;
    }

    private static Expression<Func<TEntity, bool>> BuildPredicate<TEntity>(FeatureFilterDto filter, Dictionary<string, PropertyInfo> scalars, FeatureQueryLimits limits)
    {
        var property = Resolve(scalars, filter.Field);

        var entity = Expression.Parameter(typeof(TEntity), "e");

        var member = Expression.Property(entity, property);

        var type = property.PropertyType;

        Expression body = filter.Operator switch
        {
            FeatureFilterOperator.IsNull => IsNull(member, type),
            FeatureFilterOperator.NotNull => Expression.Not(IsNull(member, type)),
            FeatureFilterOperator.Eq => Equal(member, filter.Value, filter.Field),
            FeatureFilterOperator.Ne => Expression.Not(Equal(member, filter.Value, filter.Field)),
            FeatureFilterOperator.Lt or FeatureFilterOperator.Le or FeatureFilterOperator.Gt or FeatureFilterOperator.Ge
                => Compare(member, filter.Operator, Required(filter.Value, filter), filter.Field),
            FeatureFilterOperator.Between => Between(member, filter),
            FeatureFilterOperator.In => In(member, filter, limits),
            _ => throw new FeatureQueryException($"Unknown operator '{filter.Operator}'."),
        };

        return Expression.Lambda<Func<TEntity, bool>>(body, entity);
    }

    private static object Required(object? value, FeatureFilterDto filter)
        => IsNullValue(value) ? throw new FeatureQueryException($"'{filter.Operator}' on '{filter.Field}' needs a value.") : value!;

    private static Expression IsNull(MemberExpression member, Type type)
        => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null
            ? Expression.Equal(member, Expression.Constant(null, type))
            : Expression.Constant(false);

    /// <summary>Eq with a null value means "is null", as a filter on empty cells expects.</summary>
    private static Expression Equal(MemberExpression member, object? value, string field)
        => IsNullValue(value)
            ? IsNull(member, member.Type)
            : Expression.Equal(member, Parameter(Convert(value, member.Type, field), member.Type));

    private static Expression Compare(MemberExpression member, FeatureFilterOperator op, object value, string field)
    {
        var type = member.Type;
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        var operand = Parameter(Convert(value, type, field), type);

        if (underlying == typeof(string))
        {
            // string.Compare(a, b) op 0, which EF translates to a plain SQL comparison.
            var compare = Expression.Call(typeof(string).GetMethod(nameof(string.Compare), new[] { typeof(string), typeof(string) })!, member, operand);
            return MakeComparison(op, compare, Expression.Constant(0));
        }

        if (!IsOrderable(underlying))
            throw new FeatureQueryException($"'{op}' is not supported on '{field}' ({underlying.Name}).");

        return MakeComparison(op, member, operand);
    }

    private static Expression MakeComparison(FeatureFilterOperator op, Expression left, Expression right) => op switch
    {
        FeatureFilterOperator.Lt => Expression.LessThan(left, right),
        FeatureFilterOperator.Le => Expression.LessThanOrEqual(left, right),
        FeatureFilterOperator.Gt => Expression.GreaterThan(left, right),
        _ => Expression.GreaterThanOrEqual(left, right),
    };

    private static Expression Between(MemberExpression member, FeatureFilterDto filter)
    {
        var hasFrom = !IsNullValue(filter.Value);
        var hasTo = !IsNullValue(filter.ValueTo);

        if (!hasFrom && !hasTo)
            throw new FeatureQueryException($"'Between' on '{filter.Field}' needs at least one bound.");

        Expression? from = hasFrom ? Compare(member, FeatureFilterOperator.Ge, filter.Value!, filter.Field) : null;
        Expression? to = hasTo ? Compare(member, FeatureFilterOperator.Le, filter.ValueTo!, filter.Field) : null;

        return from is not null && to is not null ? Expression.AndAlso(from, to) : (from ?? to)!;
    }

    private static Expression In(MemberExpression member, FeatureFilterDto filter, FeatureQueryLimits limits)
    {
        var values = filter.Values ?? new List<object?>();

        if (values.Count == 0)
            throw new FeatureQueryException($"'In' on '{filter.Field}' needs at least one value.");

        if (values.Count > limits.MaxInValues)
            throw new FeatureQueryException($"'In' on '{filter.Field}' has more than {limits.MaxInValues} values.");

        var type = member.Type;

        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type))!;

        var includesNull = false;

        foreach (var value in values)
        {
            if (IsNullValue(value))
                includesNull = true;
            else
                list.Add(Convert(value, type, filter.Field));
        }

        Expression? body = null;

        if (list.Count > 0)
        {
            var contains = typeof(Enumerable).GetMethods()
                .Single(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
                .MakeGenericMethod(type);

            body = Expression.Call(contains, Parameter(list, list.GetType()), member);
        }

        if (includesNull)
        {
            var isNull = IsNull(member, type);
            body = body is null ? isNull : Expression.OrElse(body, isNull);
        }

        return body!;
    }

    private static bool IsOrderable(Type type)
        => type.IsPrimitive && type != typeof(bool) && type != typeof(char)
           || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan);

    private static bool IsNullValue(object? value) => value is null || value is JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined };

    /// <summary>
    /// A value as an EF parameter, not a SQL literal: EF parameterizes a member of a captured object
    /// (as for a closure), which also keeps the query plan the same for every value.
    /// </summary>
    private static Expression Parameter(object? value, Type type)
    {
        var holder = Activator.CreateInstance(typeof(ValueHolder<>).MakeGenericType(type))!;

        holder.GetType().GetProperty(nameof(ValueHolder<int>.Value))!.SetValue(holder, value);

        return Expression.Property(Expression.Constant(holder), nameof(ValueHolder<int>.Value));
    }

    private sealed class ValueHolder<T>
    {
        public T Value { get; set; } = default!;
    }

    /// <summary>A JSON or CLR value as the field's type; anything that does not fit is a 400, never a guess.</summary>
    private static object Convert(object? value, Type type, string field)
    {
        var target = Nullable.GetUnderlyingType(type) ?? type;

        if (value is JsonElement json)
        {
            value = json.ValueKind switch
            {
                JsonValueKind.String => json.GetString(),
                JsonValueKind.Number => json.GetRawText(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => throw new FeatureQueryException($"'{field}' cannot be compared with a JSON {json.ValueKind}."),
            };
        }

        try
        {
            if (target == typeof(string))
                return System.Convert.ToString(value, CultureInfo.InvariantCulture)!;

            if (target == typeof(Guid))
                return value is Guid guid ? guid : Guid.Parse(value!.ToString()!);

            if (target == typeof(DateTime))
                return value is DateTime date ? date : DateTime.Parse(value!.ToString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

            if (target == typeof(DateTimeOffset))
                return value is DateTimeOffset offset ? offset : DateTimeOffset.Parse(value!.ToString()!, CultureInfo.InvariantCulture);

            if (target.IsEnum)
                return value is string name ? Enum.Parse(target, name, ignoreCase: true) : Enum.ToObject(target, System.Convert.ToInt64(value, CultureInfo.InvariantCulture));

            if (target == typeof(bool) && value is string text)
                return bool.Parse(text);

            return System.Convert.ChangeType(value, target, CultureInfo.InvariantCulture)!;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            throw new FeatureQueryException($"'{value}' is not a valid {target.Name} for '{field}'.");
        }
    }

    #endregion

    #region Sort

    private static IQueryable<TEntity> Order<TEntity>(IQueryable<TEntity> source, FeatureQueryDto query, Dictionary<string, PropertyInfo> scalars, PropertyInfo key)
    {
        if (string.IsNullOrWhiteSpace(query.SortField))
            return OrderBy(source, key, query.SortDescending, thenBy: false);

        var property = Resolve(scalars, query.SortField);

        var ordered = OrderBy(source, property, query.SortDescending, thenBy: false);

        return property == key ? ordered : OrderBy(ordered, key, descending: false, thenBy: true);
    }

    private static IQueryable<TEntity> OrderBy<TEntity>(IQueryable<TEntity> source, PropertyInfo property, bool descending, bool thenBy)
    {
        var entity = Expression.Parameter(typeof(TEntity), "e");

        var selector = Expression.Lambda(Expression.Property(entity, property), entity);

        var name = (thenBy, descending) switch
        {
            (false, false) => nameof(Queryable.OrderBy),
            (false, true) => nameof(Queryable.OrderByDescending),
            (true, false) => nameof(Queryable.ThenBy),
            _ => nameof(Queryable.ThenByDescending),
        };

        var method = typeof(Queryable).GetMethods()
            .Single(m => m.Name == name && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TEntity), property.PropertyType);

        return (IQueryable<TEntity>)method.Invoke(null, new object[] { source, selector })!;
    }

    #endregion
}
