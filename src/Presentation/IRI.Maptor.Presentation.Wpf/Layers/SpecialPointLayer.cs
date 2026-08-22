using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Models;

namespace IRI.Maptor.Presentation.Wpf.Layers;

public class SpecialPointLayer : BaseLayer
{
    public Action<System.Collections.Specialized.NotifyCollectionChangedEventArgs> HandleCollectionChanged;

    private ObservableCollection<Locateable> _items;
    public ObservableCollection<Locateable> Items
    {
        get => _items;
        set
        {
            _items = value;
            RaisePropertyChanged();
        }
    }

    public override BoundingBox Extent
    {
        get => Items.Count < 1 ? BoundingBox.NaN : BoundingBox.CalculateBoundingBox(Items.Select(i => new Point(i.X, i.Y)));
        protected set => throw new NotImplementedException();
    }

    private bool _alwaysTop;

    public bool AlwaysTop
    {
        get => _alwaysTop;
        set
        {
            _alwaysTop = value;
            RaisePropertyChanged();
        }
    }

    private LayerType _type;
    public override LayerType Type => _type;

    public SpecialPointLayer(string name, Locateable item, double opacity = 1, ScaleInterval? visibleRange = null, LayerType type = LayerType.Complex)
        : this(name, new List<Locateable>() { item }, opacity, visibleRange, type)
    {

    }

    public SpecialPointLayer(string name, IEnumerable<Locateable> items, double opacity = 1, ScaleInterval? visibleRange = null, LayerType type = LayerType.Complex)
    {
        LayerId = Guid.NewGuid();


        //if (type.HasFlag(LayerType.Complex) ||
        //    type.HasFlag(LayerType.RightClickOption) ||
        //    type.HasFlag(LayerType.GridAndGraticule) ||
        //    type.HasFlag(LayerType.MoveableItem) ||
        //    type.HasFlag(LayerType.EditableItem))
        if (type == LayerType.Complex ||
            type == LayerType.RightClickOption ||
            type == LayerType.GridAndGraticule ||
            //type == LayerType.MoveableItem ||
            type == LayerType.EditableItem)
            _type = type;
        else
            throw new NotImplementedException();

        LayerName = name;

        ZIndex = int.MaxValue;

        Items = new ObservableCollection<Locateable>();

        Items.CollectionChanged -= Items_CollectionChanged;
        Items.CollectionChanged += Items_CollectionChanged;

        if (visibleRange == null)
        {
            VisibleRange = ScaleInterval.All;
        }
        else
        {
            VisibleRange = visibleRange;
        }

        //this.VisualParameters = new VisualParameters(null, null, 0, opacity, System.Windows.Visibility.Visible);

        if (items == null)
        {
            return;
        }

        foreach (var item in items)
        {
            Items.Add(item);
        }

    }

    private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (HandleCollectionChanged == null)
            return;

        HandleCollectionChanged(e);
    }

    public Locateable Get(System.Windows.FrameworkElement element)
    {
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i].Element == element)
            {
                return Items[i];
            }
        }

        return null;
    }

    public IEnumerable<Locateable> Get(Guid id)
    {
        return Items.Where(i => i.Id == id);
    }

    public void Remove(Guid id)
    {
        if (Items.Count(i => i.Id == id) > 1)
        {
            throw new NotImplementedException("more than one locateable found");
        }

        for (int i = Items.Count - 1; i >= 0; i--)
        {
            if (Items[i].Id == id)
            {
                Items.RemoveAt(i);
            }
        }
    }

    public void Remove(IPoint[] points)
    {
        for (int i = Items.Count - 1; i >= 0; i--)
        {
            for (int j = 0; j < points.Length; j++)
            {
                if (Items[i].X == points[j].X && Items[i].Y == points[j].Y)
                {
                    Items.Remove(Items[i]);

                    break;
                }
            }

        }
    }

    public void SelectLocatable(System.Windows.FrameworkElement element)
    {
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i].IsSelected = Items[i].Element == element;
        }

        RequestSelectedLocatableChanged?.Invoke(FindSelectedLocatable(), FindSelectedLocatableIndex());
    }

    public void SelectLocatable(int index)
    {
        bool change = false;

        for (int i = 0; i < Items.Count; i++)
        {
            var newValue = index == i;

            if (newValue != Items[i].IsSelected)
            {
                Items[i].IsSelected = newValue;
                change = true;
            }

            //Items[i].IsSelected = index == i;
        }

        if (change)
            RequestSelectedLocatableChanged?.Invoke(FindSelectedLocatable(), FindSelectedLocatableIndex());
    }

    public Locateable? FindSelectedLocatable()
    {
        return Items.FirstOrDefault(l => l.IsSelected);

        //for (int i = 0; i < Items.Count; i++)
        //{
        //    if (Items[i].IsSelected)
        //        return Items[i];
        //}

        //return null;
    }

    public int FindSelectedLocatableIndex()
    {
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i].IsSelected)
            {
                return i;
            }
        }

        return -1;
    }

    public void SelectNextLocatable()
    {
        if (!(Items?.Count > 0))
            return;

        var current = FindSelectedLocatableIndex();

        if (current < 0)
            SelectLocatable(0);

        var next = current + 1 < Items.Count ? current + 1 : 0;

        SelectLocatable(next);
    }

    public void SelectPreviousLocatable()
    {
        if (!(Items?.Count > 0))
            return;

        var current = FindSelectedLocatableIndex();

        if (current < 0)
            SelectLocatable(0);

        var next = current - 1 >= 0 ? current - 1 : Items.Count - 1;

        SelectLocatable(next);
    }

    public Action<Locateable?, int> RequestSelectedLocatableChanged;


    #region /*Overrides*/

    public /*override*/ Task<FeatureSet<Point>> GetFeatureSet(BoundingBox mapExtent, double mapScale)
    {
        if (Items.IsNullOrEmpty())
            return Task.FromResult(FeatureSet<Point>.Empty);

        // Filter points within bounding box
        var points = Items
            .Where(item => mapExtent.Contains(new Point(item.X, item.Y)))
            .Select(item => new Point(item.X, item.Y))
            .ToList();

        if (points.Count == 0)
            return Task.FromResult(FeatureSet<Point>.Empty);

        // Create single Point or MultiPoint geometry
        Geometry<Point> geometry;

        if (points.Count == 1)
        {
            geometry = Geometry<Point>.Create(points, GeometryType.Point, SridHelper.WebMercator);
        }
        else
        {
            geometry = Geometry<Point>.Create(points, GeometryType.MultiPoint, SridHelper.WebMercator);
        }

        //return new List<Feature<Point>> { new Feature<Point>(geometry) };

        return Task.FromResult(FeatureSet<Point>.Create($"{nameof(SpecialPointLayer)}-{LayerId}", [geometry.AsFeature()]));
        //return new List<Feature<Point>> { new Feature<Point>(geometry) };
    }

    #endregion
}
