using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Microsoft.Win32;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Core.Ogc.SLD;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Symbology;

public class SldEditorViewModel : Notifier
{
    public Action? RequestCloseAction;

    public Action<SldEditorViewModel>? RequestApplyAction;

    // (message, title) — wired by the hosting window; the view model raises
    // these instead of talking to MessageBox so it stays view-free and testable.
    public Action<string, string>? RequestShowWarning;

    public Action<string, string>? RequestShowError;

    /// <summary>
    /// Restores the layer's default symbology and re-seeds the editor from it. Wired by
    /// hosts whose layer carries a captured default (see <see cref="ShowResetOption"/>).
    /// </summary>
    public Action? RequestResetToDefaultAction;

    private bool _showResetOption;

    /// <summary>Shows the reset toolbar button when the host wired <see cref="RequestResetToDefaultAction"/>.</summary>
    public bool ShowResetOption
    {
        get => _showResetOption;
        set
        {
            _showResetOption = value;
            RaisePropertyChanged();
        }
    }

    private string _layerName;
    public string LayerName
    {
        get => _layerName;
        set
        {
            _layerName = value;
            RaisePropertyChanged();
        }
    }

    private string _styleName;
    public string StyleName
    {
        get => _styleName;
        set
        {
            _styleName = value;
            RaisePropertyChanged();
        }
    }

    private string _styleTitle;
    public string StyleTitle
    {
        get => _styleTitle;
        set
        {
            _styleTitle = value;
            RaisePropertyChanged();
        }
    }

    private string _styleAbstract;
    public string StyleAbstract
    {
        get => _styleAbstract;
        set
        {
            _styleAbstract = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Attribute field names of the layer being styled; offered as suggestions in the
    /// filter/label property pickers. Empty when the editor is opened without layer context.
    /// </summary>
    public ObservableCollection<string> FieldNames { get; } = new ObservableCollection<string>();

    private SpatialModelMode? _geometryType;
    /// <summary>Geometry type of the layer being styled; null when unknown.</summary>
    public SpatialModelMode? GeometryType
    {
        get => _geometryType;
        set
        {
            _geometryType = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(CanAddRasterSymbolizer));
        }
    }

    /// <summary>
    /// Raster symbolizers have no runtime renderer for vector layers (the parser skips
    /// them), so authoring one is only offered when the layer is raster or unknown.
    /// </summary>
    public bool CanAddRasterSymbolizer => GeometryType is null || GeometryType == SpatialModelMode.Raster;

    // Deep clone of the loaded document. Everything the editor cannot represent
    // (UserLayers, additional NamedLayers/UserStyles/FeatureTypeStyles, constraints)
    // is carried through ToStyledLayerDescriptor() unchanged instead of being dropped.
    private StyledLayerDescriptor? _sourceSld;

    private bool _hasPreservedContent;
    /// <summary>
    /// True when the loaded SLD contains parts beyond the first
    /// NamedLayer/UserStyle/FeatureTypeStyle; they round-trip unchanged but are not
    /// editable here, and the view shows a notice.
    /// </summary>
    public bool HasPreservedContent
    {
        get => _hasPreservedContent;
        private set
        {
            _hasPreservedContent = value;
            RaisePropertyChanged();
        }
    }

    public ObservableCollection<RuleViewModel> Rules { get; } = new ObservableCollection<RuleViewModel>();

    private RuleViewModel _selectedRule;
    public RuleViewModel SelectedRule
    {
        get => _selectedRule;
        set
        {
            _selectedRule = value;
            RaisePropertyChanged();
        }
    }

    private string _xmlPreview;
    public string XmlPreview
    {
        get => _xmlPreview;
        set
        {
            _xmlPreview = value;
            RaisePropertyChanged();
        }
    }

    public ICommand AddRuleCommand { get; }
    public ICommand RemoveRuleCommand { get; }
    public ICommand MoveRuleUpCommand { get; }
    public ICommand MoveRuleDownCommand { get; }
    public ICommand ImportSldCommand { get; }
    public ICommand ExportSldCommand { get; }
    public ICommand RefreshPreviewCommand { get; }
    public ICommand OkCommand { get; }
    public ICommand ApplyCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ResetToDefaultCommand { get; }

    public SldEditorViewModel()
    {
        AddRuleCommand = new RelayCommand(_ => AddRule());
        RemoveRuleCommand = new RelayCommand(_ => RemoveRule(), _ => SelectedRule != null);
        MoveRuleUpCommand = new RelayCommand(_ => MoveRuleUp(), _ => SelectedRule != null && Rules.IndexOf(SelectedRule) > 0);
        MoveRuleDownCommand = new RelayCommand(_ => MoveRuleDown(), _ => SelectedRule != null && Rules.IndexOf(SelectedRule) < Rules.Count - 1);
        ImportSldCommand = new RelayCommand(_ => ImportSld());
        ExportSldCommand = new RelayCommand(_ => ExportSld());
        RefreshPreviewCommand = new RelayCommand(_ => RefreshPreview());
        OkCommand = new RelayCommand(_ =>
        {
            RequestApplyAction?.Invoke(this);
            RequestCloseAction?.Invoke();
        });
        ApplyCommand = new RelayCommand(_ => RequestApplyAction?.Invoke(this));
        CancelCommand = new RelayCommand(_ => RequestCloseAction?.Invoke());
        ResetToDefaultCommand = new RelayCommand(_ => RequestResetToDefaultAction?.Invoke());

        // Initialize with default values
        LayerName = "NewLayer";
        StyleName = "default";
        StyleTitle = "Default Style";
    }

    /// <summary>
    /// Creates an editor seeded with a layer's current SLD and context. The layer name
    /// falls back to <paramref name="layerName"/> when the SLD carries none, and the
    /// first rule is pre-selected so the editor never opens on a disabled detail pane.
    /// </summary>
    public static SldEditorViewModel Create(string? layerName, StyledLayerDescriptor? sld, IEnumerable<string>? fieldNames = null, SpatialModelMode? geometryType = null)
    {
        var result = new SldEditorViewModel() { GeometryType = geometryType };

        foreach (var fieldName in fieldNames ?? Enumerable.Empty<string>())
        {
            result.FieldNames.Add(fieldName);
        }

        if (sld is not null)
        {
            result.FromStyledLayerDescriptor(sld);
            result.RefreshPreview();
        }

        var sldProvidedName = sld?.NamedLayers?.FirstOrDefault()?.Name;

        if (string.IsNullOrWhiteSpace(sldProvidedName) && !string.IsNullOrWhiteSpace(layerName))
        {
            result.LayerName = layerName;
        }

        result.SelectedRule = result.Rules.FirstOrDefault();

        return result;
    }

    private void AddRule()
    {
        var rule = new RuleViewModel
        {
            Name = $"Rule{Rules.Count + 1}",
            Title = $"Rule {Rules.Count + 1}"
        };
        Rules.Add(rule);
        SelectedRule = rule;
    }

    private void RemoveRule()
    {
        if (SelectedRule != null)
        {
            var index = Rules.IndexOf(SelectedRule);
            Rules.Remove(SelectedRule);

            if (Rules.Count > 0)
            {
                SelectedRule = Rules[Math.Min(index, Rules.Count - 1)];
            }
        }
    }

    private void MoveRuleUp()
    {
        if (SelectedRule == null) return;

        var index = Rules.IndexOf(SelectedRule);
        if (index > 0)
        {
            Rules.Move(index, index - 1);
        }
    }

    private void MoveRuleDown()
    {
        if (SelectedRule == null) return;

        var index = Rules.IndexOf(SelectedRule);
        if (index < Rules.Count - 1)
        {
            Rules.Move(index, index + 1);
        }
    }

    public StyledLayerDescriptor ToStyledLayerDescriptor()
    {
        // Start from a clone of the loaded document so unedited parts survive; a fresh
        // clone per call keeps repeated Apply/OK/preview calls from aliasing each other.
        var sld = CloneSld(_sourceSld);

        if (sld is null)
            sld = new StyledLayerDescriptor();

        var namedLayer = sld.NamedLayers.FirstOrDefault();
        if (namedLayer is null)
        {
            namedLayer = new NamedLayer();
            sld.NamedLayers.Add(namedLayer);
        }
        namedLayer.Name = LayerName;

        var userStyle = namedLayer.UserStyles.FirstOrDefault();
        if (userStyle is null)
        {
            userStyle = new UserStyle { IsDefault = true };
            namedLayer.UserStyles.Add(userStyle);
        }
        userStyle.Name = StyleName;
        userStyle.Title = StyleTitle;
        userStyle.Abstract = StyleAbstract;

        var featureTypeStyles = userStyle.FeatureTypeStyles;
        if (featureTypeStyles is null || featureTypeStyles.Count == 0)
        {
            featureTypeStyles = new List<FeatureTypeStyle> { new FeatureTypeStyle() };
            userStyle.FeatureTypeStyles = featureTypeStyles;
        }

        // Distribute the edited rules back into the FeatureTypeStyle each was loaded from,
        // keeping the document's compositing structure; rules created in the editor go to
        // the last FeatureTypeStyle (rendered topmost).
        foreach (var featureTypeStyle in featureTypeStyles)
            featureTypeStyle.Rules = new List<Rule>();

        foreach (var ruleVm in Rules)
        {
            var index = ruleVm.SourceFeatureTypeStyleIndex;

            if (index < 0 || index >= featureTypeStyles.Count)
                index = featureTypeStyles.Count - 1;

            featureTypeStyles[index].Rules.Add(ruleVm.ToRule());
        }

        // an SLD FeatureTypeStyle must carry at least one rule — drop the ones left empty
        // by rule deletion, but keep one (empty) style so the document stays well-formed
        userStyle.FeatureTypeStyles = featureTypeStyles.Where(f => f.Rules.Count > 0).ToList();

        if (userStyle.FeatureTypeStyles.Count == 0)
            userStyle.FeatureTypeStyles.Add(new FeatureTypeStyle());

        return sld;
    }

    public void FromStyledLayerDescriptor(StyledLayerDescriptor sld)
    {
        if (sld == null)
            return;

        _sourceSld = CloneSld(sld);

        HasPreservedContent = CountPreservedParts(sld) > 0;

        Rules.Clear();

        var namedLayer = sld.NamedLayers.FirstOrDefault();
        if (namedLayer is null)
            return;

        LayerName = namedLayer.Name;

        var userStyle = namedLayer.UserStyles.FirstOrDefault();
        if (userStyle is null)
            return;

        StyleName = userStyle.Name;
        StyleTitle = userStyle.Title;
        StyleAbstract = userStyle.Abstract;

        // Surface the rules of EVERY FeatureTypeStyle of the primary style: GeoServer-authored
        // SLDs routinely hold one rule per FeatureTypeStyle, so editing only the first would
        // hide most of the layer's symbology. Each rule remembers its origin for save.
        for (var ftsIndex = 0; ftsIndex < (userStyle.FeatureTypeStyles?.Count ?? 0); ftsIndex++)
        {
            foreach (var rule in userStyle.FeatureTypeStyles![ftsIndex].Rules ?? Enumerable.Empty<Rule>())
            {
                var ruleVm = new RuleViewModel { SourceFeatureTypeStyleIndex = ftsIndex };
                ruleVm.FromRule(rule);
                Rules.Add(ruleVm);
            }
        }
    }

    // Serialize/parse round-trip as a deep clone — the same path every SLD takes anyway,
    // so anything that survives save/load survives the clone.
    private static StyledLayerDescriptor? CloneSld(StyledLayerDescriptor? sld)
        => sld is null ? null : SldHelper.Parse(SldHelper.Serialize(sld, indented: false));

    // Parts of the document the editor does not surface: everything beyond the first
    // NamedLayer → first UserStyle (whose FeatureTypeStyles are ALL editable).
    private static int CountPreservedParts(StyledLayerDescriptor sld)
    {
        var count = sld.UserLayers?.Count ?? 0;

        count += Math.Max(0, (sld.NamedLayers?.Count ?? 0) - 1);

        var primaryLayer = sld.NamedLayers?.FirstOrDefault();
        if (primaryLayer is null)
            return count;

        count += primaryLayer.NamedStyles?.Count ?? 0;
        count += Math.Max(0, (primaryLayer.UserStyles?.Count ?? 0) - 1);

        return count;
    }

    public void RefreshPreview()
    {
        XmlPreview = SldHelper.Serialize(ToStyledLayerDescriptor()) ?? string.Empty;
    }

    private void ImportSld()
    {
        var title = LocalizationManager.Instance["sldEditor_common_importTooltip"];

        var dialog = new OpenFileDialog
        {
            Filter = "SLD files (*.sld;*.xml)|*.sld;*.xml|All files (*.*)|*.*",
            Title = title
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            if (!SldHelper.TryParse(File.ReadAllText(dialog.FileName), out var sld, out var error))
            {
                RequestShowWarning?.Invoke($"{LocalizationManager.Instance["sldEditor_message_invalidSldFile"]} {error}", title);
                return;
            }

            FromStyledLayerDescriptor(sld);
            RefreshPreview();
        }
        catch (Exception ex)
        {
            RequestShowError?.Invoke(string.Format(LocalizationManager.Instance["sldEditor_message_importError"], ex.Message), title);
        }
    }

    private void ExportSld()
    {
        var title = LocalizationManager.Instance["sldEditor_common_exportTooltip"];

        var dialog = new SaveFileDialog
        {
            Filter = "SLD files (*.sld)|*.sld|XML files (*.xml)|*.xml|All files (*.*)|*.*",
            Title = title,
            FileName = string.IsNullOrWhiteSpace(StyleName) ? "style.sld" : $"{StyleName}.sld"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            SldHelper.Save(dialog.FileName, ToStyledLayerDescriptor());
            RefreshPreview();
        }
        catch (Exception ex)
        {
            RequestShowError?.Invoke(string.Format(LocalizationManager.Instance["sldEditor_message_exportError"], ex.Message), title);
        }
    }
}

