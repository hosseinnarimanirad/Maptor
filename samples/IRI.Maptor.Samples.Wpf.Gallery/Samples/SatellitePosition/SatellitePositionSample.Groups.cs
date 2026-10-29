using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using IRI.Maptor.Core.Satellite.Elements;
using IRI.Maptor.Core.Satellite.Sources;

namespace IRI.Maptor.Samples.Wpf.Gallery.Samples.SatellitePosition;

/// <summary>
/// "Load a group": a whole CelesTrak group in place of the list, through the same <see cref="ElementStore"/>
/// as the updates, so it obeys the 2-hour rule and works offline from the last copy.
/// </summary>
public partial class SatellitePositionSample
{
    /// <summary>One entry of the Load a group menu.</summary>
    internal sealed record SatelliteGroup(string Key, string Title, string CelesTrakGroup, Func<OrbitalElements, bool>? Filter = null);

    /// <summary>The menu's groups, by the <c>Tag</c> of their menu item.</summary>
    internal static readonly IReadOnlyList<SatelliteGroup> Groups = new[]
    {
        new SatelliteGroup("gps", "GPS (operational)", "gps-ops"),
        new SatelliteGroup("galileo", "Galileo", "galileo"),
        // CelesTrak's weather group mixes low polar orbiters with geostationary ones; keep the latter
        new SatelliteGroup("geo-weather", "Geostationary weather", "weather",
            e => OrbitClasses.Of(e) is OrbitClass.Geostationary or OrbitClass.Geosynchronous),
        new SatelliteGroup("resource", "Earth resources", "resource"),
        new SatelliteGroup("stations", "Space stations", "stations"),
    };

    /// <summary>The group the list came from; null for the sample, a file or pasted text.</summary>
    private SatelliteGroup? _currentGroup;

    private void OnShowGroups(object sender, RoutedEventArgs e)
    {
        groupMenu.PlacementTarget = groupButton;
        groupMenu.Placement = PlacementMode.Bottom;
        groupMenu.IsOpen = true;
    }

    private async void OnLoadGroup(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string key })
            return;

        if (key == "sample")
        {
            LoadSample();
            return;
        }

        await LoadGroupAsync(Groups.Single(g => g.Key == key));
    }

    /// <summary>Replaces the list with <paramref name="group"/>; keeps it when nothing could be had.</summary>
    internal async Task LoadGroupAsync(SatelliteGroup group)
    {
        groupButton.IsEnabled = false;
        celestrakButton.IsEnabled = false;

        sourceText.Text = $"Asking CelesTrak for {group.Title}…";

        try
        {
            var result = await Store.GetAsync(CelesTrakQuery.Group(group.CelesTrakGroup));

            var elements = group.Filter is null ? result.Elements.ToList() : result.Elements.Where(group.Filter).ToList();

            if (elements.Count == 0)
            {
                // offline with no copy, or an empty answer: the list stays as it was
                sourceText.Text = $"{group.Title}: nothing to show. {result.Message}";
                return;
            }

            if (Show(elements, result.Errors, group.Title))
            {
                _currentGroup = group;
                sourceText.Text = $"{group.Title}: {elements.Count} satellite{(elements.Count == 1 ? "" : "s")}. {result.Message}";
            }
        }
        finally
        {
            groupButton.IsEnabled = true;
            celestrakButton.IsEnabled = true;
        }
    }
}
