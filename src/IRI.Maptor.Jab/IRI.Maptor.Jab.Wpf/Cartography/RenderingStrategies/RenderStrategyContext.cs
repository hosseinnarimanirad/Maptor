using IRI.Maptor.Jab.Wpf.Layers;
using IRI.Maptor.Jab.Core;
using System;

namespace IRI.Maptor.Jab.Wpf.Cartography;

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
