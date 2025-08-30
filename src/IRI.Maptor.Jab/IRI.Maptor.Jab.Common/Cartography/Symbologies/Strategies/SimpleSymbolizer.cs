using System;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives; 

namespace IRI.Maptor.Jab.Common.Cartography.Symbologies;

public class SimpleSymbolizer : SymbolizerBase
{
    public override SymbologyType Type { get => SymbologyType.Single; }

    //private VisualParameters _param;

    //public VisualParameters Param
    //{
    //    get { return _param; }
    //    set
    //    {
    //        _param = value;
    //        RaisePropertyChanged();
    //    }
    //}

    public SimpleSymbolizer(VisualParameters visualParameters)
    {
        Param = visualParameters;
    }

    public SimpleSymbolizer(Func<Feature<Point>, bool> filter, VisualParameters visualParameters):this(visualParameters)
    {
        this.IsFilterPassed = filter;
    }
      
}
