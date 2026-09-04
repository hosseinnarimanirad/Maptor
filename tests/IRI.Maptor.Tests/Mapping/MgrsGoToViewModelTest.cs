using System.Collections.Generic;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Presentation.Wpf.ViewModels;

using Xunit;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// The MGRS panel's view model. It resolves a reference at any level to the region that reference
/// names, and zooms to it; a shorter reference means a bigger region.
/// </summary>
public class MgrsGoToViewModelTest
{
    private static MgrsGoToViewModel Create(List<BoundingBox> zoomed)
        => new MgrsGoToViewModel(extent => zoomed.Add(extent));

    private static MgrsGoToViewModel Create() => Create(new List<BoundingBox>());

    #region Resolving

    [Theory]
    [InlineData("39")]
    [InlineData("39S")]
    [InlineData("39S WV")]
    [InlineData("39S WV 53 39")]
    [InlineData("39S WV 53516 39501")]
    [InlineData("39swv5351639501")]
    public void Reference_AtEveryLevel_Resolves(string reference)
    {
        var model = Create();

        model.Reference = reference;

        Assert.True(model.IsValid, $"'{reference}' did not resolve");
        Assert.NotEqual(string.Empty, model.Status);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("0")]
    [InlineData("61")]
    [InlineData("39WV")]        // a square with no band
    [InlineData("32X")]         // a Svalbard cell that does not exist
    [InlineData("39S IQ")]      // I is not an MGRS letter
    public void Reference_Malformed_IsRejectedWithAMessage(string reference)
    {
        var model = Create();

        model.Reference = reference;

        Assert.False(model.IsValid);
        Assert.NotEqual(string.Empty, model.Status);
    }

    /// <summary>An untouched panel is not an error, so it says nothing.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Reference_Empty_SaysNothing(string reference)
    {
        var model = Create();

        model.Reference = "39S";
        model.Reference = reference;

        Assert.False(model.IsValid);
        Assert.Equal(string.Empty, model.Status);
    }

    #endregion

    #region Zooming

    [Fact]
    public void ZoomToCommand_IsDisabledUntilTheReferenceResolves()
    {
        var model = Create();

        Assert.False(model.ZoomToCommand.CanExecute(null));

        model.Reference = "39S";

        Assert.True(model.ZoomToCommand.CanExecute(null));

        model.Reference = "not a reference";

        Assert.False(model.ZoomToCommand.CanExecute(null));
    }

    [Fact]
    public void ZoomToCommand_PassesTheRegionTheReferenceNames()
    {
        var zoomed = new List<BoundingBox>();

        var model = Create(zoomed);

        model.Reference = "39S";

        model.ZoomToCommand.Execute(null);

        var extent = Assert.Single(zoomed);

        Assert.Equal(48.0, extent.XMin, 9);
        Assert.Equal(54.0, extent.XMax, 9);
        Assert.Equal(32.0, extent.YMin, 9);
        Assert.Equal(40.0, extent.YMax, 9);
    }

    [Fact]
    public void ZoomToCommand_WhileInvalid_DoesNothing()
    {
        var zoomed = new List<BoundingBox>();

        var model = Create(zoomed);

        model.Reference = "not a reference";

        model.ZoomToCommand.Execute(null);

        Assert.Empty(zoomed);
    }

    /// <summary>
    /// The whole point of accepting a partial reference: each extra piece narrows the region.
    /// </summary>
    [Fact]
    public void Reference_TheMoreItSays_TheSmallerTheRegion()
    {
        var zoomed = new List<BoundingBox>();

        var model = Create(zoomed);

        foreach (var reference in new[] { "39", "39S", "39S WV", "39S WV 53 39", "39S WV 53516 39501" })
        {
            model.Reference = reference;

            model.ZoomToCommand.Execute(null);
        }

        Assert.Equal(5, zoomed.Count);

        for (var i = 1; i < zoomed.Count; i++)
        {
            Assert.True(zoomed[i].Height < zoomed[i - 1].Height,
                $"region {i} is not shorter than the one before it");
        }
    }

    /// <summary>
    /// Copy follows the same gate as zoom. Execute is deliberately not exercised here: it writes
    /// to the real clipboard, which a test run has no business clobbering.
    /// </summary>
    [Fact]
    public void CopyCommand_IsDisabledUntilTheReferenceResolves()
    {
        var model = Create();

        Assert.False(model.CopyCommand.CanExecute(null));

        model.Reference = "39swv5351639501";

        Assert.True(model.CopyCommand.CanExecute(null));

        model.Reference = "not a reference";

        Assert.False(model.CopyCommand.CanExecute(null));
    }

    #endregion

    #region Where the cursor is

    /// <summary>
    /// The readout answers what the grid on the map cannot: its lines carry principal digits, and
    /// this is the whole reference those digits belong to. Always at one metre.
    /// </summary>
    [Fact]
    public void UpdateCurrentPosition_ShowsTheFullReferenceAtOneMetre()
    {
        var model = Create();

        model.UpdateCurrentPosition(new Point(51.3380, 35.6997));

        Assert.True(model.HasCurrentPosition);
        Assert.StartsWith("39S WV", model.CurrentPosition);

        // "39S WV 30578 50694" — five digits an axis
        Assert.Equal(18, model.CurrentPosition.Length);
    }

    [Fact]
    public void UpdateCurrentPosition_TracksTheCursor()
    {
        var model = Create();

        model.UpdateCurrentPosition(new Point(51.3380, 35.6997));

        var tehran = model.CurrentPosition;

        model.UpdateCurrentPosition(new Point(2.2945, 48.8584));

        Assert.NotEqual(tehran, model.CurrentPosition);
        Assert.StartsWith("31U DQ", model.CurrentPosition);
    }

    /// <summary>
    /// Off the grid the last good reading stays put. Blanking it as the cursor crosses the poles
    /// or leaves the map would make the panel flicker for no gain.
    /// </summary>
    [Fact]
    public void UpdateCurrentPosition_OffTheGrid_KeepsTheLastReading()
    {
        var model = Create();

        model.UpdateCurrentPosition(new Point(51.3380, 35.6997));

        var last = model.CurrentPosition;

        model.UpdateCurrentPosition(new Point(51.0, 88.0));     // past the top of the grid
        model.UpdateCurrentPosition(null);

        Assert.Equal(last, model.CurrentPosition);
    }

    [Fact]
    public void CopyCurrentPositionCommand_IsDisabledUntilThereIsAPosition()
    {
        var model = Create();

        Assert.False(model.CopyCurrentPositionCommand.CanExecute(null));

        model.UpdateCurrentPosition(new Point(51.3380, 35.6997));

        Assert.True(model.CopyCurrentPositionCommand.CanExecute(null));
    }

    /// <summary>The readout is independent of the reference being typed: both live at once.</summary>
    [Fact]
    public void CurrentPosition_AndTheTypedReference_DoNotInterfere()
    {
        var model = Create();

        model.Reference = "39S";
        model.UpdateCurrentPosition(new Point(2.2945, 48.8584));

        Assert.True(model.IsValid);
        Assert.StartsWith("31U DQ", model.CurrentPosition);
        Assert.Equal("39S", model.Reference);
    }

    #endregion
}
