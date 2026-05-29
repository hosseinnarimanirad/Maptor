using System;
using System.Linq;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Jab.Common.Layers;

public class GroupLayer : BaseLayer
{
    public override LayerType Type => LayerType.GroupLayer;

    public override BoundingBox Extent { get => BoundingBox.NaN; protected set => throw new NotImplementedException(); }

    //public override RenderingApproach Rendering { get => RenderingApproach.Default; protected set => throw new NotImplementedException(); }

    public GroupLayer(string title, string tocGroup = IRI.Maptor.Jab.Common.ViewModels.Map.LegendViewModel.DefaultTocGroup)
    {
        LayerName = title;

        IsGroupLayer = true;

        this.TocGroup = tocGroup;

        SubLayers = new System.Collections.ObjectModel.ObservableCollection<ILayer>();

        VisibleRange = ScaleInterval.All;
         
        Visibility = System.Windows.Visibility.Collapsed;
    }

    public void AddSubLayer(ILayer layer)
    {
        //layer.ParentLayerId = this.LayerId;

        //layer.ParentLayerName = this.LayerName;
        layer.Parent = this;

        if (!SubLayers.Contains(layer))
        {
            var index = SubLayers.Count(x => x.ZIndex > layer.ZIndex);

            SubLayers.Insert(index, layer);

            //this.SubLayers.Add(layer);
        }

        UpdateAllChildsVisible();
    }

    public override string ToString()
    {
        return $"GROUP LAYER; Sublayers:({SubLayers?.Count ?? 0}), TocOrder: {TocOrder}, ZIndex: {ZIndex}, Name: {LayerName}";
    }

}
