using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IRI.Maptor.Jab.Core;
using IRI.Maptor.Jab.Core.Localization;
using IRI.Maptor.Jab.Core.Models;
using IRI.Maptor.Sta.Versioning;

namespace IRI.Maptor.Jab.Wpf.ViewModels.Versioning;

public class MyPendingFunctions
{
    public Func<CancellationToken, Task<List<MyProposalDto>>> LoadMineAsync { get; set; }

    public Action<string> ShowMessage { get; set; } = _ => { };
}

/// <summary>
/// The editor's own proposals (doc 05 §3) with COLLAPSED statuses — provisional review
/// outcomes are indistinguishable by design (doc 02 §7 amendment). Manual refresh (D37).
/// </summary>
public class MyPendingViewModel : Notifier
{
    private readonly MyPendingFunctions _functions;

    public MyPendingViewModel(MyPendingFunctions functions)
    {
        _functions = functions;

        RefreshCommand = new RelayCommand(_ => _ = RefreshAsync(), _ => !IsBusy);

        Rows.CollectionChanged += (_, _) => RaisePropertyChanged(nameof(IsEmpty));
    }

    public ObservableCollection<MyPendingRowViewModel> Rows { get; } = new();

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

    public RelayCommand RefreshCommand { get; }

    public async Task RefreshAsync()
    {
        IsBusy = true;

        try
        {
            var items = await _functions.LoadMineAsync(CancellationToken.None);

            Rows.Clear();
            foreach (var item in items)
                Rows.Add(new MyPendingRowViewModel(item));

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
}

public class MyPendingRowViewModel
{
    public MyPendingRowViewModel(MyProposalDto item)
    {
        Item = item;
    }

    public MyProposalDto Item { get; }

    public string TargetLabel => Item.TargetFeatureId?.ToString() ?? "—";

    public string SubmittedAtLabel => Item.SubmittedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm");

    public string ChangeTypeLabel => VersioningLabels.ChangeType(Item.ChangeType);

    public string StatusLabel => Item.Status switch
    {
        EditorFacingStatus.PendingReview => LocalizationManager.Instance["versioning_status_pendingReview"],
        EditorFacingStatus.InCompetition => string.Format(LocalizationManager.Instance["versioning_status_inCompetition"], Item.CompetitorCount),
        EditorFacingStatus.UnderReview => LocalizationManager.Instance["versioning_status_underReview"],
        EditorFacingStatus.Committed => LocalizationManager.Instance["versioning_status_committed"],
        EditorFacingStatus.Rejected => LocalizationManager.Instance["versioning_status_rejected"],
        _ => LocalizationManager.Instance["versioning_status_withdrawn"],
    };
}
