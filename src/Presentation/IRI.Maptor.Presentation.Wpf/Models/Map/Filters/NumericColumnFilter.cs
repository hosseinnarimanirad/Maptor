using System;
using System.Globalization;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Dtos;
using IRI.Maptor.Core.Spatial.Primitives;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Wpf.Models.Filters;

public enum NumericFilterOperator
{
    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    Between,
}

public class NumericColumnFilter : ColumnFilterViewModel
{
    public NumericColumnFilter(Field field, Func<IEnumerable<Feature<Point>>> featuresProvider)
        : base(field, featuresProvider)
    {
    }

    private NumericFilterOperator _operator = NumericFilterOperator.Equal;
    public NumericFilterOperator Operator
    {
        get => _operator;
        set
        {
            if (_operator == value)
                return;

            _operator = value;

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsBetween));

            RaiseFilterChanged();
        }
    }

    private double? _value;
    public double? Value
    {
        get => _value;
        set
        {
            if (_value == value)
                return;

            _value = value;

            RaisePropertyChanged();

            RaiseFilterChanged();
        }
    }

    private double? _valueTo;
    public double? ValueTo
    {
        get => _valueTo;
        set
        {
            if (_valueTo == value)
                return;

            _valueTo = value;

            RaisePropertyChanged();

            RaiseFilterChanged();
        }
    }

    public bool IsBetween => Operator == NumericFilterOperator.Between;

    public override bool IsActive => Value.HasValue || (IsBetween && ValueTo.HasValue);

    public override IEnumerable<FeatureFilterDto> ToServerFilters()
    {
        if (!IsActive)
            yield break;

        if (Operator == NumericFilterOperator.Between)
        {
            yield return new FeatureFilterDto { Field = FieldName, Operator = FeatureFilterOperator.Between, Value = Value, ValueTo = ValueTo };
            yield break;
        }

        yield return new FeatureFilterDto
        {
            Field = FieldName,
            Value = Value,
            Operator = Operator switch
            {
                NumericFilterOperator.NotEqual => FeatureFilterOperator.Ne,
                NumericFilterOperator.LessThan => FeatureFilterOperator.Lt,
                NumericFilterOperator.LessThanOrEqual => FeatureFilterOperator.Le,
                NumericFilterOperator.GreaterThan => FeatureFilterOperator.Gt,
                NumericFilterOperator.GreaterThanOrEqual => FeatureFilterOperator.Ge,
                _ => FeatureFilterOperator.Eq,
            },
        };

        // as in Matches: an empty value never matches, NotEqual included (SQL's <> would keep NULLs)
        if (Operator == NumericFilterOperator.NotEqual)
            yield return new FeatureFilterDto { Field = FieldName, Operator = FeatureFilterOperator.NotNull };
    }

    // null or unparseable attribute values never match an active numeric filter,
    // including NotEqual: comparisons are only meaningful for actual numbers
    public override bool Matches(object? rawValue)
    {
        if (!IsActive)
            return true;

        if (!TryToDouble(rawValue, out var number))
            return false;

        if (Operator == NumericFilterOperator.Between)
        {
            if (Value.HasValue && number < Value.Value)
                return false;

            if (ValueTo.HasValue && number > ValueTo.Value)
                return false;

            return true;
        }

        var operand = Value!.Value;

        return Operator switch
        {
            NumericFilterOperator.Equal => number == operand,
            NumericFilterOperator.NotEqual => number != operand,
            NumericFilterOperator.LessThan => number < operand,
            NumericFilterOperator.LessThanOrEqual => number <= operand,
            NumericFilterOperator.GreaterThan => number > operand,
            NumericFilterOperator.GreaterThanOrEqual => number >= operand,
            _ => true,
        };
    }

    public override void Clear()
    {
        _value = null;
        _valueTo = null;
        _operator = NumericFilterOperator.Equal;

        RaisePropertyChanged(nameof(Value));
        RaisePropertyChanged(nameof(ValueTo));
        RaisePropertyChanged(nameof(Operator));
        RaisePropertyChanged(nameof(IsBetween));

        RaiseFilterChanged();
    }

    internal static bool TryToDouble(object? rawValue, out double number)
    {
        number = 0;

        switch (rawValue)
        {
            case null:
            case DBNull:
                return false;

            case double d:
                number = d;
                return true;

            case string s:
                return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out number) ||
                       double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out number);

            case IConvertible convertible:
                try
                {
                    number = convertible.ToDouble(CultureInfo.InvariantCulture);
                    return true;
                }
                catch
                {
                    return false;
                }

            default:
                return false;
        }
    }
}
