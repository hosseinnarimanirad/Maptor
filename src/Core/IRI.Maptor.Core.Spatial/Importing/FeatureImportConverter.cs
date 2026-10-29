using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Core.Spatial.Importing;

/// <summary>
/// Fits source rows (a geodatabase, a shapefile) to a layer of the data model: every mapped value converted to its
/// field's type, domain values and codes resolved, required fields and lengths checked, the shape checked and brought
/// to WGS84. A row that does not fit comes back with its reasons instead of values. Pure and thread-safe after
/// construction, so the importer (and later a server) can run it over millions of rows.
/// </summary>
public class FeatureImportConverter
{
    private const int TargetSrid = SridHelper.GeodeticWGS84;

    private readonly IReadOnlyList<ImportFieldMapping> _mappings;

    private readonly FeatureImportOptions _options;

    // per mapping with a domain: its values by comparable key, and its codes
    private readonly Dictionary<ImportFieldMapping, DomainLookup> _domains = new Dictionary<ImportFieldMapping, DomainLookup>();

    public FeatureImportConverter(IReadOnlyList<ImportFieldMapping> mappings, FeatureImportOptions? options = null)
    {
        _mappings = mappings ?? throw new ArgumentNullException(nameof(mappings));
        _options = options ?? new FeatureImportOptions();

        foreach (var mapping in mappings)
        {
            if (mapping.Domain is not null)
                _domains[mapping] = new DomainLookup(mapping.Domain, _options.NormalizePersianLetters);
        }
    }

    /// <summary>
    /// Mapping mistakes to show before any row is read: a required field with neither a source column nor a default
    /// (no row would fit), and a domain on a field that is neither text nor a number.
    /// </summary>
    public IReadOnlyList<string> GetSetupProblems()
    {
        var problems = new List<string>();

        foreach (var mapping in _mappings)
        {
            var target = mapping.Target;

            var constant = mapping.Function is { Kind: ImportFunctionKind.Constant } function && !string.IsNullOrEmpty(function.Parameter);

            if (target.IsRequired && string.IsNullOrWhiteSpace(mapping.SourceName) && IsEmpty(target.DefaultValue) && !constant)
                problems.Add($"{target.Name} is required, but has no source column and no default: no row would fit.");

            if (mapping.Domain is not null && !IsText(target.DataType) && !IsNumber(target.DataType))
                problems.Add($"{target.Name} is {target.DataType}: a domain cannot be applied to it.");

            if (mapping.Function?.Info.Output is FieldDataType output && !CanStore(output, target.DataType))
                problems.Add($"{target.Name} is {target.DataType}: «{mapping.Function.Info.Title}» gives {output}, which it cannot hold.");
        }

        return problems;
    }

    /// <param name="attributes">The source row's values by column name (looked up exactly, then ignoring case).</param>
    /// <param name="shape">The source shape with its SRID; ignored for a layer without shapes.</param>
    public FeatureImportResult Convert(IReadOnlyDictionary<string, object?> attributes, Geometry<Point>? shape)
    {
        var result = new FeatureImportResult();

        foreach (var mapping in _mappings)
            ConvertField(mapping, attributes, result);

        if (_options.GeometryCategory != GeometryCategory.None)
            result.Shape = ConvertShape(shape, result);

        return result;
    }

    private void ConvertField(ImportFieldMapping mapping, IReadOnlyDictionary<string, object?> attributes, FeatureImportResult result)
    {
        var target = mapping.Target;

        object? value = string.IsNullOrWhiteSpace(mapping.SourceName) ? null : Find(attributes, mapping.SourceName!);

        value = CleanText(value);

        if (mapping.Function is { } function && (!IsEmpty(value) || !function.NeedsSource))
        {
            if (!function.TryApply(value, out var applied))
            {
                // the user's choice: a value the function cannot read leaves the field empty (or its default)
                if (target.IsRequired && IsEmpty(target.DefaultValue))
                {
                    result.Issues.Add(new ImportIssue(ImportIssueKind.FunctionFailed, target.Name, value));
                }
                else
                {
                    result.Values[target.Name] = IsEmpty(target.DefaultValue) ? null : target.DefaultValue;
                    result.FunctionFailures.Add(new ImportIssue(ImportIssueKind.FunctionFailed, target.Name, value));
                }

                return;
            }

            value = CleanText(applied);
        }

        if (IsEmpty(value))
        {
            if (!IsEmpty(target.DefaultValue))
                result.Values[target.Name] = target.DefaultValue;
            else if (target.IsRequired)
                result.Issues.Add(new ImportIssue(ImportIssueKind.Required, target.Name, null));
            else
                result.Values[target.Name] = null;

            return;
        }

        if (_domains.TryGetValue(mapping, out var domain))
        {
            if (domain.TryGetValue(value!, out var stored, out var wasCode))
            {
                value = stored;

                if (wasCode)
                    result.TranslatedCodes.Add(target.Name);
            }
            else if (_options.EmptyOutOfDomainValues && (!target.IsRequired || !IsEmpty(target.DefaultValue)))
            {
                result.Values[target.Name] = IsEmpty(target.DefaultValue) ? null : target.DefaultValue;
                result.EmptiedFields.Add(target.Name);
                return;
            }
            else
            {
                result.Issues.Add(new ImportIssue(ImportIssueKind.OutOfDomain, target.Name, value));
                return;
            }
        }

        if (!TryConvert(value!, target.DataType, out var converted))
        {
            result.Issues.Add(new ImportIssue(ImportIssueKind.WrongType, target.Name, value));
            return;
        }

        if (converted is string text && target.Length > 0 && text.Length > target.Length)
        {
            if (!mapping.TruncateTooLong)
            {
                result.Issues.Add(new ImportIssue(ImportIssueKind.TooLong, target.Name, text));
                return;
            }

            converted = text.Substring(0, target.Length);
            result.TruncatedFields.Add(target.Name);
        }

        result.Values[target.Name] = converted;
    }

    private Geometry<Point>? ConvertShape(Geometry<Point>? shape, FeatureImportResult result)
    {
        if (shape is null || shape.IsNullOrEmpty())
        {
            result.Issues.Add(new ImportIssue(ImportIssueKind.NoShape, null, null));
            return null;
        }

        if (shape.Type.GetCategory() != _options.GeometryCategory)
        {
            result.Issues.Add(new ImportIssue(ImportIssueKind.WrongShapeType, null, shape.Type));
            return null;
        }

        var srid = shape.Srid == 0 && _options.AssumedSourceSrid is int assumed ? assumed : shape.Srid;

        try
        {
            switch (srid)
            {
                case TargetSrid:
                    return shape.Srid == TargetSrid ? shape : shape.Transform(p => p, TargetSrid);

                case SridHelper.WebMercator:
                case SridHelper.EsriWebMercator:
                    return shape.Transform(MapProjects.WebMercatorToGeodeticWgs84, TargetSrid);

                default:
                    var srs = srid == 0 ? null : SridHelper.AsSrsBase(srid);

                    if (srs is null)
                    {
                        result.Issues.Add(new ImportIssue(ImportIssueKind.UnknownSrid, null, srid));
                        return null;
                    }

                    return shape.Transform(p => srs.ToWgs84Geodetic(p), TargetSrid);
            }
        }
        catch (Exception ex)
        {
            result.Issues.Add(new ImportIssue(ImportIssueKind.ShapeNotConverted, null, ex.Message));
            return null;
        }
    }

    #region Values

    private static object? Find(IReadOnlyDictionary<string, object?> attributes, string name)
    {
        if (attributes.TryGetValue(name, out var value))
            return value;

        foreach (var pair in attributes)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
                return pair.Value;
        }

        return null;
    }

    // text is trimmed and, by default, written with Persian «ی ک»
    private object? CleanText(object? value)
    {
        if (value is not string text)
            return value;

        text = text.Trim();

        return _options.NormalizePersianLetters ? text.ArabicToFarsi() : text;
    }

    private static bool IsEmpty(object? value) => value is null || value is DBNull || (value is string text && string.IsNullOrWhiteSpace(text));

    private static bool IsText(FieldDataType type) => type == FieldDataType.Text || type == FieldDataType.Unknown;

    private static bool IsNumber(FieldDataType type) => type == FieldDataType.Integer || type == FieldDataType.Long || type == FieldDataType.Double || type == FieldDataType.Decimal;

    // what a function's result can be converted into (text takes anything)
    private static bool CanStore(FieldDataType output, FieldDataType target) => IsText(target) || output switch
    {
        FieldDataType.Text => true,
        FieldDataType.DateTime => target == FieldDataType.DateTime,
        FieldDataType.Boolean => target == FieldDataType.Boolean || IsNumber(target),
        _ when IsNumber(output) => IsNumber(target) || target == FieldDataType.Boolean,
        _ => output == target,
    };

    /// <summary>The value as the field's type: a whole number stays whole only when it has no fraction, text is read with the invariant culture (Persian digits allowed).</summary>
    internal static bool TryConvert(object value, FieldDataType type, out object? result)
    {
        result = null;

        try
        {
            switch (type)
            {
                case FieldDataType.Text:
                case FieldDataType.Unknown:
                    result = value is DateTime date
                        ? date.ToString(date.TimeOfDay == TimeSpan.Zero ? "yyyy-MM-dd" : "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                        : System.Convert.ToString(value, CultureInfo.InvariantCulture);
                    return true;

                case FieldDataType.Integer:
                    if (!TryGetWhole(value, out var whole) || whole < int.MinValue || whole > int.MaxValue)
                        return false;
                    result = (int)whole;
                    return true;

                case FieldDataType.Long:
                    if (!TryGetWhole(value, out var big))
                        return false;
                    result = big;
                    return true;

                case FieldDataType.Double:
                    if (!TryGetDouble(value, out var number))
                        return false;
                    result = number;
                    return true;

                case FieldDataType.Decimal:
                    if (value is string decimalText)
                    {
                        if (!decimal.TryParse(Digits(decimalText), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                            return false;
                        result = parsed;
                        return true;
                    }
                    if (value is bool)
                        return false;
                    result = System.Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                    return true;

                case FieldDataType.Boolean:
                    switch (value)
                    {
                        case bool flag:
                            result = flag;
                            return true;
                        case string flagText:
                            var t = Digits(flagText).Trim().ToLowerInvariant();
                            if (t == "true" || t == "1") { result = true; return true; }
                            if (t == "false" || t == "0") { result = false; return true; }
                            return false;
                        default:
                            if (!TryGetWhole(value, out var bit) || (bit != 0 && bit != 1))
                                return false;
                            result = bit == 1;
                            return true;
                    }

                case FieldDataType.DateTime:
                    switch (value)
                    {
                        case DateTime dateTime:
                            result = dateTime;
                            return true;
                        case DateTimeOffset offset:
                            result = offset.DateTime;
                            return true;
                        // a year before 1700 is a Jalali date read as a Gregorian one («1403/10/05»): refused, so it is
                        // not written 600 years off; the mapping's Jalali → date function reads it
                        case string dateText when DateTime.TryParse(Digits(dateText), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate):
                            if (parsedDate.Year < 1700)
                                return false;
                            result = parsedDate;
                            return true;
                        default:
                            return false;
                    }

                case FieldDataType.Guid:
                    switch (value)
                    {
                        case Guid guid:
                            result = guid;
                            return true;
                        case string guidText when Guid.TryParse(guidText, out var parsedGuid):
                            result = parsedGuid;
                            return true;
                        default:
                            return false;
                    }

                case FieldDataType.Binary:
                    result = value as byte[];
                    return result is not null;

                default:
                    result = value;
                    return true;
            }
        }
        catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
        {
            return false;
        }
    }

    internal static bool TryGetWhole(object value, out long whole)
    {
        whole = 0;

        switch (value)
        {
            case bool:
                return false;
            case string text:
                return long.TryParse(Digits(text).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out whole)
                    || (TryGetDouble(text, out var fromText) && TryWhole(fromText, out whole));
            case float or double:
                return TryWhole(System.Convert.ToDouble(value, CultureInfo.InvariantCulture), out whole);
            case decimal d:
                if (decimal.Truncate(d) != d || d < long.MinValue || d > long.MaxValue)
                    return false;
                whole = (long)d;
                return true;
            case ulong u:
                if (u > long.MaxValue)
                    return false;
                whole = (long)u;
                return true;
            case IConvertible convertible when IsWholeType(convertible.GetTypeCode()):
                whole = convertible.ToInt64(CultureInfo.InvariantCulture);
                return true;
            default:
                return false;
        }
    }

    private static bool TryWhole(double number, out long whole)
    {
        whole = 0;

        if (double.IsNaN(number) || double.IsInfinity(number) || Math.Floor(number) != number || number < long.MinValue || number > long.MaxValue)
            return false;

        whole = (long)number;
        return true;
    }

    private static bool TryGetDouble(object value, out double number)
    {
        number = 0;

        switch (value)
        {
            case bool:
                return false;
            case string text:
                return double.TryParse(Digits(text).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out number) && !double.IsNaN(number);
            case IConvertible convertible when IsWholeType(convertible.GetTypeCode()) || convertible.GetTypeCode() is TypeCode.Single or TypeCode.Double or TypeCode.Decimal:
                number = convertible.ToDouble(CultureInfo.InvariantCulture);
                return !double.IsNaN(number);
            default:
                return false;
        }
    }

    private static bool IsWholeType(TypeCode code) => code is TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16
        or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64;

    // Persian and Arabic-Indic digits (and the Arabic decimal separator) read as Latin ones
    private static string Digits(string text) => text.FarsiNumbersToLatinNumbers();

    #endregion

    /// <summary>A domain's values by a key that compares numbers by value and text ordinally (as <see cref="FieldDomain.Contains"/>), and its codes.</summary>
    private sealed class DomainLookup
    {
        private readonly Dictionary<string, object?> _values = new Dictionary<string, object?>(StringComparer.Ordinal);

        private readonly Dictionary<long, object?> _codes = new Dictionary<long, object?>();

        public DomainLookup(FieldDomain domain, bool normalizePersianLetters)
        {
            foreach (var item in domain.Values ?? new List<FieldDomainValue>())
            {
                var stored = item.Value;

                var key = Key(stored is string text && normalizePersianLetters ? text.Trim().ArabicToFarsi() : stored);

                if (key is not null && !_values.ContainsKey(key))
                    _values[key] = stored;

                if (item.Code is int code && !_codes.ContainsKey(code))
                    _codes[code] = stored;
            }
        }

        /// <summary>The domain's stored value for <paramref name="value"/>: itself when it is a value, else the value of the code it is.</summary>
        public bool TryGetValue(object value, out object? stored, out bool wasCode)
        {
            wasCode = false;

            var key = Key(value);

            if (key is not null && _values.TryGetValue(key, out stored))
                return true;

            if (TryGetWhole(value, out var code) && _codes.TryGetValue(code, out stored))
            {
                wasCode = true;
                return true;
            }

            stored = null;
            return false;
        }

        private static string? Key(object? value) => value switch
        {
            null or DBNull => null,
            string text => "s:" + text,
            bool flag => "b:" + flag,
            IConvertible number when IsWholeType(number.GetTypeCode()) || number.GetTypeCode() is TypeCode.Single or TypeCode.Double or TypeCode.Decimal
                => "n:" + number.ToDouble(CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture),
            _ => "o:" + System.Convert.ToString(value, CultureInfo.InvariantCulture),
        };
    }
}
