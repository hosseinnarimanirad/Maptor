using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

using IRI.Maptor.Jab.Core;
using IRI.Maptor.Jab.Core.Localization;
using IRI.Maptor.Jab.Core.Models;
using IRI.Maptor.Sta.Versioning;

namespace IRI.Maptor.Jab.Wpf.ViewModels.Versioning;

/// <summary>
/// Reviewer queue (doc 05 §4): competitions on top, singleton proposals in a bulk-accept
/// section below. Manual refresh only (D37). Opening a row navigates to the compare view
/// (master-detail inside one window via <see cref="CurrentCompare"/>).
/// </summary>
public class ReviewQueueViewModel : Notifier
{
    private readonly ReviewFunctions _functions;

    public ReviewQueueViewModel(ReviewFunctions functions)
    {
        _functions = functions;

        RefreshCommand = new RelayCommand(_ => _ = RefreshAsync(), _ => !IsBusy);
        OpenCompareCommand = new RelayCommand(p => _ = OpenCompareAsync(p as ReviewQueueRowViewModel), _ => !IsBusy);
        BulkAcceptSelectedCommand = new RelayCommand(_ => _ = BulkAcceptSelectedAsync(), _ => !IsBusy && Singles.Any(s => s.IsSelected));
        GroupSelectedCommand = new RelayCommand(_ => _ = GroupSelectedAsync(), _ => !IsBusy && Singles.Count(s => s.IsSelected && s.Item.TargetFeatureId is null) >= 2);

        Competitions.CollectionChanged += (_, _) => RaisePropertyChanged(nameof(IsCompetitionsEmpty));
        Singles.CollectionChanged += (_, _) => RaisePropertyChanged(nameof(IsSinglesEmpty));
    }

    public ObservableCollection<ReviewQueueRowViewModel> Competitions { get; } = new();

    public ObservableCollection<ReviewQueueRowViewModel> Singles { get; } = new();

    /// <summary>Drive the per-section empty-state captions; an empty grid alone reads as broken.</summary>
    public bool IsCompetitionsEmpty => Competitions.Count == 0;

    public bool IsSinglesEmpty => Singles.Count == 0;

    private CompetitionCompareViewModel? _currentCompare;
    /// <summary>Non-null while the compare detail is shown instead of the queue.</summary>
    public CompetitionCompareViewModel? CurrentCompare
    {
        get => _currentCompare;
        private set { _currentCompare = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(IsQueueVisible)); }
    }

    public bool IsQueueVisible => CurrentCompare is null;

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

    public RelayCommand RefreshCommand { get; }

    public RelayCommand OpenCompareCommand { get; }

    public RelayCommand BulkAcceptSelectedCommand { get; }

    public RelayCommand GroupSelectedCommand { get; }

    public async Task RefreshAsync()
    {
        IsBusy = true;

        try
        {
            var items = await _functions.LoadQueueAsync(CancellationToken.None);

            Competitions.Clear();
            Singles.Clear();

            foreach (var item in items)
            {
                var row = new ReviewQueueRowViewModel(item);

                if (item.ProposalCount > 1)
                    Competitions.Add(row);
                else
                    Singles.Add(row);
            }

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

    private async Task OpenCompareAsync(ReviewQueueRowViewModel? row)
    {
        if (row is null)
            return;

        IsBusy = true;

        try
        {
            var compare = new CompetitionCompareViewModel(_functions)
            {
                RequestShowGeometryComparison = _functions.ShowGeometryComparison,
            };

            await compare.LoadAsync(row.Item.CompetitionId);

            compare.CloseRequested += async (_, decisionMade) =>
            {
                CurrentCompare = null;

                if (decisionMade)
                    await RefreshAsync();
            };

            CurrentCompare = compare;
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

    private async Task BulkAcceptSelectedAsync()
    {
        var selected = Singles.Where(s => s.IsSelected).ToList();

        if (selected.Count == 0)
            return;

        IsBusy = true;

        try
        {
            var request = new BulkAcceptRequestDto
            {
                Items = selected.Select(s => new BulkAcceptItemDto
                {
                    CompetitionId = s.Item.CompetitionId,
                    CompetitionRowVersion = s.Item.CompetitionRowVersion,
                }).ToList(),
            };

            var results = await _functions.BulkAcceptAsync(request);

            var failed = results.Where(r => !r.Succeeded).ToList();

            if (failed.Count > 0)
                _functions.ShowMessage(string.Join(Environment.NewLine, failed.Select(f => f.Error)));

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

    private async Task GroupSelectedAsync()
    {
        var creates = Singles
            .Where(s => s.IsSelected && s.Item.TargetFeatureId is null && s.Item.SingleProposalId is not null)
            .Select(s => s.Item.SingleProposalId!.Value)
            .ToList();

        if (creates.Count < 2)
            return;

        IsBusy = true;

        try
        {
            await _functions.GroupProposalsAsync(new GroupProposalsRequestDto { ProposalIds = creates });
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

public class ReviewQueueRowViewModel : Notifier
{
    public ReviewQueueRowViewModel(ReviewQueueItemDto item)
    {
        Item = item;
    }

    public ReviewQueueItemDto Item { get; }

    public string TargetLabel => Item.TargetFeatureId?.ToString() ?? "—";

    public string AuthorsLabel => string.Join("، ", Item.AuthorDisplayNames);

    public string AgeLabel
    {
        get
        {
            var age = DateTime.UtcNow - Item.OldestSubmittedAt;

            // localized units: a bare "3d" reads as a stray Latin token in a Persian column
            if (age.TotalDays >= 1)
                return string.Format(LocalizationManager.Instance["versioning_review_ageDays"], (int)age.TotalDays);

            if (age.TotalHours >= 1)
                return string.Format(LocalizationManager.Instance["versioning_review_ageHours"], (int)age.TotalHours);

            return string.Format(LocalizationManager.Instance["versioning_review_ageMinutes"], Math.Max(1, (int)age.TotalMinutes));
        }
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; RaisePropertyChanged(); }
    }
}
