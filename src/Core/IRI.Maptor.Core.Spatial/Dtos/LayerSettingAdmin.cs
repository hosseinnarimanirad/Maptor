using System;
using System.Collections.Generic;
using System.Linq;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Core.Spatial.Dtos;

/// <summary>
/// The columns of an application's LayerSetting entity that the administrator edits. Barg and Gas each
/// have their own entity with these same properties; implementing this lets both servers share
/// <see cref="LayerSettingAdmin"/>.
/// </summary>
public interface ILayerSettingRecord
{
    int Id { get; set; }

    string Title { get; set; }

    string? TableName { get; set; }

    bool IsActive { get; set; }

    int? MinLayerZoomLevel { get; set; }

    int? MaxLayerZoomLevel { get; set; }

    string? HexFill { get; set; }

    string? HexStroke { get; set; }

    double StrokeThickness { get; set; }

    double Opacity { get; set; }

    bool IsLayerOn { get; set; }

    string? ServiceUrl { get; set; }

    bool IsGroupLayer { get; set; }

    int? GroupLayerId { get; set; }

    bool IsExpandedInToc { get; set; }

    bool IsSearchable { get; set; }

    bool IsLabeled { get; set; }

    bool IsLabelOn { get; set; }

    string? LabelColumn { get; set; }

    double LabelSize { get; set; }

    string? LabelHexColor { get; set; }

    int? MinLabelZoomLevel { get; set; }

    int? MaxLabelZoomLevel { get; set; }

    int TocOrder { get; set; }

    string? LayerSld { get; set; }

    LayerLoadMode LoadMode { get; set; }

    DateTime UpdatedAt { get; set; }
}

/// <summary>How an admin request ended; each server maps it to its HTTP status.</summary>
public enum LayerSettingAdminStatus
{
    Ok,

    /// <summary>404: no such row.</summary>
    NotFound,

    /// <summary>400: see <see cref="LayerSettingAdminResult{T}.Errors"/>.</summary>
    Invalid,

    /// <summary>409: the row was saved by someone else after the client read it.</summary>
    Conflict,

    /// <summary>400: the row is a layer, not a group.</summary>
    NotAGroup,

    /// <summary>409: the group still lists layers.</summary>
    GroupNotEmpty,
}

public class LayerSettingAdminResult<T>
{
    public LayerSettingAdminStatus Status { get; set; }

    public T? Value { get; set; }

    public List<LayerSettingError> Errors { get; set; } = new();

    public static LayerSettingAdminResult<T> Ok(T value) => new() { Status = LayerSettingAdminStatus.Ok, Value = value };

    public static LayerSettingAdminResult<T> Fail(LayerSettingAdminStatus status) => new() { Status = status };

    public static LayerSettingAdminResult<T> Invalid(List<LayerSettingError> errors) => new() { Status = LayerSettingAdminStatus.Invalid, Errors = errors };
}

/// <summary>The server side of the layer-settings admin window, shared by Barg and Gas.</summary>
public static class LayerSettingAdmin
{
    /// <summary>
    /// Groups created by an administrator get ids from here up, far above the seeded groups and layer ids
    /// of both applications, so a later seed never collides with them.
    /// </summary>
    public const int FirstCreatedGroupId = 900_000;

    public static LayerSettingAdminDto ToAdminDto(ILayerSettingRecord row, List<string>? fieldNames) => new()
    {
        Id = row.Id,
        IsGroupLayer = row.IsGroupLayer,
        GroupLayerId = row.GroupLayerId,
        TocOrder = row.TocOrder,
        TableName = row.TableName,
        ServiceUrl = row.ServiceUrl,
        FieldNames = fieldNames,
        Title = row.Title,
        IsActive = row.IsActive,
        IsLayerOn = row.IsLayerOn,
        IsExpandedInToc = row.IsExpandedInToc,
        IsSearchable = row.IsSearchable,
        MinLayerZoomLevel = row.MinLayerZoomLevel,
        MaxLayerZoomLevel = row.MaxLayerZoomLevel,
        // fixed-width colour columns (Barg's nchar(9)) come back padded: "#646464  "
        HexFill = row.HexFill?.Trim(),
        HexStroke = row.HexStroke?.Trim(),
        StrokeThickness = row.StrokeThickness,
        Opacity = row.Opacity,
        LayerSld = row.LayerSld,
        IsLabeled = row.IsLabeled,
        IsLabelOn = row.IsLabelOn,
        LabelColumn = row.LabelColumn,
        LabelSize = row.LabelSize,
        LabelHexColor = row.LabelHexColor?.Trim(),
        MinLabelZoomLevel = row.MinLabelZoomLevel,
        MaxLabelZoomLevel = row.MaxLabelZoomLevel,
        LoadMode = row.LoadMode,
        UpdatedAt = row.UpdatedAt,
    };

    /// <summary>
    /// Copies a validated edit onto the row. Text is trimmed and blank text stored as null. A group keeps
    /// its layer-only columns as they are.
    /// </summary>
    public static void Apply(ILayerSettingRecord row, LayerSettingUpdateDto edit)
    {
        row.Title = edit.Title.Trim();
        row.IsActive = edit.IsActive;
        row.IsLayerOn = edit.IsLayerOn;
        row.IsExpandedInToc = edit.IsExpandedInToc;

        if (row.IsGroupLayer)
            return;

        row.IsSearchable = edit.IsSearchable;
        row.MinLayerZoomLevel = edit.MinLayerZoomLevel;
        row.MaxLayerZoomLevel = edit.MaxLayerZoomLevel;
        row.HexFill = NullIfBlank(edit.HexFill);
        row.HexStroke = NullIfBlank(edit.HexStroke);
        row.StrokeThickness = edit.StrokeThickness;
        row.Opacity = edit.Opacity;
        row.LayerSld = NullIfBlank(edit.LayerSld);
        row.IsLabeled = edit.IsLabeled;
        row.IsLabelOn = edit.IsLabeled && edit.IsLabelOn;
        row.LabelColumn = NullIfBlank(edit.LabelColumn);
        row.LabelSize = edit.LabelSize;
        row.LabelHexColor = NullIfBlank(edit.LabelHexColor);
        row.MinLabelZoomLevel = edit.MinLabelZoomLevel;
        row.MaxLabelZoomLevel = edit.MaxLabelZoomLevel;
        row.LoadMode = edit.LoadMode;
    }

    /// <summary>
    /// The concurrency check: <paramref name="readUpdatedAt"/> is the value the client read. Compared by
    /// ticks, ignoring <see cref="DateTimeKind"/> (the database returns Unspecified).
    /// </summary>
    public static bool IsCurrent(ILayerSettingRecord row, DateTime readUpdatedAt) => row.UpdatedAt.Ticks == readUpdatedAt.Ticks;

    /// <summary>Why <paramref name="order"/> cannot be applied to <paramref name="rows"/> (all rows of the table); empty when it can.</summary>
    public static List<LayerSettingError> ValidateOrder(IReadOnlyCollection<ILayerSettingRecord> rows, LayerSettingOrderDto order)
    {
        var errors = new List<LayerSettingError>();

        var byId = rows.ToDictionary(r => r.Id);

        if (order.Items.Select(i => i.Id).Distinct().Count() != order.Items.Count)
            errors.Add(LayerSettingError.DuplicateRow);

        foreach (var item in order.Items)
        {
            if (!byId.TryGetValue(item.Id, out var row))
            {
                errors.Add(LayerSettingError.UnknownRow);
                continue;
            }

            if (row.IsGroupLayer && item.GroupLayerId is not null)
                errors.Add(LayerSettingError.GroupInGroup);
            else if (item.GroupLayerId is int groupId && !(byId.TryGetValue(groupId, out var group) && group.IsGroupLayer))
                errors.Add(LayerSettingError.UnknownGroup);
        }

        return errors.Distinct().ToList();
    }

    /// <summary>Applies a validated order; returns the rows that actually changed.</summary>
    public static List<ILayerSettingRecord> ApplyOrder(IReadOnlyCollection<ILayerSettingRecord> rows, LayerSettingOrderDto order)
    {
        var byId = rows.ToDictionary(r => r.Id);

        var changed = new List<ILayerSettingRecord>();

        foreach (var item in order.Items)
        {
            var row = byId[item.Id];

            if (row.GroupLayerId == item.GroupLayerId && row.TocOrder == item.TocOrder)
                continue;

            row.GroupLayerId = item.GroupLayerId;
            row.TocOrder = item.TocOrder;
            changed.Add(row);
        }

        return changed;
    }

    /// <summary>The id of a group created now: the next one from <see cref="FirstCreatedGroupId"/>.</summary>
    public static int NextGroupId(IEnumerable<ILayerSettingRecord> rows) =>
        Math.Max(FirstCreatedGroupId, rows.Select(r => r.Id).DefaultIfEmpty(0).Max() + 1);

    /// <summary>A new group goes on top of the top-level rows (a higher TocOrder is listed first).</summary>
    public static int NextTopLevelTocOrder(IEnumerable<ILayerSettingRecord> rows) =>
        rows.Where(r => r.GroupLayerId is null).Select(r => r.TocOrder).DefaultIfEmpty(0).Max() + 1;

    /// <summary>Fills a new group row; the caller adds it to the table.</summary>
    public static void InitializeGroup(ILayerSettingRecord row, int id, LayerGroupCreateDto request, int tocOrder)
    {
        row.Id = id;
        row.Title = request.Title.Trim();
        row.IsGroupLayer = true;
        row.GroupLayerId = null;
        row.IsActive = true;
        row.IsLayerOn = true;
        row.IsExpandedInToc = request.IsExpandedInToc;
        row.Opacity = 1;
        row.TocOrder = tocOrder;
        row.LoadMode = LayerLoadMode.Cache;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value!.Trim();
}
