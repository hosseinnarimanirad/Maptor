using System;

using IRI.Maptor.Core.FileGdb;
using IRI.Maptor.Core.FileGdb.Enums;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Localization;

namespace IRI.Maptor.Presentation.Wpf.Models.FileGdb;

/// <summary>
/// One row of the file geodatabase picker: a dataset, whether it can be added (and why not), whether it needs the
/// picker's coordinate-system choice, and whether it is ticked. Built off the UI thread: reading the row count reads
/// the table's header.
/// </summary>
public class FileGdbDatasetPickerItem : Notifier
{
    private bool _isSelected;

    private FileGdbDatasetPickerItem(FileGdbDataset dataset)
    {
        Dataset = dataset;
    }

    public FileGdbDataset Dataset { get; }

    public string Name => Dataset.Name;

    /// <summary>The alias, or the name when there is none; the layer gets this name.</summary>
    public string Title => string.IsNullOrWhiteSpace(Dataset.AliasName) ? Dataset.Name : Dataset.AliasName!;

    /// <summary>The name too, when the title is an alias that differs from it.</summary>
    public bool ShowsName => !string.Equals(Title, Name, StringComparison.Ordinal);

    /// <summary>The feature dataset, or null at the geodatabase root.</summary>
    public string? FeatureDatasetName => Dataset.FeatureDatasetName;

    /// <summary>The list groups its rows by this: the feature dataset's name, or the root's label.</summary>
    public string GroupName { get; private set; } = string.Empty;

    public string GeometryTypeText { get; private set; } = string.Empty;

    public string RowCountText { get; private set; } = string.Empty;

    public bool IsAvailable => UnavailableReason is null;

    /// <summary>Why the dataset cannot be added, in the user's language; null when it can.</summary>
    public string? UnavailableReason { get; private set; }

    /// <summary>The geodatabase gives no coordinate system Maptor can use: the picker's CRS choice applies.</summary>
    public bool NeedsSrs { get; private set; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            var newValue = value && IsAvailable;

            if (_isSelected == newValue)
                return;

            _isSelected = newValue;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// A row for <paramref name="dataset"/>. Datasets over <paramref name="rowLimit"/> rows (when given) are not
    /// available: the reader loads a whole dataset into memory.
    /// </summary>
    public static FileGdbDatasetPickerItem Create(FileGdbDataset dataset, long? rowLimit)
    {
        var loc = LocalizationManager.Instance;

        var item = new FileGdbDatasetPickerItem(dataset)
        {
            GroupName = dataset.FeatureDatasetName ?? loc["dialog_fileGdbPicker_rootGroup"],
        };

        // the same checks as FileGdbDataSource's constructor, as reasons the user can read
        if (dataset.Kind != GdbDatasetKind.FeatureClass)
            return item.Unavailable(loc["dialog_fileGdbPicker_reasonTable"]);

        if (dataset.Storage == GdbTableStorage.Compressed)
            return item.Unavailable(loc["dialog_fileGdbPicker_reasonCompressed"]);

        if (dataset.Storage != GdbTableStorage.Table)
            return item.Unavailable(loc["dialog_fileGdbPicker_reasonMissing"]);

        long rowCount;
        GdbTableGeometryType geometryType;

        try
        {
            rowCount = dataset.RowCount;
            geometryType = dataset.GeometryType;
        }
        catch (Exception)
        {
            return item.Unavailable(loc["dialog_fileGdbPicker_reasonUnreadable"]);
        }

        item.GeometryTypeText = ToText(geometryType);
        item.RowCountText = string.Format(loc.CurrentCulture, loc["dialog_fileGdbPicker_rowCount"], rowCount);

        if (geometryType == GdbTableGeometryType.Multipatch)
            return item.Unavailable(loc["dialog_fileGdbPicker_reasonMultipatch"]);

        if (rowLimit is long limit && rowCount > limit)
            return item.Unavailable(string.Format(loc.CurrentCulture, loc["dialog_fileGdbPicker_reasonTooLarge"], limit));

        item.NeedsSrs = !FileGdbDataSource.HasKnownSrs(dataset);

        return item;
    }

    private FileGdbDatasetPickerItem Unavailable(string reason)
    {
        UnavailableReason = reason;

        return this;
    }

    private static string ToText(GdbTableGeometryType type)
    {
        var loc = LocalizationManager.Instance;

        return type switch
        {
            GdbTableGeometryType.Point => loc["common_geometryType_point"],
            GdbTableGeometryType.Multipoint => loc["dialog_fileGdbPicker_multipoint"],
            GdbTableGeometryType.Polyline => loc["common_geometryType_polyline"],
            GdbTableGeometryType.Polygon => loc["common_geometryType_polygon"],
            _ => type.ToString(),
        };
    }
}
