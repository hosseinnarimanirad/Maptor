using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace IRI.Maptor.Presentation.Blazor.Rendering;

/// <summary>
/// Thin JS-interop boundary to the canvas. Every frame is handed over as a single batched
/// call (module.drawTiles(canvas, tiles)) rather than one interop call per tile — the WASM/JS
/// marshaling cost is what makes or breaks a Blazor rendering loop, so batching here is the
/// one non-negotiable rule for anything added to this class later.
/// </summary>
public sealed class CanvasTileRenderer : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;
    private IJSObjectReference? _canvasHandle;

    public CanvasTileRenderer(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitAsync<TComponent>(ElementReference canvasElement, DotNetObjectReference<TComponent> dotNetRef)
        where TComponent : class
    {
        _module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/IRI.Maptor.Presentation.Blazor/mapCanvas.js");

        _canvasHandle = await _module.InvokeAsync<IJSObjectReference>("init", canvasElement, dotNetRef);
    }

    public async Task ResizeAsync(double width, double height)
    {
        if (_module is null || _canvasHandle is null) return;

        await _module.InvokeVoidAsync("resize", _canvasHandle, width, height);
    }

    public async Task DrawAsync(IReadOnlyList<TileDrawCommand> tiles)
    {
        if (_module is null || _canvasHandle is null) return;

        await _module.InvokeVoidAsync("drawTiles", _canvasHandle, tiles);
    }

    /// <summary>
    /// Hands over every visible vector layer as one batched call, for the same reason tiles are
    /// batched: per-layer interop would put the marshaling cost back in the frame loop.
    /// </summary>
    public async Task DrawVectorsAsync(IReadOnlyList<VectorDrawCommand> layers)
    {
        if (_module is null || _canvasHandle is null) return;

        try
        {
            await _module.InvokeVoidAsync("drawVectors", _canvasHandle, layers);
        }
        catch (JSException ex)
        {
            // A browser serving a cached mapCanvas.js from before drawVectors existed throws here.
            // The overlays are lost either way, but the basemap and every map interaction must
            // keep working, so this is reported and swallowed rather than propagated.
            Console.Error.WriteLine($"MapCanvas: vector drawing unavailable ({ex.Message}). " +
                                    "This usually means a stale cached mapCanvas.js — hard-reload the page.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null && _canvasHandle is not null)
        {
            await _module.InvokeVoidAsync("dispose", _canvasHandle);
        }

        if (_canvasHandle is not null)
        {
            await _canvasHandle.DisposeAsync();
        }

        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }
}
