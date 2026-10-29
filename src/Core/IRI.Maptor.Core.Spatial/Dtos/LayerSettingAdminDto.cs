using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Core.Spatial.Dtos;

/// <summary>
/// The layer registry as the administrator edits it (<c>GET LayerSetting/Admin/List</c>): every row,
/// inactive ones included, with what the editor needs besides the row itself.
/// </summary>
public class LayerSettingAdminListDto
{
    public List<LayerSettingAdminDto> Items { get; set; } = new();

    /// <summary>The longest title the server stores (the column width differs per application).</summary>
    public int MaxTitleLength { get; set; }
}

/// <summary>One LayerSetting row: a TOC group (<see cref="IsGroupLayer"/>) or a layer.</summary>
public class LayerSettingAdminDto : LayerSettingUpdateDto
{
    public int Id { get; set; }

    public bool IsGroupLayer { get; set; }

    /// <summary>The group this layer is listed under; null for a top-level row. Changed only through the order request.</summary>
    public int? GroupLayerId { get; set; }

    /// <summary>A higher value is listed and drawn on top. Changed only through the order request.</summary>
    public int TocOrder { get; set; }

    /// <summary>Read-only: the table behind the layer.</summary>
    public string? TableName { get; set; }

    /// <summary>Read-only: the layer's List endpoint.</summary>
    public string? ServiceUrl { get; set; }

    /// <summary>The attribute names of the layer's features (the label column choices); null for groups or an unknown table.</summary>
    public List<string>? FieldNames { get; set; }
}

/// <summary>
/// The editable part of a row (<c>PUT LayerSetting/{id}</c>). The server checks it with
/// <see cref="LayerSettingValidator"/> and refuses the request when <see cref="UpdatedAt"/> is not the
/// row's current value (someone else saved it in the meantime).
/// </summary>
public class LayerSettingUpdateDto
{
    public string Title { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    /// <summary>Visible when the map opens.</summary>
    public bool IsLayerOn { get; set; }

    public bool IsExpandedInToc { get; set; }

    public bool IsSearchable { get; set; }

    public int? MinLayerZoomLevel { get; set; }

    public int? MaxLayerZoomLevel { get; set; }

    /// <summary>#AARRGGBB or #RRGGBB; null for no fill.</summary>
    public string? HexFill { get; set; }

    public string? HexStroke { get; set; }

    public double StrokeThickness { get; set; }

    /// <summary>0 (transparent) to 1.</summary>
    public double Opacity { get; set; }

    /// <summary>When set, it replaces the colours above on the map.</summary>
    public string? LayerSld { get; set; }

    /// <summary>The layer has labels at all; <see cref="IsLabelOn"/> is whether they show when the map opens.</summary>
    public bool IsLabeled { get; set; }

    public bool IsLabelOn { get; set; }

    public string? LabelColumn { get; set; }

    public double LabelSize { get; set; }

    public string? LabelHexColor { get; set; }

    public int? MinLabelZoomLevel { get; set; }

    public int? MaxLabelZoomLevel { get; set; }

    public LayerLoadMode LoadMode { get; set; }

    /// <summary>The row's last-saved time as read; the optimistic-concurrency token.</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary><c>POST LayerSetting/Group</c>: a new top-level group, listed above the existing ones.</summary>
public class LayerGroupCreateDto
{
    public string Title { get; set; } = string.Empty;

    public bool IsExpandedInToc { get; set; } = true;
}

/// <summary>
/// <c>PUT LayerSetting/Order</c>: where rows are listed. Only the rows given change; a layer may move to
/// another group (or to the top level), a group stays at the top level.
/// </summary>
public class LayerSettingOrderDto
{
    public List<LayerSettingOrderItemDto> Items { get; set; } = new();
}

public class LayerSettingOrderItemDto
{
    public int Id { get; set; }

    public int? GroupLayerId { get; set; }

    public int TocOrder { get; set; }
}

/// <summary>
/// The body of a refused admin request, for a server without its own error envelope (Barg; Gas sends the
/// same names in its ApiResponse error parameters).
/// </summary>
public class LayerSettingAdminErrorDto
{
    /// <summary>A <see cref="LayerSettingAdminStatus"/> name.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary><see cref="LayerSettingError"/> names; empty unless <see cref="Status"/> is Invalid.</summary>
    public List<string> Errors { get; set; } = new();
}

/// <summary>A reason the server refuses an edit; sent by name, so the client can show it in its own language.</summary>
public enum LayerSettingError
{
    TitleRequired,
    TitleTooLong,
    LayerZoomOutOfRange,
    LayerZoomRangeInverted,
    LabelZoomOutOfRange,
    LabelZoomRangeInverted,
    InvalidFillColor,
    InvalidStrokeColor,
    InvalidLabelColor,
    OpacityOutOfRange,
    StrokeThicknessOutOfRange,
    LabelSizeOutOfRange,
    LabelColumnRequired,
    UnknownLabelColumn,
    InvalidSld,
    InvalidLoadMode,

    // Order and group requests
    UnknownRow,
    UnknownGroup,
    GroupInGroup,
    DuplicateRow,

    // The named styles inside the SLD (checked by the client: the server keeps the SLD as text)
    StyleTitleRequired,
    DuplicateStyleTitle,
}

/// <summary>The rules for an edit, shared by both servers and the admin window, so the window can show them as you type.</summary>
public static class LayerSettingValidator
{
    /// <summary>Zoom levels of the web-mercator tile pyramid that Maptor renders (<c>WebMercatorUtility</c>).</summary>
    public const int MinZoomLevel = 0;

    public const int MaxZoomLevel = 24;

    public const double MaxStrokeThickness = 50;

    public const double MinLabelSize = 1;

    public const double MaxLabelSize = 72;

    private static readonly Regex HexColor = new("^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$", RegexOptions.CultureInvariant);

    /// <summary>
    /// Every rule <paramref name="setting"/> breaks; empty when it may be saved. A group is checked only for
    /// its title. <paramref name="fieldNames"/> null skips the label-column check (unknown table);
    /// <paramref name="isValidSld"/> null skips the SLD check.
    /// </summary>
    public static List<LayerSettingError> Validate(
        LayerSettingUpdateDto setting,
        bool isGroupLayer,
        int maxTitleLength,
        IReadOnlyCollection<string>? fieldNames,
        Func<string, bool>? isValidSld)
    {
        var errors = new List<LayerSettingError>();

        if (string.IsNullOrWhiteSpace(setting.Title))
            errors.Add(LayerSettingError.TitleRequired);
        else if (setting.Title.Trim().Length > maxTitleLength)
            errors.Add(LayerSettingError.TitleTooLong);

        if (isGroupLayer)
            return errors;

        CheckZoomRange(setting.MinLayerZoomLevel, setting.MaxLayerZoomLevel, LayerSettingError.LayerZoomOutOfRange, LayerSettingError.LayerZoomRangeInverted, errors);

        if (!IsValidOptionalColor(setting.HexFill))
            errors.Add(LayerSettingError.InvalidFillColor);

        if (!IsValidOptionalColor(setting.HexStroke))
            errors.Add(LayerSettingError.InvalidStrokeColor);

        if (double.IsNaN(setting.Opacity) || setting.Opacity < 0 || setting.Opacity > 1)
            errors.Add(LayerSettingError.OpacityOutOfRange);

        if (double.IsNaN(setting.StrokeThickness) || setting.StrokeThickness < 0 || setting.StrokeThickness > MaxStrokeThickness)
            errors.Add(LayerSettingError.StrokeThicknessOutOfRange);

        if (!Enum.IsDefined(typeof(LayerLoadMode), setting.LoadMode))
            errors.Add(LayerSettingError.InvalidLoadMode);

        if (!string.IsNullOrWhiteSpace(setting.LayerSld) && isValidSld is not null && !isValidSld(setting.LayerSld!))
            errors.Add(LayerSettingError.InvalidSld);

        if (setting.IsLabeled)
        {
            if (string.IsNullOrWhiteSpace(setting.LabelColumn))
                errors.Add(LayerSettingError.LabelColumnRequired);
            else if (fieldNames is not null && !fieldNames.Contains(setting.LabelColumn!, StringComparer.OrdinalIgnoreCase))
                errors.Add(LayerSettingError.UnknownLabelColumn);

            if (double.IsNaN(setting.LabelSize) || setting.LabelSize < MinLabelSize || setting.LabelSize > MaxLabelSize)
                errors.Add(LayerSettingError.LabelSizeOutOfRange);

            if (!IsValidOptionalColor(setting.LabelHexColor))
                errors.Add(LayerSettingError.InvalidLabelColor);

            CheckZoomRange(setting.MinLabelZoomLevel, setting.MaxLabelZoomLevel, LayerSettingError.LabelZoomOutOfRange, LayerSettingError.LabelZoomRangeInverted, errors);
        }

        return errors;
    }

    /// <summary>Empty (no colour) or #RRGGBB / #AARRGGBB.</summary>
    public static bool IsValidOptionalColor(string? hex) => string.IsNullOrEmpty(hex) || HexColor.IsMatch(hex);

    private static void CheckZoomRange(int? min, int? max, LayerSettingError outOfRange, LayerSettingError inverted, List<LayerSettingError> errors)
    {
        if (min is < MinZoomLevel or > MaxZoomLevel || max is < MinZoomLevel or > MaxZoomLevel)
            errors.Add(outOfRange);
        else if (min.HasValue && max.HasValue && min.Value > max.Value)
            errors.Add(inverted);
    }
}
