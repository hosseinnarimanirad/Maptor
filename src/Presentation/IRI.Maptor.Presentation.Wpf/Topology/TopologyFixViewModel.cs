using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Fixes;
using IRI.Maptor.Presentation.Core;

namespace IRI.Maptor.Presentation.Wpf.Topology;

/// <summary>
/// The fix box of the selected issue (doc 05 §7): what the fix would do, Preview on the map, then Apply or Cancel. A
/// fix that is not offered or is refused shows why. Holds state and commands only.
/// </summary>
public sealed class TopologyFixViewModel : Notifier
{
    private readonly TopologyFixSession _session;
    private readonly TopologyOverlay _overlay;

    private TopologyIssueRow? _row;
    private TopologyFixEvaluation? _evaluation;
    private CancellationTokenSource? _evaluating;

    public TopologyFixViewModel(TopologyFixSession session, TopologyOverlay overlay)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));

        PreviewCommand = new RelayCommand(async _ => await PreviewAsync(), _ => CanPreview);
        ApplyCommand = new RelayCommand(async _ => await ApplyAsync(), _ => IsPreviewing && !IsBusy);
        CancelCommand = new RelayCommand(async _ => await CancelAsync(), _ => IsPreviewing && !IsBusy);
    }

    public ICommand PreviewCommand { get; }

    public ICommand ApplyCommand { get; }

    public ICommand CancelCommand { get; }

    /// <summary>The selected issue has a kind some fix type handles.</summary>
    public bool IsVisible => _row is not null && !_row.IsGone && _session.HasFixFor(_row.Issue);

    public bool IsPending => _row is not null && _session.IsPending(_row.Issue);

    private bool _isEvaluating;

    public bool IsEvaluating
    {
        get => _isEvaluating;
        private set { _isEvaluating = value; RaiseAll(); }
    }

    private bool _isBusy;

    private bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; RaiseAll(); }
    }

    public bool IsPreviewing => _evaluation?.Offer.Proposal is { } proposal && ReferenceEquals(_overlay.Preview, proposal);

    public bool CanPreview => !IsPending && !IsEvaluating && !IsBusy && !IsPreviewing && _evaluation?.CanApply == true;

    /// <summary>"A vertex of Route [77] moves 0.8 m onto Tower [1234]."</summary>
    public string FixText => _evaluation?.Offer.Proposal is { } proposal ? ProposalText(proposal) : string.Empty;

    public bool HasFixText => FixText.Length > 0;

    private string? _failure;

    /// <summary>Why there is no fix, why it is refused or cannot be applied, or that it is pending.</summary>
    public string Note
    {
        get
        {
            if (_failure is not null)
                return _failure;

            if (IsPending)
                return TopologyMessages.Text("topology_fix_pendingNote") ?? string.Empty;

            if (IsEvaluating)
                return TopologyMessages.Text("topology_fix_evaluating") ?? string.Empty;

            return _evaluation is null ? string.Empty : ReasonText(_evaluation);
        }
    }

    public bool HasNote => Note.Length > 0;

    /// <summary>Shows the fix of another issue (or none); a running evaluation and any preview are dropped.</summary>
    public async Task SetIssueAsync(TopologyIssueRow? row)
    {
        _evaluating?.Cancel();
        _evaluating = null;

        _row = row;
        _evaluation = null;
        _failure = null;

        if (_overlay.Preview is not null)
            await _overlay.ShowPreviewAsync(null);

        RaiseAll();

        if (!IsVisible || IsPending)
            return;

        var cancellation = new CancellationTokenSource();
        _evaluating = cancellation;

        IsEvaluating = true;

        try
        {
            var evaluations = await _session.EvaluateAsync(row!.Issue, cancellation.Token);

            if (cancellation.IsCancellationRequested)
                return;

            _evaluation = evaluations.FirstOrDefault(e => e.CanApply) ?? evaluations.FirstOrDefault();
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _failure = TopologyMessages.Format("topology_fix_failed", ex.Message);
        }
        finally
        {
            if (ReferenceEquals(_evaluating, cancellation))
            {
                _evaluating = null;
                IsEvaluating = false;
            }
        }
    }

    private async Task PreviewAsync()
    {
        if (!CanPreview)
            return;

        await _overlay.ShowPreviewAsync(_evaluation!.Offer.Proposal);

        RaiseAll();
    }

    private async Task CancelAsync()
    {
        await _overlay.ShowPreviewAsync(null);

        RaiseAll();
    }

    private async Task ApplyAsync()
    {
        if (!IsPreviewing || _evaluation is null)
            return;

        IsBusy = true;

        try
        {
            await _session.ApplyAsync(_evaluation);
        }
        catch (Exception ex)
        {
            _failure = TopologyMessages.Format("topology_fix_failed", ex.Message);
        }
        finally
        {
            await _overlay.ShowPreviewAsync(null);

            IsBusy = false;
        }
    }

    private string ProposalText(TopologyFixProposal proposal)
    {
        var ruleSet = _session.Workspace.RuleSet;
        var finding = proposal.Finding;

        string FeatureOf(FeatureRole role)
            => finding.Features.FirstOrDefault(f => f.Role == role) is { } f
                ? TopologyMessages.FeatureText(TopologyMessages.LayerName(ruleSet, f.EntityName), $"[{f.FeatureId}]")
                : string.Empty;

        var distance = proposal.MessageArguments.Count > 0 && proposal.MessageArguments[0] is double d ? TopologyMessages.QuantityText(d, ValueUnit.Metres) : string.Empty;

        return TopologyMessages.Format($"topology_fix_{proposal.FixTypeId}_{proposal.Variant}", FeatureOf(FeatureRole.A), distance, FeatureOf(FeatureRole.B));
    }

    private string ReasonText(TopologyFixEvaluation evaluation)
    {
        var ruleSet = _session.Workspace.RuleSet;

        if (!evaluation.Offer.IsOffered)
        {
            var arguments = evaluation.Offer.ReasonArguments.Select(a => a is double d ? (object)TopologyMessages.QuantityText(d, ValueUnit.Metres) : a).ToArray();

            return TopologyMessages.Format($"topology_fix_reason_{evaluation.Offer.Reason}", arguments);
        }

        if (evaluation.Verdict is { IsAccepted: false } verdict)
        {
            if (verdict.CausedIssue is { } caused)
            {
                var sentence = TopologyMessages.Sentence(caused, ruleSet.FindRule(caused.RuleId));

                return TopologyMessages.Format($"topology_fix_reason_{verdict.Reason}", sentence, TopologyMessages.FeaturesText(caused.Finding, ruleSet));
            }

            return TopologyMessages.Format($"topology_fix_reason_{verdict.Reason}");
        }

        if (evaluation.CannotApply is { } cannot)
            return TopologyMessages.Format($"topology_fix_reason_{cannot.Reason}", cannot.LayerName ?? string.Empty);

        return string.Empty;
    }

    private void RaiseAll()
    {
        foreach (var name in new[] { nameof(IsVisible), nameof(IsPending), nameof(IsEvaluating), nameof(IsPreviewing), nameof(CanPreview), nameof(FixText), nameof(HasFixText), nameof(Note), nameof(HasNote) })
            RaisePropertyChanged(name);

        CommandManager.InvalidateRequerySuggested();
    }
}
