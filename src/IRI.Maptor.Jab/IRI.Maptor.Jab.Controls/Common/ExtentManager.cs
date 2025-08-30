
using IRI.Maptor.Sta.Common.Model;
using IRI.Maptor.Jab.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IRI.Maptor.Sta.Spatial.Model;
using IRI.Maptor.Jab.Common.Events;

namespace IRI.Maptor.Jab.Controls.Model;

public class ExtentManager : Notifier
{
    private ObservableCollection<TileInfo> _currentTiles;

    private readonly object locker = new object();

    public ExtentManager()
    {
        this._currentTiles = new ObservableCollection<TileInfo>();
    }

    public void Update(List<TileInfo> newTiles)
    {
        var toBeRemoved = new List<TileInfo>();

        var toBeAdded = new List<TileInfo>();

        lock (locker)
        {
            if (newTiles == null)
            {
                toBeRemoved = this._currentTiles.ToList();
            }
            else
            {
                toBeAdded = newTiles.Except(this._currentTiles).ToList();

                toBeRemoved = this._currentTiles.Except(newTiles).ToList();
            }

            for (int i = 0; i < toBeRemoved.Count; i++)
            {
                this._currentTiles.Remove(toBeRemoved.ElementAt(i));
            }

            for (int i = 0; i < toBeAdded.Count; i++)
            {
                this._currentTiles.Add(toBeAdded.ElementAt(i));
            }
        }

        if (toBeAdded.Count > 0 && OnTilesAdded != null)
        {
            OnTilesAdded?.Invoke(this, new CustomEventArgs<List<TileInfo>>(toBeAdded));
        }

        if (toBeRemoved.Count > 0 && OnTilesRemoved != null)
        {
            OnTilesRemoved?.Invoke(this, new CustomEventArgs<List<TileInfo>>(toBeRemoved));
        }

    }

    public event EventHandler<CustomEventArgs<List<TileInfo>>> OnTilesAdded;

    public event EventHandler<CustomEventArgs<List<TileInfo>>> OnTilesRemoved;
}
