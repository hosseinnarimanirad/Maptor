using System.Collections.Generic;

using IRI.Maptor.Core.Spatial.Helpers.MapGrids;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;

using IRI.Maptor.Presentation.Core.Localization;

namespace IRI.Maptor.Presentation.Wpf.Models.Map;

/// <summary>
/// The grids offered in the picker.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately a fixed list of <em>named</em> systems, each fully specified: a grid the user picks
/// off a menu should not need parameters filled in first. Custom projections — a transverse
/// Mercator on chosen constants, say — arrive in step 3 through a dialog, appended to this list at
/// run time.
/// </para>
/// <para>
/// The named systems come straight from <see cref="SrsBases"/> rather than through
/// <c>ProjectionPreset</c>. That is the same set of instances the Go To dialog's picker offers for
/// these entries — its named presets return exactly these objects — but going direct avoids
/// pulling a dialog's catalogue, its editable-parameter machinery and its ellipsoid rules into a
/// menu that needs none of them.
/// </para>
/// <para>
/// Titles are built when this is called, so they follow the UI language current at that moment.
/// </para>
/// </remarks>
public static class MapGridCatalog
{
    public static List<MapGridDefinition> CreateDefaults()
    {
        var l = LocalizationManager.Instance;

        var lcc = l["srs_lccTitle"];

        return new List<MapGridDefinition>
        {
            MapGridDefinition.Geodetic(l["layer_mapGrid_geodetic"]),

            MapGridDefinition.Utm(l["layer_mapGrid_utm"]),

            MapGridDefinition.Projected(SrsBases.WebMercator, "webMercator", l["srs_webMercatorTitle"]),

            MapGridDefinition.Projected(SrsBases.LccNiocWithClarke1880Rgs, "lccNioc", $"{lcc} · NIOC"),

            MapGridDefinition.Projected(SrsBases.LccFd58, "lccFd58", $"{lcc} · FD58"),

            MapGridDefinition.Projected(SrsBases.LccNahrawanIraq, "lccNahrwan", $"{lcc} · Nahrwan"),
        };
    }
}
