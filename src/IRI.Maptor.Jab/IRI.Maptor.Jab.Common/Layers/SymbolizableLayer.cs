using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Ogc.SLD;
using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;
using IRI.Maptor.Jab.Common.Cartography.Legend;


namespace IRI.Maptor.Jab.Common.Layers;

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

            _visualParameters.Add(symbolizer.Param);
        }

        _symbolizers.Add(symbolizer);

        _symbologyLegend = null;

        RaisePropertyChanged(nameof(HasMultiSymbolizers));
        RaisePropertyChanged(nameof(DefaultSymbology));
        RaisePropertyChanged(nameof(SymbologyLegend));
    }

    public override bool HasMultiSymbolizers => Symbolizers?.Count(s => s is not LabelSymbolizer) > 1;

    public VisualParameters? DefaultSymbology => _visualParameters?.FirstOrDefault(/*s => !s.HasLabelParameters*/ );

    public VisualParameters? DefaultLabel => _visualParameters?.FirstOrDefault(s => s.HasLabelParameters);

    public VisualParameters GetMainOrDefaultSymbology() => _symbolizers.FirstOrDefault(v => v is SimpleSymbolizer)?.Param ?? VisualParameters.CreateNew();

    public VisualParameters? GetDefaultLabelParams() => _symbolizers.FirstOrDefault(v => v is LabelSymbolizer)?.Param ?? null;

    public abstract Task<FeatureSet<Point>> GetFeatureSet(BoundingBox mapExtent, double mapScale);

    public async Task<List<Feature<Point>>> GetRenderReadyFeatures(BoundingBox mapExtent, double mapScale, double screenWidth, double screenHeight)
    {
        var feature = await GetFeatureSet(mapExtent, mapScale);

        if (feature is null || feature.HasNoGeometry())
            return new List<Feature<Point>>();

        //double xScale = imageWidth / mapExtent.Width;
        //double yScale = imageHeight / mapExtent.Height;
        //double scale = xScale > yScale ? yScale : xScale;
        //Func<Point, Point> mapToScreen = new Func<Point, Point>(p => new Point((p.X - mapExtent.XMin) * scale, -(p.Y - mapExtent.YMax) * scale));
        var mapToScreen = Utility.CreateMapToScreenMapFunc(mapExtent, screenWidth, screenHeight);

        return feature.Transform(mapToScreen).Features.ToList();
    }

    private StyledLayerDescriptor? _sourceSld;

    /// <summary>
    /// The original SLD this layer's symbolizers were parsed from, when the layer was styled
    /// from one (deserialized drawing item, server-provided layer style, ...). Kept because the
    /// runtime <see cref="ISymbolizer"/>s cannot be losslessly converted back to SLD — rule
    /// names/titles, filters and mark shapes are lost — so the symbology-details legend prefers
    /// this over the <see cref="GetSld"/> round-trip.
    /// </summary>
    public StyledLayerDescriptor? SourceSld
    {
        get => _sourceSld;
        set
        {
            _sourceSld = value;
            _symbologyLegend = null;
            RaisePropertyChanged(nameof(SymbologyLegend));
        }
    }

    public StyledLayerDescriptor GetSld()
    {
        return Symbolizers.ParseToSld(SpatialModelMode);
    }

    #region Symbology details (complex-SLD legend)

    private bool _isSymbologyDetailsOpen;

    /// <summary>
    /// Bound to the legend "palette" toggle. When true, the map legend shows a details panel with
    /// the per-rule swatches, filters and scale ranges for this layer's (complex) symbology.
    /// </summary>
    public bool IsSymbologyDetailsOpen
    {
        get => _isSymbologyDetailsOpen;
        set
        {
            _isSymbologyDetailsOpen = value;
            RaisePropertyChanged();
        }
    }

    private SymbologyLegend? _symbologyLegend;

    /// <summary>
    /// Lazily-built legend model (per-rule swatch + filter/scale text) for the details panel.
    /// Built from the original <see cref="SourceSld"/> when available (lossless: keeps rule
    /// titles, filters and mark shapes); otherwise from the <see cref="GetSld"/> round-trip.
    /// Rebuilt whenever the symbolizers change (see <see cref="SetSymbolizer"/>).
    /// </summary>
    public SymbologyLegend SymbologyLegend => _symbologyLegend ??= SldLegendBuilder.Build(SourceSld ?? GetSld());

    private RelayCommand? _exportSymbologyLegendCommand;

    /// <summary>Exports the current symbology legend to a PNG chosen via a save-file dialog.</summary>
    public RelayCommand ExportSymbologyLegendCommand =>
        _exportSymbologyLegendCommand ??= new RelayCommand(_ => ExportSymbologyLegend());

    private void ExportSymbologyLegend()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PNG image (*.png)|*.png",
            FileName = $"{LayerName}-legend.png"
        };

        if (dialog.ShowDialog() == true)
        {
            // A synthetic GetSld() yields a single style whose group header is not meaningful;
            // the original SourceSld carries real style titles worth showing.
            var options = new SldLegendOptions { ShowGroupHeaders = SourceSld is not null };
            SldLegendPngRenderer.RenderToFile(SourceSld ?? GetSld(), dialog.FileName, options);
        }
    }

    #endregion
}
