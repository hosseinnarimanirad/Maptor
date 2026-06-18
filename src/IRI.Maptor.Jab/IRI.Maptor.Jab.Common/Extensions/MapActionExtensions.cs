using System;

using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Core;

namespace IRI.Maptor.Extensions;

public static class MapActionExtensions
{
    public static bool IsIdentifyAction(this MapAction action) =>
        action == MapAction.Identify;

    public static bool IsDrawAction(this MapAction action) =>
        action is MapAction.DrawPoint
            or MapAction.DrawPolyline
            or MapAction.DrawPolygon
            or MapAction.DrawRectangle;

    public static DrawMode ToDrawMode(this MapAction action) => action switch
    {
        MapAction.DrawPoint => DrawMode.Point,
        MapAction.DrawPolyline => DrawMode.Polyline,
        MapAction.DrawPolygon => DrawMode.Polygon,
        MapAction.DrawRectangle => DrawMode.Rectangle,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Not a draw MapAction.")
    };
}
