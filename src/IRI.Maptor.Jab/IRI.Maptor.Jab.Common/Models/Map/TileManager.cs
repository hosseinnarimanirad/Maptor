using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IRI.Maptor.Sta.Spatial.Model;

namespace IRI.Maptor.Jab.Common.Models;

public class TileManager : Notifier
{
    //public event EventHandler<CustomEventArgs<List<Tile>>> OnTilesAdded;

    //public event EventHandler<CustomEventArgs<List<Tile>>> OnTilesRemoved;

    private ObservableCollection<Tile> _currentTiles;

    public TileManager()
    {
        this._currentTiles = new ObservableCollection<Tile>();

        //this._currentTiles.CollectionChanged += _currentTiles_CollectionChanged;
    }

    //private void _currentTiles_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    //{
    //    switch (e.Action)
    //    {
    //        case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
    //            break;
    //        case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
    //            break;
    //        case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
    //        case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
    //        case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
    //        default:
    //            throw new NotImplementedException();
    //    }
    //}

    public void Update(List<Tile> newTiles)
    {
        var toBeRemoved = this._currentTiles.Except(newTiles).ToList();

        var toBeAdded = newTiles.Except(this._currentTiles).ToList();

        for (int i = 0; i < toBeRemoved.Count; i++)
        {
            this._currentTiles.Remove(toBeRemoved.ElementAt(i));
        }

        for (int i = 0; i < toBeAdded.Count; i++)
        {
            this._currentTiles.Add(toBeAdded.ElementAt(i));
        }

        //if (toBeAdded.Count > 0 && OnTilesAdded != null)
        //{
        //    OnTilesAdded(this, new CustomEventArgs<List<Tile>>(toBeAdded));
        //}

        //if (toBeRemoved.Count > 0 && OnTilesRemoved != null)
        //{
        //    OnTilesRemoved(this, new CustomEventArgs<List<Tile>>(toBeRemoved));
        //}
    }

    public Tile Find(TileInfo info)
    {
        foreach (var item in _currentTiles)
        {
            if ((item as TileInfo).Equals(info))
            {
                return item;
            }
        }

        return null;
    }

    public void TryAdd(TileInfo tile)
    {
        if (this._currentTiles.Count(i => i.ToShortString() == tile.ToShortString()) < 1)
        {
            this._currentTiles.Add(new Tile(tile.RowNumber, tile.ColumnNumber, tile.ZoomLevel));
        }
    }
}
