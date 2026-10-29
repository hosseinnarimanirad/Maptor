using IRI.Maptor.Core.MachineLearning;
using System.Linq;
using Xunit;

namespace IRI.Maptor.Tests.MachineLearning;

// The label grid exported for the figures must be the training data, cell by cell.
public class SyntheticLabelGridTest
{
    [Theory]
    [InlineData(SyntheticLabelRules.V23)]
    [InlineData(SyntheticLabelRules.V24)]
    public void LabelGrid_EqualsTheLabelsOfCreateAll(SyntheticLabelRules rules)
    {
        var grid = SyntheticDataFactory.CreateLabelGrid(rules);
        var items = SyntheticDataFactory.CreateAll(rules);

        Assert.Equal(items.Count, grid.Count);

        for (int i = 0; i < grid.Count; i++)
        {
            Assert.Equal(items[i].Note == "RETAINED", grid[i].IsRetained);
            Assert.StartsWith($"Base{grid[i].BaseLength}-", items[i].Title);
        }
    }

    [Fact]
    public void V23_HasThePublishedCounts()
    {
        var grid = SyntheticDataFactory.CreateLabelGrid(SyntheticLabelRules.V23);

        Assert.Equal(4182, grid.Count);
        Assert.Equal(736, grid.Count(c => !c.IsRetained));
        Assert.Equal(new[] { 306, 323, 357, 425, 561, 833, 1377 }, grid.GroupBy(c => c.BaseLength).OrderBy(g => g.Key).Select(g => g.Count()));
    }

    [Fact]
    public void V24_DiffersFromV23OnlyInTheCollinearRowOfTheHalfPixelBase()
    {
        var v23 = SyntheticDataFactory.CreateLabelGrid(SyntheticLabelRules.V23);
        var v24 = SyntheticDataFactory.CreateLabelGrid(SyntheticLabelRules.V24);

        var changed = Enumerable.Range(0, v23.Count).Where(i => v23[i].IsRetained != v24[i].IsRetained).Select(i => v23[i]).ToList();

        Assert.NotEmpty(changed);
        Assert.All(changed, c => Assert.Equal(0.5, c.BaseLength));
        Assert.All(changed, c => Assert.Equal(0.0, c.Dy));
        Assert.All(changed, c => Assert.True(c.IsRetained));

        // the collinear row of the 1-px base removes the same overhang (2.5 px)
        Assert.Equal(changed.Min(c => c.X), v23.Where(c => c.BaseLength == 1 && c.Dy == 0 && !c.IsRetained).Min(c => c.X));
    }
}
