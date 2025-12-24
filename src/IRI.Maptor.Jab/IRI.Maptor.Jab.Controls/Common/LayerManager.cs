using System;
using System.Linq; 
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IRI.Maptor.Jab.Common;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Jab.Controls;

public class LayerManager : Notifier
{
    List<ILayer> allLayers;

    private ObservableCollection<ILayer> _currentLayers;

    public ObservableCollection<ILayer> CurrentLayers
    {
        get { return _currentLayers; }
        set
        {
            _currentLayers = value;
            RaisePropertyChanged();
        }
    }

    public LayerManager()
    {
        this.allLayers = new List<ILayer>();

        this.CurrentLayers = new ObservableCollection<ILayer>(); 
    }
     

    public void Add(ILayer layer, double inverseMapScale)
    {
        if (allLayers.Any(l => l == layer))
            return;

        layer.OnVisibilityChanged -= RefreshLayerVisibility;
        layer.OnVisibilityChanged += RefreshLayerVisibility;

        UpdateIsInRange(layer, inverseMapScale);

        try
        {
            // 1401.12.05
            if (layer.ParentLayerId != Guid.Empty)
            {
                // do not add it to the current layers
            }
            else if (layer.ZIndex > CurrentLayers.Count || layer.ZIndex < 1)
            {
                this.CurrentLayers.Add(layer);
            }
            else
            {
                this.CurrentLayers.Insert(layer.ZIndex, layer);
            }
        }
        catch (Exception ex)
        {
            this.CurrentLayers.Add(layer);
        }


        //98.01.20
        //layer.ZIndex = this.allLayers.Count(i => !i.Type.HasFlag(LayerType.Complex) && !i.Type.HasFlag(LayerType.Drawing));

        this.allLayers.Add(layer);

        this.allLayers = GetOrderedLayers();

        ArrangeZIndex();

        if (layer.IsGroupLayer)
        {
            foreach (var item in layer.SubLayers)
            {
                Add(item, inverseMapScale);
            }
        }
    }

    public void Remove(ILayer layer, bool forceRemove) => Clear(lyr => lyr == layer, forceRemove);

    public void Remove(string layerName, bool forceRemove) => Clear(layer => layer?.LayerName == layerName, forceRemove);

    internal void Remove(LayerType type, bool forceRemove) => Clear(layer => layer.Type.HasFlag(type), forceRemove);

    public void Remove(Predicate<ILayer> rule, bool forceRemove) => Clear(rule, forceRemove);

    public void RemoveTile(string providerFullName, bool forceRemove)
    {
        Clear(layer => (layer as TileServiceLayer)?.ProviderFullName?.ToUpper() == providerFullName?.ToUpper() , forceRemove);
    }


    private void Clear(Predicate<ILayer> rule, bool forceRemove)
    {
        //for (int i = CurrentLayers.Count - 1; i >= 0; i--)
        //{
        //    if (CurrentLayers[i].IsGroupLayer)
        //    {
        //        Clear(CurrentLayers[i].SubLayers, rule, forceRemove);
        //    }
        //    else if ((forceRemove || CurrentLayers[i]?.CanUserDelete == true) && rule(CurrentLayers[i]))
        //    {
        //        this.CurrentLayers.Remove(CurrentLayers[i]);
        //    }

        //    if (CurrentLayers[i].IsGroupLayer && CurrentLayers[i].SubLayers.Count ==0)
        //    {
        //        this.CurrentLayers
        //    }
        //}
        Clear(this.CurrentLayers, rule, forceRemove);

        this.allLayers.RemoveAll(layer => (forceRemove || layer?.CanUserDelete == true) && rule(layer));
    }

    private void Clear(ObservableCollection<ILayer> layers, Predicate<ILayer> rule, bool forceRemove)
    {
        for (int i = layers.Count - 1; i >= 0; i--)
        {
            var layer = layers[i];

            if (layer.IsGroupLayer)
            {
                Clear(layer.SubLayers, rule, forceRemove);
            }
            else if ((forceRemove || layer.CanUserDelete == true) && rule(layer))
            {
                layers.Remove(layer);
            }

            // remove group layer if all childs have been removed.
            // e.g. in the case of DXF group layer.
            if (layer.IsGroupLayer && layer.SubLayers.Count==0)
            {
                layers.Remove(layer);
            }
        }
    }

    public void Clear()
    {
        this.allLayers.Clear();

        for (int i = CurrentLayers.Count - 1; i >= 0; i--)
        {
            this.CurrentLayers.Remove(CurrentLayers[i]);
        }
    }

    public List<ILayer> GetOrderedLayers()
    {
        return allLayers.OrderBy(i => i.Type.HasFlag(LayerType.RightClickOption))
                                     .ThenBy(i => i.Type.HasFlag(LayerType.MoveableItem))
                                     .ThenBy(i => i.Type.HasFlag(LayerType.EditableItem))
                                     .ThenBy(i => i.Type.HasFlag(LayerType.Complex))
                                     .ThenBy(e => e.Type.HasFlag(LayerType.Selection))
                                     .ThenBy(i => i.Type.HasFlag(LayerType.Drawing))
                                     .ThenByDescending(i => i.Type.HasFlag(LayerType.BaseMap))
                                     .ThenBy(i => i.ZIndex)
                                     .ToList();
    }

    private void ArrangeZIndex()
    {
        var orderedLayers = GetOrderedLayers();

        for (int i = 0; i < orderedLayers.Count(); i++)
        {
            orderedLayers[i].ZIndex = i;
        }
    }

    public IEnumerable<ILayer> UpdateAndGetLayers(double inverseMapScale, RenderMode rendering)
    {
        //Debug.WriteLine($"LayerManager; {DateTime.Now.ToLongTimeString()}; UpdateAndGetLayers called");

        ArrangeZIndex();

        var newLayers = allLayers.Where(l => l.VisibleRange.IsInRange(inverseMapScale) && l.RenderMode == rendering)
                                    .OrderByDescending(i => i.Type == LayerType.BaseMap)
                                    //.ThenByDescending(i => i.Type == LayerType.Raster)
                                    //.ThenByDescending(i => i.Type == LayerType.ImagePyramid)
                                    //.ThenByDescending(i => i.Type == LayerType.VectorLayer)
                                    //.ThenByDescending(i => i.Type == LayerType.FeatureLayer)
                                    //.ThenByDescending(i => i.Type == LayerType.Selection)
                                    .ThenBy(i => i.Type == LayerType.RightClickOption)
                                    .ThenBy(i => i.Type == LayerType.MoveableItem)
                                    .ThenBy(i => i.Type == LayerType.EditableItem)
                                    .ThenBy(i => i.Type == LayerType.Complex)
                                    .ThenBy(i => i.Type == LayerType.Drawing)
                                    .ThenBy(i => i.Type == LayerType.GroupLayer)
                                    .ThenBy(i => i.ZIndex);

        //if (rendering == RenderingApproach.Default)
        //{
        //    System.Diagnostics.Debug.WriteLine($"UpdateAndGetLayers layercounts:{  newLayers.Count()}");
        //}

        var toBeRemovedLayers = this.CurrentLayers.Where(i => i.RenderMode == rendering && newLayers.All(l => l.LayerId != i.LayerId)).ToList();

        for (int i = 0; i < toBeRemovedLayers.Count; i++)
        {
            this.CurrentLayers.Remove(toBeRemovedLayers[i]);
        }

        var toBeAdded = newLayers.Where(i => i.RenderMode == rendering && this.CurrentLayers.All(l => l.LayerId != i.LayerId)).ToList();

        for (int i = 0; i < toBeAdded.Count; i++)
        {
            // 1401.12.05
            // child layers are already shown in parent layer hierarchy
            if (toBeAdded[i].ParentLayerId != Guid.Empty)
                continue;

            this.CurrentLayers.Add(toBeAdded[i]);
        }

        //Debug.WriteLine($"LayerManager; {DateTime.Now.ToLongTimeString()}; UpdateAndGetLayers finished");

        return newLayers;
    }

    internal void UpdateIsInRange(double inverseMapScale)
    {
        foreach (var layer in allLayers)
        {
            UpdateIsInRange(layer, inverseMapScale);
        }
    }

    private void UpdateIsInRange(ILayer layer, double inverseMapScale)
    {
        layer.IsInScaleRange = layer.VisibleRange.IsInRange(inverseMapScale);
    }

    public BoundingBox CalculateCurrentMapExtent()
    {
        return CalculateExtent(this.CurrentLayers);
    }

    public BoundingBox CalculateMapExtent()
    {
        return CalculateExtent(this.allLayers);
    }

    private BoundingBox CalculateExtent(IList<ILayer> layers)
    {
        if (layers == null || layers.Count < 1)
        {
            return new BoundingBox(double.NaN, double.NaN, double.NaN, double.NaN);
        }

        var extents = layers.Where(i => !i.Type.HasFlag(LayerType.BaseMap) && !i.Type.HasFlag(LayerType.ImagePyramid))
                            .Select(i => i.Extent)
                            .Where(i => !double.IsNaN(i.Width) && !double.IsNaN(i.Height));

        return BoundingBox.GetMergedBoundingBox(extents);
    }

    private void RefreshLayerVisibility(object sender, EventArgs e)
    {
        this.RequestRefreshVisibility?.Invoke(sender as BaseLayer);
    }

    public Action<BaseLayer>? RequestRefreshVisibility;
     
    //internal void ChangeLayerZIndex(ILayer layer, int newZIndex)
    //{

    //}
}
