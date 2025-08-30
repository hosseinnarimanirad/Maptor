using System;
using System.Collections.Generic;
using System.Collections.ObjectModel; 

using IRI.Maptor.Jab.Common.Models.Legend;

namespace IRI.Maptor.Jab.Common.Presenters;

public class LegendPresenter : Notifier
{
    public LegendPresenter(IEnumerable<LegendItem> layers)
    {
        if (layers == null)
        {
            Layers = new ObservableCollection<LegendItem>();
        }
        else
        {
            Layers = new ObservableCollection<LegendItem>(layers);
        }

    }

    private ObservableCollection<LegendItem> _layers;

    public ObservableCollection<LegendItem> Layers
    {
        get { return _layers; }
        set
        {
            _layers = value;
            RaisePropertyChanged();

            PrepareEvents();
        }
    }

    private void PrepareEvents()
    {
        Layers.CollectionChanged -= Layers_CollectionChanged;
        Layers.CollectionChanged += Layers_CollectionChanged;

        foreach (var item in Layers)
        {
            SetEvents(item);
        }
    }

    private void Layers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        foreach (var item in Layers)
        {
            SetEvents(item);
        }
    }

    private void SetEvents(LegendItem item)
    {
        item.OnRequestForSelectByDrawing -= RequestForSelectByDrawing;
        item.OnRequestForSelectByDrawing += RequestForSelectByDrawing; ;

        item.OnRequestShowAll -= Item_OnRequestShowAll;
        item.OnRequestShowAll += Item_OnRequestShowAll;

        if (item.SubLayers == null)
            return;

        foreach (var subLayer in item.SubLayers)
        {
            SetEvents(subLayer);
        }
    }

    private void Item_OnRequestShowAll(object sender, LegendItemEventArgs e)
    {
        OnRequestShowAll?.Invoke(this, e);
    }

    private void RequestForSelectByDrawing(object sender, LegendItemEventArgs e)
    {
        OnRequestSelectByDrawing?.Invoke(this, e);
    }


    #region Events

    public event EventHandler<LegendItemEventArgs> OnRequestSelectByDrawing;

    public event EventHandler<LegendItemEventArgs> OnRequestShowAll;

    #endregion
}
