using IRI.Maptor.Jab.Common.Cartography.Symbologies;
using IRI.Maptor.Jab.Common.Events;
using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common;

public abstract class SymbolizableLayer : BaseLayer
{
    //public event EventHandler<CustomEventArgs<VisualParameters>>? OnLabelChanged;

    protected List<VisualParameters> _visualParameters = [];

    private List<ISymbolizer> _symbolizers = [];

    public IReadOnlyCollection<ISymbolizer> Symbolizers
    {
        get => _symbolizers.AsReadOnly();
    }

    public void SetSymbolizer(ISymbolizer symbolizer)
    {
        if (symbolizer.Param is not null)
        {
            //symbolizer.Param.OnIsOnChanged -= RaiseVisibilityChanged;
            //symbolizer.Param.OnIsOnChanged += RaiseVisibilityChanged;

            this._visualParameters.Add(symbolizer.Param);
        }

        this._symbolizers.Add(symbolizer);

        RaisePropertyChanged(nameof(HasMultiSymbolizers));
        RaisePropertyChanged(nameof(DefaultSymbology));
    }

    public override bool HasMultiSymbolizers => Symbolizers?.Count(s => s is not LabelSymbolizer) > 1;

    public VisualParameters? DefaultSymbology => _visualParameters?.FirstOrDefault(/*s => !s.HasLabelParameters*/ );

    public VisualParameters? DefaultLabel => _visualParameters?.FirstOrDefault(s => s.HasLabelParameters);

    public VisualParameters GetMainOrDefaultSymbology() => _symbolizers.FirstOrDefault(v => v is SimpleSymbolizer)?.Param ?? VisualParameters.CreateNew();
     
    public VisualParameters? GetDefaultLabelParams() => _symbolizers.FirstOrDefault(v => v is LabelSymbolizer)?.Param ?? null;

    public abstract Task<FeatureSet<Point>> GetFeatureSet(BoundingBox mapExtent, double mapScale);
     
    public async Task<List<Feature<Point>>> GetRenderReadyFeatures(BoundingBox mapExtent, double mapScale, double screenWidth, double screenHeight)
    {
        var feature = await this.GetFeatureSet(mapExtent, mapScale);

        if (feature is null || feature.HasNoGeometry())
            return new List<Feature<Point>>();

        //double xScale = imageWidth / mapExtent.Width;
        //double yScale = imageHeight / mapExtent.Height;
        //double scale = xScale > yScale ? yScale : xScale;
        //Func<Point, Point> mapToScreen = new Func<Point, Point>(p => new Point((p.X - mapExtent.XMin) * scale, -(p.Y - mapExtent.YMax) * scale));
        var mapToScreen = Utility.CreateMapToScreenMapFunc(mapExtent, screenWidth, screenHeight);

        return feature.Transform(mapToScreen).Features.ToList();
    }
}
