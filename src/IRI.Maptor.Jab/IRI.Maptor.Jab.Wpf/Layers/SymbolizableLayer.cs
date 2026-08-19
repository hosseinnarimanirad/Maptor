using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Ogc.SLD;
using IRI.Maptor.Jab.Wpf.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Jab.Wpf.Cartography.Symbologies;
using IRI.Maptor.Jab.Wpf.Cartography.Legend;


namespace IRI.Maptor.Jab.Wpf.Layers;

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

    /// <summary>
    /// Replaces all symbolizers at once (e.g. when restoring saved symbology).
    /// The optional <paramref name="sourceSld"/> becomes the new <see cref="SourceSld"/>.
    /// </summary>
    public void ReplaceSymbolizers(IEnumerable<ISymbolizer> symbolizers, StyledLayerDescriptor? sourceSld = null)
    {
        _symbolizers.Clear();
        _visualParameters.Clear();

        foreach (var symbolizer in symbolizers)
        {
            if (symbolizer.Param is not null)
                _visualParameters.Add(symbolizer.Param);

            _symbolizers.Add(symbolizer);
        }

        SourceSld = sourceSld;

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

    #region Default symbology (reset + user-modification tracking)

    /// <summary>
    /// Pristine snapshot of the symbology the layer was created with (e.g. the
    /// server-provided SLD, or the synthesized simple style when there was none).
    /// Captured via <see cref="CaptureDefaultSymbology"/>; null when the creator
    /// never captured one — then reset is unavailable.
    /// </summary>
    public StyledLayerDescriptor? DefaultSld { get; private set; }

    public bool CanResetSymbology => DefaultSld is not null;

    private bool _isSymbologyUserModified;

    /// <summary>
    /// True once the user restyled this layer (quick dialog or SLD editor, or a
    /// project override replay). Lets hosts persist a symbology override only for
    /// layers that genuinely diverged from their default, so untouched layers keep
    /// tracking future default changes. Cleared by <see cref="ResetSymbologyToDefault"/>.
    /// </summary>
    public bool IsSymbologyUserModified
    {
        get => _isSymbologyUserModified;
        set
        {
            _isSymbologyUserModified = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Snapshots the current symbology (<see cref="SourceSld"/> when present, else the
    /// <see cref="GetSld"/> reconstruction) as the layer's default. Call once right after
    /// the layer is created and styled.
    /// </summary>
    public void CaptureDefaultSymbology()
    {
        DefaultSld = CloneSld(SourceSld ?? GetSld());
        IsSymbologyUserModified = false;
        RaisePropertyChanged(nameof(CanResetSymbology));
    }

    /// <summary>
    /// Restores the symbology captured by <see cref="CaptureDefaultSymbology"/> and clears
    /// <see cref="IsSymbologyUserModified"/>. Returns false when no default is available.
    /// </summary>
    public bool ResetSymbologyToDefault()
    {
        // hand a clone to the layer so later edits can never reach the pristine snapshot
        var restored = CloneSld(DefaultSld);

        if (restored is null)
            return false;

        ReplaceSymbolizers(restored.ParseToSymbolizers(), restored);

        IsSymbologyUserModified = false;

        return true;
    }

    // Serialize/parse round-trip as a deep clone — the same path every SLD takes anyway.
    private static StyledLayerDescriptor? CloneSld(StyledLayerDescriptor? sld)
        => sld is null ? null : SldHelper.Parse(SldHelper.Serialize(sld, indented: false));

    #endregion

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

            if (value)
            {
                // The data source's fields (and their aliases) may load after the legend was first
                // built and cached — rebuild on open so filter captions pick up the aliases.
                _symbologyLegend = null;
                RaisePropertyChanged(nameof(SymbologyLegend));
            }

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
    public SymbologyLegend SymbologyLegend => _symbologyLegend ??=
        SldLegendBuilder.Build(SourceSld ?? GetSld(), new SldLegendOptions { FieldAliasResolver = CreateFieldAliasResolver() });

    /// <summary>The layer's attribute fields, when known; drives filter-text aliases in the legend.</summary>
    public virtual List<Field>? GetFields() => null;

    /// <summary>
    /// Field-name → alias lookup over <see cref="GetFields"/> (case-insensitive), or null when the
    /// layer has no fields or no field carries a real alias (an alias equal to the name is a filler).
    /// </summary>
    private Func<string, string?>? CreateFieldAliasResolver()
    {
        var fields = GetFields();

        if (fields is null || fields.Count == 0)
            return null;

        var aliasByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field.Name) || string.IsNullOrWhiteSpace(field.Alias))
                continue;

            if (string.Equals(field.Name, field.Alias, StringComparison.OrdinalIgnoreCase))
                continue;

            aliasByName.TryAdd(field.Name, field.Alias!);
        }

        if (aliasByName.Count == 0)
            return null;

        return name => aliasByName.TryGetValue(name, out var alias) ? alias : null;
    }

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
            // Style titles are SLD internals users don't need; the layer name is the legend title.
            var options = new SldLegendOptions
            {
                Title = LayerName,
                ShowGroupHeaders = false,
                FieldAliasResolver = CreateFieldAliasResolver()
            };
            SldLegendPngRenderer.RenderToFile(SourceSld ?? GetSld(), dialog.FileName, options);
        }
    }

    #endregion
}
