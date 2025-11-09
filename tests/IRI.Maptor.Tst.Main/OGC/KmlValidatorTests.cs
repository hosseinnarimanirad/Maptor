using System;
using System.Collections.Generic;
using System.IO;
using IRI.Maptor.Ket.KmlFormat;
using Xunit;

namespace IRI.Maptor.Tst.Standards.OGC.KML;

public class KmlValidatorTests
{
    private static readonly string SolutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
    private static readonly string SampleKmlDirectory = Path.Combine(SolutionRoot, "assets", "Sample Kml files");

    public static IEnumerable<object[]> GetSampleKmlFiles()
    {
        if (!Directory.Exists(SampleKmlDirectory))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(SampleKmlDirectory, "*.kml"))
        {
            yield return new object[] { file };
        }
    }

    [Theory]
    [MemberData(nameof(GetSampleKmlFiles))]
    public void ValidateSampleKmlFiles_ShouldPassSchemaChecks(string filePath)
    {
        var isValid = KmlValidator.ValidateFile(filePath, out var errors, out var warnings);

        Assert.True(isValid, string.Join(Environment.NewLine, errors));
        Assert.Empty(errors);
        Assert.True(warnings.Count == 0, $"Expected no warnings but found:{Environment.NewLine}{string.Join(Environment.NewLine, warnings)}");
    }

    [Fact]
    public void Validate_InvalidKml_ShouldSurfaceSchemaErrors()
    {
        const string invalidKml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"">
  <Document>
    <Placemark>
      <name>Invalid Feature</name>
      <Point>
        <!-- Coordinates intentionally omitted -->
      </Point>
    </Placemark>
  </Document>
</kml>";

        var isValid = KmlValidator.Validate(invalidKml, out var errors, out var warnings);

        Assert.False(isValid);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, message => message.Contains("Point", StringComparison.OrdinalIgnoreCase));
    }
}

