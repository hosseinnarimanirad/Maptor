using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IRI.Maptor.Jab.Core;
using IRI.Maptor.Jab.Core.Localization;
using IRI.Maptor.Jab.Core.Models;
using IRI.Maptor.Sta.Versioning;

namespace IRI.Maptor.Jab.Wpf.ViewModels.Versioning;

/// <summary>Transport delegates for the inbox view; the host wires VersioningWebApi.</summary>
public class InboxFunctions
{
    public Func<CancellationToken, Task<List<InboxItemDto>>> LoadInboxAsync { get; set; }

    public Func<InboxMarkReadRequestDto, Task> MarkReadAsync { get; set; }

    public Action<string> ShowMessage { get; set; } = _ => { };
}

/// <summary>
/// The notification inbox (doc 02 §8): the user's own digests (N1–N5), newest first,
/// unread rows emphasized. Manual refresh only (D37) — no polling, no push.
/// </summary>
public class InboxViewModel : Notifier
{
    private readonly InboxFunctions _functions;

    public InboxViewModel(InboxFunctions functions)
    {
        _functions = functions;

        RefreshCommand = new RelayCommand(_ => _ = RefreshAsync(), _ => !IsBusy);
        MarkAllReadCommand = new RelayCommand(_ => _ = MarkAllReadAsync(), _ => !IsBusy && UnreadCount > 0);

        Rows.CollectionChanged += (_, _) => RaisePropertyChanged(nameof(IsEmpty));
    }

    public ObservableCollection<InboxRowViewModel> Rows { get; } = new();

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

    public int UnreadCount => Rows.Count(r => !r.Item.IsRead);

    public string UnreadLabel => string.Format(LocalizationManager.Instance["versioning_inbox_unread"], UnreadCount);

    public RelayCommand RefreshCommand { get; }

    public RelayCommand MarkAllReadCommand { get; }

    public async Task RefreshAsync()
    {
        IsBusy = true;

        try
        {
            var items = await _functions.LoadInboxAsync(CancellationToken.None);

            Rows.Clear();
            foreach (var item in items)
                Rows.Add(new InboxRowViewModel(item));

            LastRefreshedAt = DateTime.Now;
        }
        catch (Exception ex)
        {
            _functions.ShowMessage(ex.Message);
        }
        finally
        {
            IsBusy = false;

            RaisePropertyChanged(nameof(UnreadCount));
            RaisePropertyChanged(nameof(UnreadLabel));
        }
    }

    private async Task MarkAllReadAsync()
    {
        IsBusy = true;

        try
        {
            await _functions.MarkReadAsync(new InboxMarkReadRequestDto { All = true });

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

public class InboxRowViewModel
{
    public InboxRowViewModel(InboxItemDto item)
    {
        Item = item;
    }

    public InboxItemDto Item { get; }

    public bool IsUnread => !Item.IsRead;

    public string WhenLabel => Item.CreatedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm");

    public string TypeLabel => Item.Type switch
    {
        NotificationType.CompetitionJoined => LocalizationManager.Instance["versioning_inbox_typeCompetitionJoined"],
        NotificationType.Committed => LocalizationManager.Instance["versioning_inbox_typeCommitted"],
        NotificationType.Rejected => LocalizationManager.Instance["versioning_inbox_typeRejected"],
        NotificationType.ClosedNoWinner => LocalizationManager.Instance["versioning_inbox_typeClosedNoWinner"],
        NotificationType.Returned => LocalizationManager.Instance["versioning_inbox_typeReturned"],
        _ => LocalizationManager.Instance["versioning_inbox_typeOrphaned"],
    };

    public string DetailLabel
    {
        get
        {
            var localization = LocalizationManager.Instance;
            var parts = new List<string>();

            if (Item.ItemCount > 0)
                parts.Add(string.Format(localization["versioning_inbox_items"], Item.ItemCount));

            if (Item.TargetFeatureIds.Count > 0)
                parts.Add(string.Format(localization["versioning_inbox_features"], string.Join(", ", Item.TargetFeatureIds)));

            if (Item.Reasons.Count > 0)
                parts.Add(string.Format(localization["versioning_inbox_reason"], string.Join("; ", Item.Reasons)));

            return string.Join(" — ", parts);
        }
    }
}
