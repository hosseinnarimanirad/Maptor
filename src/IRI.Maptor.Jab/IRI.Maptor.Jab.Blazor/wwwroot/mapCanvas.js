// Minimal Canvas2D tile compositor for MapCanvas.razor. Kept intentionally small: C# owns all
// map/tile math (MapViewport, TileInfo) and hands over one batched draw list per frame; this
// module's only job is turning that list into pixels and caching the Image objects.

export function init(canvasElement, dotNetRef) {
  const state = {
    canvas: canvasElement,
    ctx: canvasElement.getContext("2d"),
    cache: new Map(),
    lastTiles: [],
  };

  // The container (not the canvas itself) drives layout size; the canvas's own width/height
  // attributes are set explicitly from C# in resize(), so we observe the parent instead of
  // creating a feedback loop by observing the canvas.
  const container = canvasElement.parentElement;
  state.resizeObserver = new ResizeObserver((entries) => {
    const entry = entries[0];
    if (!entry) return;
    const { width, height } = entry.contentRect;
    dotNetRef.invokeMethodAsync("OnCanvasResized", width, height);
  });
  state.resizeObserver.observe(container);

  return state;
}

export function dispose(state) {
  state.resizeObserver?.disconnect();
}

export function resize(state, width, height) {
  const dpr = window.devicePixelRatio || 1;
  state.canvas.width = Math.round(width * dpr);
  state.canvas.height = Math.round(height * dpr);
  state.canvas.style.width = `${width}px`;
  state.canvas.style.height = `${height}px`;
  state.ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  redraw(state);
}

export function drawTiles(state, tiles) {
  state.lastTiles = tiles;

  for (const tile of tiles) {
    if (state.cache.has(tile.key)) continue;

    const img = new Image();
    // No crossOrigin/CORS mode here: tile.openstreetmap.org sends no Access-Control-Allow-Origin
    // header, so a CORS-mode request is rejected outright. drawImage() doesn't need CORS unless
    // the canvas is later read back (getImageData/toDataURL), which this module never does.
    // A tile that finishes loading after the viewport has already moved on must not paint
    // itself directly onto whatever is on screen now — it re-triggers a redraw of the current
    // tile list instead, so a stale image can only appear if it is still actually visible.
    img.onload = () => redraw(state);
    img.src = tile.url;
    state.cache.set(tile.key, img);
  }

  redraw(state);
}

function redraw(state) {
  const { ctx, canvas } = state;
  const dpr = window.devicePixelRatio || 1;
  const width = canvas.width / dpr;
  const height = canvas.height / dpr;

  ctx.fillStyle = "#dbe4e6";
  ctx.fillRect(0, 0, width, height);

  for (const tile of state.lastTiles) {
    const img = state.cache.get(tile.key);
    if (img && img.complete && img.naturalWidth > 0) {
      ctx.drawImage(img, tile.x, tile.y, tile.width, tile.height);
    }
  }
}
