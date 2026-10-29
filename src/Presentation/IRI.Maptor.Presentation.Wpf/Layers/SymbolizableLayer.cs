using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Ogc.SLD;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;
using IRI.Maptor.Presentation.Wpf.Cartography.Legend;


namespace IRI.Maptor.Presentation.Wpf.Layers;

public abstract class SymbolizableLayer : BaseLayer
{
    //public event EventHandler<CustomEventArgs<VisualParameters>>? OnLabelChanged;

    protected List<VisualParameters> _visualParameters = [];

    private List<ISymbolizer> _symbolizers = [];

    protected SymbolizableLayer()
    {
        // the legend names an untitled rule after the layer: a rename must rebuild it
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LayerName) && _symbologyLegend is not null)
                RaiseSymbologyChanged();
        };
    }

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
        RaisePropertyChanged(nameof(ShowSymbologyDetails));
        RaisePropertyChanged(nameof(DefaultSymbology));
        RaisePropertyChanged(nameof(SymbologyLegend));
    }

    /// <summary>
    /// Replaces all symbolizers at once (e.g. when restoring saved symbology).
    /// The optional <paramref name="sourceSld"/> becomes the new <see cref="SourceSld"/>, and its
    /// default style becomes the <see cref="ActiveStyleName"/> — callers pass the symbolizers of that
    /// style (<c>sld.ParseToSymbolizers()</c>). To draw another style use <see cref="ApplySld"/>.
    /// </summary>
    public void ReplaceSymbolizers(IEnumerable<ISymbolizer> symbolizers, StyledLayerDescriptor? sourceSld = null)
        => ReplaceSymbolizers(symbolizers, sourceSld, sourceSld.GetDefaultStyleName());

    private void ReplaceSymbolizers(IEnumerable<ISymbolizer> symbolizers, StyledLayerDescriptor? sourceSld, string? activeStyleName)
    {
        _symbolizers.Clear();
        _visualParameters.Clear();

        foreach (var symbolizer in symbolizers.Concat(_styleIndependentSymbolizers).Distinct())
        {
            if (symbolizer.Param is not null)
                _visualParameters.Add(symbolizer.Param);

            _symbolizers.Add(symbolizer);
        }

        // Not through the SourceSld setter: it would announce the SLD's default style first, and a picker
        // bound two-way to SelectedStyleName answers that by switching again, endlessly (stack overflow).
        // The style list is announced only when the SLD itself is a different one.
        if (!ReferenceEquals(_sourceSld, sourceSld))
        {
            StoreSourceSld(sourceSld);
            RaiseStylesChanged();
        }

        SetActiveStyleName(activeStyleName);

        RaisePropertyChanged(nameof(HasMultiSymbolizers));
        RaisePropertyChanged(nameof(ShowSymbologyDetails));
        RaisePropertyChanged(nameof(DefaultSymbology));
        RaisePropertyChanged(nameof(SymbologyLegend));
    }

    /// <summary>
    /// Makes <paramref name="sld"/> the layer's <see cref="SourceSld"/> and draws it with the style named
    /// <paramref name="styleName"/> (the SLD's default style when null or not found).
    /// </summary>
    public void ApplySld(StyledLayerDescriptor sld, string? styleName = null)
    {
        var activeStyleName = sld.HasStyle(styleName) ? styleName : sld.GetDefaultStyleName();

        ReplaceSymbolizers(sld.ParseToSymbolizers(activeStyleName), sld, activeStyleName);
    }

    /// <summary>
    /// Replays a personal SLD saved in a project. On a layer drawn with simple colours, an SLD that is only
    /// those colours (the quick dialog saves them as one style with one plain rule) stays simple colours, so
    /// the quick dialog keeps working after the project is reopened; anything richer becomes the layer's SLD.
    /// </summary>
    public void ApplySavedSld(StyledLayerDescriptor sld, string? styleName)
    {
        if (SourceSld is null && IsSimpleColours(sld))
            ReplaceSymbolizers(sld.ParseToSymbolizers());
        else
            ApplySld(sld, styleName);
    }

    // one layer, one style, one rule without filter or scale range, one non-text symbolizer
    private static bool IsSimpleColours(StyledLayerDescriptor sld)
    {
        if ((sld.NamedLayers?.Count ?? 0) + (sld.UserLayers?.Count ?? 0) != 1)
            return false;

        var styles = sld.GetStyles();

        if (styles.Count != 1)
            return false;

        var rules = sld.FindStyleAt(styles[0].Index)?.FeatureTypeStyles?.SelectMany(f => f.Rules ?? []).ToList();

        return rules is [{ Filter: null, ElseFilter: null, MinScaleDenominator: null, MaxScaleDenominator: null } rule]
            && rule.Symbolizers is [not TextSymbolizer];
    }

    #region Styles (several named symbologies in one SLD)

    private readonly List<ISymbolizer> _styleIndependentSymbolizers = [];

    /// <summary>
    /// Keeps <paramref name="symbolizer"/> on the layer whatever SLD or style it is drawn with: it belongs
    /// to the layer, not to its SLD (e.g. the label of the server's simple label settings, drawn beside the
    /// SLD's own symbolizers). Without this, switching style or resetting would drop it. It is left out of
    /// <see cref="GetSld"/>, so the SLD editor never copies it into an SLD.
    /// </summary>
    public void KeepAcrossStyles(ISymbolizer symbolizer)
    {
        if (!_styleIndependentSymbolizers.Contains(symbolizer))
            _styleIndependentSymbolizers.Add(symbolizer);

        if (!_symbolizers.Contains(symbolizer))
            SetSymbolizer(symbolizer);
    }

    private string? _activeStyleName;

    /// <summary>
    /// The name of the SLD style the layer is drawn with; null when the layer has no SLD or the style is
    /// unnamed. Changed through <see cref="SetActiveStyle"/>.
    /// </summary>
    public string? ActiveStyleName => _activeStyleName;

    private IReadOnlyList<SldStyleInfo> _styles = [];

    /// <summary>
    /// The styles in <see cref="SourceSld"/> the layer can be drawn with (one is active). Read once per SLD:
    /// a new list on every read would make a bound picker reload, and reset its selection, on each change.
    /// </summary>
    public IReadOnlyList<SldStyleInfo> Styles => _styles;

    public bool HasMultipleStyles => Styles.Count > 1;

    /// <summary>The legend's style picker: reads the active style, and setting it switches (see <see cref="SetActiveStyle"/>).</summary>
    public string? SelectedStyleName
    {
        get => ActiveStyleName;
        set
        {
            if (value is not null)
                SetActiveStyle(value);
        }
    }

    /// <summary>The legend's palette button: there is a multi-rule legend to show, or styles to choose from.</summary>
    public bool ShowSymbologyDetails => HasMultiSymbolizers || HasMultipleStyles;

    /// <summary>Set by the map: redraws the layer after <see cref="SetActiveStyle"/> switched its style.</summary>
    public Action<SymbolizableLayer>? RequestRepaint { get; set; }

    /// <summary>
    /// Draws the layer with another style of its <see cref="SourceSld"/>. Returns false when there is no
    /// such style. Switching does not count as a user modification: the SLD itself is unchanged.
    /// </summary>
    private bool _isSwitchingStyle;

    public bool SetActiveStyle(string styleName)
    {
        if (SourceSld is null || !SourceSld.HasStyle(styleName))
            return false;

        if (string.Equals(styleName, ActiveStyleName, StringComparison.Ordinal))
            return true;

        // a bound picker reacting to the change notifications below must not start a second switch
        if (_isSwitchingStyle)
            return false;

        _isSwitchingStyle = true;

        try
        {
            ApplySld(SourceSld, styleName);
        }
        finally
        {
            _isSwitchingStyle = false;
        }

        RequestRepaint?.Invoke(this);

        return true;
    }

    private void SetActiveStyleName(string? styleName)
    {
        var changed = !string.Equals(_activeStyleName, styleName, StringComparison.Ordinal);

        _activeStyleName = styleName;
        _symbologyLegend = null;

        if (changed)
        {
            RaisePropertyChanged(nameof(ActiveStyleName));
            RaisePropertyChanged(nameof(SelectedStyleName));
        }

        RaisePropertyChanged(nameof(SymbologyLegend));
    }

    private void StoreSourceSld(StyledLayerDescriptor? sld)
    {
        _sourceSld = sld;
        _styles = sld.GetStyles();
    }

    private void RaiseStylesChanged()
    {
        RaisePropertyChanged(nameof(SourceSld));
        RaisePropertyChanged(nameof(Styles));
        RaisePropertyChanged(nameof(HasMultipleStyles));
        RaisePropertyChanged(nameof(ShowSymbologyDetails));
    }

    // what the legend shows: the active style only, never the alternatives
    private StyledLayerDescriptor GetActiveStyleSld()
        => SourceSld is null ? GetSld() : SourceSld.ExtractStyle(ActiveStyleName);

    #endregion

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
            StoreSourceSld(value);
            RaiseStylesChanged();

            // a directly assigned SLD is drawn with its default style (callers parse that one)
            SetActiveStyleName(value.GetDefaultStyleName());
        }
    }

    public StyledLayerDescriptor GetSld()
    {
        return Symbolizers.Where(s => !_styleIndependentSymbolizers.Contains(s)).ParseToSld(SpatialModelMode);
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

    // the default was the simple colours (no SourceSld): DefaultSld is only their SLD form
    private bool _isDefaultSimple;

    /// <summary>
    /// Call after changing a symbolizer's parameters in place (the quick dialog's colours): the legend
    /// built from them is rebuilt.
    /// </summary>
    public void RaiseSymbologyChanged()
    {
        _symbologyLegend = null;
        RaisePropertyChanged(nameof(SymbologyLegend));
    }

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
        _isDefaultSimple = SourceSld is null;
        DefaultSld = CloneSld(SourceSld ?? GetSld());
        IsSymbologyUserModified = false;
        RaisePropertyChanged(nameof(CanResetSymbology));
    }

    /// <summary>
    /// Restores the symbology captured by <see cref="CaptureDefaultSymbology"/> (all its styles) and
    /// clears <see cref="IsSymbologyUserModified"/>. The active style stays when the restored SLD still
    /// has it, else the default style is drawn. Returns false when no default is available.
    /// </summary>
    public bool ResetSymbologyToDefault()
    {
        // hand a clone to the layer so later edits can never reach the pristine snapshot
        var restored = CloneSld(DefaultSld);

        if (restored is null)
            return false;

        // a layer created with simple colours goes back to them, not to an SLD made from them: else it would
        // count as an SLD layer from then on (quick dialog no longer offered)
        if (_isDefaultSimple)
            ReplaceSymbolizers(restored.ParseToSymbolizers());
        else
            ApplySld(restored, ActiveStyleName);

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
    /// Built from the active style of the original <see cref="SourceSld"/> when available (lossless:
    /// keeps rule titles, filters and mark shapes); otherwise from the <see cref="GetSld"/> round-trip.
    /// Rebuilt whenever the symbolizers or the active style change (see <see cref="SetSymbolizer"/>).
    /// </summary>
    public SymbologyLegend SymbologyLegend => _symbologyLegend ??=
        SldLegendBuilder.Build(GetActiveStyleSld(), new SldLegendOptions
        {
            FieldAliasResolver = CreateFieldAliasResolver(),
            // a simple-colours layer has one unnamed rule: it stands for the layer itself
            UntitledRuleText = LayerName ?? string.Empty,
        });

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
                // the title already names the layer: an unnamed rule is a bare swatch (as in the PDF legend)
                UntitledRuleText = string.Empty,
                ShowGroupHeaders = false,
                FieldAliasResolver = CreateFieldAliasResolver()
            };
            SldLegendPngRenderer.RenderToFile(GetActiveStyleSld(), dialog.FileName, options);
        }
    }

    #endregion
}
