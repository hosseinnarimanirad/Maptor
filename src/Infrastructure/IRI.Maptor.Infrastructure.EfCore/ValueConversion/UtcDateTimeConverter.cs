using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace IRI.Maptor.Infrastructure.EfCore.ValueConversion;

/// <summary>
/// For databases that store every <see cref="DateTime"/> in UTC in a column without a time zone (SQL Server
/// <c>datetime2</c>, PostgreSQL <c>timestamp without time zone</c>). Values are read back with
/// <see cref="DateTimeKind.Utc"/>, so they serialize with a <c>Z</c> suffix and clients can show them in local
/// time. On write a <see cref="DateTimeKind.Local"/> value is converted to UTC and the kind is dropped (Npgsql
/// refuses a Kind=Utc value for <c>timestamp without time zone</c>); an Unspecified value is taken as UTC.
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(value => ToStore(value),
               value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }

    private static DateTime ToStore(DateTime value)
        => DateTime.SpecifyKind(value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value, DateTimeKind.Unspecified);
}
