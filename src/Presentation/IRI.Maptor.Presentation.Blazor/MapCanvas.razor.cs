using IRI.Maptor.Presentation.Blazor.Rendering;
using IRI.Maptor.Presentation.Core.TileServices;
using IRI.Maptor.Core.Common.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace IRI.Maptor.Presentation.Blazor;

/// <summary>
/// Step-1 vertical slice: a pannable/zoomable OSM raster basemap on a plain HTML canvas.
/// No layers, no auth, no API calls yet — this component exists to prove the WASM-canvas
/// rendering loop (see Barg web-client Step 1 plan) before the layer/symbology stack is built
/// on top of it.
/// </summary>
public partial class MapCanvas : ComponentBase, IAsyncDisposable
{
    private readonly MapViewport _viewport = new(centerLongitude: 51.389, centerLatitude: 35.6892, zoomLevel: 5); // Tehran

    private ElementReference _canvasRef;
    private CanvasTileRenderer? _renderer;
    private DotNetObjectReference<MapCanvas>? _selfRef;

    private bool _isDragging;
    private double _lastPointerX;
    private double _lastPointerY;

    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    /// <summary>
    /// Raised whenever the zoom level changes, and once on first sizing so a host starts in sync.
    /// Exists for chrome that has to track the scale — the layer TOC greys out layers outside
    /// their zoom band — without that chrome reaching into the viewport itself.
    /// </summary>
    [Parameter] public EventCallback<int> ZoomLevelChanged { get; set; }

    public int ZoomLevel => _viewport.ZoomLevel;

    /// <summary>
    /// Vector layers drawn over the basemap, in DrawOrder. The host owns fetching and mutating
    /// these; call <see cref="RedrawVectorsAsync"/> after changing one in place (a visibility or
    /// opacity toggle does not replace the list, so Blazor cannot detect it as a parameter change).
    /// </summary>
    [Parameter] public IReadOnlyList<MapVectorLayer>? VectorLayers { get; set; }

    /// <summary>Raised when a click lands on a feature. A click on empty map raises it with null.</summary>
    [Parameter] public EventCallback<MapFeatureHit?> FeatureClicked { get; set; }

    /// <summary>
    /// What is currently selected, drawn over every layer. Null clears it. Unlike
    /// <see cref="VectorLayers"/> this is expected to be replaced rather than mutated, so a plain
    /// parameter change is enough to repaint it.
    /// </summary>
    [Parameter] public MapHighlight? Highlight { get; set; }

    /// <summary>Pixel slop allowed between pointer down and up before it counts as a drag.</summary>
    private const double ClickMovementTolerance = 4;

    private double _pointerDownX;
    private double _pointerDownY;
    private bool _hasDraggedSincePointerDown;

    /// <summary>Last VectorLayers/Highlight instances actually drawn, so a re-render that did not
    /// replace either can be told apart from one that did.</summary>
    private IReadOnlyList<MapVectorLayer>? _drawnVectorLayers;
    private MapHighlight? _drawnHighlight;

    /// <summary>
    /// Picks up a <em>replaced</em> VectorLayers list. In-place edits to an existing list are
    /// invisible to Blazor's parameter diffing, which is why <see cref="RedrawVectorsAsync"/> is
    /// public.
    ///
    /// <para>The reference check is not an optimisation, it is the difference between a responsive
    /// map and a freezing one: the host re-renders for reasons that have nothing to do with the map
    /// — opening the identify card, a loading counter ticking, the TOC collapsing — and every one
    /// of those re-runs OnParametersSetAsync. Redrawing unconditionally meant re-projecting every
    /// vertex of every layer and re-marshalling the whole path set over JS interop each time.</para>
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        if (_renderer is null)
            return;

        if (ReferenceEquals(VectorLayers, _drawnVectorLayers) && ReferenceEquals(Highlight, _drawnHighlight))
            return;

        await RedrawVectorsAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        _selfRef = DotNetObjectReference.Create(this);
        _renderer = new CanvasTileRenderer(JsRuntime);
        await _renderer.InitAsync(_canvasRef, _selfRef);
    }

    [JSInvokable]
    public async Task OnCanvasResized(double width, double height)
    {
        if (_renderer is null) return;

        _viewport.Resize(width, height);
        await _renderer.ResizeAsync(width, height);
        await RenderAsync();

        // First sizing is the earliest point a host can be told the starting zoom.
        await ZoomLevelChanged.InvokeAsync(_viewport.ZoomLevel);
    }

    private void OnPointerDown(PointerEventArgs e)
    {
        _isDragging = true;
        _lastPointerX = e.OffsetX;
        _lastPointerY = e.OffsetY;

        _pointerDownX = e.OffsetX;
        _pointerDownY = e.OffsetY;
        _hasDraggedSincePointerDown = false;
    }

    private async Task OnPointerMove(PointerEventArgs e)
    {
        if (!_isDragging) return;

        var dx = e.OffsetX - _lastPointerX;
        var dy = e.OffsetY - _lastPointerY;
        _lastPointerX = e.OffsetX;
        _lastPointerY = e.OffsetY;

        if (Math.Abs(e.OffsetX - _pointerDownX) > ClickMovementTolerance
            || Math.Abs(e.OffsetY - _pointerDownY) > ClickMovementTolerance)
        {
            _hasDraggedSincePointerDown = true;
        }

        _viewport.PanByPixels(dx, dy);
        await RenderAsync();
    }

    private async Task OnPointerUp(PointerEventArgs e)
    {
        _isDragging = false;

        if (_hasDraggedSincePointerDown || !FeatureClicked.HasDelegate)
            return;

        // The second release of a double-click is a zoom gesture, not a selection — Detail carries
        // the browser's click count, so the zoom does not also re-raise FeatureClicked.
        if (e.Detail > 1)
            return;

        // Releasing where the press started is a click, not the end of a pan — only then does it
        // make sense to ask what is under the cursor.
        var hit = VectorLayers is null
            ? null
            : VectorHitTester.HitTest(VectorLayers, _viewport, e.OffsetX, e.OffsetY);

        await FeatureClicked.InvokeAsync(hit);
    }

    /// <summary>Leaving the canvas ends a drag but is never a click.</summary>
    private void OnPointerLeave(PointerEventArgs e)
    {
        _isDragging = false;
    }

    private async Task OnWheel(WheelEventArgs e)
    {
        // Horizontal-only scrolling (trackpad shift-scroll) is not a zoom gesture.
        if (e.DeltaY == 0) return;

        await ZoomStepAsync(e.DeltaY < 0 ? 1 : -1, e.OffsetX, e.OffsetY);
    }

    /// <summary>Double-click zooms one level in at the cursor, mirroring the WPF MapViewer's
    /// MapView_MouseDownForDoubleClickZoom.</summary>
    private async Task OnDoubleClick(MouseEventArgs e)
    {
        // A double-click is never the tail of a pan, but the pointer handlers may still have a
        // drag flagged from the two presses; clear it so the next click hit-tests normally.
        _isDragging = false;
        _hasDraggedSincePointerDown = false;

        await ZoomStepAsync(1, e.OffsetX, e.OffsetY);
    }

    /// <summary>
    /// One zoom step anchored at a screen point: the map coordinate under (screenX, screenY) stays
    /// on that exact pixel afterwards. Shared by the wheel and double-click gestures.
    /// </summary>
    private async Task ZoomStepAsync(int levelDelta, double screenX, double screenY)
    {
        var levelBefore = _viewport.ZoomLevel;

        _viewport.ZoomAtScreenPoint(levelDelta, screenX, screenY);

        // Clamped at a limit: nothing moved, so skip the render and the host notification rather
        // than re-rendering the host's chrome on every wheel tick at full zoom.
        if (_viewport.ZoomLevel == levelBefore) return;

        await RenderAsync();

        await ZoomLevelChanged.InvokeAsync(_viewport.ZoomLevel);
    }

    /// <summary>
    /// Redraws the vector layers without touching the basemap. For state changes that do not move
    /// the map — a TOC visibility or opacity toggle, or features finishing their download — where
    /// re-sending the tile list would be pure waste.
    /// </summary>
    public async Task RedrawVectorsAsync()
    {
        if (_renderer is null) return;

        _drawnVectorLayers = VectorLayers;
        _drawnHighlight = Highlight;

        var commands = VectorLayers is null
            ? []
            : VectorProjector.Project(VectorLayers, _viewport);

        // Appended last so the selection paints over every layer.
        if (Highlight is { Geometries.Count: > 0 })
            commands.AddRange(VectorProjector.ProjectHighlight(Highlight, _viewport));

        await _renderer.DrawVectorsAsync(commands);
    }

    /// <summary>
    /// Frames a Web Mercator extent — what zoom-to-search-result and zoom-to-feature need. No-ops
    /// on an empty extent, and reports the resulting zoom to the host like any other zoom change.
    /// </summary>
    public async Task ZoomToExtentAsync(BoundingBox extentWm)
    {
        if (_renderer is null) return;

        var levelBefore = _viewport.ZoomLevel;

        if (!_viewport.ZoomToExtent(extentWm))
            return;

        await RenderAsync();

        if (_viewport.ZoomLevel != levelBefore)
            await ZoomLevelChanged.InvokeAsync(_viewport.ZoomLevel);
    }

    private async Task RenderAsync()
    {
        if (_renderer is null) return;

        // The basemap is drawn FIRST and is never made conditional on the vector pass. An earlier
        // version projected vectors first, which meant any failure there — including a browser
        // holding a cached mapCanvas.js from before drawVectors existed — left the user with no
        // map at all rather than a map missing its overlays.
        await RenderTilesAsync();

        await RedrawVectorsAsync();
    }

    private async Task RenderTilesAsync()
    {
        if (_renderer is null) return;

        var makeUrl = TileMapWebUrlFactory.GetMakeUrlFunc("tile_provider_osm", "tile_mapType_street");
        if (makeUrl is null) return;

        var tiles = _viewport.GetVisibleTiles();
        var commands = new List<TileDrawCommand>(tiles.Count);

        foreach (var tile in tiles)
        {
            var url = makeUrl(tile);
            if (url is null) continue;

            var rect = _viewport.GetScreenRect(tile);
            commands.Add(new TileDrawCommand(tile.ToShortString(), url, rect.x, rect.y, rect.width, rect.height));
        }

        await _renderer.DrawAsync(commands);
    }

    public async ValueTask DisposeAsync()
    {
        if (_renderer is not null)
        {
            await _renderer.DisposeAsync();
        }

        _selfRef?.Dispose();
    }
}
