using System;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Model;

namespace IRI.Maptor.Jab.Common.Models;

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
}
