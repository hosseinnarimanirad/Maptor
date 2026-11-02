using System.Linq;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Presentation.Wpf.ViewModels.Dialogs;

using Xunit;

namespace IRI.Maptor.Tests.Mapping;

public class DelimitedTextValidatorTest
{
    private static DelimitedTextValidation Validate(string text, char delimiter = '\t', bool header = false,
        GeometryType type = GeometryType.Polygon, bool preview = false)
        => DelimitedTextValidator.Validate(text, delimiter, header, isLongitudeFirst: true, type, preview);

    [Fact]
    public void ValidTabText_IsValid()
    {
        Assert.True(Validate("10.5\t20.5\r\n11\t21\r\n12\t22\r\n").IsValid);
    }

    [Fact]
    public void TabTextUnderCsv_TooFewColumnsOnFirstLine()
    {
        var r = Validate("10.5\t20.5\n11\t21\n12\t22", delimiter: ',');
        Assert.Equal(DelimitedTextError.TooFewColumns, r.Error);
        Assert.Equal(1, r.Line);
    }

    [Fact]
    public void SpaceSeparated_TooFewColumns()
    {
        Assert.Equal(DelimitedTextError.TooFewColumns, Validate("10.5 20.5\n11 21\n12 22").Error);
    }

    [Fact]
    public void ColumnCountMismatch_ReportsPhysicalLine()
    {
        // Blank line 2 still counts, so the bad row is line 4.
        var r = Validate("1,2,a\n\n3,4,b\n5,6\n7,8,c", delimiter: ',');
        Assert.Equal(DelimitedTextError.ColumnCountMismatch, r.Error);
        Assert.Equal(4, r.Line);
        Assert.Equal(2, r.Columns);
        Assert.Equal(3, r.ExpectedColumns);
    }

    [Fact]
    public void NonNumericCoordinate_NotNumber()
    {
        var r = Validate("1,2\n3,x\n5,6", delimiter: ',');
        Assert.Equal(DelimitedTextError.NotNumber, r.Error);
        Assert.Equal(2, r.Line);
    }

    [Fact]
    public void HeaderLine_IsSkipped()
    {
        Assert.True(Validate("X\tY\n1\t2\n3\t4\n5\t6", header: true).IsValid);
        Assert.Equal(DelimitedTextError.NotNumber, Validate("X\tY\n1\t2\n3\t4\n5\t6", header: false).Error);
    }

    [Fact]
    public void PointHeader_FindsXyByName()
    {
        // name column first; lat/lon looked up by header name as the import does
        Assert.True(Validate("name,lat,lon\nA,35.1,51.2", delimiter: ',', header: true, type: GeometryType.Point).IsValid);
    }

    [Theory]
    [InlineData(GeometryType.Polygon, 3)]
    [InlineData(GeometryType.LineString, 2)]
    [InlineData(GeometryType.Point, 1)]
    public void TooFewPoints(GeometryType type, int required)
    {
        var text = string.Join("\n", Enumerable.Range(0, required - 1).Select(i => $"{i},{i}"));
        var r = Validate(text, delimiter: ',', type: type);
        Assert.Equal(DelimitedTextError.TooFewPoints, r.Error);
        Assert.Equal(required, r.RequiredPoints);

        text = string.Join("\n", Enumerable.Range(0, required).Select(i => $"{i},{i}"));
        Assert.True(Validate(text, delimiter: ',', type: type).IsValid);
    }

    [Fact]
    public void FilePreviewMarker_IsIgnored()
    {
        var text = "1,2\n3,4\n5,6\n... (120 more lines)";
        Assert.True(Validate(text, delimiter: ',', preview: true).IsValid);
        Assert.False(Validate(text, delimiter: ',', preview: false).IsValid);
    }

    private static string Convert(string text, char delimiter = '\t', bool header = false)
        => DelimitedTextValidator.ConvertSpaceSeparatedXy(text, delimiter, header);

    [Theory]
    [InlineData('\t')]
    [InlineData(',')]
    public void SpaceSeparatedXy_ConvertedToSelectedDelimiter(char delimiter)
    {
        var converted = Convert("10.5 20.5\n11   21\n\n 12 22 \n", delimiter);
        var d = delimiter.ToString();
        Assert.Equal(string.Join(Environment.NewLine, $"10.5{d}20.5", $"11{d}21", "", $"12{d}22", ""), converted);
        Assert.True(Validate(converted, delimiter).IsValid);
    }

    [Fact]
    public void SpaceSeparatedXy_TwoTokenHeaderConverted()
    {
        var converted = Convert("X Y\n1 2\n3 4\n5 6", header: true);
        Assert.StartsWith("X\tY", converted);
        Assert.True(Validate(converted, header: true).IsValid);
    }

    [Theory]
    [InlineData("tower 1 10 20\n2 3")]   // attributes with spaces: not supported
    [InlineData("1 2 3\n4 5 6")]         // three columns
    [InlineData("1 2\na b")]             // non-numeric data line
    [InlineData("X Y\n1 2")]             // header text but header option off
    [InlineData("1\t2\n3 4")]            // already has a tab
    [InlineData("1,5 2,5")]              // has a comma
    public void OtherText_NotConverted(string text)
    {
        Assert.Same(text, Convert(text));
    }
}
