using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Importing;
using IRI.Maptor.Core.Spatial.Primitives;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Tests.Spatial;

/// <summary>
/// Fitting source rows to a data-model layer (<see cref="FeatureImportConverter"/>): a Gas-like coded domain (codes and
/// titles), a Saba-like list of allowed values (no codes), types, required fields, lengths and shapes. Synthetic rows only.
/// </summary>
public class FeatureImportConverterTest
{
    // Gas: CI_Material_Riser stores the title; the gdb's codes come in as integers
    private static readonly FieldDomain RiserMaterial = new()
    {
        Name = "CI_Material_Riser",
        Values =
        {
            new FieldDomainValue { Value = "نا مشخص", Code = 0 },
            new FieldDomainValue { Value = "فولادی", Code = 1 },
            new FieldDomainValue { Value = "پلی فولادی", Code = 5 },
            new FieldDomainValue { Value = "150", Code = 150 },
        },
    };

    // Saba: a plain list of allowed values
    private static readonly FieldDomain JoinBoxKind = FieldDomain.FromValues(new object?[] { "دو راهه", "سه راهه", "چهار راهه" })!;

    private static readonly ImportField Material = new() { Name = "CI_Material_Riser", DataType = FieldDataType.Text, Length = 100, IsRequired = true, DefaultValue = "نا مشخص", Domain = RiserMaterial };

    private static readonly ImportField Kind = new() { Name = "jb_kind", DataType = FieldDataType.Text, Length = 10, Domain = JoinBoxKind };

    private static readonly ImportField Code = new() { Name = "Riser_Code", DataType = FieldDataType.Text, Length = 8, IsRequired = true };

    private static readonly ImportField Count = new() { Name = "Riser_Counts", DataType = FieldDataType.Integer };

    private static readonly ImportField Length = new() { Name = "Service_Pipe_Length", DataType = FieldDataType.Double };

    private static readonly ImportField Built = new() { Name = "Built", DataType = FieldDataType.DateTime };

    private static readonly ImportField Active = new() { Name = "Active", DataType = FieldDataType.Boolean };

    private static FeatureImportResult Convert(ImportFieldMapping mapping, object? value, FeatureImportOptions? options = null)
        => new FeatureImportConverter(new[] { mapping }, options).Convert(new Dictionary<string, object?> { ["src"] = value }, null);

    [Theory]
    [InlineData(1, "فولادی")]
    [InlineData(5L, "پلی فولادی")]
    [InlineData((short)0, "نا مشخص")]
    [InlineData("1", "فولادی")]
    [InlineData("۵", "پلی فولادی")]            // Persian digit
    [InlineData(1.0, "فولادی")]
    [InlineData("فولادی", "فولادی")]          // already the value
    [InlineData("فولادي", "فولادی")]          // Arabic ي
    [InlineData("  پلی فولادی ", "پلی فولادی")]
    [InlineData(150, "150")]                  // a title that is a number: the integer is code 150, whose title is "150"
    public void CodedDomain_ValuesAndCodesBecomeTheValue(object source, string expected)
    {
        var result = Convert(new ImportFieldMapping(Material, "src", RiserMaterial), source);

        Assert.True(result.Fits, string.Join("; ", result.Issues));
        Assert.Equal(expected, result.Values["CI_Material_Riser"]);
    }

    [Fact]
    public void CodedDomain_CodeTranslationsAreReported_ValuesAreNot()
    {
        Assert.Equal(new[] { "CI_Material_Riser" }, Convert(new ImportFieldMapping(Material, "src", RiserMaterial), 1).TranslatedCodes);
        Assert.Empty(Convert(new ImportFieldMapping(Material, "src", RiserMaterial), "فولادی").TranslatedCodes);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(1.5)]
    [InlineData("آلومینیوم")]
    [InlineData(true)]
    public void CodedDomain_OtherValuesDoNotFit(object source)
    {
        var result = Convert(new ImportFieldMapping(Material, "src", RiserMaterial), source);

        var issue = Assert.Single(result.Issues);
        Assert.Equal(ImportIssueKind.OutOfDomain, issue.Kind);
        Assert.Equal("CI_Material_Riser", issue.Field);
    }

    [Fact]
    public void OutOfDomain_LeftEmpty_WhenChosen()
    {
        var options = new FeatureImportOptions { EmptyOutOfDomainValues = true };

        // optional: empty, the row still fits
        var kind = Convert(new ImportFieldMapping(Kind, "src", JoinBoxKind), "پنج راهه", options);
        Assert.True(kind.Fits);
        Assert.Null(kind.Values["jb_kind"]);
        Assert.Equal(new[] { "jb_kind" }, kind.EmptiedFields);

        // required with a default: the default
        var material = Convert(new ImportFieldMapping(Material, "src", RiserMaterial), 99, options);
        Assert.True(material.Fits);
        Assert.Equal("نا مشخص", material.Values["CI_Material_Riser"]);

        // required without a default: nothing valid to write
        var strict = new ImportField { Name = "Strict", DataType = FieldDataType.Text, IsRequired = true, Domain = JoinBoxKind };
        Assert.Equal(ImportIssueKind.OutOfDomain, Assert.Single(Convert(new ImportFieldMapping(strict, "src", JoinBoxKind), "x", options).Issues).Kind);

        var report = new FeatureImportReport();
        report.Add(1, kind);
        Assert.Equal(1, report.EmptiedValues["jb_kind"]);
    }

    [Fact]
    public void NoDomainChosen_KeepsTheSourceValue()
    {
        // the user's choice: without a domain the code is written as it is (as text in a text field)
        var result = Convert(new ImportFieldMapping(Material, "src"), 1);

        Assert.True(result.Fits);
        Assert.Equal("1", result.Values["CI_Material_Riser"]);
        Assert.Empty(result.TranslatedCodes);
    }

    [Theory]
    [InlineData("دو راهه", "دو راهه")]
    [InlineData("سه راهه ", "سه راهه")]
    public void AllowedValues_WithoutCodes(string source, string expected)
    {
        var result = Convert(new ImportFieldMapping(Kind, "src", JoinBoxKind), source);

        Assert.True(result.Fits);
        Assert.Equal(expected, result.Values["jb_kind"]);

        // no codes in a plain list: a number is just out of the domain
        Assert.Equal(ImportIssueKind.OutOfDomain, Assert.Single(Convert(new ImportFieldMapping(Kind, "src", JoinBoxKind), 1).Issues).Kind);
    }

    [Fact]
    public void PersianLetters_NormalisedByDefault_KeptWhenOff()
    {
        var plain = new ImportField { Name = "Note", DataType = FieldDataType.Text };

        Assert.Equal("کیسه", Convert(new ImportFieldMapping(plain, "src"), "كيسه").Values["Note"]);
        Assert.Equal("كيسه", Convert(new ImportFieldMapping(plain, "src"), "كيسه", new FeatureImportOptions { NormalizePersianLetters = false }).Values["Note"]);
    }

    [Fact]
    public void Required_TakesTheDefault_OrDoesNotFit()
    {
        Assert.Equal("نا مشخص", Convert(new ImportFieldMapping(Material, "src", RiserMaterial), null).Values["CI_Material_Riser"]);
        Assert.Equal("نا مشخص", Convert(new ImportFieldMapping(Material, "src", RiserMaterial), "   ").Values["CI_Material_Riser"]);

        var missing = Convert(new ImportFieldMapping(Code, "src"), null);
        Assert.Equal(ImportIssueKind.Required, Assert.Single(missing.Issues).Kind);

        // an unmapped required field without a default: reported before any row is read
        var converter = new FeatureImportConverter(new[] { new ImportFieldMapping(Code, null), new ImportFieldMapping(Material, null) });
        Assert.Contains(converter.GetSetupProblems(), p => p.StartsWith("Riser_Code"));
        Assert.DoesNotContain(converter.GetSetupProblems(), p => p.StartsWith("CI_Material_Riser"));

        // an optional empty value is written as null
        Assert.Null(Convert(new ImportFieldMapping(Count, "src"), null).Values["Riser_Counts"]);
    }

    [Fact]
    public void Text_LongerThanTheField_DoesNotFit()
    {
        Assert.True(Convert(new ImportFieldMapping(Code, "src"), "R-123456").Fits);
        Assert.Equal(ImportIssueKind.TooLong, Assert.Single(Convert(new ImportFieldMapping(Code, "src"), "R-1234567").Issues).Kind);
    }

    [Fact]
    public void Text_Truncated_WhenTheColumnSaysSo()
    {
        // the user's choice for the delivery's In_Service_Date / Postal_Code / ID_Number (shapes of the delivery, made-up values)
        var postal = new ImportField { Name = "Postal_Code", DataType = FieldDataType.Text, Length = 10 };
        var date = new ImportField { Name = "In_Service_Date", DataType = FieldDataType.Text, Length = 10 };

        var converter = new FeatureImportConverter(new[]
        {
            new ImportFieldMapping(postal, "postal") { TruncateTooLong = true },
            new ImportFieldMapping(date, "date") { TruncateTooLong = true },
        });

        var result = converter.Convert(new Dictionary<string, object?> { ["postal"] = "1234567890.0000", ["date"] = "1403/10/12345" }, null);

        Assert.True(result.Fits);
        Assert.Equal("1234567890", result.Values["Postal_Code"]);
        Assert.Equal("1403/10/12", result.Values["In_Service_Date"]);
        Assert.Equal(new[] { "Postal_Code", "In_Service_Date" }, result.TruncatedFields);

        // a value that fits is not counted
        Assert.Empty(converter.Convert(new Dictionary<string, object?> { ["postal"] = "1234567890", ["date"] = "1403/10/01" }, null).TruncatedFields);

        var report = new FeatureImportReport();
        report.Add(1, result);
        Assert.Equal(1, report.TruncatedValues["Postal_Code"]);
    }

    [Theory]
    [InlineData(7, 7)]
    [InlineData(7L, 7)]
    [InlineData(7.0, 7)]
    [InlineData("7", 7)]
    [InlineData("۱۲", 12)]
    [InlineData(" 3.0 ", 3)]
    public void Integer_FromWholeValues(object source, int expected)
    {
        Assert.Equal(expected, Convert(new ImportFieldMapping(Count, "src"), source).Values["Riser_Counts"]);
    }

    [Theory]
    [InlineData(3.5)]
    [InlineData("abc")]
    [InlineData(true)]
    [InlineData(5_000_000_000L)]
    public void Integer_OtherValuesDoNotFit(object source)
    {
        Assert.Equal(ImportIssueKind.WrongType, Assert.Single(Convert(new ImportFieldMapping(Count, "src"), source).Issues).Kind);
    }

    [Fact]
    public void OtherTypes()
    {
        Assert.Equal(12.5, Convert(new ImportFieldMapping(Length, "src"), "12.5").Values["Service_Pipe_Length"]);
        Assert.Equal(12.5, Convert(new ImportFieldMapping(Length, "src"), 12.5f).Values["Service_Pipe_Length"]);
        Assert.Equal(new DateTime(2024, 3, 1), Convert(new ImportFieldMapping(Built, "src"), "2024-03-01").Values["Built"]);
        Assert.Equal(ImportIssueKind.WrongType, Assert.Single(Convert(new ImportFieldMapping(Built, "src"), "not a date").Issues).Kind);
        Assert.Equal(true, Convert(new ImportFieldMapping(Active, "src"), 1).Values["Active"]);
        Assert.Equal(false, Convert(new ImportFieldMapping(Active, "src"), "false").Values["Active"]);
        Assert.Equal("2024-03-01", Convert(new ImportFieldMapping(new ImportField { Name = "T", DataType = FieldDataType.Text }, "src"), new DateTime(2024, 3, 1)).Values["T"]);
    }

    [Fact]
    public void SourceColumn_FoundIgnoringCase()
    {
        var converter = new FeatureImportConverter(new[] { new ImportFieldMapping(Count, "RISER_COUNTS") });

        Assert.Equal(4, converter.Convert(new Dictionary<string, object?> { ["Riser_Counts"] = 4 }, null).Values["Riser_Counts"]);
    }

    [Fact]
    public void Shapes_KindAndCoordinateSystem()
    {
        var points = new FeatureImportOptions { GeometryCategory = GeometryCategory.Point };
        var converter = new FeatureImportConverter(new ImportFieldMapping[0], points);
        var none = new Dictionary<string, object?>();

        // WGS84 kept, multi-part allowed
        var wgs84 = converter.Convert(none, Geometry<Point>.Create(51.4, 35.7, 4326));
        Assert.True(wgs84.Fits);
        Assert.Equal(4326, wgs84.Shape!.Srid);
        Assert.True(converter.Convert(none, Geometry<Point>.FromWkt("MULTIPOINT ((51.4 35.7), (51.5 35.8))", 4326)).Fits);

        // Web Mercator and UTM 39N brought to WGS84
        var mercator = converter.Convert(none, Geometry<Point>.Create(5_721_000, 4_261_000, 3857));
        Assert.True(mercator.Fits);
        Assert.Equal(4326, mercator.Shape!.Srid);
        Assert.InRange(mercator.Shape.Points![0].X, 51.3, 51.5);
        Assert.InRange(mercator.Shape.Points![0].Y, 35.6, 35.8);

        var utm = converter.Convert(none, Geometry<Point>.Create(535_000, 3_951_000, 32639));
        Assert.True(utm.Fits, string.Join("; ", utm.Issues));
        Assert.InRange(utm.Shape!.Points![0].X, 51.3, 51.5);

        // problems
        Assert.Equal(ImportIssueKind.NoShape, Assert.Single(converter.Convert(none, null).Issues).Kind);
        Assert.Equal(ImportIssueKind.WrongShapeType, Assert.Single(converter.Convert(none, Geometry<Point>.FromWkt("LINESTRING (51.4 35.7, 51.5 35.8)", 4326)).Issues).Kind);
        Assert.Equal(ImportIssueKind.UnknownSrid, Assert.Single(converter.Convert(none, Geometry<Point>.Create(51.4, 35.7, 0)).Issues).Kind);

        // a source without a coordinate system, with the SRID the user gives
        var assumed = new FeatureImportConverter(new ImportFieldMapping[0], new FeatureImportOptions { GeometryCategory = GeometryCategory.Point, AssumedSourceSrid = 4326 });
        var fromZero = assumed.Convert(none, Geometry<Point>.Create(51.4, 35.7, 0));
        Assert.True(fromZero.Fits);
        Assert.Equal(4326, fromZero.Shape!.Srid);

        // a table without shapes ignores them
        Assert.True(new FeatureImportConverter(new ImportFieldMapping[0]).Convert(none, null).Fits);
    }

    [Fact]
    public void Report_CountsReasonsAndExamples()
    {
        var converter = new FeatureImportConverter(new[]
        {
            new ImportFieldMapping(Material, "material", RiserMaterial),
            new ImportFieldMapping(Code, "code"),
        }, new FeatureImportOptions { GeometryCategory = GeometryCategory.Point });

        var report = new FeatureImportReport(examplesPerProblem: 2);
        var shape = Geometry<Point>.Create(51.4, 35.7, 4326);

        var rows = new (object? Material, object? Code, Geometry<Point>? Shape)[]
        {
            (1, "A", shape),          // fits, code translated
            ("فولادی", "B", shape),   // fits
            (99, "C", shape),         // out of domain
            (98, "D", shape),         // out of domain
            (97, "E", shape),         // out of domain (beyond the examples)
            (0, null, null),          // required + no shape: two problems, one skipped row
        };

        for (int i = 0; i < rows.Length; i++)
            report.Add(i + 1, converter.Convert(new Dictionary<string, object?> { ["material"] = rows[i].Material, ["code"] = rows[i].Code }, rows[i].Shape));

        Assert.Equal(6, report.RowCount);
        Assert.Equal(2, report.FitCount);
        Assert.Equal(4, report.SkippedCount);
        Assert.Equal(1, report.TranslatedCodes["CI_Material_Riser"]);

        var outOfDomain = report.Problems.First();
        Assert.Equal(ImportIssueKind.OutOfDomain, outOfDomain.Kind);
        Assert.Equal(3, outOfDomain.RowCount);
        Assert.Equal(new[] { 3, 4 }, outOfDomain.Examples.Select(e => e.RowNumber));
        Assert.Contains(report.Problems, p => p.Kind == ImportIssueKind.Required && p.Field == "Riser_Code" && p.RowCount == 1);
        Assert.Contains(report.Problems, p => p.Kind == ImportIssueKind.NoShape && p.Field is null && p.RowCount == 1);
    }
}
