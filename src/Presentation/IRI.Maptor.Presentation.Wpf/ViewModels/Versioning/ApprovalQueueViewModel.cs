using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Core.Models;
using IRI.Maptor.Core.Versioning;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Versioning;

/// <summary>Transport delegates for the approval window; the host wires VersioningWebApi.</summary>
public class ApprovalFunctions
{
    public Func<CancellationToken, Task<List<ApprovalQueueItemDto>>> LoadQueueAsync { get; set; }

    public Func<CommitRequestDto, Task<CommitResultDto>> CommitAsync { get; set; }

    public Func<ReturnRequestDto, Task> ReturnAsync { get; set; }

    public Action<string> ShowMessage { get; set; } = _ => { };
}

/// <summary>
/// Approver's queue (doc 05 §6): Resolved competitions with fresh stale/orphan flags,
/// multi-select batch commit (all-or-nothing, E9) and per-row return with a reason.
/// Manual refresh only (D37).
/// </summary>
public class ApprovalQueueViewModel : Notifier
{
    private readonly ApprovalFunctions _functions;

    public ApprovalQueueViewModel(ApprovalFunctions functions)
    {
        _functions = functions;

        RefreshCommand = new RelayCommand(_ => _ = RefreshAsync(), _ => !IsBusy);
        CommitSelectedCommand = new RelayCommand(_ => _ = CommitSelectedAsync(), _ => !IsBusy && Rows.Any(r => r.IsSelected));
        ReturnCommand = new RelayCommand(p => _ = ReturnAsync(p as ApprovalQueueRowViewModel), _ => !IsBusy);

        Rows.CollectionChanged += (_, _) => RaisePropertyChanged(nameof(IsEmpty));
    }

    public ObservableCollection<ApprovalQueueRowViewModel> Rows { get; } = new();

    /// <summary>Drives the empty-state caption; an empty grid alone reads as a broken screen.</summary>
    public bool IsEmpty => Rows.Count == 0;

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set { _isBusy = value; RaisePropertyChanged(); }
    }

    private DateTime? _lastRefreshedAt;
    public DateTime? LastRefreshedAt
    {
        get => _lastRefreshedAt;
        private set { _lastRefreshedAt = value; RaisePropertyChanged(); }
    }

    private bool _staleOverride;
    /// <summary>D8: applies to selected stale rows on commit; recorded per competition.</summary>
    public bool StaleOverride
    {
        get => _staleOverride;
        set { _staleOverride = value; RaisePropertyChanged(); }
    }

    private string? _returnReason;
    public string? ReturnReason
    {
        get => _returnReason;
        set { _returnReason = value; RaisePropertyChanged(); }
    }

    public RelayCommand RefreshCommand { get; }

    public RelayCommand CommitSelectedCommand { get; }

    public RelayCommand ReturnCommand { get; }

    public async Task RefreshAsync()
    {
        IsBusy = true;

        try
        {
            var items = await _functions.LoadQueueAsync(CancellationToken.None);

            Rows.Clear();
            foreach (var item in items)
                Rows.Add(new ApprovalQueueRowViewModel(item));

            LastRefreshedAt = DateTime.Now;
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

    private async Task CommitSelectedAsync()
    {
        var selected = Rows.Where(r => r.IsSelected).ToList();

        if (selected.Count == 0)
            return;

        if (selected.Any(r => r.Item.IsStale) && !StaleOverride)
        {
            _functions.ShowMessage(LocalizationManager.Instance["versioning_approval_staleNeedsOverride"]);
            return;
        }

        IsBusy = true;

        try
        {
            var request = new CommitRequestDto
            {
                Items = selected.Select(r => new CommitItemDto
                {
                    CompetitionId = r.Item.CompetitionId,
                    CompetitionRowVersion = r.Item.CompetitionRowVersion,
                    StaleOverride = StaleOverride && r.Item.IsStale,
                }).ToList(),
            };

            var result = await _functions.CommitAsync(request);

            if (result.Warnings.Count > 0)
                _functions.ShowMessage(string.Join(Environment.NewLine, result.Warnings));

            StaleOverride = false;

            await RefreshAsync();
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

    private async Task ReturnAsync(ApprovalQueueRowViewModel? row)
    {
        if (row is null)
            return;

        if (string.IsNullOrWhiteSpace(ReturnReason))
        {
            _functions.ShowMessage(LocalizationManager.Instance["versioning_approval_returnReasonRequired"]);
            return;
        }

        IsBusy = true;

        try
        {
            await _functions.ReturnAsync(new ReturnRequestDto
            {
                CompetitionId = row.Item.CompetitionId,
                CompetitionRowVersion = row.Item.CompetitionRowVersion,
                Reason = ReturnReason!,
            });

            ReturnReason = null;

            await RefreshAsync();
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
}

public class ApprovalQueueRowViewModel : Notifier
{
    public ApprovalQueueRowViewModel(ApprovalQueueItemDto item)
    {
        Item = item;
    }

    public ApprovalQueueItemDto Item { get; }

    public string TargetLabel => Item.TargetFeatureId?.ToString() ?? "—";

    public string ResolvedAtLabel => Item.ResolvedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm");

    public string ChangeTypeLabel => VersioningLabels.ChangeType(Item.WinnerChangeType);

    public bool IsDelete => Item.WinnerChangeType == ProposalChangeType.Delete;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; RaisePropertyChanged(); }
    }
}
