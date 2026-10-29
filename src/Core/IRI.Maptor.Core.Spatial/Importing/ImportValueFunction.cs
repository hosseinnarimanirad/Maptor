using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Core.Spatial.Importing;

/// <summary>The built-in functions a mapping can run on a source value before it is converted to the field's type.</summary>
public enum ImportFunctionKind
{
    /// <summary>The value as it is (converted to the field's type as without a function).</summary>
    None = 0,

    /// <summary>Jalali text («1403/10/05», «1403-1-5 14:30», «14031005», Persian digits) → a date, on the Iran clock.</summary>
    JalaliToDate,

    /// <summary>A date → Jalali text in a format (default yyyy/MM/dd).</summary>
    DateToJalali,

    /// <summary>Text with Persian or Arabic digits and thousands separators → a number.</summary>
    TextToNumber,

    /// <summary>A number (or numeric text) → text; whole values without decimals unless a number of decimals is given.</summary>
    NumberToText,

    /// <summary>Yes/no words → true/false.</summary>
    TextToBoolean,

    /// <summary>Repeated white space collapsed to one space.</summary>
    Trim,

    /// <summary>Placeholders written instead of nothing (e.g. «-») → empty.</summary>
    EmptyMarkers,

    /// <summary>A fixed value for every row; needs no source column.</summary>
    Constant,
}

/// <summary>What a function takes, for offering only the functions that fit the source column.</summary>
public enum ImportFunctionInput
{
    Any,
    Text,
    Number,
    Date,

    /// <summary>No source value is read (<see cref="ImportFunctionKind.Constant"/>).</summary>
    None,
}

/// <summary>A function's description for the mapping grid: title, what it takes and gives, and its parameter.</summary>
public class ImportFunctionInfo
{
    internal ImportFunctionInfo(ImportFunctionKind kind, string title, ImportFunctionInput input, FieldDataType? output, string? parameterLabel = null, string? defaultParameter = null)
    {
        Kind = kind;
        Title = title;
        Input = input;
        Output = output;
        ParameterLabel = parameterLabel;
        DefaultParameter = defaultParameter;
    }

    public ImportFunctionKind Kind { get; }

    public string Title { get; }

    public ImportFunctionInput Input { get; }

    /// <summary>The type of what it gives; null when it gives what it takes (or text the field's type is read from).</summary>
    public FieldDataType? Output { get; }

    /// <summary>The parameter's label; null for a function without one.</summary>
    public string? ParameterLabel { get; }

    public string? DefaultParameter { get; }

    public bool HasParameter => ParameterLabel is not null;

    public override string ToString() => Title;
}

/// <summary>
/// A function a mapping runs on each non-empty source value (a constant runs without one) before the value is converted
/// to the field's type and looked up in the domain. Explicit by design (the user's decision): there is no general text →
/// date guess, only Jalali → date. A value the function cannot read makes the field empty (or its default) and is counted;
/// a required field without a default then does not fit.
/// </summary>
public class ImportValueFunction
{
    public const string DefaultJalaliFormat = "yyyy/MM/dd";

    public const string DefaultTrueFalseWords = "بله,آری,دارد,Y,Yes,True,1|خیر,نه,ندارد,N,No,False,0";

    public const string DefaultEmptyMarkers = "-,_,.,—,?,؟";

    public static IReadOnlyList<ImportFunctionInfo> All { get; } = new List<ImportFunctionInfo>
    {
        new ImportFunctionInfo(ImportFunctionKind.None, "(none)", ImportFunctionInput.Any, null),
        new ImportFunctionInfo(ImportFunctionKind.JalaliToDate, "Jalali → date", ImportFunctionInput.Text, FieldDataType.DateTime),
        new ImportFunctionInfo(ImportFunctionKind.DateToJalali, "Date → Jalali text", ImportFunctionInput.Date, FieldDataType.Text, "Format", DefaultJalaliFormat),
        new ImportFunctionInfo(ImportFunctionKind.TextToNumber, "Text → number", ImportFunctionInput.Text, FieldDataType.Double),
        new ImportFunctionInfo(ImportFunctionKind.NumberToText, "Number → text", ImportFunctionInput.Number, FieldDataType.Text, "Decimals", string.Empty),
        new ImportFunctionInfo(ImportFunctionKind.TextToBoolean, "Text → yes/no", ImportFunctionInput.Text, FieldDataType.Boolean, "Yes words|no words", DefaultTrueFalseWords),
        new ImportFunctionInfo(ImportFunctionKind.Trim, "Trim spaces", ImportFunctionInput.Text, null),
        new ImportFunctionInfo(ImportFunctionKind.EmptyMarkers, "Empty markers → empty", ImportFunctionInput.Any, null, "Markers", DefaultEmptyMarkers),
        new ImportFunctionInfo(ImportFunctionKind.Constant, "Constant", ImportFunctionInput.None, null, "Value", string.Empty),
    };

    public static ImportFunctionInfo Describe(ImportFunctionKind kind) => All.First(f => f.Kind == kind);

    public ImportValueFunction(ImportFunctionKind kind, string? parameter = null)
    {
        Kind = kind;
        Parameter = parameter;
    }

    public ImportFunctionKind Kind { get; }

    /// <summary>The function's parameter (format, decimals, words, markers, the constant); null or empty takes its default.</summary>
    public string? Parameter { get; }

    public ImportFunctionInfo Info => Describe(Kind);

    /// <summary>False only for <see cref="ImportFunctionKind.Constant"/>, which runs for every row.</summary>
    public bool NeedsSource => Kind != ImportFunctionKind.Constant;

    private string ParameterOr(string fallback) => string.IsNullOrWhiteSpace(Parameter) ? fallback : Parameter!.Trim();

    /// <summary>The value after the function; false when the function cannot read it.</summary>
    public bool TryApply(object? value, out object? result)
    {
        result = value;

        try
        {
            switch (Kind)
            {
                case ImportFunctionKind.None:
                    return true;

                case ImportFunctionKind.JalaliToDate:
                    // a value that already is a date has nothing to read
                    if (value is DateTime)
                        return true;
                    if (value is string jalali && TryParseJalali(jalali, out var date))
                    {
                        result = date;
                        return true;
                    }
                    return false;

                case ImportFunctionKind.DateToJalali:
                    var gregorian = value switch
                    {
                        DateTime dateTime => dateTime,
                        DateTimeOffset offset => offset.DateTime,
                        _ => (DateTime?)null,
                    };
                    if (gregorian is null)
                        return false;
                    result = FormatJalali(gregorian.Value, ParameterOr(DefaultJalaliFormat));
                    return true;

                case ImportFunctionKind.TextToNumber:
                    if (value is string numberText)
                        return TryParseNumber(numberText, out result);
                    return IsNumber(value);

                case ImportFunctionKind.NumberToText:
                    return TryFormatNumber(value, Parameter, out result);

                case ImportFunctionKind.TextToBoolean:
                    return TryParseBoolean(value, ParameterOr(DefaultTrueFalseWords), out result);

                case ImportFunctionKind.Trim:
                    if (value is string text)
                        result = Regex.Replace(text, @"\s+", " ").Trim();
                    return true;

                case ImportFunctionKind.EmptyMarkers:
                    if (value is string marker && Split(ParameterOr(DefaultEmptyMarkers)).Contains(marker.Trim()))
                        result = null;
                    return true;

                case ImportFunctionKind.Constant:
                    result = string.IsNullOrEmpty(Parameter) ? null : Parameter;
                    return true;

                default:
                    return false;
            }
        }
        catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException)
        {
            return false;
        }
    }

    public override string ToString() => string.IsNullOrEmpty(Parameter) ? Info.Title : $"{Info.Title} ({Parameter})";

    #region Jalali

    private static readonly PersianCalendar Calendar = new PersianCalendar();

    private static readonly Regex JalaliPattern = new Regex(
        @"^(?<y>\d{4})[/\-.](?<m>\d{1,2})[/\-.](?<d>\d{1,2})(?:[ T]+(?<h>\d{1,2}):(?<min>\d{1,2})(?::(?<s>\d{1,2}))?)?$",
        RegexOptions.CultureInvariant);

    private static readonly Regex CompactJalaliPattern = new Regex(@"^(?<y>\d{4})(?<m>\d{2})(?<d>\d{2})$", RegexOptions.CultureInvariant);

    /// <summary>
    /// A Jalali date (and optional time) as a <see cref="DateTime"/> on the same clock (Kind Unspecified). Years outside
    /// 1200–1600 are refused, so a Gregorian date given by mistake is not read as one far in the future.
    /// </summary>
    public static bool TryParseJalali(string text, out DateTime date)
    {
        date = default;

        var normalized = text.FarsiNumbersToLatinNumbers().Trim();

        var match = JalaliPattern.Match(normalized);

        if (!match.Success)
            match = CompactJalaliPattern.Match(normalized);

        if (!match.Success)
            return false;

        int Part(string name) => match.Groups[name].Success ? int.Parse(match.Groups[name].Value, CultureInfo.InvariantCulture) : 0;

        var year = Part("y");
        var month = Part("m");
        var day = Part("d");

        if (year < 1200 || year > 1600 || month < 1 || month > 12 || day < 1 || day > Calendar.GetDaysInMonth(year, month))
            return false;

        var hour = Part("h");
        var minute = Part("min");
        var second = Part("s");

        if (hour > 23 || minute > 59 || second > 59)
            return false;

        date = Calendar.ToDateTime(year, month, day, hour, minute, second, 0);
        return true;
    }

    /// <summary>yyyy, MM, M, dd, d, HH, mm, ss in <paramref name="format"/> become the Jalali parts; anything else is kept.</summary>
    public static string FormatJalali(DateTime date, string format)
    {
        var year = Calendar.GetYear(date);
        var month = Calendar.GetMonth(date);
        var day = Calendar.GetDayOfMonth(date);

        return Regex.Replace(format, "yyyy|MM|M|dd|d|HH|mm|ss", token => token.Value switch
        {
            "yyyy" => year.ToString("0000", CultureInfo.InvariantCulture),
            "MM" => month.ToString("00", CultureInfo.InvariantCulture),
            "M" => month.ToString(CultureInfo.InvariantCulture),
            "dd" => day.ToString("00", CultureInfo.InvariantCulture),
            "d" => day.ToString(CultureInfo.InvariantCulture),
            "HH" => date.Hour.ToString("00", CultureInfo.InvariantCulture),
            "mm" => date.Minute.ToString("00", CultureInfo.InvariantCulture),
            _ => date.Second.ToString("00", CultureInfo.InvariantCulture),
        });
    }

    private static TimeZoneInfo? _iran;

    private static bool _iranLooked;

    /// <summary>
    /// An Iran clock time (as <see cref="TryParseJalali"/> gives) in UTC, for the columns the apps keep in UTC
    /// (CreatedAt, LastUpdatedAt). The system's Iran zone ("Iran Standard Time" on Windows, "Asia/Tehran" elsewhere) knows
    /// the daylight saving Iran kept until 2022; without it, +03:30.
    /// </summary>
    public static DateTime IranTimeToUtc(DateTime iranTime)
    {
        if (!_iranLooked)
        {
            foreach (var id in new[] { "Iran Standard Time", "Asia/Tehran" })
            {
                try
                {
                    _iran = TimeZoneInfo.FindSystemTimeZoneById(id);
                    break;
                }
                catch (Exception ex) when (ex is TimeZoneNotFoundException || ex is InvalidTimeZoneException)
                {
                }
            }

            _iranLooked = true;
        }

        var unspecified = DateTime.SpecifyKind(iranTime, DateTimeKind.Unspecified);

        return _iran is null
            ? DateTime.SpecifyKind(unspecified - new TimeSpan(3, 30, 0), DateTimeKind.Utc)
            : TimeZoneInfo.ConvertTimeToUtc(unspecified, _iran);
    }

    #endregion

    #region Numbers and words

    private static string NumberText(string text) => text.FarsiNumbersToLatinNumbers()
        .Replace(",", string.Empty).Replace("٬", string.Empty).Replace(" ", string.Empty).Trim();

    private static bool TryParseNumber(string text, out object? result)
    {
        result = null;

        var normalized = NumberText(text);

        if (long.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole))
        {
            result = whole;
            return true;
        }

        if (decimal.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            result = number;
            return true;
        }

        return false;
    }

    private static bool IsNumber(object? value) => value is IConvertible convertible
        && convertible.GetTypeCode() is TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32
            or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal;

    // decimal rather than double, so long codes such as postal codes keep every digit
    private static bool TryFormatNumber(object? value, string? decimals, out object? result)
    {
        result = null;

        decimal number;

        if (value is string text)
        {
            if (!decimal.TryParse(NumberText(text), NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                return false;
        }
        else if (IsNumber(value))
        {
            if (value is double d && (double.IsNaN(d) || double.IsInfinity(d)) || value is float f && (float.IsNaN(f) || float.IsInfinity(f)))
                return false;

            number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }
        else
        {
            return false;
        }

        if (int.TryParse(decimals?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var places) && places >= 0 && places <= 10)
            result = number.ToString("F" + places, CultureInfo.InvariantCulture);
        else
            result = decimal.Truncate(number) == number
                ? decimal.Truncate(number).ToString(CultureInfo.InvariantCulture)
                : number.ToString("0.##########", CultureInfo.InvariantCulture);

        return true;
    }

    private static bool TryParseBoolean(object? value, string words, out object? result)
    {
        result = null;

        if (value is bool flag)
        {
            result = flag;
            return true;
        }

        var parts = words.Split('|');
        var yes = Split(parts[0]);
        var no = parts.Length > 1 ? Split(parts[1]) : new List<string>();

        var text = Convert.ToString(value, CultureInfo.InvariantCulture)?.FarsiNumbersToLatinNumbers().ArabicToFarsi().Trim();

        if (string.IsNullOrEmpty(text))
            return false;

        if (yes.Contains(text!, StringComparer.OrdinalIgnoreCase))
        {
            result = true;
            return true;
        }

        if (no.Contains(text!, StringComparer.OrdinalIgnoreCase))
        {
            result = false;
            return true;
        }

        return false;
    }

    private static List<string> Split(string list) => list.Split(',')
        .Select(w => w.ArabicToFarsi().Trim())
        .Where(w => w.Length > 0)
        .ToList();

    #endregion
}
