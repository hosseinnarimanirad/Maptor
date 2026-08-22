// Minimal Canvas2D tile compositor for MapCanvas.razor. Kept intentionally small: C# owns all
// map/tile math (MapViewport, TileInfo) and hands over one batched draw list per frame; this
// module's only job is turning that list into pixels and caching the Image objects.

export function init(canvasElement, dotNetRef) {
  const state = {
    canvas: canvasElement,
    ctx: canvasElement.getContext("2d"),
    cache: new Map(),
    lastTiles: [],
    lastVectors: [],
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

// Vector layers are stored separately from tiles and repainted by the same redraw(), so a tile
// finishing its download does not wipe the vectors drawn over it (and vice versa).
export function drawVectors(state, layers) {
  state.lastVectors = layers;
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

  drawVectorLayers(ctx, state.lastVectors);
}

// C# hands over flat [x0,y0,x1,y1,…] paths already in screen pixels, so this only walks numbers
// and paints — no projection, no per-vertex objects.
function drawVectorLayers(ctx, layers) {
  if (!layers || layers.length === 0) return;

  const previousAlpha = ctx.globalAlpha;

  ctx.lineJoin = 'round';
  ctx.lineCap = 'round';

  for (const layer of layers) {
    const paths = layer.paths;
    if (!paths || paths.length === 0) continue;

    ctx.globalAlpha = layer.alpha ?? 1;

    if (layer.kind === 'point') {
      ctx.fillStyle = layer.fill;
      ctx.strokeStyle = layer.stroke;
      ctx.lineWidth = layer.lineWidth;

      const radius = layer.pointRadius ?? 4;

      for (const path of paths) {
        for (let i = 0; i < path.length; i += 2) {
          ctx.beginPath();
          ctx.arc(path[i], path[i + 1], radius, 0, Math.PI * 2);
          ctx.fill();
          ctx.stroke();
        }
      }

      continue;
    }

    // One Path2D for the whole layer: a single fill/stroke pair instead of one per feature, which
    // is what keeps a layer of thousands of parts from costing thousands of canvas state changes.
    const path2d = new Path2D();

    for (const path of paths) {
      if (path.length < 4) continue;

      path2d.moveTo(path[0], path[1]);

      for (let i = 2; i < path.length; i += 2) {
        path2d.lineTo(path[i], path[i + 1]);
      }

      if (layer.kind === 'polygon') path2d.closePath();
    }

    if (layer.kind === 'polygon' && layer.fill !== 'transparent') {
      ctx.fillStyle = layer.fill;
      ctx.fill(path2d);
    }

    if (layer.stroke !== 'transparent') {
      ctx.strokeStyle = layer.stroke;
      ctx.lineWidth = layer.lineWidth;
      ctx.stroke(path2d);
    }
  }

  ctx.globalAlpha = previousAlpha;
}
