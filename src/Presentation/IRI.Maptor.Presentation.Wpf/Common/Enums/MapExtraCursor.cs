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
}
