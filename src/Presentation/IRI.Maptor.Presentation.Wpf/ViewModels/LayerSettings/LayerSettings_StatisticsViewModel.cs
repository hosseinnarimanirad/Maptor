using System;
using System.Linq;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Persistence.Abstractions;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Presentation.Wpf.Converters;
using IRI.Maptor.Presentation.Wpf.Models.LayerSettings;
using IRI.Maptor.Presentation.Core.Localization;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.LayerSettings;

public enum StatisticsMode
{
    /// <summary>The statistics of one field.</summary>
    Field,

    /// <summary>Count, length/area and a field total per value of one field.</summary>
    GroupBy,

    /// <summary>Empty geometries and the per-feature length/area of the layer; no field.</summary>
    Geometry,
}

/// <summary>What the result panel shows when both are available (a numeric field).</summary>
public enum StatisticsResultView
{
    Histogram,
    Table,
}

/// <summary>One column of the histogram: its height in pixels and the tooltip with its range and count.</summary>
public record StatisticsHistogramBin(double BarHeight, string ToolTip);

/// <summary>A field in the statistics tab's pickers: the alias when the source has one. A null field is the "none" choice.</summary>
public record StatisticsFieldOption(Field? Field, string DisplayName);

/// <summary>A headline number of the result (feature count, total length, ...).</summary>
public record StatisticsTile(string Caption, string Value);

/// <summary>One line of the detail list (caption and formatted value).</summary>
public record StatisticsRow(string Label, string Value);

/// <summary>
/// One line of the result table: a value of the field with its count and share (both modes) and, in group-by
/// mode, its total length/area and field total. <see cref="Bar"/> is the share relative to the largest row, 0..1;
/// <see cref="IsEmptyValue"/> marks the row of the empty (null or blank) features, shown muted;
/// <see cref="IsOutOfDomain"/> a value the field's domain does not allow, shown as a warning.
/// </summary>
public record StatisticsTableRow(string Value, string Count, string Percent, double Bar, string? Measure, string? Sum, bool IsEmptyValue = false, bool IsOutOfDomain = false);

/// <summary>
/// The Statistics tab of the layer settings dialog: the statistics of one attribute, or the features grouped
/// by one attribute, or the geometries, over all the layer's features. Computed in memory for Cache layers (and
/// file layers); a Live layer holds only part of its features, so its statistics are computed by the server
/// (<see cref="IServerQueryableVectorDataSource"/>) and shown the same way (<see cref="ServerStatistics"/>).
/// </summary>
public class LayerSettings_StatisticsViewModel : ViewModelBase
{
    /// <summary>Group-by rows shown at most; a field with more distinct values is better read elsewhere.</summary>
    public const int MaxGroupRows = 1_000;

    private static readonly LocalizedDateTimeConverter DateConverter = new() { Format = "yyyy/MM/dd" };

    private readonly VectorLayer? _layer;

    // read once per dialog: the dialog is modal, so the layer cannot change under it
    private IReadOnlyList<Feature<Point>>? _features;

    private GeometryMeasureKind _measureKind;

    private double?[]? _measures;

    private CancellationTokenSource? _cancellation;

    // bumped by every compute and every change of the inputs: a run whose number is stale shows nothing
    private int _version;

    public ObservableCollection<StatisticsFieldOption> Fields { get; } = new();

    /// <summary>Numeric fields for the group-by total, after a "none" choice.</summary>
    public ObservableCollection<StatisticsFieldOption> SumFields { get; } = new();

    private StatisticsMode _mode;
    public StatisticsMode Mode
    {
        get { return _mode; }
        set
        {
            _mode = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsGroupByMode));
            RaisePropertyChanged(nameof(IsGeometryMode));

            InputsChanged();
        }
    }

    public bool IsGroupByMode => Mode == StatisticsMode.GroupBy;

    public bool IsGeometryMode => Mode == StatisticsMode.Geometry;

    /// <summary>Height of the histogram's plot area, pixels.</summary>
    public const double HistogramHeight = 140;

    private StatisticsResultView _resultView = StatisticsResultView.Histogram;
    public StatisticsResultView ResultView
    {
        get { return _resultView; }
        set
        {
            _resultView = value;
            RaisePropertyChanged();

            RaiseResultLayoutChanged();
        }
    }

    public ObservableCollection<StatisticsHistogramBin> HistogramBins { get; } = new();

    private string? _histogramTitle;

    private string? _histogramSubtitle;

    // lower edge of the first bin, the middle and the upper edge of the last bin, as axis labels
    private string? _histogramAxisStart, _histogramAxisMiddle, _histogramAxisEnd;

    public string? HistogramAxisStart { get => _histogramAxisStart; private set { _histogramAxisStart = value; RaisePropertyChanged(); } }

    public string? HistogramAxisMiddle { get => _histogramAxisMiddle; private set { _histogramAxisMiddle = value; RaisePropertyChanged(); } }

    public string? HistogramAxisEnd { get => _histogramAxisEnd; private set { _histogramAxisEnd = value; RaisePropertyChanged(); } }

    /// <summary>The count of the tallest bin, shown at the top of the plot.</summary>
    private string? _histogramMaxCount;
    public string? HistogramMaxCount { get => _histogramMaxCount; private set { _histogramMaxCount = value; RaisePropertyChanged(); } }

    public bool HasHistogram => HistogramBins.Count > 0;

    // the table wins only when the user picked it and there is one
    public bool IsHistogramVisible => HasHistogram && (ResultView == StatisticsResultView.Histogram || TableRows.Count == 0);

    public bool IsTableVisible => TableRows.Count > 0 && !IsHistogramVisible;

    public bool IsResultViewSwitchVisible => HasHistogram && TableRows.Count > 0;

    public bool IsResultPanelVisible => IsHistogramVisible || IsTableVisible;

    public string? ResultPanelTitle => IsHistogramVisible ? _histogramTitle : TableTitle;

    public string? ResultPanelSubtitle => IsHistogramVisible ? _histogramSubtitle : TableSubtitle;

    private StatisticsFieldOption? _selectedField;
    public StatisticsFieldOption? SelectedField
    {
        get { return _selectedField; }
        set
        {
            _selectedField = value;
            RaisePropertyChanged();

            InputsChanged();
        }
    }

    private StatisticsFieldOption? _selectedSumField;
    public StatisticsFieldOption? SelectedSumField
    {
        get { return _selectedSumField; }
        set
        {
            _selectedSumField = value;
            RaisePropertyChanged();

            InputsChanged();
        }
    }

    /// <summary>The layer is in Live mode: its features stay on the server, which computes the statistics (unsaved edits are not in them).</summary>
    public bool IsLiveLayer { get; }

    public bool CanCompute => _layer is not null;

    private bool _isBusy;
    public bool IsBusy
    {
        get { return _isBusy; }
        set
        {
            _isBusy = value;
            RaisePropertyChanged();

            ComputeCommand.RaiseCanExecuteChanged();
            CancelCommand.RaiseCanExecuteChanged();
        }
    }

    private bool _hasResult;
    public bool HasResult
    {
        get { return _hasResult; }
        set
        {
            _hasResult = value;
            RaisePropertyChanged();
        }
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get { return _errorMessage; }
        set
        {
            _errorMessage = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>A remark under the result (rows left out, unreadable values, ...).</summary>
    private string? _resultNote;
    public string? ResultNote
    {
        get { return _resultNote; }
        set
        {
            _resultNote = value;
            RaisePropertyChanged();
        }
    }

    public ObservableCollection<StatisticsTile> Tiles { get; } = new();

    /// <summary>Field mode: min, max, mean, ... of the field.</summary>
    public ObservableCollection<StatisticsRow> DetailRows { get; } = new();

    /// <summary>Field mode: the most frequent values; group-by mode: every group.</summary>
    public ObservableCollection<StatisticsTableRow> TableRows { get; } = new();

    /// <summary>Group-by mode: the totals line under the groups; null hides it.</summary>
    private StatisticsTableRow? _tableFooter;
    public StatisticsTableRow? TableFooter
    {
        get { return _tableFooter; }
        set
        {
            _tableFooter = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>A short remark beside the table title (how many rows of how many, the order).</summary>
    private string? _tableSubtitle;
    public string? TableSubtitle
    {
        get { return _tableSubtitle; }
        set
        {
            _tableSubtitle = value;
            RaisePropertyChanged();
        }
    }

    private string? _tableTitle;
    public string? TableTitle
    {
        get { return _tableTitle; }
        set
        {
            _tableTitle = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>Header of the length/area column, with its unit; null hides the column.</summary>
    private string? _measureHeader;
    public string? MeasureHeader
    {
        get { return _measureHeader; }
        set
        {
            _measureHeader = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>Header of the field-total column; null hides the column.</summary>
    private string? _sumHeader;
    public string? SumHeader
    {
        get { return _sumHeader; }
        set
        {
            _sumHeader = value;
            RaisePropertyChanged();
        }
    }

    public RelayCommand ComputeCommand { get; }

    public RelayCommand CancelCommand { get; }

    public LayerSettings_StatisticsViewModel(VectorLayer? layer)
    {
        _layer = layer;

        IsLiveLayer = layer?.DataSource is IServerQueryableVectorDataSource { CanQueryServer: true };

        ComputeCommand = new RelayCommand(async _ => await ComputeAsync(), _ => CanCompute && (IsGeometryMode || SelectedField?.Field is not null) && !IsBusy);

        CancelCommand = new RelayCommand(_ => _cancellation?.Cancel(), _ => IsBusy);

        SumFields.Add(new StatisticsFieldOption(null, Localize("layerSettings_statistics_none")));

        foreach (var field in layer?.GetFields() ?? [])
        {
            if (!IsStatisticsField(field))
                continue;

            var option = new StatisticsFieldOption(field, string.IsNullOrWhiteSpace(field.Alias) ? field.Name : field.Alias!);

            Fields.Add(option);

            if (FieldStatistics.GetKind(field) == FieldStatisticsKind.Numeric)
                SumFields.Add(option);
        }

        _selectedField = Fields.FirstOrDefault();
        _selectedSumField = SumFields[0];
    }

    // mirrors the skip rules of the attribute table (FeatureTableFilterManager.IsFilterableField)
    private static bool IsStatisticsField(Field field)
    {
        if (field is null || string.IsNullOrEmpty(field.Name))
            return false;

        if (FeatureTableHelper.IsGeometryField(field))
            return false;

        if (field.Name.EqualsIgnoreCase("rowversion"))
            return false;

        return field.CanRead;
    }

    private void InputsChanged()
    {
        _version++;

        ClearResult();
        ComputeCommand?.RaiseCanExecuteChanged();
    }

    private async Task ComputeAsync()
    {
        if (_layer is null || (!IsGeometryMode && SelectedField?.Field is null))
            return;

        var field = SelectedField?.Field;
        var sumField = SelectedSumField?.Field;
        var mode = Mode;
        var version = ++_version;

        _cancellation?.Dispose();
        _cancellation = new CancellationTokenSource();
        var token = _cancellation.Token;

        ClearResult();
        IsBusy = true;

        try
        {
            if (IsLiveLayer && _layer.DataSource is IServerQueryableVectorDataSource server)
            {
                await ComputeOnServerAsync(server, mode, field, sumField, version, token);
                return;
            }

            if (_features is null)
            {
                var featureSet = await _layer.GetFeaturesAsync();

                _features = featureSet?.Features.ToList() ?? [];

                _measureKind = GeometryMeasure.GetKind(_layer.DataSource is IVectorDataSource source ? source.GeometryType : null, _features);
            }

            var features = _features;

            if (mode == StatisticsMode.Field)
            {
                var statistics = await Task.Run(() => FieldStatistics.Calculate(features, field!, token), token);

                if (version == _version)
                    ShowFieldResult(statistics);

                return;
            }

            if (_measures is null && _measureKind != GeometryMeasureKind.None)
            {
                var kind = _measureKind;

                _measures = await Task.Run(() => GeometryMeasure.Calculate(features, kind, token), token);
            }

            var measures = _measures;
            var measureKind = _measureKind;

            if (mode == StatisticsMode.GroupBy)
            {
                var statistics = await Task.Run(() => GroupStatistics.Calculate(features, field!, measureKind, measures, sumField, token), token);

                if (version == _version)
                    ShowGroupResult(statistics, SelectedSumField?.DisplayName);
            }
            else
            {
                var statistics = await Task.Run(() => GeometryStatistics.Calculate(features, measureKind, measures), token);

                if (version == _version)
                    ShowGeometryResult(statistics);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = $"{Localize("layerSettings_statistics_failed")} {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // the same three results as in memory, from the server's statistics over the whole layer
    private async Task ComputeOnServerAsync(IServerQueryableVectorDataSource server, StatisticsMode mode, Field? field, Field? sumField, int version, CancellationToken token)
    {
        var measureKind = GeometryMeasure.GetKind(server.GeometryType);

        var serverMeasureKind = ServerStatistics.ToServerMeasureKind(measureKind);

        if (mode == StatisticsMode.Field)
        {
            var statistics = ServerStatistics.ToFieldStatistics(await server.GetFieldStatisticsAsync(field!.Name, token), field);

            if (version == _version)
                ShowFieldResult(statistics);
        }
        else if (mode == StatisticsMode.GroupBy)
        {
            var dto = await server.GetGroupStatisticsAsync(field!.Name, sumField?.Name, serverMeasureKind, token);

            var statistics = ServerStatistics.ToGroupStatistics(dto, field);

            if (version == _version)
                ShowGroupResult(statistics, SelectedSumField?.DisplayName);
        }
        else
        {
            var statistics = ServerStatistics.ToGeometryStatistics(await server.GetGeometryStatisticsAsync(serverMeasureKind, token));

            if (version == _version)
                ShowGeometryResult(statistics);
        }
    }

    private void ShowFieldResult(FieldStatistics statistics)
    {
        Tiles.Add(new(Localize("layerSettings_statistics_featureCount"), FormatCount(statistics.FeatureCount)));
        Tiles.Add(new(Localize("layerSettings_statistics_emptyCount"), FormatCountWithPercent(statistics.EmptyCount, statistics.FeatureCount)));
        Tiles.Add(new(Localize("layerSettings_statistics_distinctCount"), FormatCount(statistics.DistinctCount)));

        if (statistics.Domain is not null)
            Tiles.Add(new(Localize("layerSettings_statistics_outOfDomainCount"), FormatCountWithPercent(statistics.OutOfDomainCount, statistics.FeatureCount)));

        if (statistics.Kind == FieldStatisticsKind.Numeric && statistics.Min.HasValue)
        {
            DetailRows.Add(new(Localize("layerSettings_statistics_min"), FormatNumber(statistics.Min)));
            DetailRows.Add(new(Localize("layerSettings_statistics_max"), FormatNumber(statistics.Max)));
            DetailRows.Add(new(Localize("layerSettings_statistics_sum"), FormatNumber(statistics.Sum)));
            DetailRows.Add(new(Localize("layerSettings_statistics_mean"), FormatNumber(statistics.Mean)));
            DetailRows.Add(new(Localize("layerSettings_statistics_median"), FormatNumber(statistics.Median)));
            DetailRows.Add(new(Localize("layerSettings_statistics_standardDeviation"), FormatNumber(statistics.StandardDeviation)));
        }

        if (statistics.Kind == FieldStatisticsKind.Date && statistics.MinDate.HasValue)
        {
            DetailRows.Add(new(Localize("layerSettings_statistics_minDate"), FormatDate(statistics.MinDate.Value)));
            DetailRows.Add(new(Localize("layerSettings_statistics_maxDate"), FormatDate(statistics.MaxDate!.Value)));
        }

        TableTitle = Localize("layerSettings_statistics_topValues");
        TableSubtitle = statistics.Domain is null
            ? string.Format(Localize("layerSettings_statistics_topOfDistinct"), FormatCount(statistics.TopValues.Count), FormatCount(statistics.DistinctCount + (statistics.EmptyCount > 0 ? 1 : 0)))
            : string.Format(Localize("layerSettings_statistics_domainUsage"), FormatCount(statistics.UsedDomainValueCount), FormatCount(statistics.Domain.Values.Count));

        var maxCount = statistics.TopValues.Select(v => v.Count).DefaultIfEmpty(0).Max();

        foreach (var frequency in statistics.TopValues)
        {
            TableRows.Add(new StatisticsTableRow(
                FormatValue(frequency.Value),
                FormatCount(frequency.Count),
                FormatPercent(frequency.Percent),
                maxCount == 0 ? 0 : (double)frequency.Count / maxCount,
                null,
                null,
                frequency.Value is null,
                frequency.IsOutOfDomain));
        }

        if (statistics.Histogram is not null)
            ShowHistogram(statistics.Histogram, Localize("layerSettings_statistics_distribution"), 1, null);

        if (statistics.InvalidCount > 0)
            ResultNote = string.Format(Localize("layerSettings_statistics_invalidNote"), FormatCount(statistics.InvalidCount));

        HasResult = true;
        RaiseResultLayoutChanged();
    }

    private void ShowGeometryResult(GeometryStatistics statistics)
    {
        Tiles.Add(new(Localize("layerSettings_statistics_featureCount"), FormatCount(statistics.FeatureCount)));
        Tiles.Add(new(Localize("layerSettings_statistics_emptyGeometryCount"), FormatCountWithPercent(statistics.EmptyGeometryCount, statistics.FeatureCount)));

        var summary = statistics.Measures;
        var isLength = statistics.MeasureKind == GeometryMeasureKind.Length;

        if (summary is not null)
        {
            var totalUnit = MeasureUnit.For(statistics.MeasureKind, summary.Sum)!;

            Tiles.Add(new(Localize(isLength ? "layerSettings_statistics_totalLength" : "layerSettings_statistics_totalArea"), $"{totalUnit.Format(summary.Sum)} {totalUnit.Symbol}"));

            // per-feature values: one unit for the details and the histogram, picked from the largest
            var unit = MeasureUnit.For(statistics.MeasureKind, summary.Max)!;

            string WithUnit(double? value) => value.HasValue ? $"{unit.Format(value)} {unit.Symbol}" : "-";

            DetailRows.Add(new(Localize("layerSettings_statistics_min"), WithUnit(summary.Min)));
            DetailRows.Add(new(Localize("layerSettings_statistics_max"), WithUnit(summary.Max)));
            DetailRows.Add(new(Localize("layerSettings_statistics_mean"), WithUnit(summary.Mean)));
            DetailRows.Add(new(Localize("layerSettings_statistics_median"), WithUnit(summary.Median)));
            DetailRows.Add(new(Localize("layerSettings_statistics_standardDeviation"), WithUnit(summary.StandardDeviation)));

            if (summary.Histogram is not null)
            {
                var title = string.Format(Localize(isLength ? "layerSettings_statistics_lengthPerFeature" : "layerSettings_statistics_areaPerFeature"), unit.Symbol);

                ShowHistogram(summary.Histogram, title, unit.Factor, unit.Symbol);
            }
        }

        var notes = new List<string>();

        if (statistics.MeasureKind == GeometryMeasureKind.None)
            notes.Add(Localize("layerSettings_statistics_noMeasureNote"));

        if (statistics.UnmeasuredCount > 0)
            notes.Add(string.Format(Localize("layerSettings_statistics_unmeasuredNote"), FormatCount(statistics.UnmeasuredCount)));

        ResultNote = notes.Count == 0 ? null : string.Join(" ", notes);

        HasResult = true;
        RaiseResultLayoutChanged();
    }

    /// <summary>Fills the histogram; <paramref name="factor"/> divides the edges into the display unit.</summary>
    private void ShowHistogram(Histogram histogram, string title, double factor, string? unitSymbol)
    {
        var maxCount = histogram.Counts.DefaultIfEmpty(0).Max();
        var total = histogram.Counts.Sum();

        string Edge(double value) => FormatNumber(value / factor);

        for (int i = 0; i < histogram.BinCount; i++)
        {
            var count = histogram.Counts[i];

            // a non-empty bin always shows, however small next to the tallest
            var height = maxCount == 0 || count == 0 ? 0 : Math.Max(2, HistogramHeight * count / maxCount);

            var range = $"{Edge(histogram.GetLowerEdge(i))} – {Edge(histogram.GetUpperEdge(i))}{(unitSymbol is null ? "" : " " + unitSymbol)}";

            HistogramBins.Add(new StatisticsHistogramBin(height, $"{range}\n{FormatCount(count)} ({FormatPercent(total == 0 ? 0 : 100.0 * count / total)})"));
        }

        _histogramTitle = title;
        _histogramSubtitle = string.Format(Localize("layerSettings_statistics_binCount"), FormatCount(histogram.BinCount));

        HistogramAxisStart = Edge(histogram.GetLowerEdge(0));
        HistogramAxisMiddle = Edge(histogram.GetLowerEdge(0) + histogram.BinCount * histogram.BinWidth / 2);
        HistogramAxisEnd = Edge(histogram.GetUpperEdge(histogram.BinCount - 1));
        HistogramMaxCount = FormatCount(maxCount);
    }

    private void RaiseResultLayoutChanged()
    {
        RaisePropertyChanged(nameof(HasHistogram));
        RaisePropertyChanged(nameof(IsHistogramVisible));
        RaisePropertyChanged(nameof(IsTableVisible));
        RaisePropertyChanged(nameof(IsResultViewSwitchVisible));
        RaisePropertyChanged(nameof(IsResultPanelVisible));
        RaisePropertyChanged(nameof(ResultPanelTitle));
        RaisePropertyChanged(nameof(ResultPanelSubtitle));
    }

    private void ShowGroupResult(GroupStatistics statistics, string? sumFieldName)
    {
        var unit = MeasureUnit.For(statistics.MeasureKind, statistics.TotalMeasure ?? 0);

        Tiles.Add(new(Localize("layerSettings_statistics_groupCount"), FormatCount(statistics.GroupCount)));
        Tiles.Add(new(Localize("layerSettings_statistics_featureCount"), FormatCount(statistics.FeatureCount)));

        if (unit is not null)
        {
            var caption = Localize(statistics.MeasureKind == GeometryMeasureKind.Length ? "layerSettings_statistics_totalLength" : "layerSettings_statistics_totalArea");

            Tiles.Add(new(caption, $"{unit.Format(statistics.TotalMeasure)} {unit.Symbol}"));

            MeasureHeader = $"{caption} ({unit.Symbol})";
        }

        if (statistics.TotalSum.HasValue || SelectedSumField?.Field is not null)
        {
            var caption = string.Format(Localize("layerSettings_statistics_sumOf"), sumFieldName);

            Tiles.Add(new(caption, FormatNumber(statistics.TotalSum)));

            SumHeader = caption;
        }

        TableTitle = Localize("layerSettings_statistics_groups");
        TableSubtitle = Localize("layerSettings_statistics_sortedByCount");

        var maxCount = statistics.Rows.Select(r => r.Count).DefaultIfEmpty(0).Max();

        foreach (var row in statistics.Rows.Take(MaxGroupRows))
        {
            TableRows.Add(new StatisticsTableRow(
                FormatValue(row.Key),
                FormatCount(row.Count),
                FormatPercent(row.Percent),
                maxCount == 0 ? 0 : (double)row.Count / maxCount,
                unit?.Format(row.Measure),
                SumHeader is null ? null : FormatNumber(row.Sum),
                row.Key is null,
                row.IsOutOfDomain));
        }

        TableFooter = new StatisticsTableRow(
            Localize("layerSettings_statistics_total"),
            FormatCount(statistics.FeatureCount),
            FormatPercent(100),
            0,
            unit?.Format(statistics.TotalMeasure),
            SumHeader is null ? null : FormatNumber(statistics.TotalSum));

        var notes = new List<string>();

        // in memory every group is a row; the server sends the largest ones only (its cap is the same 1,000)
        var shownGroups = Math.Min(MaxGroupRows, statistics.Rows.Count);

        if (statistics.GroupCount > shownGroups)
            notes.Add(string.Format(Localize("layerSettings_statistics_groupsTruncated"), FormatCount(shownGroups), FormatCount(statistics.GroupCount)));

        if (statistics.InvalidCount > 0)
            notes.Add(string.Format(Localize("layerSettings_statistics_invalidNote"), FormatCount(statistics.InvalidCount)));

        if (statistics.OutOfDomainCount > 0)
            notes.Add(string.Format(Localize("layerSettings_statistics_outOfDomainNote"), FormatCount(statistics.OutOfDomainCount)));

        ResultNote = notes.Count == 0 ? null : string.Join(" ", notes);

        HasResult = true;
        RaiseResultLayoutChanged();
    }

    private void ClearResult()
    {
        Tiles.Clear();
        DetailRows.Clear();
        TableRows.Clear();
        TableTitle = null;
        TableSubtitle = null;
        TableFooter = null;
        MeasureHeader = null;
        SumHeader = null;
        ResultNote = null;
        HistogramBins.Clear();
        _histogramTitle = null;
        _histogramSubtitle = null;
        HistogramAxisStart = HistogramAxisMiddle = HistogramAxisEnd = HistogramMaxCount = null;
        HasResult = false;
        ErrorMessage = null;

        RaiseResultLayoutChanged();
    }

    private static string Localize(string key) => LocalizationManager.Instance[key];

    private static string FormatCount(int count) => count.ToString("#,0", CultureInfo.InvariantCulture);

    private static string FormatPercent(double percent) => percent.ToString("0.#", CultureInfo.InvariantCulture) + "%";

    private static string FormatCountWithPercent(int count, int total) =>
        total == 0 ? FormatCount(count) : $"{FormatCount(count)} ({FormatPercent(100.0 * count / total)})";

    private static string FormatNumber(double? value) => value?.ToString("#,0.###", CultureInfo.InvariantCulture) ?? "-";

    private static string FormatDate(DateTime date) => DateConverter.Convert(date, typeof(string), null!, CultureInfo.CurrentCulture) as string ?? date.ToString("yyyy/MM/dd");

    private static string FormatValue(object? value) => value switch
    {
        null => Localize("layerSettings_statistics_emptyValue"),
        double number => FormatNumber(number),
        DateTime date => FormatDate(date),
        _ => value.ToString() ?? string.Empty,
    };

    /// <summary>One unit for a whole length/area column, picked from the column's total so the rows compare at a glance.</summary>
    private record MeasureUnit(string Symbol, double Factor)
    {
        public static MeasureUnit? For(GeometryMeasureKind kind, double total) => kind switch
        {
            GeometryMeasureKind.Length => total >= 10_000 ? new("km", 1e3) : new("m", 1),
            GeometryMeasureKind.Area => total >= 1e6 ? new("km²", 1e6) : total >= 1e4 ? new("ha", 1e4) : new("m²", 1),
            _ => null,
        };

        public string Format(double? value) => value.HasValue ? (value.Value / Factor).ToString("#,0.##", CultureInfo.InvariantCulture) : "-";
    }
}
