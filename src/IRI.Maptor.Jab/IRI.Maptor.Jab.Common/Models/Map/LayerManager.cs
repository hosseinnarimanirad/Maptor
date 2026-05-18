using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common.Layers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Jab.Common.ViewModels.Map;
using IRI.Maptor.Sta.Ogc.WMS;

namespace IRI.Maptor.Jab.Common.Models;

public class LayerManager : Notifier
{
    public Action<BaseLayer>? RequestRefreshVisibility;

    public Action<IEnumerable<ILayer>> RequestUpdateLayerTocOrder;

    /// <summary>
    /// Optional. When set, passed to LoadAsync when loading layer data. Used to cancel loads on sign out.
    /// </summary>
    public CancellationToken _loadCancellationToken { get; set; }

    private List<ILayer> _allLayers;

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
        _allLayers = new List<ILayer>();

        CurrentLayers = new ObservableCollection<ILayer>();
    }

    public bool HasLayer(Guid layerGuid) => _allLayers.Any(l => l.LayerId == layerGuid);

    public void Add(LegendViewModel legendViewModel, ILayer layer, double inverseMapScale)
    {
        if (_allLayers.Any(l => l == layer))
            return;

        layer.OnVisibilityChanged -= RefreshLayerVisibility;
        layer.OnVisibilityChanged += RefreshLayerVisibility;

        layer.OnLayerInitilized -= Layer_OnLayerInitilized;
        layer.OnLayerInitilized += Layer_OnLayerInitilized;

        UpdateIsInRange(layer, inverseMapScale);

        try
        {
            // 1401.12.05
            //if (layer.ParentLayerId != Guid.Empty)
            if (!layer.IsRootLayer)
            {
                // todo: consider using layer.Parent
                var parentLayer = _allLayers.FirstOrDefault(l => l.LayerId == layer.ParentLayerId);

                if (parentLayer != null && parentLayer != layer.Parent)
                {
                    // why this happened?
                }

                if (parentLayer != null && parentLayer.IsGroupLayer && parentLayer.SubLayers?.Contains(layer) == false)
                {
                    parentLayer.SubLayers.Insert(0, layer);
                }
                // do not add it to the current layers
            }
            else if (layer.ZIndex > CurrentLayers.Count || layer.ZIndex < 1)
            {
                CurrentLayers.Insert(0, layer);
            }
            else
            {
                CurrentLayers.Insert(layer.ZIndex, layer);
            }
        }
        catch (Exception ex)
        {
            CurrentLayers.Add(layer);
        }

        _allLayers.Add(layer);

        // ********************************************************************
        // update toc order values
        ArrangeTocOrder(legendViewModel, layer);

        // ********************************************************************

        _allLayers = GetOrderedLayers();

        ArrangeZIndex();

        if (layer.IsGroupLayer)
        {
            foreach (var item in layer.SubLayers)
            {
                Add(legendViewModel, item, inverseMapScale);
            }
        }
        else
        {
            if (layer is VectorLayer vl && vl.DataSource is IDataSource ds && !ds.IsLoaded)
            {
                vl.RequestRefreshWhenDataLoaded = l => RequestRefreshVisibility?.Invoke(l as BaseLayer);
                _ = ds.LoadAsync(_loadCancellationToken);
            }
            else if (layer is RasterLayer rl && rl.DataSource is IDataSource rds && !rds.IsLoaded)
            {
                rl.RequestRefreshWhenDataLoaded = l => RequestRefreshVisibility?.Invoke(l as BaseLayer);
                _ = rds.LoadAsync(_loadCancellationToken);
            }
        }
    }

    public void Remove(ILayer layer, bool forceRemove, bool keepEmptyParentGroup) => Remove(lyr => lyr == layer, forceRemove, keepEmptyParentGroup);

    //public void Remove(Predicate<ILayer> rule, bool forceRemove, bool keepEmptyParentGroup) => Clear(rule, forceRemove, keepEmptyParentGroup);

    public void RemoveTile(string providerFullName, bool forceRemove)
    {
        Remove(layer => (layer as TileServiceLayer)?.ProviderFullName?.ToUpper() == providerFullName?.ToUpper(), forceRemove, false);
    }


    public void Remove(Predicate<ILayer> rule, bool forceRemove, bool keepEmptyParentGroup)
    {
        Remove(CurrentLayers, rule, forceRemove, keepEmptyParentGroup);

        _allLayers.RemoveAll(layer => (forceRemove || layer?.CanUserDelete == true) && rule(layer));
    }

    private void Remove(ObservableCollection<ILayer> layers, Predicate<ILayer> rule, bool forceRemove, bool keepEmptyParentGroup)
    {
        for (int i = layers.Count - 1; i >= 0; i--)
        {
            var layer = layers[i];

            if (layer.IsGroupLayer)
            {
                Remove(layer.SubLayers, rule, forceRemove, keepEmptyParentGroup);
            }
            else if ((forceRemove || layer.CanUserDelete == true) && rule(layer))
            {
                layers.Remove(layer);
            }

            // in the case of chaning symbology layer is removed and
            // added agian. this behavior cause problem with dxf files
            // after change symbology they cannot apear because their
            // group layer has been removed
            if (keepEmptyParentGroup)
                continue;

            // remove group layer if all childs have been removed.
            // e.g. in the case of DXF group layer.
            if (layer.IsGroupLayer && layer.SubLayers.Count == 0)
            {
                layers.Remove(layer);
            }
        }
    }

    //public void Clear()
    //{
    //    this._allLayers.Clear();

    //    for (int i = CurrentLayers.Count - 1; i >= 0; i--)
    //    {
    //        this.CurrentLayers.Remove(CurrentLayers[i]);
    //    }
    //}


    #region Arrange

    public List<ILayer> GetOrderedLayers()
    {
        return _allLayers.OrderBy(i => i.Type == LayerType.RightClickOption)
                                     //.ThenBy(i => i.Type == (LayerType.MoveableItem))
                                     .ThenBy(i => i.Type == LayerType.EditableItem)
                                     .ThenBy(i => i.Type == LayerType.Complex)
                                     .ThenBy(e => e.Type == LayerType.Highlight)
                                     .ThenBy(e => e.Type == LayerType.Selection)
                                     .ThenBy(i => i.Type == LayerType.Drawing)
                                     .ThenByDescending(i => i.Type == LayerType.BaseMap)
                                     .ThenBy(i => i.ZIndex)
                                     .ToList();
    }

    private void ArrangeTocOrder(LegendViewModel legendViewModel, ILayer layer)
    {
        var layers = layer.Parent != null ?
            layer.Parent.SubLayers :
            _allLayers.Where(l => l.Parent == null);

        if (layers.Any(l => l.TocOrder == layer.TocOrder))
        {
            var maxToc = layers.Select(l => l.TocOrder).DefaultIfEmpty(0).Max();

            layer.TocOrder = maxToc + 1;

            if (legendViewModel?.RequestRefreshView is not null)
            {
                legendViewModel.RequestRefreshView.Invoke();
            }
        }

        // add this code so when adding new layers (e.g. DXF)
        // its move up/down buttons have correct enabled/disabled value
        RequestUpdateLayerTocOrder?.Invoke(layers);
    }

    private void ArrangeZIndex()
    {
        var orderedLayers = GetOrderedLayers().Where(l => l.Parent is null).OrderBy(l => l.TocOrder).ToList();

        var zindex = 1;

        for (int i = 0; i < orderedLayers.Count; i++)
        {
            ArrangeZIndex(orderedLayers[i], ref zindex);
        }

        //for (int i = 0; i < orderedLayers.Count(); i++)
        //{
        //    orderedLayers[i].ZIndex = i;
        //}
    }

    private void ArrangeZIndex(ILayer layer, ref int zIndex)
    {
        layer.ZIndex = zIndex;

        zIndex++;

        if (layer.IsGroupLayer)
        {
            var subLayers = layer.SubLayers.OrderBy(l => l.TocOrder).ToList();

            for (int i = 0; i < subLayers.Count; i++)
            {
                ArrangeZIndex(subLayers[i], ref zIndex);
            }
        }
    }

    public IEnumerable<ILayer> UpdateAndGetLayers(double inverseMapScale, RenderMode rendering)
    {
        ArrangeZIndex();

        var newLayers = _allLayers.Where(l => l.VisibleRange.IsInRange(inverseMapScale) && l.RenderMode == rendering)
                                    .OrderByDescending(i => i.Type == LayerType.BaseMap)
                                    //.ThenByDescending(i => i.Type == LayerType.Raster)
                                    //.ThenByDescending(i => i.Type == LayerType.ImagePyramid)
                                    //.ThenByDescending(i => i.Type == LayerType.VectorLayer)
                                    //.ThenByDescending(i => i.Type == LayerType.FeatureLayer)
                                    //.ThenByDescending(i => i.Type == LayerType.Selection)
                                    .ThenBy(i => i.Type == LayerType.RightClickOption)
                                    //.ThenBy(i => i.Type == LayerType.MoveableItem)
                                    .ThenBy(i => i.Type == LayerType.EditableItem)
                                    .ThenBy(i => i.Type == LayerType.Complex)
                                    .ThenBy(i => i.Type == LayerType.Drawing)
                                    .ThenBy(i => i.Type == LayerType.Selection)
                                    .ThenBy(i => i.Type == LayerType.Highlight)
                                    .ThenBy(i => i.Type == LayerType.GroupLayer)
                                    .ThenBy(i => i.ZIndex);

        var toBeRemovedLayers = CurrentLayers.Where(i => i.RenderMode == rendering && newLayers.All(l => l.LayerId != i.LayerId)).ToList();

        for (int i = 0; i < toBeRemovedLayers.Count; i++)
        {
            CurrentLayers.Remove(toBeRemovedLayers[i]);
        }

        var toBeAdded = newLayers.Where(i => i.RenderMode == rendering && CurrentLayers.All(l => l.LayerId != i.LayerId)).ToList();

        for (int i = 0; i < toBeAdded.Count; i++)
        {
            // 1401.12.05
            // child layers are already shown in parent layer hierarchy
            //if (toBeAdded[i].ParentLayerId != Guid.Empty)            
            //    continue;

            //this.CurrentLayers.Add(toBeAdded[i]);

            // child layers are already shown in parent layer hierarchy
            if (toBeAdded[i].IsRootLayer)
                CurrentLayers.Add(toBeAdded[i]);

        }

        return newLayers;
    }

    internal void UpdateIsInRange(double inverseMapScale)
    {
        foreach (var layer in _allLayers)
        {
            UpdateIsInRange(layer, inverseMapScale);
        }
    }

    private void UpdateIsInRange(ILayer layer, double inverseMapScale)
    {
        layer.IsInScaleRange = layer.VisibleRange.IsInRange(inverseMapScale);
    }

    #endregion


    #region Calculate Extent

    public BoundingBox CalculateCurrentMapExtent()
    {
        return CalculateExtent(CurrentLayers);
    }

    public BoundingBox CalculateMapExtent()
    {
        return CalculateExtent(_allLayers);
    }

    private BoundingBox CalculateExtent(IList<ILayer> layers)
    {
        if (layers.IsNullOrEmpty())
            return BoundingBox.NaN;

        var extents = layers.Where(i => i.Type != LayerType.BaseMap && i.Type != LayerType.ImagePyramid)
                            .Select(i => i.Extent)
                            .Where(i => !double.IsNaN(i.Width) && !double.IsNaN(i.Height));

        return BoundingBox.GetMergedBoundingBox(extents);
    }

    #endregion

    private void RefreshLayerVisibility(object sender, EventArgs e) => RequestRefreshVisibility?.Invoke(sender as BaseLayer);

    private void Layer_OnLayerInitilized(object? sender, ILayer e) => RequestRefreshVisibility?.Invoke(sender as BaseLayer);
}
