using System.Windows.Media;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;

namespace IRI.Maptor.Presentation.Wpf.Cartography;

public abstract class RenderStrategy
{
    protected readonly IEnumerable<ISymbolizer> _symbolizers;

    public RenderStrategy(IEnumerable<ISymbolizer> symbolizer)
    {
        _symbolizers = symbolizer;
    }

    /// <summary>
    /// True when <see cref="Render"/> may be called from a thread pool thread and its result
    /// handed back to the ui thread. Such a strategy must touch no <see cref="System.Windows.Threading.DispatcherObject"/>
    /// and must freeze the brush it returns.
    /// <para>
    /// GDI+ qualifies; strategies that go through <see cref="System.Windows.Media.Imaging.RenderTargetBitmap"/>
    /// do not, because it requires an STA thread and the thread pool is MTA.
    /// </para>
    /// </summary>
    public virtual bool CanRenderOffUiThread => false;

    /// <summary>
    /// Call on the ui thread before handing <see cref="Render"/> to a worker: the symbolizers hold
    /// wpf brushes and fonts, which have thread affinity until frozen (GdiBitmapRenderStrategy
    /// reads them through AsGdiBrush / GetGdiPlusPen). Returns false when they cannot all be
    /// frozen, in which case the caller must render on the ui thread instead.
    /// </summary>
    public bool TryPrepareForOffUiThread()
    {
        if (_symbolizers is null)
            return true;

        foreach (var symbolizer in _symbolizers)
        {
            if (symbolizer is SymbolizerBase { Param: not null } symbolizerBase && !symbolizerBase.Param.TryFreezeVisuals())
                return false;
        }

        return true;
    }

    public abstract ImageBrush? Render(IEnumerable<Feature<Point>> features, double mapScale, double screenWidth, double screenHeight);
}
