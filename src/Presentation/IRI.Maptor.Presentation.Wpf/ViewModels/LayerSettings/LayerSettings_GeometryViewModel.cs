using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.Abstractions;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Spatial.Validation;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Wpf.Layers;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.LayerSettings;

/// <summary>One kind of problem in the tab's summary, with how many times it was found.</summary>
public record GeometryIssueGroupRow(GeometryIssueKind Kind, string Title, int Count);

/// <summary>One feature with problems in the tab's list.</summary>
public class GeometryProblemRow : Notifier
{
    public GeometryProblemRow(FeatureValidationResult<Point> result)
    {
        Result = result;

        FeatureLabel = GeometryIssueTexts.FeatureLabel(result.Feature);

        Problems = GeometryIssueTexts.IssuesText(result.Issues.Where(i => !i.IsFixedAutomatically));

        OutcomeText = GeometryIssueTexts.OutcomeText(result.Outcome);
    }

    public FeatureValidationResult<Point> Result { get; }

    public Feature<Point> Feature => Result.Feature;

    public string FeatureLabel { get; }

    public string Problems { get; }

    public string OutcomeText { get; }

    /// <summary>Fix applies a repaired geometry; otherwise the feature can only be deleted.</summary>
    public bool CanRepair => Result.Outcome == GeometryRepairOutcome.Repaired;
}

/// <summary>
/// Layer Settings › Geometry: checks the layer's geometries (<see cref="GeometryValidator"/>), lists the features with
/// problems, shows the selected one on the map, and fixes or deletes them as tracked edits saved with the layer's
/// Save. A Live layer is checked on the features loaded so far only (it would otherwise have to download the whole
/// layer). Without an editable source or the permission to edit geometries the tab only reports.
/// </summary>
public class LayerSettings_GeometryViewModel : ViewModelBase
{
    private readonly VectorLayer? _layer;

    private CancellationTokenSource? _cancellation;

    public LayerSettings_GeometryViewModel(VectorLayer? layer, bool canEditGeometry)
    {
        _layer = layer;

        IsLiveLayer = layer?.DataSource is IServerQueryableVectorDataSource { CanQueryServer: true };

        IsEditable = canEditGeometry && layer?.DataSource is IEditableVectorDataSource;

        NotEditableReason = IsEditable ? null : Localize(canEditGeometry ? "layerSettings_geometry_notEditable" : "layerSettings_geometry_noPermission");

        CheckCommand = new RelayCommand(async _ => await CheckAsync(), _ => _layer is not null && !IsBusy);

        CancelCommand = new RelayCommand(_ => _cancellation?.Cancel(), _ => IsBusy);

        FixSelectedCommand = new RelayCommand(async _ => await FixSelectedAsync(), _ => IsEditable && !IsBusy && SelectedProblem?.CanRepair == true);

        FixAllCommand = new RelayCommand(async _ => await FixAllAsync(), _ => IsEditable && !IsBusy && _repairable.Count > 0);

        DeleteSelectedCommand = new RelayCommand(async _ => await DeleteSelectedAsync(), _ => IsEditable && !IsBusy && SelectedProblem is { CanRepair: false });

        SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => IsEditable && !IsBusy && HasPendingChanges);
    }

    #region Callbacks (wired by the dialog's owner)

    /// <summary>Zoom to the feature and highlight it on the map.</summary>
    public Func<Feature<Point>, Task>? RequestShowOnMap { get; set; }

    public Action? RequestClearHighlight { get; set; }

    /// <summary>Repaint the map after a fix or a delete.</summary>
    public Action? RequestRefreshMap { get; set; }

    /// <summary>Save the layer's pending edits (through its attribute table when one is open).</summary>
    public Func<Task>? RequestSaveAsync { get; set; }

    /// <summary>Ask a yes/no question; null means yes.</summary>
    public Func<string, Task<bool>>? RequestConfirmAsync { get; set; }

    #endregion

    public bool IsLiveLayer { get; }

    public bool IsEditable { get; }

    /// <summary>Why the Fix/Delete buttons are off: no editable source, or no permission to edit geometries.</summary>
    public string? NotEditableReason { get; }

    public bool HasNotEditableReason => !string.IsNullOrEmpty(NotEditableReason);

    public ObservableCollection<GeometryIssueGroupRow> Groups { get; } = new();

    public ObservableCollection<GeometryProblemRow> Problems { get; } = new();

    // features with a repaired geometry: the listed ones and those whose only problem is ring orientation
    private List<GeometryProblemRow> _repairable = new();

    private GeometryProblemRow? _selectedProblem;
    public GeometryProblemRow? SelectedProblem
    {
        get => _selectedProblem;
        set
        {
            if (ReferenceEquals(_selectedProblem, value))
                return;

            _selectedProblem = value;
            RaisePropertyChanged();
            RaiseCommandsChanged();

            if (value is not null)
                _ = RequestShowOnMap?.Invoke(value.Feature);
            else
                RequestClearHighlight?.Invoke();
        }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            _isBusy = value;
            RaisePropertyChanged();
            RaiseCommandsChanged();
        }
    }

    private bool _hasResult;
    /// <summary>A check has run (the list and summary belong to it).</summary>
    public bool HasResult
    {
        get => _hasResult;
        private set
        {
            _hasResult = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasProblems));
            RaisePropertyChanged(nameof(IsClean));
        }
    }

    public bool HasProblems => HasResult && Problems.Count > 0;

    public bool IsClean => HasResult && Problems.Count == 0;

    private string? _summary;
    /// <summary>"3 of 120 features have geometry problems." or "No geometry problems in 120 features."</summary>
    public string? Summary
    {
        get => _summary;
        private set { _summary = value; RaisePropertyChanged(); }
    }

    private string? _orientationNote;
    /// <summary>"5 features have rings in the wrong direction; Fix all corrects them too." (null when none)</summary>
    public string? OrientationNote
    {
        get => _orientationNote;
        private set { _orientationNote = value; RaisePropertyChanged(); }
    }

    private string? _statusMessage;
    /// <summary>The result of the last fix, delete or save.</summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        private set { _statusMessage = value; RaisePropertyChanged(); }
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set { _errorMessage = value; RaisePropertyChanged(); }
    }

    public bool HasPendingChanges => _layer?.DataSource is IEditableVectorDataSource and IDataSource { HasPendingChanges: true };

    public RelayCommand CheckCommand { get; }

    public RelayCommand CancelCommand { get; }

    public RelayCommand FixSelectedCommand { get; }

    public RelayCommand FixAllCommand { get; }

    public RelayCommand DeleteSelectedCommand { get; }

    public RelayCommand SaveCommand { get; }

    /// <summary>Reads the features (Live: the loaded ones), checks them off the UI thread and fills the list.</summary>
    public async Task CheckAsync()
    {
        if (_layer?.DataSource is not IVectorDataSource dataSource)
            return;

        _cancellation?.Dispose();
        _cancellation = new CancellationTokenSource();
        var token = _cancellation.Token;

        ErrorMessage = null;
        IsBusy = true;

        try
        {
            IReadOnlyList<Feature<Point>> features;

            if (IsLiveLayer && dataSource is IServerQueryableVectorDataSource server)
            {
                features = server.GetLoadedFeatures()?.Features.ToList() ?? [];
            }
            else
            {
                // a lazy source (shapefile) reads its file on first use
                if (!dataSource.IsLoaded)
                    await dataSource.LoadAsync(token);

                features = (await _layer.GetFeaturesAsync())?.Features.ToList() ?? [];
            }

            var report = await Task.Run(() => GeometryValidator.Validate(features), token);

            token.ThrowIfCancellationRequested();

            ShowReport(report);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = $"{Localize("layerSettings_geometry_failed")} {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ShowReport(GeometryValidationReport<Point> report)
    {
        // keep the map selection only if the same feature is still listed
        var previous = SelectedProblem?.Feature;

        _selectedProblem = null;

        Groups.Clear();
        Problems.Clear();

        foreach (var group in report.Issues.Where(i => !i.IsFixedAutomatically).GroupBy(i => i.Kind).OrderBy(g => g.Key))
            Groups.Add(new GeometryIssueGroupRow(group.Key, GeometryIssueTexts.KindTitle(group.Key), group.Count()));

        var rows = report.ProblemFeatures.Select(p => new GeometryProblemRow(p)).ToList();

        foreach (var row in rows.Where(r => !r.Result.HasAutomaticFixesOnly))
            Problems.Add(row);

        _repairable = rows.Where(r => r.CanRepair).ToList();

        Summary = Problems.Count == 0
            ? string.Format(Localize("layerSettings_geometry_noProblems"), FormatCount(report.Features.Count))
            : string.Format(Localize("layerSettings_geometry_summary"), FormatCount(Problems.Count), FormatCount(report.Features.Count));

        OrientationNote = report.AutomaticallyFixedFeatureCount + report.ProblemFeatures.Count(p => !p.HasAutomaticFixesOnly && p.Issues.Any(i => i.IsFixedAutomatically)) is var withOrientation and > 0
            ? string.Format(Localize("layerSettings_geometry_orientationNote"), FormatCount(withOrientation))
            : null;

        HasResult = true;

        RaisePropertyChanged(nameof(SelectedProblem));

        if (previous is not null)
            SelectedProblem = Problems.FirstOrDefault(p => ReferenceEquals(p.Feature, previous));

        RaiseCommandsChanged();
    }

    /// <summary>Applies the repaired geometry of the selected feature (a tracked edit).</summary>
    public Task FixSelectedAsync() => FixAsync(SelectedProblem is { CanRepair: true } row ? [row] : []);

    /// <summary>Applies every repaired geometry, ring-orientation-only fixes included (tracked edits).</summary>
    public Task FixAllAsync() => FixAsync(_repairable);

    private async Task FixAsync(IReadOnlyList<GeometryProblemRow> rows)
    {
        if (!IsEditable || _layer?.DataSource is not IEditableVectorDataSource editable || rows.Count == 0)
            return;

        var fixedCount = 0;

        foreach (var row in rows.Where(r => r.CanRepair).ToList())
        {
            if (editable.UpdateGeometry(row.Feature, row.Result.RepairedGeometry!))
                fixedCount++;
        }

        await AfterEditAsync(string.Format(Localize("layerSettings_geometry_fixed"), FormatCount(fixedCount)));
    }

    /// <summary>Deletes the selected feature when it cannot be repaired, after asking (a tracked edit).</summary>
    public async Task DeleteSelectedAsync()
    {
        if (!IsEditable || _layer?.DataSource is not IEditableVectorDataSource editable || SelectedProblem is not { CanRepair: false } row)
            return;

        var question = string.Format(Localize("layerSettings_geometry_deleteConfirm"), row.FeatureLabel);

        if (RequestConfirmAsync is not null && !await RequestConfirmAsync(question))
            return;

        editable.Remove(row.Feature);

        await AfterEditAsync(string.Format(Localize("layerSettings_geometry_deleted"), row.FeatureLabel));
    }

    // the edits are tracked by the source; repaint the map and check again so the list shows what is left
    private async Task AfterEditAsync(string message)
    {
        RequestClearHighlight?.Invoke();

        RequestRefreshMap?.Invoke();

        await CheckAsync();

        StatusMessage = message;

        RaisePropertyChanged(nameof(HasPendingChanges));
        RaiseCommandsChanged();
    }

    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;

            if (RequestSaveAsync is not null)
                await RequestSaveAsync();
            else if (_layer?.DataSource is IEditableVectorDataSource editable)
                await editable.SaveChangesAsync();

            StatusMessage = Localize("layerSettings_geometry_saved");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"{Localize("layerSettings_geometry_failed")} {ex.Message}";
        }
        finally
        {
            IsBusy = false;

            RaisePropertyChanged(nameof(HasPendingChanges));
            RaiseCommandsChanged();
        }
    }

    private void RaiseCommandsChanged()
    {
        CheckCommand?.RaiseCanExecuteChanged();
        CancelCommand?.RaiseCanExecuteChanged();
        FixSelectedCommand?.RaiseCanExecuteChanged();
        FixAllCommand?.RaiseCanExecuteChanged();
        DeleteSelectedCommand?.RaiseCanExecuteChanged();
        SaveCommand?.RaiseCanExecuteChanged();
    }

    private static string Localize(string key) => LocalizationManager.Instance[key];

    private static string FormatCount(int count) => count.ToString("#,0", CultureInfo.InvariantCulture);
}
