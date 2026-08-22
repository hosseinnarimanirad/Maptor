using System;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Presentation.Wpf.ViewModels.Map;
using IRI.Maptor.Presentation.Core;

namespace IRI.Maptor.Presentation.Wpf.Layers;

public class ActiveExtentLayer : BaseLayer
{
    public override LayerType Type => LayerType.ActiveExtent;

    public override string TocGroup
    {
        get => LegendViewModel.NoneTocGroup;
        set => throw new NotImplementedException();
    }
    //// check if it is needed
    //private BoundingBox _activeExtent;
    //public BoundingBox ActiveExtent
    //{
    //    get => _activeExtent;
    //    private set
    //    {
    //        _activeExtent = value;
    //        RaisePropertyChanged(nameof(Extent));
    //    }
    //}

    //public override BoundingBox Extent { get => base.Extent; protected set => throw new NotImplementedException(); }

    public ActiveExtentLayer(BoundingBox extent)
    {
        this.Extent = extent;
    }
}
