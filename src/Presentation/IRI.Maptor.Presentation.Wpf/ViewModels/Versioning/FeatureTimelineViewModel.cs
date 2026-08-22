using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Core.Models;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Core.Versioning;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Versioning;

/// <summary>Transport delegates for the timeline view; the host wires VersioningWebApi.</summary>
public class TimelineFunctions
{
    public Func<CancellationToken, Task<List<VersionedLayerInfoDto>>> LoadLayersAsync { get; set; }

    public Func<Guid, long, CancellationToken, Task<FeatureTimelineDto>> LoadTimelineAsync { get; set; }

    public Action<string> ShowMessage { get; set; } = _ => { };

    /// <summary>(old, new) in WebMercator — wire to the presenter's RequestShowGeometryComparison.</summary>
    public Action<Geometry<Point>?, Geometry<Point>?>? ShowGeometryComparison { get; set; }
}

/// <summary>
/// Feature timeline (doc 03 §5.4): current live state plus the copy-on-write hops
/// newest-first, each carrying the provenance of the change that replaced it.
/// </summary>
public class FeatureTimelineViewModel : Notifier
{
    private readonly TimelineFunctions _functions;

    private FeatureTimelineDto? _timeline;

    public FeatureTimelineViewModel(TimelineFunctions functions)
    {
        _functions = functions;

        LoadCommand = new RelayCommand(_ => _ = LoadAsync(), _ => !IsBusy);
        ShowOnMapCommand = new RelayCommand(p => ShowOnMap(p as TimelineRowViewModel));
    }

    public ObservableCollection<VersionedLayerInfoDto> Layers { get; } = new();

    private VersionedLayerInfoDto? _selectedLayer;
    public VersionedLayerInfoDto? SelectedLayer
    {
        get => _selectedLayer;
        set { _selectedLayer = value; RaisePropertyChanged(); }
    }

    private string? _featureIdText;
    public string? FeatureIdText
    {
        get => _featureIdText;
        set { _featureIdText = value; RaisePropertyChanged(); }
    }

    public ObservableCollection<TimelineRowViewModel> Rows { get; } = new();

    public ObservableCollection<AttributeRowViewModel> AttributeRows { get; } = new();

    private TimelineRowViewModel? _selectedRow;
    public TimelineRowViewModel? SelectedRow
    {
        get => _selectedRow;
        set
        {
            _selectedRow = value;
            RaisePropertyChanged();
            BuildAttributeRows();
        }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set { _isBusy = value; RaisePropertyChanged(); }
    }

    public string TitleLabel => _timeline is null ? string.Empty : $"{_timeline.EntityName} — {_timeline.FeatureId}";

    public bool HasResult => _timeline is not null;

    /// <summary>The feature no longer exists live; the newest hop is its last state.</summary>
    public bool IsDeleted => _timeline is not null && _timeline.Live is null;

    public bool ShowEmpty => _timeline is not null && Rows.Count == 0;

    public RelayCommand LoadCommand { get; }

    public RelayCommand ShowOnMapCommand { get; }

    public async Task InitializeAsync()
    {
        IsBusy = true;

        try
        {
            var layers = await _functions.LoadLayersAsync(CancellationToken.None);

            Layers.Clear();
            foreach (var layer in layers)
                Layers.Add(layer);

            SelectedLayer = Layers.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _functions.ShowMessage(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Entry point for callers that already know the target (map context menu, queue rows).</summary>
    public async Task LoadForAsync(Guid layerKey, long featureId)
    {
        if (Layers.Count == 0)
            await InitializeAsync();

        SelectedLayer = Layers.FirstOrDefault(l => l.LayerKey == layerKey) ?? SelectedLayer;
        FeatureIdText = featureId.ToString();

        await LoadAsync();
    }

    public async Task LoadAsync()
    {
        if (SelectedLayer is null || !long.TryParse(FeatureIdText, out var featureId))
        {
            _functions.ShowMessage(LocalizationManager.Instance["versioning_timeline_featureIdInvalid"]);
            return;
        }

        IsBusy = true;

        try
        {
            _timeline = await _functions.LoadTimelineAsync(SelectedLayer.LayerKey, featureId, CancellationToken.None);

            Rows.Clear();

            if (_timeline.Live is not null)
                Rows.Add(new TimelineRowViewModel(_timeline.Live));

            foreach (var entry in _timeline.Entries)
                Rows.Add(new TimelineRowViewModel(entry));

            SelectedRow = Rows.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _timeline = null;
            Rows.Clear();
            SelectedRow = null;

            _functions.ShowMessage(ex.Message);
        }
        finally
        {
            IsBusy = false;

            RaisePropertyChanged(nameof(TitleLabel));
            RaisePropertyChanged(nameof(HasResult));
            RaisePropertyChanged(nameof(IsDeleted));
            RaisePropertyChanged(nameof(ShowEmpty));
        }
    }

    private void BuildAttributeRows()
    {
        AttributeRows.Clear();

        if (SelectedRow is null)
            return;

        foreach (var pair in SelectedRow.Attributes)
            AttributeRows.Add(new AttributeRowViewModel { Name = pair.Key, Value = FormatValue(pair.Value) });
    }

    private void ShowOnMap(TimelineRowViewModel? row)
    {
        if (row is null)
            return;

        // Historical state as "old", current live state as "new" (null when deleted).
        _functions.ShowGeometryComparison?.Invoke(
            row.IsLive ? null : ParseToMap(row.GeometryWkb, row.Srid),
            ParseToMap(_timeline?.Live?.GeometryWkb, _timeline?.Live?.Srid ?? 0));
    }

    private static string? FormatValue(object? value) => value switch
    {
        null => null,
        byte[] bytes => Convert.ToBase64String(bytes),
        DateTime dt => dt.ToString("yyyy/MM/dd HH:mm"),
        _ => value.ToString(),
    };

    private static Geometry<Point>? ParseToMap(byte[]? wkb, int srid)
    {
        if (wkb is null || wkb.Length == 0)
            return null;

        var geometry = Geometry<Point>.FromWkb(wkb, srid == 0 ? SridHelper.GeodeticWGS84 : srid);

        if (geometry.IsNullOrEmpty())
            return null;

        // The map draws in WebMercator.
        return geometry!.Srid == SridHelper.WebMercator
            ? geometry
            : geometry.Project(SrsBase.Create(SridHelper.WebMercator)!);
    }
}

public class TimelineRowViewModel
{
    private readonly FeatureTimelineEntryDto? _entry;

    public TimelineRowViewModel(LiveFeatureStateDto live)
    {
        IsLive = true;
        GeometryWkb = live.GeometryWkb;
        Srid = live.Srid;
        Attributes = live.Attributes;
    }

    public TimelineRowViewModel(FeatureTimelineEntryDto entry)
    {
        _entry = entry;
        GeometryWkb = entry.GeometryWkb;
        Srid = entry.Srid;
        Attributes = entry.Attributes;
    }

    public bool IsLive { get; }

    public byte[]? GeometryWkb { get; }

    public int Srid { get; }

    public Dictionary<string, object?> Attributes { get; }

    public string WhenLabel => IsLive
        ? LocalizationManager.Instance["versioning_timeline_liveNow"]
        : _entry!.SupersededAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm");

    public string ChangeTypeLabel => IsLive ? string.Empty : VersioningLabels.ChangeType(_entry!.ChangeType);

    public string ReplacedByLabel => IsLive ? string.Empty : _entry!.EditorDisplayName;

    public string SessionLabel => IsLive ? string.Empty : _entry!.SessionTitle ?? string.Empty;

    public string ApproverLabel => IsLive ? string.Empty : _entry!.ApproverDisplayName;

    public string BatchLabel => IsLive ? string.Empty : _entry!.CommitBatchId.ToString();
}

public class AttributeRowViewModel
{
    public string Name { get; set; } = string.Empty;

    public string? Value { get; set; }
}
