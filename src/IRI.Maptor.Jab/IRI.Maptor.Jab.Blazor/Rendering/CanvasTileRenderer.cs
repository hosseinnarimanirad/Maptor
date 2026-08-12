using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace IRI.Maptor.Jab.Blazor.Rendering;

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
            "import", "./_content/IRI.Maptor.Jab.Blazor/mapCanvas.js");

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
