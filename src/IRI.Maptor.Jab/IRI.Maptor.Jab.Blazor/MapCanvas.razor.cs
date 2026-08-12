using IRI.Maptor.Jab.Blazor.Rendering;
using IRI.Maptor.Jab.Core.TileServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace IRI.Maptor.Jab.Blazor;

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
    }

    private void OnPointerDown(PointerEventArgs e)
    {
        _isDragging = true;
        _lastPointerX = e.OffsetX;
        _lastPointerY = e.OffsetY;
    }

    private async Task OnPointerMove(PointerEventArgs e)
    {
        if (!_isDragging) return;

        var dx = e.OffsetX - _lastPointerX;
        var dy = e.OffsetY - _lastPointerY;
        _lastPointerX = e.OffsetX;
        _lastPointerY = e.OffsetY;

        _viewport.PanByPixels(dx, dy);
        await RenderAsync();
    }

    private void OnPointerUp(PointerEventArgs e)
    {
        _isDragging = false;
    }

    private async Task OnWheel(WheelEventArgs e)
    {
        var levelDelta = e.DeltaY < 0 ? 1 : -1;
        _viewport.ZoomAtScreenPoint(levelDelta, e.OffsetX, e.OffsetY);
        await RenderAsync();
    }

    private async Task RenderAsync()
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
