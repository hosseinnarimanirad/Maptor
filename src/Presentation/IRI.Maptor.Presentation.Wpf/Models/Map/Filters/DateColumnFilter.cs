using System;
using System.Globalization;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Dtos;
using IRI.Maptor.Core.Spatial.Primitives;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Wpf.Models.Filters;

/// <summary>
/// Inclusive From/To date-range filter; comparisons are date-only so time parts don't bite.
/// </summary>
public class DateColumnFilter : ColumnFilterViewModel
{
    public DateColumnFilter(Field field, Func<IEnumerable<Feature<Point>>> featuresProvider)
        : base(field, featuresProvider)
    {
    }

    private DateTime? _fromDate;
    public DateTime? FromDate
    {
        get => _fromDate;
        set
        {
            if (_fromDate == value)
                return;

            _fromDate = value;

            RaisePropertyChanged();

            RaiseFilterChanged();
        }
    }

    private DateTime? _toDate;
    public DateTime? ToDate
    {
        get => _toDate;
        set
        {
            if (_toDate == value)
                return;

            _toDate = value;

            RaisePropertyChanged();

            RaiseFilterChanged();
        }
    }

    public override bool IsActive => FromDate.HasValue || ToDate.HasValue;

    // date-only, as in Matches: the whole To day is included. The days are local; the server stores UTC.
    public override IEnumerable<FeatureFilterDto> ToServerFilters()
    {
        if (!IsActive)
            yield break;

        yield return new FeatureFilterDto
        {
            Field = FieldName,
            Operator = FeatureFilterOperator.Between,
            Value = FromDate.HasValue ? LocalToUtc(FromDate.Value.Date) : null,
            ValueTo = ToDate.HasValue ? LocalToUtc(ToDate.Value.Date.AddDays(1).AddTicks(-1)) : null,
        };
    }

    private static DateTime LocalToUtc(DateTime local) => DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();

    public override bool Matches(object? rawValue)
    {
        if (!IsActive)
            return true;

        if (!TryGetDateTime(rawValue, out var date))
            return false;

        // compared by the day the user sees
        if (date.Kind == DateTimeKind.Utc)
            date = date.ToLocalTime();

        if (FromDate.HasValue && date.Date < FromDate.Value.Date)
            return false;

        if (ToDate.HasValue && date.Date > ToDate.Value.Date)
            return false;

        return true;
    }

    public override void Clear()
    {
        _fromDate = null;
        _toDate = null;

        RaisePropertyChanged(nameof(FromDate));
        RaisePropertyChanged(nameof(ToDate));

        RaiseFilterChanged();
    }

    // attribute values may arrive as strings from some data sources; mirror the
    // tolerance of the display path (LocalizedDateTimeConverter)
    internal static bool TryGetDateTime(object? rawValue, out DateTime date)
    {
        switch (rawValue)
        {
            case DateTime dt:
                date = dt;
                return true;

            case DateTimeOffset dto:
                date = dto.LocalDateTime;
                return true;

            case string s when !string.IsNullOrWhiteSpace(s):
                return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out date) ||
                       DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out date) ||
                       DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

            default:
                date = default;
                return false;
        }
    }
}
