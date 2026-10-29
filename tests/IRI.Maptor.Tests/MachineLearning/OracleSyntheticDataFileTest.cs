using IRI.Maptor.Core.MachineLearning;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IRI.Maptor.Tests.MachineLearning;

public class OracleSyntheticDataFileTest
{
    private static OracleSyntheticDataFile Sample() => new()
    {
        Renderer = "test",
        CanvasSize = 64,
        GridStep = 0.5,
        Margin = 4,
        BaseLengths = new List<double> { 4 },
        RotationsDegrees = new List<double> { 0, 45 },
        Offsets = new List<double[]> { new[] { 0.0, 0.0 } },
        Triads = new List<OracleLabelledTriad>
        {
            // collinear middle point: nothing changes
            new() { BaseLength = 4, Dx1 = 2, Dx2 = -2, Dy = 0, X1 = 30, Y1 = 32, X2 = 32, Y2 = 32, X3 = 34, Y3 = 32, OriginalPixels = 9, SimplifiedPixels = 9, DifferentPixels = 0 },
            // half a pixel off: one pixel changes
            new() { BaseLength = 4, Dx1 = 2, Dx2 = -2, Dy = 0.5, X1 = 30, Y1 = 32, X2 = 32, Y2 = 31.5, X3 = 34, Y3 = 32, OriginalPixels = 10, SimplifiedPixels = 9, DifferentPixels = 1 },
            // a spike: many pixels change
            new() { BaseLength = 4, Dx1 = 2, Dx2 = -2, Dy = 3, RotationDegrees = 45, X1 = 30, Y1 = 30, X2 = 34, Y2 = 30, X3 = 33, Y3 = 33, OriginalPixels = 14, SimplifiedPixels = 8, DifferentPixels = 7 },
        },
    };

    [Fact]
    public void IsRetained_DependsOnTheTolerance()
    {
        var triads = Sample().Triads;

        Assert.Equal(new[] { false, true, true }, triads.Select(t => t.IsRetained(0)));
        Assert.Equal(new[] { false, false, true }, triads.Select(t => t.IsRetained(1)));
        Assert.Equal(new[] { false, false, false }, triads.Select(t => t.IsRetained(7)));

        // relative: 1 of 10 pixels changed is within 10 %, 7 of 14 is not within 25 %
        Assert.Equal(new[] { false, false, true }, triads.Select(t => t.IsRetainedRelative(0.10)));
        Assert.Equal(new[] { false, false, false }, triads.Select(t => t.IsRetainedRelative(0.50)));

        // opaque counts are independent of the any-coverage counts
        triads[1].DifferentPixelsOpaque = 0;
        Assert.False(triads[1].IsRetainedOpaque(0));
        Assert.True(triads[1].IsRetained(0));
    }

    [Fact]
    public void ToTrainingData_AcceptsAnyRule()
    {
        var features = new List<LRSimplificationFeatures> { LRSimplificationFeatures.BaseLength };

        var data = Sample().ToTrainingData(t => t.IsRetainedRelative(0.10), features);

        Assert.Equal(new[] { false, false, true }, data.Records.Select(r => r.IsRetained));
    }

    [Fact]
    public void ToTrainingData_ProducesOneRecordPerTriad_WithTheRequestedFeaturesAndLabels()
    {
        var features = new List<LRSimplificationFeatures> { LRSimplificationFeatures.VerticalDistance, LRSimplificationFeatures.Overhang, LRSimplificationFeatures.BaseLength };

        var data = Sample().ToTrainingData(1, features);

        Assert.Equal(3, data.GetCountOfRecords());
        Assert.Equal(3, data.GetCountOfFeatures());
        Assert.Equal(new[] { false, false, true }, data.Records.Select(r => r.IsRetained));

        // features are computed from the placed coordinates
        Assert.Equal(0.5, data.Records[1].VerticalDistance!.Value, 9);
        Assert.Equal(4, data.Records[1].BaseLength!.Value, 9);
        Assert.Equal(0, data.Records[1].Overhang!.Value, 9);

        // filtering by rotation
        Assert.Equal(1, Sample().ToTrainingData(0, features, t => t.RotationDegrees == 45).GetCountOfRecords());
    }

    [Fact]
    public void SaveAndLoad_RoundTrip()
    {
        var path = Path.Combine(Path.GetTempPath(), $"oracle-{System.Guid.NewGuid():N}.json");

        try
        {
            Sample().Save(path);

            var loaded = OracleSyntheticDataFile.Load(path);

            Assert.NotNull(loaded);
            Assert.Equal(3, loaded!.Triads.Count);
            Assert.Equal(new List<double> { 0, 45 }, loaded.RotationsDegrees);
            Assert.Equal(7, loaded.Triads[2].DifferentPixels);
            Assert.Equal(31.5, loaded.Triads[1].Y2);
        }
        finally
        {
            File.Delete(path);
        }

        Assert.Null(OracleSyntheticDataFile.Load(path));
    }
}
