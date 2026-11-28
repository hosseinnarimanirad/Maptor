namespace IRI.Maptor.Presentation.Wpf;

/// <summary>
/// Map cursors for states that are not a <see cref="MapAction"/>: dragging the map, and Measure,
/// which runs while <see cref="MapAction"/> stays <see cref="MapAction.Pan"/>.
/// </summary>
public enum MapExtraCursor
{
    /// <summary>While the map is being dragged in Pan mode (grabbing hand).</summary>
    PanDragging,

    /// <summary>While measuring a length.</summary>
    MeasureLength,

    /// <summary>While measuring an area.</summary>
    MeasureArea,

    /// <summary>Over an edge of an edited geometry: a press adds a vertex (crosshair with a plus).</summary>
    AddVertex,

    /// <summary>Over a vertex of an edited geometry: a press moves it (four arrows).</summary>
    MoveVertex,
}
