using System.Windows;

using IRI.Maptor.Presentation.Core.Data;
using IRI.Maptor.Presentation.Wpf.Controls;
using IRI.Maptor.Presentation.Wpf.ViewModels.Map;
using IRI.Maptor.Tests.Common;

using Xunit;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// The MGRS field on the coordinate panel. It follows the same shape as the height field — a
/// value plus a gate, both dependency properties on the view — except that the value is derived
/// from the position the panel already receives rather than supplied by the host.
/// </summary>
[Collection(WpfCollection.Name)]
public class CoordinatePanelMgrsTest
{
    /// <summary>Azadi Tower, Tehran — grid zone 39S, 100 km square WV.</summary>
    private static readonly Point Tehran = new Point(51.3380, 35.6997);

    private static CoordinatePanelView CreatePanel(bool showMgrs)
    {
        var panel = new CoordinatePanelView { DataContext = new CoordinatePanelViewModel() };

        panel.ShowMgrs = showMgrs;

        return panel;
    }

    /// <summary>
    /// Off is the default and it must cost nothing: the reference is not computed at all, because
    /// this runs on every mouse move.
    /// </summary>
    [Fact]
    public void ShowMgrs_Off_LeavesTheReferenceEmpty()
    {
        string? mgrs = null;

        WpfTestHost.Run(() =>
        {
            var panel = CreatePanel(showMgrs: false);

            panel.Position = Tehran;

            mgrs = panel.CurrentMgrs;
        });

        Assert.Equal(string.Empty, mgrs);
    }

    [Fact]
    public void ShowMgrs_DefaultsToOff()
    {
        var isOn = true;

        WpfTestHost.Run(() => { isOn = new CoordinatePanelView().ShowMgrs; });

        Assert.False(isOn);
    }

    /// <summary>
    /// The position arrives as (longitude, latitude) — <c>MapViewer.CurrentPoint</c> is
    /// <c>ScreenToGeodetic(...)</c> — so a swapped pair here would put Tehran in the wrong zone
    /// entirely, which the grid zone designator catches.
    /// </summary>
    [Fact]
    public void ShowMgrs_On_FillsTheReferenceForThePosition()
    {
        string? mgrs = null;

        WpfTestHost.Run(() =>
        {
            var panel = CreatePanel(showMgrs: true);

            panel.Position = Tehran;

            mgrs = panel.CurrentMgrs;
        });

        Assert.StartsWith("39S WV", mgrs);
    }

    /// <summary>
    /// MGRS has no coverage over the poles without UPS, which this library has no projection for.
    /// Off the top of the grid the field goes blank rather than throwing on a mouse move.
    /// </summary>
    [Theory]
    [InlineData(51.0, 88.0)]
    [InlineData(51.0, -88.0)]
    public void ShowMgrs_On_OutsideTheUtmBand_LeavesTheReferenceEmpty(double longitude, double latitude)
    {
        string? mgrs = null;

        WpfTestHost.Run(() =>
        {
            var panel = CreatePanel(showMgrs: true);

            panel.Position = new Point(longitude, latitude);

            mgrs = panel.CurrentMgrs;
        });

        Assert.Equal(string.Empty, mgrs);
    }

    /// <summary>
    /// MGRS is a military grid, not something most maps want on screen, so the persisted setting
    /// starts off.
    /// </summary>
    [Fact]
    public void GeneralSettings_ShowMgrs_DefaultsToOff()
    {
        Assert.False(GeneralSettings.Default.CoordinatePanel_ShowMgrs);
    }
}
