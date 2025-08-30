using System; 

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Jab.Common.Cartography.Symbologies;

public abstract class SymbolizerBase : Notifier, ISymbolizer
{
    public abstract SymbologyType Type { get; }

    public double? MinScaleDenominator { get; set; }

    public double? MaxScaleDenominator { get; set; }


    private VisualParameters? _param;
    public VisualParameters? Param
    {
        get { return _param; }
        set
        {
            _param = value;
            RaisePropertyChanged();

            //this.OnLabelChanged?.Invoke(this, new CustomEventArgs<LabelParameters>(value));
        }
    }

    private Func<Feature<Point>, bool> _filter = f => true;
    public virtual Func<Feature<Point>, bool> IsFilterPassed
    {
        get => _filter;
        set => _filter = value;
    }
     
    public bool IsInScaleRange(double scale)
    {
        return scale >= (MinScaleDenominator ?? 0) &&
                scale <= (MaxScaleDenominator ?? double.MaxValue);
    }
}
