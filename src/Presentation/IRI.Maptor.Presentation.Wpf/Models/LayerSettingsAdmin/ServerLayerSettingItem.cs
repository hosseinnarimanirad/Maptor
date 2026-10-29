using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Media;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Ogc.SLD;
using IRI.Maptor.Core.Spatial.Dtos;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Wpf.Models.LayerSettings;
using IRI.Maptor.Presentation.Wpf.Cartography.Legend;

namespace IRI.Maptor.Presentation.Wpf.Models.LayerSettingsAdmin;

/// <summary>
/// One row of the server's layer registry (a TOC group or a layer) as the admin window edits it: the
/// editable values, the copy last read from the server, and the rule check of
/// <see cref="LayerSettingValidator"/> run on every change, so mistakes show while typing.
/// </summary>
public class ServerLayerSettingItem : Notifier, INotifyDataErrorInfo
{
    private static LocalizationManager Localization => LocalizationManager.Instance;

    /// <summary>The colour the stored form uses (#AARRGGBB), like the seeded rows.</summary>
    private const string HexFormat = "#{0:X2}{1:X2}{2:X2}{3:X2}";

    private static readonly Color DefaultFillColor = Color.FromArgb(0x66, 0x33, 0x99, 0xFF);

    private static readonly Color DefaultStrokeColor = Color.FromArgb(0xFF, 0x33, 0x66, 0xCC);

    private static readonly Color DefaultLabelColor = Colors.Black;

    private readonly int _maxTitleLength;

    private LayerSettingAdminDto _original;

    private List<LayerSettingError> _errors = new();

    private bool _isLoading;

    public ServerLayerSettingItem(LayerSettingAdminDto row, int maxTitleLength)
    {
        _maxTitleLength = maxTitleLength;
        _original = row;

        Load(row);
    }

    #region Identity (read-only)

    public int Id => _original.Id;

    public bool IsGroup => _original.IsGroupLayer;

    public bool IsLayer => !IsGroup;

    private int? _groupLayerId;

    /// <summary>Where the row is listed now (moves are local until Save); see <see cref="IsMoved"/>.</summary>
    public int? GroupLayerId => _groupLayerId;

    private int _tocOrder;

    /// <summary>Higher is listed (and drawn) on top.</summary>
    public int TocOrder => _tocOrder;

    public string? TableName => _original.TableName;

    public string? ServiceUrl => _original.ServiceUrl;

    /// <summary>The label column choices; empty when the server could not resolve the layer's table.</summary>
    public IReadOnlyList<string> FieldNames => _original.FieldNames ?? (IReadOnlyList<string>)Array.Empty<string>();

    /// <summary>False: the label column is typed in instead of picked from a list.</summary>
    public bool HasFieldList => _original.FieldNames is { Count: > 0 };

    public DateTime UpdatedAt => _original.UpdatedAt;

    public ServerLayerSettingItem? Parent { get; internal set; }

    public ObservableCollection<ServerLayerSettingItem> Children { get; } = new();

    /// <summary>The group a layer is listed under, or "top level".</summary>
    public string GroupTitle => Parent?.Title ?? Localization["serverLayerSettings_topLevel"];

    /// <summary>The header's second line: a layer's group, a group's layer count.</summary>
    public string Caption => IsGroup ? string.Format(Localization["serverLayerSettings_layerCount"], Children.Count) : GroupTitle;

    #endregion

    #region Editable values

    private string _title = string.Empty;

    public string Title
    {
        get => _title;
        set => Set(ref _title, value ?? string.Empty);
    }

    private bool _isActive;

    public bool IsActive
    {
        get => _isActive;
        set => Set(ref _isActive, value);
    }

    private bool _isLayerOn;

    public bool IsLayerOn
    {
        get => _isLayerOn;
        set => Set(ref _isLayerOn, value);
    }

    private bool _isExpandedInToc;

    public bool IsExpandedInToc
    {
        get => _isExpandedInToc;
        set => Set(ref _isExpandedInToc, value);
    }

    private bool _isSearchable;

    public bool IsSearchable
    {
        get => _isSearchable;
        set => Set(ref _isSearchable, value);
    }

    // Zoom levels are doubles because NumericUpDown binds double?; empty means no limit.
    private double? _minLayerZoomLevel;

    public double? MinLayerZoomLevel
    {
        get => _minLayerZoomLevel;
        set => Set(ref _minLayerZoomLevel, value);
    }

    private double? _maxLayerZoomLevel;

    public double? MaxLayerZoomLevel
    {
        get => _maxLayerZoomLevel;
        set => Set(ref _maxLayerZoomLevel, value);
    }

    private string? _hexFill;

    /// <summary>Off: the layer has no fill (lines, outlines-only polygons).</summary>
    public bool HasFill
    {
        get => _hexFill is not null;
        set
        {
            if (value == HasFill)
                return;

            _hexFill = value ? ToHex(DefaultFillColor) : null;
            OnColorChanged(nameof(HasFill), nameof(FillColor));
        }
    }

    public Color FillColor
    {
        get => ParseOr(_hexFill, DefaultFillColor);
        set
        {
            _hexFill = ToHex(value);
            OnColorChanged(nameof(HasFill), nameof(FillColor));
        }
    }

    private string? _hexStroke;

    public bool HasStroke
    {
        get => _hexStroke is not null;
        set
        {
            if (value == HasStroke)
                return;

            _hexStroke = value ? ToHex(DefaultStrokeColor) : null;
            OnColorChanged(nameof(HasStroke), nameof(StrokeColor));
        }
    }

    public Color StrokeColor
    {
        get => ParseOr(_hexStroke, DefaultStrokeColor);
        set
        {
            _hexStroke = ToHex(value);
            OnColorChanged(nameof(HasStroke), nameof(StrokeColor));
        }
    }

    private double _strokeThickness;

    public double StrokeThickness
    {
        get => _strokeThickness;
        set => Set(ref _strokeThickness, value);
    }

    private double _opacity;

    /// <summary>0 to 1; the view shows it as a percentage.</summary>
    public double Opacity
    {
        get => _opacity;
        set
        {
            if (Set(ref _opacity, Math.Round(value, 2)))
                RaisePropertyChanged(nameof(OpacityPercentText));
        }
    }

    public string OpacityPercentText => string.Format(CultureInfo.InvariantCulture, "{0:0}%", Opacity * 100);

    private string? _layerSld;

    /// <summary>
    /// When set, the map draws the layer with it and ignores the colours above. It can hold several named
    /// styles (<see cref="Styles"/>); users switch between them and start on the default one.
    /// </summary>
    public string? LayerSld
    {
        get => _layerSld;
        set
        {
            if (Set(ref _layerSld, string.IsNullOrWhiteSpace(value) ? null : value))
            {
                RaisePropertyChanged(nameof(HasSld));
                RaisePropertyChanged(nameof(IsSimpleMode));
                RebuildStyles(_styleToSelect ?? SelectedStyle?.Name);
            }

            _styleToSelect = null;
        }
    }

    public bool HasSld => _layerSld is not null;

    /// <summary>
    /// The Display tab's Simple | Styles switch: Simple draws the layer with the colours below; Styles with
    /// the SLD's named styles, and then those colours are not used (so the tab hides them).
    /// </summary>
    public bool IsSimpleMode => !HasSld;

    #region Styles (the named symbologies inside the SLD)

    // the style to select once LayerSld has been rewritten by a style action
    private string? _styleToSelect;

    /// <summary>The SLD's styles, in the order users see them.</summary>
    public ObservableCollection<LayerStyleItem> Styles { get; } = new();

    public bool HasStyles => Styles.Count > 0;

    private LayerStyleItem? _selectedStyle;

    public LayerStyleItem? SelectedStyle
    {
        get => _selectedStyle;
        set
        {
            if (ReferenceEquals(_selectedStyle, value))
                return;

            _selectedStyle = value;
            RaisePropertyChanged();

            UpdateSelectedStyleLegend();
        }
    }

    /// <summary>The rules of the selected style, as the map legend draws them (the Styles mode preview).</summary>
    public IReadOnlyList<LegendRuleRow> SelectedStyleLegendRows { get; private set; } = [];

    /// <summary>"Legend: By material", the title of the preview.</summary>
    public string SelectedStyleLegendTitle => SelectedStyle is null ? string.Empty : string.Format(Localization["layerStyles_previewOf"], SelectedStyle.DisplayTitle);

    private void UpdateSelectedStyleLegend()
    {
        SelectedStyleLegendRows = BuildLegendRows(SelectedStyle);

        RaisePropertyChanged(nameof(SelectedStyleLegendRows));
        RaisePropertyChanged(nameof(SelectedStyleLegendTitle));
    }

    // the legend of one style: the SLD cut down to that style alone (by position, so a legacy unnamed style works too)
    private IReadOnlyList<LegendRuleRow> BuildLegendRows(LayerStyleItem? style)
    {
        if (style is null || TryParseSld(_layerSld) is not { } sld || sld.FindStyleAt(style.Index) is not { } userStyle)
            return [];

        var styles = sld.GetOrCreatePrimaryStyles();
        styles.RemoveAll(s => !ReferenceEquals(s, userStyle));

        try
        {
            return SldLegendBuilder.Build(sld, new SldLegendOptions { ShowGroupHeaders = false, UntitledRuleText = Title ?? string.Empty }).Groups.SelectMany(g => g.Rows).ToList();
        }
        catch
        {
            // a style the legend builder cannot draw just has no preview
            return [];
        }
    }

    /// <summary>False when the stored SLD cannot be read: the style actions would lose it, so they are off.</summary>
    public bool CanEditStyles => _layerSld is null || TryParseSld(_layerSld) is not null;

    /// <summary>Appends <paramref name="style"/> (titled <paramref name="title"/>) and selects it.</summary>
    public void AddStyle(UserStyle style, string title)
    {
        style.Title = title;

        ChangeSld(sld => sld.AddStyle(style));
    }

    // The actions address a style by its position, not its name: a legacy style may have no name until
    // ChangeSld gives it one, and then the name is only known inside the action.

    /// <summary>A copy of the style, titled <paramref name="title"/>, placed right after it.</summary>
    public void CopyStyle(LayerStyleItem style, string title) => ChangeSld(sld => sld.CopyStyle(NameAt(sld, style), title));

    /// <summary>Removes the style; removing the last one removes the SLD (the colours above apply again).</summary>
    public void RemoveStyle(LayerStyleItem style)
        => ChangeSld(sld => sld.RemoveStyle(NameAt(sld, style)) ? sld.FindStyleAt(Math.Max(0, style.Index - 1))?.Name ?? string.Empty : null);

    public void SetDefaultStyle(LayerStyleItem style) => ChangeSld(sld => sld.SetDefaultStyle(NameAt(sld, style)) ? NameAt(sld, style) : null);

    public void MoveStyle(LayerStyleItem style, int offset) => ChangeSld(sld =>
    {
        // the name before the move: afterwards the style's old position holds its neighbour
        var name = NameAt(sld, style);

        return sld.MoveStyle(name, offset) ? name : null;
    });

    private static string NameAt(StyledLayerDescriptor sld, LayerStyleItem style) => sld.FindStyleAt(style.Index)?.Name ?? string.Empty;

    /// <summary>The SLD editor's result for one style; the style stays selected.</summary>
    public void ReplaceSld(StyledLayerDescriptor sld, string? selectStyleName)
    {
        sld.EnsureStyleNames();

        _styleToSelect = selectStyleName;

        LayerSld = SldHelper.Serialize(sld);
    }

    private void OnStyleTitleEdited(LayerStyleItem style, string? title)
        => ChangeSld(sld => sld.SetStyleTitle(NameAt(sld, style), title) ? NameAt(sld, style) : null);

    // Runs one style action on a fresh copy of the SLD and stores the result. The action returns the name
    // of the style to select, or null when it did nothing. Legacy unnamed styles get names on the way.
    private void ChangeSld(Func<StyledLayerDescriptor, string?> change)
    {
        var sld = _layerSld is null ? new StyledLayerDescriptor() : TryParseSld(_layerSld);

        if (sld is null)
            return;

        sld.EnsureStyleNames();

        var select = change(sld);

        if (select is null)
            return;

        _styleToSelect = select.Length == 0 ? null : select;

        LayerSld = sld.GetStyles().Count == 0 ? null : SldHelper.Serialize(sld);
    }

    private void RebuildStyles(string? selectStyleName)
    {
        foreach (var style in Styles)
            style.TitleEdited = null;

        Styles.Clear();

        foreach (var style in LayerStyleItem.FromSld(TryParseSld(_layerSld), null))
        {
            style.TitleEdited = OnStyleTitleEdited;
            Styles.Add(style);
        }

        SelectedStyle = Styles.FirstOrDefault(s => selectStyleName is not null && s.Name == selectStyleName)
            ?? Styles.FirstOrDefault(s => s.IsDefault)
            ?? Styles.FirstOrDefault();

        RaisePropertyChanged(nameof(HasStyles));
        RaisePropertyChanged(nameof(CanEditStyles));
    }

    // With several styles each needs a title users can tell apart; a single (legacy) style may have none.
    private IEnumerable<LayerSettingError> ValidateStyles()
    {
        if (IsGroup || _layerSld is null || TryParseSld(_layerSld) is not { } sld)
            yield break;

        var styles = sld.GetStyles();

        if (styles.Count < 2)
            yield break;

        if (styles.Any(s => string.IsNullOrWhiteSpace(s.Title)))
            yield return LayerSettingError.StyleTitleRequired;

        var titles = styles.Where(s => !string.IsNullOrWhiteSpace(s.Title)).Select(s => s.Title!.Trim()).ToList();

        if (titles.Distinct(StringComparer.OrdinalIgnoreCase).Count() != titles.Count)
            yield return LayerSettingError.DuplicateStyleTitle;
    }

    private static StyledLayerDescriptor? TryParseSld(string? sld)
        => !string.IsNullOrWhiteSpace(sld) && SldHelper.TryParse(sld, out var result, out _) ? result : null;

    #endregion

    private bool _isLabeled;

    public bool IsLabeled
    {
        get => _isLabeled;
        set
        {
            if (!Set(ref _isLabeled, value) || _isLoading)
                return;

            // turning labels on needs a colour and a size; a row loaded as it is stays as it is
            if (value && _labelHexColor is null)
            {
                _labelHexColor = ToHex(DefaultLabelColor);
                RaisePropertyChanged(nameof(LabelColor));
            }

            if (value && LabelSize < LayerSettingValidator.MinLabelSize)
                LabelSize = 10;
        }
    }

    private bool _isLabelOn;

    public bool IsLabelOn
    {
        get => _isLabelOn;
        set => Set(ref _isLabelOn, value);
    }

    private string? _labelColumn;

    public string? LabelColumn
    {
        get => _labelColumn;
        set => Set(ref _labelColumn, string.IsNullOrWhiteSpace(value) ? null : value.Trim());
    }

    private double _labelSize;

    public double LabelSize
    {
        get => _labelSize;
        set => Set(ref _labelSize, value);
    }

    private string? _labelHexColor;

    public Color LabelColor
    {
        get => ParseOr(_labelHexColor, DefaultLabelColor);
        set
        {
            _labelHexColor = ToHex(value);
            OnColorChanged(nameof(LabelColor));
        }
    }

    private double? _minLabelZoomLevel;

    public double? MinLabelZoomLevel
    {
        get => _minLabelZoomLevel;
        set => Set(ref _minLabelZoomLevel, value);
    }

    private double? _maxLabelZoomLevel;

    public double? MaxLabelZoomLevel
    {
        get => _maxLabelZoomLevel;
        set => Set(ref _maxLabelZoomLevel, value);
    }

    private LayerLoadMode _loadMode;

    public LayerLoadMode LoadMode
    {
        get => _loadMode;
        set
        {
            if (!Set(ref _loadMode, value))
                return;

            RaisePropertyChanged(nameof(IsCacheMode));
            RaisePropertyChanged(nameof(IsLiveMode));
        }
    }

    // Radio-button pair for LoadMode.
    public bool IsCacheMode
    {
        get => LoadMode == LayerLoadMode.Cache;
        set { if (value) LoadMode = LayerLoadMode.Cache; }
    }

    public bool IsLiveMode
    {
        get => LoadMode == LayerLoadMode.Live;
        set { if (value) LoadMode = LayerLoadMode.Live; }
    }

    #endregion

    #region State

    private bool _isDirty;

    /// <summary>Differs from the copy last read from the server: its values (<see cref="HasValueChanges"/>), its place (<see cref="IsMoved"/>), or both.</summary>
    public bool IsDirty
    {
        get => _isDirty;
        private set => Set(ref _isDirty, value, validate: false);
    }

    private bool _hasValueChanges;

    /// <summary>The editable values differ from the server's; saved with <c>PUT LayerSetting/{id}</c>.</summary>
    public bool HasValueChanges
    {
        get => _hasValueChanges;
        private set => Set(ref _hasValueChanges, value, validate: false);
    }

    /// <summary>The group or the order differs from the server's; saved with <c>PUT LayerSetting/Order</c>.</summary>
    public bool IsMoved => _groupLayerId != _original.GroupLayerId || _tocOrder != _original.TocOrder;

    private string? _serverMessage;

    /// <summary>Why the server refused the last save of this row (someone else saved it first, it no longer exists, ...).</summary>
    public string? ServerMessage
    {
        get => _serverMessage;
        internal set
        {
            if (Set(ref _serverMessage, value, validate: false))
                RaisePropertyChanged(nameof(HasServerMessage));
        }
    }

    public bool HasServerMessage => !string.IsNullOrEmpty(_serverMessage);

    /// <summary>Someone else saved this row first: only a reload can bring it back to a savable state.</summary>
    public bool HasConflict { get; internal set; }

    public IReadOnlyList<LayerSettingError> Errors => _errors;

    /// <summary>The broken rules, in the current language, for the banner above the tabs.</summary>
    public IReadOnlyList<string> ErrorMessages => _errors.Select(ErrorText).ToList();

    private bool _isVisible = true;

    /// <summary>Matches the search box (a group also when one of its layers does).</summary>
    public bool IsVisible
    {
        get => _isVisible;
        internal set => Set(ref _isVisible, value, validate: false);
    }

    private bool _isExpanded;

    public bool IsExpanded
    {
        get => _isExpanded;
        set => Set(ref _isExpanded, value, validate: false);
    }

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set => Set(ref _isSelected, value, validate: false);
    }

    #endregion

    #region Operations

    /// <summary>What a save sends.</summary>
    public LayerSettingUpdateDto ToUpdateDto() => new()
    {
        Title = Title,
        IsActive = IsActive,
        IsLayerOn = IsLayerOn,
        IsExpandedInToc = IsExpandedInToc,
        IsSearchable = IsSearchable,
        MinLayerZoomLevel = ToZoom(MinLayerZoomLevel),
        MaxLayerZoomLevel = ToZoom(MaxLayerZoomLevel),
        HexFill = _hexFill,
        HexStroke = _hexStroke,
        StrokeThickness = StrokeThickness,
        Opacity = Opacity,
        LayerSld = LayerSld,
        IsLabeled = IsLabeled,
        IsLabelOn = IsLabeled && IsLabelOn,
        LabelColumn = LabelColumn,
        LabelSize = LabelSize,
        LabelHexColor = _labelHexColor,
        MinLabelZoomLevel = ToZoom(MinLabelZoomLevel),
        MaxLabelZoomLevel = ToZoom(MaxLabelZoomLevel),
        LoadMode = LoadMode,
        UpdatedAt = _original.UpdatedAt,
    };

    /// <summary>The place a save sends.</summary>
    public LayerSettingOrderItemDto ToOrderItem() => new() { Id = Id, GroupLayerId = GroupLayerId, TocOrder = TocOrder };

    /// <summary>Moves the row; the window re-lists the tree afterwards.</summary>
    internal void Place(int? groupLayerId, int tocOrder)
    {
        if (_groupLayerId == groupLayerId && _tocOrder == tocOrder)
            return;

        _groupLayerId = groupLayerId;
        _tocOrder = tocOrder;

        RaisePropertyChanged(nameof(GroupLayerId));
        RaisePropertyChanged(nameof(TocOrder));
        RaisePropertyChanged(nameof(IsMoved));
        UpdateDirty();
    }

    /// <summary>The group title and layer count shown in the header, after the tree was re-listed.</summary>
    internal void RaiseStructureChanged()
    {
        RaisePropertyChanged(nameof(GroupTitle));
        RaisePropertyChanged(nameof(Caption));
    }

    /// <summary>Back to the copy last read from the server, values and place.</summary>
    public void Revert()
    {
        Load(_original);
        ServerMessage = null;
    }

    /// <summary>The server's copy after a save (or a reload): it becomes the new baseline.</summary>
    public void Accept(LayerSettingAdminDto saved)
    {
        _original = saved;
        HasConflict = false;
        ServerMessage = null;

        Load(saved);

        RaisePropertyChanged(nameof(UpdatedAt));
        RaisePropertyChanged(nameof(FieldNames));
        RaisePropertyChanged(nameof(HasFieldList));
    }

    /// <summary>
    /// The server's copy after the order was saved: its place and its new UpdatedAt become the baseline,
    /// while unsaved value edits stay (they are saved next, with the new token).
    /// </summary>
    internal void AcceptPlacement(LayerSettingAdminDto saved)
    {
        var baseline = Copy(_original);
        baseline.GroupLayerId = saved.GroupLayerId;
        baseline.TocOrder = saved.TocOrder;
        baseline.UpdatedAt = saved.UpdatedAt;
        _original = baseline;

        Place(saved.GroupLayerId, saved.TocOrder);

        RaisePropertyChanged(nameof(UpdatedAt));
        RaisePropertyChanged(nameof(IsMoved));
        UpdateDirty();
    }

    /// <summary>Rules the server reported although the local check passed (e.g. a table the client knows nothing about).</summary>
    internal void SetServerErrors(IReadOnlyList<LayerSettingError> errors)
    {
        _errors = _errors.Union(errors).ToList();
        RaiseErrorsChanged();
    }

    /// <summary>Re-runs the rule check, e.g. after a language change re-words the messages.</summary>
    public void Revalidate() => Validate();

    #endregion

    #region INotifyDataErrorInfo

    public bool HasErrors => _errors.Count > 0;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        // No object-level errors: WPF shows those on every binding whose value is this row (the detail
        // panel's DataContext, the tree item), which frames whole panels in red. The banner lists them instead.
        if (string.IsNullOrEmpty(propertyName))
            return Array.Empty<string>();

        return _errors.Where(e => PropertiesOf(e).Contains(propertyName)).Select(ErrorText).ToList();
    }

    /// <summary>The editor(s) each rule is shown on.</summary>
    private static string[] PropertiesOf(LayerSettingError error) => error switch
    {
        LayerSettingError.TitleRequired or LayerSettingError.TitleTooLong => [nameof(Title)],
        LayerSettingError.LayerZoomOutOfRange or LayerSettingError.LayerZoomRangeInverted => [nameof(MinLayerZoomLevel), nameof(MaxLayerZoomLevel)],
        LayerSettingError.LabelZoomOutOfRange or LayerSettingError.LabelZoomRangeInverted => [nameof(MinLabelZoomLevel), nameof(MaxLabelZoomLevel)],
        LayerSettingError.InvalidFillColor => [nameof(FillColor)],
        LayerSettingError.InvalidStrokeColor => [nameof(StrokeColor)],
        LayerSettingError.InvalidLabelColor => [nameof(LabelColor)],
        LayerSettingError.OpacityOutOfRange => [nameof(Opacity)],
        LayerSettingError.StrokeThicknessOutOfRange => [nameof(StrokeThickness)],
        LayerSettingError.LabelSizeOutOfRange => [nameof(LabelSize)],
        LayerSettingError.LabelColumnRequired or LayerSettingError.UnknownLabelColumn => [nameof(LabelColumn)],
        LayerSettingError.InvalidSld or LayerSettingError.StyleTitleRequired or LayerSettingError.DuplicateStyleTitle => [nameof(LayerSld)],
        LayerSettingError.InvalidLoadMode => [nameof(LoadMode)],
        _ => [],
    };

    private static readonly string[] ValidatedProperties =
    [
        nameof(Title), nameof(MinLayerZoomLevel), nameof(MaxLayerZoomLevel), nameof(MinLabelZoomLevel), nameof(MaxLabelZoomLevel),
        nameof(FillColor), nameof(StrokeColor), nameof(LabelColor), nameof(Opacity), nameof(StrokeThickness), nameof(LabelSize),
        nameof(LabelColumn), nameof(LayerSld), nameof(LoadMode),
    ];

    /// <summary>"serverLayerSettings_error_" + the rule name in camelCase, e.g. serverLayerSettings_error_titleRequired.</summary>
    public static string ErrorText(LayerSettingError error)
    {
        var name = error.ToString();

        var key = "serverLayerSettings_error_" + char.ToLowerInvariant(name[0]) + name[1..];

        var text = Localization[key];

        return string.IsNullOrEmpty(text) || text == key ? name : text;
    }

    #endregion

    #region Helpers

    private void Load(LayerSettingAdminDto row)
    {
        _isLoading = true;

        try
        {
            Title = row.Title;
            IsActive = row.IsActive;
            IsLayerOn = row.IsLayerOn;
            IsExpandedInToc = row.IsExpandedInToc;
            IsSearchable = row.IsSearchable;
            MinLayerZoomLevel = row.MinLayerZoomLevel;
            MaxLayerZoomLevel = row.MaxLayerZoomLevel;
            _hexFill = NullIfBlank(row.HexFill);
            _hexStroke = NullIfBlank(row.HexStroke);
            StrokeThickness = row.StrokeThickness;
            Opacity = row.Opacity;
            LayerSld = row.LayerSld;
            _labelHexColor = NullIfBlank(row.LabelHexColor);
            IsLabeled = row.IsLabeled;
            IsLabelOn = row.IsLabelOn;
            LabelColumn = row.LabelColumn;
            LabelSize = row.LabelSize;
            MinLabelZoomLevel = row.MinLabelZoomLevel;
            MaxLabelZoomLevel = row.MaxLabelZoomLevel;
            LoadMode = row.LoadMode;
            _groupLayerId = row.GroupLayerId;
            _tocOrder = row.TocOrder;
        }
        finally
        {
            _isLoading = false;
        }

        foreach (var name in new[] { nameof(HasFill), nameof(FillColor), nameof(HasStroke), nameof(StrokeColor), nameof(LabelColor), nameof(LabelSize), nameof(GroupLayerId), nameof(TocOrder), nameof(IsMoved) })
            RaisePropertyChanged(name);

        Validate();
        UpdateDirty();
    }

    private bool Set<T>(ref T field, T value, bool validate = true, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        RaisePropertyChanged(propertyName);

        if (validate && !_isLoading)
        {
            Validate();
            UpdateDirty();
        }

        return true;
    }

    private void OnColorChanged(params string[] propertyNames)
    {
        foreach (var name in propertyNames)
            RaisePropertyChanged(name);

        if (_isLoading)
            return;

        Validate();
        UpdateDirty();
    }

    private void Validate()
    {
        _errors = LayerSettingValidator.Validate(
            ToUpdateDto(),
            IsGroup,
            _maxTitleLength,
            HasFieldList ? FieldNames.ToList() : null,
            sld => SldHelper.TryParse(sld, out _, out _));

        _errors.AddRange(ValidateStyles());

        RaiseErrorsChanged();
    }

    private void RaiseErrorsChanged()
    {
        foreach (var name in ValidatedProperties)
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(name));

        RaisePropertyChanged(nameof(HasErrors));
        RaisePropertyChanged(nameof(Errors));
        RaisePropertyChanged(nameof(ErrorMessages));
    }

    private void UpdateDirty()
    {
        HasValueChanges = !IsSameAs(_original);
        IsDirty = HasValueChanges || IsMoved;
    }

    private static LayerSettingAdminDto Copy(LayerSettingAdminDto row) =>
        System.Text.Json.JsonSerializer.Deserialize<LayerSettingAdminDto>(System.Text.Json.JsonSerializer.Serialize(row))!;

    private bool IsSameAs(LayerSettingAdminDto row)
    {
        var edit = ToUpdateDto();

        var same = edit.Title == row.Title
            && edit.IsActive == row.IsActive
            && edit.IsLayerOn == row.IsLayerOn
            && edit.IsExpandedInToc == row.IsExpandedInToc;

        if (!same || IsGroup)
            return same;

        return edit.IsSearchable == row.IsSearchable
            && edit.MinLayerZoomLevel == row.MinLayerZoomLevel
            && edit.MaxLayerZoomLevel == row.MaxLayerZoomLevel
            && SameColor(edit.HexFill, row.HexFill)
            && SameColor(edit.HexStroke, row.HexStroke)
            && edit.StrokeThickness.Equals(row.StrokeThickness)
            && edit.Opacity.Equals(Math.Round(row.Opacity, 2))
            && edit.LayerSld == NullIfBlank(row.LayerSld)
            && edit.IsLabeled == row.IsLabeled
            && edit.IsLabelOn == (row.IsLabeled && row.IsLabelOn)
            && edit.LabelColumn == NullIfBlank(row.LabelColumn)
            && edit.LabelSize.Equals(row.LabelSize)
            && SameColor(edit.LabelHexColor, row.LabelHexColor)
            && edit.MinLabelZoomLevel == row.MinLabelZoomLevel
            && edit.MaxLabelZoomLevel == row.MaxLabelZoomLevel
            && edit.LoadMode == row.LoadMode;
    }

    // #RRGGBB and #FFRRGGBB are the same colour
    private static bool SameColor(string? a, string? b) =>
        string.IsNullOrWhiteSpace(a) ? string.IsNullOrWhiteSpace(b) : !string.IsNullOrWhiteSpace(b) && ParseOr(a, Colors.Transparent) == ParseOr(b, Colors.Transparent);

    private static int? ToZoom(double? value) => value is double d ? (int)Math.Round(d) : null;

    private static string ToHex(Color color) => string.Format(CultureInfo.InvariantCulture, HexFormat, color.A, color.R, color.G, color.B);

    private static Color ParseOr(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex) || !LayerSettingValidator.IsValidOptionalColor(hex))
            return fallback;

        try
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
        catch (FormatException)
        {
            return fallback;
        }
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public override string ToString() => $"{Id}-{Title}";

    #endregion
}
