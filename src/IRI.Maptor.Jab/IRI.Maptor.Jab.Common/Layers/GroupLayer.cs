using System;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Jab.Common;

public class GroupLayer : BaseLayer
{
    public override LayerType Type => LayerType.GroupLayer;

    public override BoundingBox Extent { get => BoundingBox.NaN; protected set => throw new NotImplementedException(); }

    //public override RenderingApproach Rendering { get => RenderingApproach.Default; protected set => throw new NotImplementedException(); }

    public GroupLayer(string title)
    {
        this.LayerName = title;

        this.IsGroupLayer = true;

        this.SubLayers = new System.Collections.ObjectModel.ObservableCollection<ILayer>();

        this.VisibleRange = ScaleInterval.All;

        this.ShowInToc = true;

        this.Visibility = System.Windows.Visibility.Collapsed;
    }

    public void AddSubLayer(ILayer layer)
    {
        layer.ParentLayerId = this.LayerId;

        if (!this.SubLayers.Contains(layer))
        {
            this.SubLayers.Add(layer);
        }

    }

    public override string ToString()
    {
        return $"GROUP LAYER - {LayerName}";
    }
      
}
