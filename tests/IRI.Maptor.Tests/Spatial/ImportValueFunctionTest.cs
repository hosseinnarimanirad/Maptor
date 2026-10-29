using System;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Spatial.Importing;

namespace IRI.Maptor.Tests.Spatial;

/// <summary>
/// The mapping functions (<see cref="ImportValueFunction"/>) and how the converter runs them: a value a function cannot
/// read leaves the field empty (or its default) and is counted; a required field without a default does not fit.
/// Synthetic values only.
/// </summary>
public class ImportValueFunctionTest
{
    private static object? Apply(ImportFunctionKind kind, object? value, string? parameter = null)
    {
        Assert.True(new ImportValueFunction(kind, parameter).TryApply(value, out var result), $"{kind} refused «{value}»");
        return result;
    }

    private static bool Refuses(ImportFunctionKind kind, object? value, string? parameter = null) => !new ImportValueFunction(kind, parameter).TryApply(value, out _);

    [Theory]
    [InlineData("1403/10/05", 2024, 12, 25, 0, 0)]
    [InlineData("۱۴۰۳/۱۰/۰۵", 2024, 12, 25, 0, 0)]       // Persian digits
    [InlineData("١٤٠٣/١٠/٠٥", 2024, 12, 25, 0, 0)]       // Arabic-Indic digits
    [InlineData("1403-1-1", 2024, 3, 20, 0, 0)]
    [InlineData("1403.01.01", 2024, 3, 20, 0, 0)]
    [InlineData("14031005", 2024, 12, 25, 0, 0)]
    [InlineData("1403/10/05 14:30", 2024, 12, 25, 14, 30)]
    [InlineData(" 1403/10/05 ", 2024, 12, 25, 0, 0)]
    [InlineData("1399/12/30", 2021, 3, 20, 0, 0)]        // the last day of a leap year
    public void JalaliToDate_ReadsTheFormatsSeen(string text, int year, int month, int day, int hour, int minute)
    {
        var date = (DateTime)Apply(ImportFunctionKind.JalaliToDate, text)!;

        Assert.Equal(new DateTime(year, month, day, hour, minute, 0), date);
        Assert.Equal(DateTimeKind.Unspecified, date.Kind);
    }

    [Theory]
    [InlineData("1403/13/01")]          // no 13th month
    [InlineData("1402/12/30")]          // 1402 is not a leap year
    [InlineData("1403/07/31")]          // the second half has 30-day months
    [InlineData("2024/12/25")]          // a Gregorian date is not read as Jalali
    [InlineData("1403/10")]
    [InlineData("1403/10/05 25:00")]
    [InlineData("نامعلوم")]
    [InlineData("1403/10/")]            // the delivery's counter-suffixed dates
    public void JalaliToDate_RefusesWhatIsNotAJalaliDate(string text) => Assert.True(Refuses(ImportFunctionKind.JalaliToDate, text));

    [Fact]
    public void JalaliToDate_KeepsAValueThatIsAlreadyADate()
    {
        var date = new DateTime(2024, 12, 25);

        Assert.Equal(date, Apply(ImportFunctionKind.JalaliToDate, date));
    }

    [Theory]
    [InlineData(null, "1403/10/05")]
    [InlineData("yyyy-M-d", "1403-10-5")]
    [InlineData("yyyy/MM/dd HH:mm", "1403/10/05 14:30")]
    public void DateToJalali_WritesTheFormat(string? format, string expected)
        => Assert.Equal(expected, Apply(ImportFunctionKind.DateToJalali, new DateTime(2024, 12, 25, 14, 30, 0), format));

    [Fact]
    public void DateToJalali_RefusesText() => Assert.True(Refuses(ImportFunctionKind.DateToJalali, "2024-12-25"));

    [Fact]
    public void JalaliRoundTrip()
    {
        var date = (DateTime)Apply(ImportFunctionKind.JalaliToDate, "1401/06/31")!;

        Assert.Equal("1401/06/31", Apply(ImportFunctionKind.DateToJalali, date));
    }

    [Fact]
    public void IranTimeToUtc_SubtractsThreeAndAHalfHours()
    {
        // Iran has kept no daylight saving since 2022
        var utc = ImportValueFunction.IranTimeToUtc(new DateTime(2024, 12, 25, 0, 0, 0));

        Assert.Equal(new DateTime(2024, 12, 24, 20, 30, 0), utc);
        Assert.Equal(DateTimeKind.Utc, utc.Kind);
    }

    [Fact]
    public void TextToNumber_ReadsPersianDigitsAndSeparators()
    {
        Assert.Equal(1234L, Apply(ImportFunctionKind.TextToNumber, "۱٬۲۳۴"));
        Assert.Equal(1234567L, Apply(ImportFunctionKind.TextToNumber, "1,234,567"));
        Assert.Equal(12.5m, Apply(ImportFunctionKind.TextToNumber, "۱۲٫۵"));
        Assert.Equal(7, Apply(ImportFunctionKind.TextToNumber, 7));
        Assert.True(Refuses(ImportFunctionKind.TextToNumber, "abc"));
    }

    [Fact]
    public void NumberToText_DropsTheDecimalsOfWholeValues()
    {
        Assert.Equal("1839796765", Apply(ImportFunctionKind.NumberToText, "1839796765.0000"));
        Assert.Equal("3", Apply(ImportFunctionKind.NumberToText, 3.0));
        Assert.Equal("12.5", Apply(ImportFunctionKind.NumberToText, 12.5));
        Assert.Equal("12.50", Apply(ImportFunctionKind.NumberToText, 12.5, "2"));
        Assert.Equal("9999999999999999", Apply(ImportFunctionKind.NumberToText, 9999999999999999L));
        Assert.True(Refuses(ImportFunctionKind.NumberToText, "abc"));
        Assert.True(Refuses(ImportFunctionKind.NumberToText, double.NaN));
    }

    [Fact]
    public void TextToBoolean_ReadsTheWords()
    {
        Assert.Equal(true, Apply(ImportFunctionKind.TextToBoolean, "بله"));
        Assert.Equal(false, Apply(ImportFunctionKind.TextToBoolean, "خير"));       // Arabic ي
        Assert.Equal(true, Apply(ImportFunctionKind.TextToBoolean, "y"));
        Assert.Equal(false, Apply(ImportFunctionKind.TextToBoolean, 0));
        Assert.Equal(true, Apply(ImportFunctionKind.TextToBoolean, "سالم", "سالم|خراب"));
        Assert.True(Refuses(ImportFunctionKind.TextToBoolean, "شاید"));
    }

    [Fact]
    public void Trim_EmptyMarkers_Constant()
    {
        Assert.Equal("a b", Apply(ImportFunctionKind.Trim, "  a \t  b "));
        Assert.Null(Apply(ImportFunctionKind.EmptyMarkers, " - "));
        Assert.Equal("x", Apply(ImportFunctionKind.EmptyMarkers, "x"));
        Assert.Null(Apply(ImportFunctionKind.EmptyMarkers, "N/A", "N/A,NA"));
        Assert.Equal("ثابت", Apply(ImportFunctionKind.Constant, "anything", "ثابت"));
    }

    // ---------- in the converter

    private static readonly ImportField Built = new() { Name = "Built", DataType = FieldDataType.DateTime };

    private static FeatureImportResult Convert(ImportFieldMapping mapping, object? value)
        => new FeatureImportConverter(new[] { mapping }).Convert(new Dictionary<string, object?> { ["src"] = value }, null);

    private static ImportFieldMapping Map(ImportField target, ImportFunctionKind kind, string? parameter = null, string? source = "src")
        => new(target, source) { Function = new ImportValueFunction(kind, parameter) };

    [Fact]
    public void Converter_JalaliToDate_WritesTheDate()
    {
        var result = Convert(Map(Built, ImportFunctionKind.JalaliToDate), "1403/10/05");

        Assert.True(result.Fits);
        Assert.Equal(new DateTime(2024, 12, 25), result.Values["Built"]);
    }

    [Fact]
    public void Converter_WithoutAFunction_RefusesAJalaliDateInsteadOfMisreadingIt()
    {
        var result = Convert(new ImportFieldMapping(Built, "src"), "1403/10/05");

        Assert.False(result.Fits);
        Assert.Equal(ImportIssueKind.WrongType, Assert.Single(result.Issues).Kind);
    }

    [Fact]
    public void Converter_AValueTheFunctionCannotRead_LeavesTheFieldEmptyAndIsCounted()
    {
        var result = Convert(Map(Built, ImportFunctionKind.JalaliToDate), "1403/10/");

        Assert.True(result.Fits);
        Assert.Null(result.Values["Built"]);
        var failure = Assert.Single(result.FunctionFailures);
        Assert.Equal(("Built", "1403/10/"), (failure.Field, (string?)failure.Value));
    }

    [Fact]
    public void Converter_AFailedValue_TakesTheDefault_OrSkipsARequiredRowWithoutOne()
    {
        var withDefault = new ImportField { Name = "Date_Reported", DataType = FieldDataType.Text, Length = 10, IsRequired = true, DefaultValue = "نامشخص" };
        var withoutDefault = new ImportField { Name = "Date_Reported", DataType = FieldDataType.Text, Length = 10, IsRequired = true };

        var defaulted = Convert(Map(withDefault, ImportFunctionKind.NumberToText), "abc");
        Assert.True(defaulted.Fits);
        Assert.Equal("نامشخص", defaulted.Values["Date_Reported"]);

        var skipped = Convert(Map(withoutDefault, ImportFunctionKind.NumberToText), "abc");
        Assert.False(skipped.Fits);
        Assert.Equal(ImportIssueKind.FunctionFailed, Assert.Single(skipped.Issues).Kind);
    }

    [Fact]
    public void Converter_AnEmptyValue_IsNotGivenToTheFunction()
    {
        var result = Convert(Map(Built, ImportFunctionKind.JalaliToDate), "  ");

        Assert.True(result.Fits);
        Assert.Empty(result.FunctionFailures);
    }

    [Fact]
    public void Converter_AConstant_NeedsNoSourceAndSatisfiesARequiredField()
    {
        var region = new ImportField { Name = "Region", DataType = FieldDataType.Text, Length = 20, IsRequired = true };
        var converter = new FeatureImportConverter(new[] { Map(region, ImportFunctionKind.Constant, "تهران", source: null) });

        Assert.Empty(converter.GetSetupProblems());
        Assert.Equal("تهران", converter.Convert(new Dictionary<string, object?>(), null).Values["Region"]);
    }

    [Fact]
    public void Converter_SetupProblem_WhenTheFunctionGivesWhatTheFieldCannotHold()
    {
        var count = new ImportField { Name = "Riser_Counts", DataType = FieldDataType.Integer };

        Assert.Single(new FeatureImportConverter(new[] { Map(count, ImportFunctionKind.JalaliToDate) }).GetSetupProblems());
        Assert.Empty(new FeatureImportConverter(new[] { Map(count, ImportFunctionKind.TextToNumber) }).GetSetupProblems());
    }

    [Fact]
    public void Converter_TextToNumber_ThenTheFieldsType()
    {
        var count = new ImportField { Name = "Riser_Counts", DataType = FieldDataType.Integer };

        Assert.Equal(1250, Convert(Map(count, ImportFunctionKind.TextToNumber), "۱٬۲۵۰").Values["Riser_Counts"]);
    }

    [Fact]
    public void Report_CountsFunctionFailuresPerFieldWithExamples()
    {
        var converter = new FeatureImportConverter(new[] { Map(Built, ImportFunctionKind.JalaliToDate) });
        var report = new FeatureImportReport(examplesPerProblem: 2);

        var values = new[] { "1403/10/05", "1403/10/", "x", "1403/13/01", "1402/01/01" };

        for (int i = 0; i < values.Length; i++)
            report.Add(i + 1, converter.Convert(new Dictionary<string, object?> { ["src"] = values[i] }, null));

        Assert.Equal(5, report.FitCount);
        var failures = Assert.Single(report.FunctionFailures);
        Assert.Equal(("Built", 3), (failures.Field, failures.RowCount));
        Assert.Equal(new[] { 2, 3 }, failures.Examples.ConvertAll(e => e.RowNumber));
    }
}
