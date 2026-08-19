using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Core;
using IRI.Maptor.Jab.Core.Models;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.Versioning;

namespace IRI.Maptor.Jab.Wpf.ViewModels.Versioning;

/// <summary>
/// N-way compare (doc 05 §5): raw states from the server, diffs computed here. Geometry
/// inspection reuses the map's existing two-way comparison — live vs the chosen proposal.
/// Reviewers see real states; the editor-facing status collapse does not apply here.
/// </summary>
public class CompetitionCompareViewModel : Notifier
{
    private readonly ReviewFunctions _functions;

    private CompetitionCompareDto? _competition;

    public CompetitionCompareViewModel(ReviewFunctions functions)
    {
        _functions = functions;

        SelectWinnerCommand = new RelayCommand(p => _ = SelectWinnerAsync(p as ProposalCompareRowViewModel), p => CanDecide && p is ProposalCompareRowViewModel);
        CloseNoWinnerCommand = new RelayCommand(_ => _ = CloseNoWinnerAsync(), _ => CanClose);
        ShowOnMapCommand = new RelayCommand(p => ShowOnMap(p as ProposalCompareRowViewModel), p => p is ProposalCompareRowViewModel);
        BackCommand = new RelayCommand(_ => RaiseClose(decisionMade: false));
    }

    /// <summary>(sender, decisionMade) — the host returns to the queue; a decision triggers a refresh.</summary>
    public event EventHandler<bool>? CloseRequested;

    /// <summary>Wire to MapViewModelBase.RequestShowGeometryComparison (live as "old", proposal as "new").</summary>
    public Action<Geometry<Point>?, Geometry<Point>?>? RequestShowGeometryComparison { get; set; }

    public ObservableCollection<ProposalCompareRowViewModel> Proposals { get; } = new();

    public ObservableCollection<AttributeCompareRowViewModel> AttributeRows { get; } = new();

    public long CompetitionId => _competition?.CompetitionId ?? 0;

    public string EntityName => _competition?.EntityName ?? string.Empty;

    public string TargetLabel => _competition?.TargetFeatureId?.ToString() ?? "—";

    public bool IsOrphaned => _competition?.IsOrphaned ?? false;

    public bool IsBlockedByPredecessor => _competition?.IsBlockedByPredecessor ?? false;

    public bool HasStaleProposal => Proposals.Any(p => p.Item.IsStale);

    /// <summary>Orphaned targets are reject-only (D23); queued competitions wait (D19).</summary>
    public bool CanDecide => !IsBusy && !IsOrphaned && !IsBlockedByPredecessor;

    public bool CanClose => !IsBusy && !IsBlockedByPredecessor;

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set { _isBusy = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(CanDecide)); RaisePropertyChanged(nameof(CanClose)); }
    }

    private string? _reasonForAll;
    /// <summary>Applied to every rejected proposal (FR-3.5 requires a reason).</summary>
    public string? ReasonForAll
    {
        get => _reasonForAll;
        set { _reasonForAll = value; RaisePropertyChanged(); }
    }

    private bool _staleOverride;
    /// <summary>D8: the recorded "I know live moved" acknowledgement.</summary>
    public bool StaleOverride
    {
        get => _staleOverride;
        set { _staleOverride = value; RaisePropertyChanged(); }
    }

    public RelayCommand SelectWinnerCommand { get; }

    public RelayCommand CloseNoWinnerCommand { get; }

    public RelayCommand ShowOnMapCommand { get; }

    public RelayCommand BackCommand { get; }

    public async Task LoadAsync(long competitionId)
    {
        _competition = await _functions.LoadCompareAsync(competitionId, CancellationToken.None);

        Proposals.Clear();
        foreach (var proposal in _competition.Proposals)
            Proposals.Add(new ProposalCompareRowViewModel(proposal));

        BuildAttributeRows();

        RaisePropertyChanged(nameof(CompetitionId));
        RaisePropertyChanged(nameof(EntityName));
        RaisePropertyChanged(nameof(TargetLabel));
        RaisePropertyChanged(nameof(IsOrphaned));
        RaisePropertyChanged(nameof(IsBlockedByPredecessor));
        RaisePropertyChanged(nameof(HasStaleProposal));
        RaisePropertyChanged(nameof(CanDecide));
        RaisePropertyChanged(nameof(CanClose));
    }

    private void BuildAttributeRows()
    {
        AttributeRows.Clear();

        if (_competition is null)
            return;

        var live = _competition.Live?.Attributes ?? new Dictionary<string, object?>();

        var fieldNames = live.Keys
            .Concat(_competition.Proposals.SelectMany(p => p.Attributes?.Keys ?? Enumerable.Empty<string>()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase);

        foreach (var field in fieldNames)
        {
            var liveValue = live.TryGetValue(field, out var lv) ? FormatValue(lv) : null;

            var row = new AttributeCompareRowViewModel { FieldName = field, LiveValue = liveValue };

            foreach (var proposal in _competition.Proposals)
            {
                string? value = null;

                if (proposal.ChangeType != ProposalChangeType.Delete && proposal.Attributes is not null)
                    value = proposal.Attributes.TryGetValue(field, out var pv) ? FormatValue(pv) : null;

                row.Cells.Add(new AttributeCompareCellViewModel
                {
                    Value = proposal.ChangeType == ProposalChangeType.Delete ? null : value,
                    IsChanged = proposal.ChangeType != ProposalChangeType.Delete
                        && !string.Equals(value ?? string.Empty, liveValue ?? string.Empty, StringComparison.Ordinal),
                });
            }

            AttributeRows.Add(row);
        }
    }

    private static string? FormatValue(object? value) => value switch
    {
        null => null,
        byte[] bytes => Convert.ToBase64String(bytes),
        DateTime dt => dt.ToString("yyyy/MM/dd HH:mm"),
        _ => value.ToString(),
    };

    private void ShowOnMap(ProposalCompareRowViewModel? row)
    {
        if (row is null)
            return;

        RequestShowGeometryComparison?.Invoke(ParseToMap(_competition?.Live?.GeometryWkb, _competition?.Live?.Srid ?? 0), ParseToMap(row.Item.GeometryWkb, row.Item.Srid));
    }

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

    private async Task SelectWinnerAsync(ProposalCompareRowViewModel? winner)
    {
        if (winner is null || _competition is null)
            return;

        if (Proposals.Count > 1 && string.IsNullOrWhiteSpace(ReasonForAll))
        {
            _functions.ShowMessage(Localization.RejectionReasonRequiredMessage);
            return;
        }

        if (winner.Item.IsStale && !StaleOverride)
        {
            _functions.ShowMessage(Localization.StaleOverrideRequiredMessage);
            return;
        }

        IsBusy = true;

        try
        {
            await _functions.SelectWinnerAsync(new SelectWinnerRequestDto
            {
                CompetitionId = _competition.CompetitionId,
                CompetitionRowVersion = _competition.CompetitionRowVersion,
                WinnerProposalId = winner.Item.ProposalId,
                ReasonForAll = ReasonForAll,
                StaleOverride = StaleOverride,
            });

            RaiseClose(decisionMade: true);
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

    private async Task CloseNoWinnerAsync()
    {
        if (_competition is null)
            return;

        if (string.IsNullOrWhiteSpace(ReasonForAll))
        {
            _functions.ShowMessage(Localization.RejectionReasonRequiredMessage);
            return;
        }

        IsBusy = true;

        try
        {
            await _functions.CloseNoWinnerAsync(new CloseNoWinnerRequestDto
            {
                CompetitionId = _competition.CompetitionId,
                CompetitionRowVersion = _competition.CompetitionRowVersion,
                ReasonForAll = ReasonForAll,
            });

            RaiseClose(decisionMade: true);
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

    private void RaiseClose(bool decisionMade) => CloseRequested?.Invoke(this, decisionMade);

    /// <summary>VM-side user messages; hosts may override before constructing the view model.</summary>
    public static class Localization
    {
        public static string RejectionReasonRequiredMessage { get; set; } = "دلیل رد پیشنهادهای بازنده الزامی است.";

        public static string StaleOverrideRequiredMessage { get; set; } = "وضعیت جاری عارضه پس از این پیشنهاد تغییر کرده است؛ برای پذیرش، گزینه تأیید آگاهانه را علامت بزنید.";
    }
}

public class ProposalCompareRowViewModel : Notifier
{
    public ProposalCompareRowViewModel(ProposalCompareDto item)
    {
        Item = item;
    }

    public ProposalCompareDto Item { get; }

    public bool IsDelete => Item.ChangeType == ProposalChangeType.Delete;

    public string Header => Item.EditorDisplayName;

    public string SubmittedAtLabel => Item.SubmittedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm");
}

public class AttributeCompareRowViewModel
{
    public string FieldName { get; set; } = string.Empty;

    public string? LiveValue { get; set; }

    /// <summary>One cell per proposal, in the same order as the Proposals collection.</summary>
    public List<AttributeCompareCellViewModel> Cells { get; } = new();
}

public class AttributeCompareCellViewModel
{
    public string? Value { get; set; }

    public bool IsChanged { get; set; }
}
