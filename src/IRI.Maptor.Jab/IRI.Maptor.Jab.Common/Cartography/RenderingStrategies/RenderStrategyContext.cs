using System;

namespace IRI.Maptor.Jab.Common.Cartography.RenderingStrategies;

public static class RenderStrategyContext
{
    public static RenderStrategy Create(VectorLayer layer)
    {
        if (layer == null)
            throw new ArgumentNullException(nameof(layer));

        return layer.RasterizationMethod switch
        {
            RasterizationMethod.DrawingVisual => new DrawingVisualRenderStrategy(layer.Symbolizers),
            RasterizationMethod.WriteableBitmap => new WriteableBitmapRenderStrategy(layer.Symbolizers),
            RasterizationMethod.GdiPlus => new GdiBitmapRenderStrategy(layer.Symbolizers),

            RasterizationMethod.StreamGeometry or
            RasterizationMethod.None or
            _ => throw new NotImplementedException(),
        };
    }
}
