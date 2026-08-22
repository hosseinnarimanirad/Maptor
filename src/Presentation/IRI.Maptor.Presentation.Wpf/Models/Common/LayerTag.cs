using System;
using System.Windows.Media;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Layers;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Model;

namespace IRI.Maptor.Presentation.Wpf.Models;

public class LayerTag
{
    public ILayer Layer { get; set; }

    public TileInfo Tile { get; set; }

    public bool IsDrawn { get; set; }

    public bool IsNew { get; set; }

    private bool isTiled;

    public bool IsTiled
    {
        get { return this.Layer != null ? this.Layer.RenderMode == RenderMode.Tiled : isTiled; }
        set { this.isTiled = value; }
    }

    private LayerType layerType;

    public LayerType LayerType
    {
        get { return (this.Layer != null) ? this.Layer.Type : layerType; }
        set { this.layerType = value; }
    }

    public bool CanUserDelete
    {
        get { return Layer?.Type != LayerType.BaseMap && Layer?.CanUserDelete == true; }
    }

    private BoundingBox boundingBox;

    public BoundingBox BoundingBox
    {
        get
        {
            if (this.Tile != null)
            {
                return this.Tile.WebMercatorExtent;
            }
            else
                return boundingBox;
        }
        set
        {
            if (this.Tile != null)
            {
                throw new NotImplementedException();
            }
            else
            {
                this.boundingBox = value;
            }
        }
    }

    public double Scale { get; set; }

    public LayerTag(double scale)
    {
        this.IsNew = true;

        this.Scale = scale;
    }

    public bool IsInTile(BoundingBox extent)
    {
        return this.BoundingBox.Intersects(extent);
    }

    public Guid AncestorLayerId { get; set; }

    // set for complex-layer items: the screen-position transform inside the element's
    // TransformGroup, so position updates don't rely on the transform's index in the group
    public TranslateTransform PositionTransform { get; set; }

    // set for complex-layer items: the Locateable this element belongs to, so handlers
    // whose sender is the element (e.g. SizeChanged) can recompute the anchored position
    public Locateable Locateable { get; set; }
}
