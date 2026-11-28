using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Presentation.Core.Data;

public interface IMapSettings
{
    BoundingBox? InitialExtent { get; set; }
    bool IsDoubleClickZoomEnabled { get; set; }
    bool IsGoogleZoomLevelsEnabled { get; set; }
    bool IsMouseWheelZoomEnabled { get; set; }
    int MaxGoogleZoomLevel { get; set; }
    int MinGoogleZoomLevel { get; set; }

    /// <summary>
    /// How many zoom steps (mouse wheel notches, zoom in/out clicks) span one google zoom level.
    /// 1 snaps every step to a google zoom level; higher values insert evenly spaced mid levels.
    /// Range 1..8.
    /// </summary>
    int ZoomStepsPerGoogleLevel { get; set; }

    /// <summary>
    /// Highest google zoom level tiles are requested at. Zooming past it keeps requesting this
    /// level and lets the tiles scale up, rather than asking the provider for a level it has no
    /// imagery for, which would blank the base map. Range 1..24.
    /// </summary>
    int MaxTileZoomLevel { get; set; }

    bool AllowLargeDataLoading { get; set; }

    bool Clipboard_IsLatitudeFirst { get; set; }
    int Clipboard_XyPrecision { get; set; }
    int Clipboard_LatLongPrecision { get; set; }

    bool ShowTileBorder { get; set; }

    bool Identify_IncludeNotInScaleRangeLayers { get; set; }
    bool Identify_IncludeInvisibleLayers { get; set; }
    int Identify_SelectionTolerance { get; set; }

    /// <summary>How near (screen pixels) a vertex or edge must be for a drawn or dragged point to snap to it.</summary>
    int Snapping_TolerancePixels { get; set; }

    /// <summary>Snap to vertices (and point features).</summary>
    bool Snapping_ToVertices { get; set; }

    /// <summary>Snap to the nearest place on an edge.</summary>
    bool Snapping_ToEdges { get; set; }

    /// <summary>Line ends as their own kind, ahead of other vertices.</summary>
    bool Snapping_ToEnds { get; set; }

    /// <summary>Snap to the middle of a segment.</summary>
    bool Snapping_ToMidpoints { get; set; }

    /// <summary>Snap to where two edges meet without a vertex there.</summary>
    bool Snapping_ToIntersections { get; set; }

    /// <summary>
    /// While drawing or moving a vertex: guides (the extension of a segment, the perpendicular and the parallel to
    /// it), the angle steps, and picking an existing edge to draw guides from.
    /// </summary>
    bool Snapping_ShowGuides { get; set; }

    /// <summary>
    /// While drawing or moving a vertex: the box beside the point with the segment's length, angle and bearing, and
    /// typing those values.
    /// </summary>
    bool Snapping_ShowCursorBox { get; set; }
}