using System;

using IRI.Maptor.Sta.Common.Primitives;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.Layers;

public class ActiveExtentLayer : BaseLayer
{
    public override LayerType Type => LayerType.ActiveExtent;

    private BoundingBox _activeExtent;
    public BoundingBox ActiveExtent
    {
        get => _activeExtent;
        private set
        {
            _activeExtent = value;
            RaisePropertyChanged(nameof(Extent));
        }
    }

    public override BoundingBox Extent { get => base.Extent; protected set => throw new NotImplementedException(); }

    public ActiveExtentLayer(BoundingBox extent)
    {
        this.ActiveExtent = extent;
    }
}
