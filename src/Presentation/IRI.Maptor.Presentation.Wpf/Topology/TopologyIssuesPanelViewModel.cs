using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.Topology.Checking;
using IRI.Maptor.Core.Topology.Findings;
using IRI.Maptor.Core.Topology.Rules;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Wpf.Helpers;

namespace IRI.Maptor.Presentation.Wpf.Topology;

/// <summary>Which issues the list shows by their change since the previous check.</summary>
public enum TopologyChangeFilter
{
    All = 0,
    New = 1,
    StillThere = 2,
    Gone = 3,
}

/// <summary>An option of a filter combo: a value and its text.</summary>
public sealed class TopologyFilterOption<T>
{
    public TopologyFilterOption(T value, string text)
    {
        Value = value;
        Text = text;
    }

    public T Value { get; }

    public string Text { get; }

    /// <summary>Equal by value, so a list rebuilt in another language still matches the selected option.</summary>
    public override bool Equals(object? obj) => obj is TopologyFilterOption<T> other && EqualityComparer<T>.Default.Equals(Value, other.Value);

    public override int GetHashCode() => Value is null ? 0 : EqualityComparer<T>.Default.GetHashCode(Value);

    public override string ToString() => Text;
}

/// <summary>One row of the issue list.</summary>
public sealed class TopologyIssueRow : Notifier
{
    public TopologyIssueRow(TopologyIssue issue, TopologyRule? rule, int ruleOrder, TopologyRuleSet ruleSet, CheckChange change)
    {
        Issue = issue;
        Rule = rule;
        RuleOrder = ruleOrder;
        Change = change;
        RuleTitle = rule is null ? issue.RuleId : TopologyMessages.RuleTitle(rule);
        Sentence = TopologyMessages.Sentence(issue, rule);
        ValueText = TopologyMessages.ValueText(issue.Finding);
        FeaturesText = TopologyMessages.FeaturesText(issue.Finding, ruleSet);
        SeverityText = TopologyMessages.SeverityText(issue.Severity);
    }

    public TopologyIssue Issue { get; }

    public TopologyRule? Rule { get; }

    /// <summary>The rule's place in the rule set; groups are ordered by worst severity, then by this.</summary>
    public int RuleOrder { get; }

    public CheckChange Change { get; }

    public IssueSeverity Severity => Issue.Severity;

    public string RuleTitle { get; }

    public string Sentence { get; }

    public string ValueText { get; }

    public string FeaturesText { get; }

    public string SeverityText { get; }

    public bool IsNew => Change == CheckChange.New;

    public bool IsGone => Change == CheckChange.Gone;

    /// <summary>The group header: the rule title and its count.</summary>
    public string GroupTitle { get; set; } = string.Empty;

    /// <summary>Orders the groups: worst severity first, then rule order.</summary>
    public string GroupSort { get; set; } = string.Empty;

    private bool _isFixPending;

    /// <summary>A fix was applied since the check (pending edits, not saved or checked yet).</summary>
    public bool IsFixPending
    {
        get => _isFixPending;
        set { if (_isFixPending != value) { _isFixPending = value; RaisePropertyChanged(); } }
    }
}

/// <summary>A feature of the selected issue, with "show in attribute table".</summary>
public sealed class TopologyIssueFeature
{
    public TopologyIssueFeature(string text, ICommand showInTable)
    {
        Text = text;
        ShowInTableCommand = showInTable;
    }

    public string Text { get; }

    public ICommand ShowInTableCommand { get; }
}

/// <summary>
/// The Issues panel (doc 05 §4): the last check's issues grouped by rule, filters, the selected issue's detail, zoom to
/// it, and the overlay's content. Holds state and commands only; the view groups and virtualizes the list.
/// </summary>
public sealed class TopologyIssuesPanelViewModel : Notifier
{
    private readonly TopologyWorkspace _workspace;
    private readonly TopologyOverlay _overlay;
    private readonly TopologyFixSession? _fixes;

    private readonly List<TopologyIssueRow> _rows = new();

    /// <param name="workspace">The tab's session.</param>
    /// <param name="overlay">The issues on the map.</param>
    /// <param name="fixes">The fixes; null for a panel without them.</param>
    public TopologyIssuesPanelViewModel(TopologyWorkspace workspace, TopologyOverlay overlay, TopologyFixSession? fixes = null)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));
        _fixes = fixes;

        if (fixes is not null)
        {
            Fix = new TopologyFixViewModel(fixes, overlay);
            fixes.Applied += (_, _) => UpdatePending();
        }

        Rows = new ObservableCollection<TopologyIssueRow>();
        View = CollectionViewSource.GetDefaultView(Rows);
        View.Filter = o => o is TopologyIssueRow row && Passes(row);
        View.GroupDescriptions.Add(new PropertyGroupDescription(nameof(TopologyIssueRow.GroupTitle)));

        ChangeOptions = new[]
        {
            new TopologyFilterOption<TopologyChangeFilter>(TopologyChangeFilter.All, TopologyMessages.Text("topology_issues_changeAll") ?? "All"),
            new TopologyFilterOption<TopologyChangeFilter>(TopologyChangeFilter.New, TopologyMessages.Text("topology_issues_changeNew") ?? "New since the last check"),
            new TopologyFilterOption<TopologyChangeFilter>(TopologyChangeFilter.StillThere, TopologyMessages.Text("topology_issues_changeStillThere") ?? "Still there"),
            new TopologyFilterOption<TopologyChangeFilter>(TopologyChangeFilter.Gone, TopologyMessages.Text("topology_issues_changeGone") ?? "Gone since the last check"),
        };

        _selectedChange = ChangeOptions[0];

        ExportCommand = new RelayCommand(async _ => await ExportAsync(), _ => VisibleIssues.Any());

        _workspace.ResultChanged += (_, _) => Load();
        _overlay.IsShownChanged += (_, _) => _ = DrawAsync();
    }

    public ObservableCollection<TopologyIssueRow> Rows { get; }

    /// <summary>The filtered, grouped view the list binds to.</summary>
    public ICollectionView View { get; }

    public string Header
    {
        get
        {
            var result = _workspace.Result;

            if (result is null)
                return TopologyMessages.Text("topology_issues_noCheck") ?? "Run a check first.";

            var time = _workspace.CheckedAt?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? string.Empty;

            // Left-to-right marks keep "11:01" in order inside Persian text.
            return TopologyMessages.Format("topology_issues_header", "‎" + LocalizedTime(time) + "‎", ScopeText(result.Summary.Scope.Kind), result.Summary.FeaturesRead.Values.Sum());
        }
    }

    public bool SettingsChanged => _workspace.CheckedWithSessionChanges;

    public string SkippedText
        => _workspace.SkippedRules.Count == 0
            ? string.Empty
            : TopologyMessages.Format("topology_issues_skipped", string.Join("، ", _workspace.SkippedRules.Select(TopologyMessages.RuleTitle)));

    public bool HasSkipped => _workspace.SkippedRules.Count > 0;

    public int ErrorCount => _rows.Count(r => !r.IsGone && r.Severity == IssueSeverity.Error);

    public int WarningCount => _rows.Count(r => !r.IsGone && r.Severity == IssueSeverity.Warning);

    public int InfoCount => _rows.Count(r => !r.IsGone && r.Severity == IssueSeverity.Info);

    public string ErrorChip => TopologyMessages.Format("topology_issues_errors", ErrorCount);

    public string WarningChip => TopologyMessages.Format("topology_issues_warnings", WarningCount);

    public string InfoChip => TopologyMessages.Format("topology_issues_infos", InfoCount);

    private bool _showErrors = true;
    private bool _showWarnings = true;
    private bool _showInfos = true;

    public bool ShowErrors { get => _showErrors; set { _showErrors = value; RaisePropertyChanged(); Refilter(); } }

    public bool ShowWarnings { get => _showWarnings; set { _showWarnings = value; RaisePropertyChanged(); Refilter(); } }

    public bool ShowInfos { get => _showInfos; set { _showInfos = value; RaisePropertyChanged(); Refilter(); } }

    public ObservableCollection<TopologyFilterOption<string?>> RuleOptions { get; } = new();

    private TopologyFilterOption<string?>? _selectedRule;

    public TopologyFilterOption<string?>? SelectedRule { get => _selectedRule; set { _selectedRule = value; RaisePropertyChanged(); Refilter(); } }

    public IReadOnlyList<TopologyFilterOption<TopologyChangeFilter>> ChangeOptions { get; }

    private TopologyFilterOption<TopologyChangeFilter> _selectedChange;

    public TopologyFilterOption<TopologyChangeFilter> SelectedChange
    {
        get => _selectedChange;
        set { _selectedChange = value ?? ChangeOptions[0]; RaisePropertyChanged(); Refilter(); }
    }

    /// <summary>The change filter needs a previous check of the same scope.</summary>
    public bool HasComparison => _workspace.Comparison is not null;

    private string _search = string.Empty;

    public string Search { get => _search; set { _search = value ?? string.Empty; RaisePropertyChanged(); Refilter(); } }

    public bool IsEmpty => _workspace.Result is not null && _rows.All(r => r.IsGone);

    public string EmptyText => _workspace.Result is null ? string.Empty : TopologyMessages.Format("topology_issues_none", _workspace.Result.Summary.FeaturesRead.Values.Sum());

    public string LimitNote => _overlay.IsLimited ? TopologyMessages.Format("topology_issues_overlayLimited", TopologyOverlay.Limit) : string.Empty;

    /// <summary>The selected issue's fix box; null when the panel has no fixes.</summary>
    public TopologyFixViewModel? Fix { get; }

    public bool HasPendingFixes => _fixes?.PendingCount > 0;

    /// <summary>"2 fixes applied. Save the layers to keep them, then check again."</summary>
    public string PendingFixText => HasPendingFixes ? TopologyMessages.Format("topology_fix_pendingFooter", _fixes!.PendingCount) : string.Empty;

    #region Selected issue

    private TopologyIssueRow? _selected;

    public TopologyIssueRow? Selected
    {
        get => _selected;
        set
        {
            if (ReferenceEquals(_selected, value))
                return;

            _selected = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasSelection));
            RaisePropertyChanged(nameof(SelectedRuleText));
            BuildSelectedFeatures();
            ZoomToSelected();
            _ = DrawAsync();
            _ = Fix?.SetIssueAsync(value);
        }
    }

    public bool HasSelection => Selected is not null;

    /// <summary>"Rule · allowed 0.5 m · Error".</summary>
    public string SelectedRuleText
    {
        get
        {
            if (Selected is null)
                return string.Empty;

            var parts = new List<string> { Selected.RuleTitle };

            if (Selected.Rule?.Parameters.Has("distance") == true)
                parts.Add(TopologyMessages.Format("topology_issues_allowed", LocalizedNumber(Selected.Rule.Parameters.GetNumber("distance", 0))));

            parts.Add(Selected.SeverityText);

            return string.Join(" · ", parts);
        }
    }

    public ObservableCollection<TopologyIssueFeature> SelectedFeatures { get; } = new();

    #endregion

    public ICommand ExportCommand { get; }

    /// <summary>The issues that pass the filters (gone ones excluded), in list order.</summary>
    public IReadOnlyList<TopologyIssue> VisibleIssues => View.Cast<TopologyIssueRow>().Where(r => !r.IsGone).Select(r => r.Issue).ToList();

    private void Load()
    {
        _rows.Clear();

        var result = _workspace.Result;
        var ruleSet = _workspace.CheckedRuleSet ?? _workspace.RuleSet;

        if (result is not null)
        {
            var order = ruleSet.Rules.Select((r, i) => (r.Id, i)).ToDictionary(x => x.Id, x => x.i);
            var comparison = _workspace.Comparison;

            TopologyIssueRow RowOf(TopologyIssue issue, CheckChange change)
            {
                var rule = ruleSet.FindRule(issue.RuleId);

                return new TopologyIssueRow(issue, rule, order.TryGetValue(issue.RuleId, out var i) ? i : int.MaxValue, ruleSet, change);
            }

            _rows.AddRange(result.Issues.Select(i => RowOf(i, comparison?.ChangeOf(i) ?? CheckChange.StillThere)));

            if (comparison is not null)
                _rows.AddRange(comparison.Gone.Select(i => RowOf(i, CheckChange.Gone)));

            // Groups: worst severity first, then rule order.
            foreach (var group in _rows.GroupBy(r => r.Issue.RuleId))
            {
                var worst = group.Where(r => !r.IsGone).Select(r => (int)r.Severity).DefaultIfEmpty(-1).Max();
                var sort = $"{9 - Math.Max(worst, 0)}{group.First().RuleOrder:D4}";
                var title = $"{group.First().RuleTitle} ({LocalizedNumber(group.Count(r => !r.IsGone))})";

                foreach (var row in group)
                {
                    row.GroupSort = sort;
                    row.GroupTitle = title;
                }
            }
        }

        RuleOptions.Clear();
        RuleOptions.Add(new TopologyFilterOption<string?>(null, TopologyMessages.Text("topology_issues_allRules") ?? "All rules"));

        foreach (var rule in ruleSet.Rules.Where(r => _rows.Any(row => row.Issue.RuleId == r.Id)))
            RuleOptions.Add(new TopologyFilterOption<string?>(rule.Id, TopologyMessages.RuleTitle(rule)));

        _selectedRule = RuleOptions[0];
        _selectedChange = ChangeOptions[0];

        Rows.Clear();

        foreach (var row in _rows.OrderBy(r => r.GroupSort, StringComparer.Ordinal).ThenByDescending(r => r.Severity).ThenBy(r => r.Issue.Finding.Features.FirstOrDefault()?.FeatureId ?? 0))
            Rows.Add(row);

        View.SortDescriptions.Clear();
        View.SortDescriptions.Add(new SortDescription(nameof(TopologyIssueRow.GroupSort), ListSortDirection.Ascending));
        View.SortDescriptions.Add(new SortDescription(nameof(TopologyIssueRow.Severity), ListSortDirection.Descending));

        Selected = null;

        UpdatePending();

        foreach (var name in new[] { nameof(Header), nameof(SettingsChanged), nameof(SkippedText), nameof(HasSkipped), nameof(ErrorCount), nameof(WarningCount), nameof(InfoCount),
                     nameof(ErrorChip), nameof(WarningChip), nameof(InfoChip), nameof(SelectedRule), nameof(SelectedChange), nameof(HasComparison), nameof(IsEmpty), nameof(EmptyText) })
            RaisePropertyChanged(name);

        Refilter();
    }

    private void UpdatePending()
    {
        foreach (var row in _rows)
            row.IsFixPending = _fixes?.IsPending(row.Issue) == true;

        RaisePropertyChanged(nameof(HasPendingFixes));
        RaisePropertyChanged(nameof(PendingFixText));
    }

    private bool Passes(TopologyIssueRow row)
    {
        var change = SelectedChange.Value;

        if (change == TopologyChangeFilter.Gone ? !row.IsGone : row.IsGone)
            return false;

        if (change == TopologyChangeFilter.New && !row.IsNew)
            return false;

        if (change == TopologyChangeFilter.StillThere && row.Change != CheckChange.StillThere)
            return false;

        var severityShown = row.Severity switch
        {
            IssueSeverity.Error => ShowErrors,
            IssueSeverity.Warning => ShowWarnings,
            _ => ShowInfos,
        };

        if (!severityShown)
            return false;

        if (SelectedRule?.Value is string ruleId && row.Issue.RuleId != ruleId)
            return false;

        if (Search.Length > 0
            && row.Sentence.IndexOf(Search, StringComparison.CurrentCultureIgnoreCase) < 0
            && row.FeaturesText.IndexOf(Search, StringComparison.CurrentCultureIgnoreCase) < 0
            && !row.Issue.Finding.Features.Any(f => f.FeatureId.ToString(CultureInfo.InvariantCulture) == Search.Trim()))
            return false;

        return true;
    }

    private void Refilter()
    {
        View.Refresh();

        if (Selected is not null && !Passes(Selected))
            Selected = null;

        _ = DrawAsync();

        CommandManager.InvalidateRequerySuggested();
    }

    private async Task DrawAsync()
    {
        await _overlay.ShowAsync(VisibleIssues, Selected?.IsGone == false ? Selected.Issue : null);

        RaisePropertyChanged(nameof(LimitNote));
    }

    private void ZoomToSelected()
    {
        var location = Selected?.Issue.Finding.Location;

        if (location is null || location.IsNullOrEmpty())
            return;

        var map = TopologyOverlay.ToMap(location);

        var box = map.GetBoundingBox();

        // Never closer than about 60 m across, so a 0.3 m gap is not shown at street-block size.
        const double minimum = 60;

        if (box.Width < minimum && box.Height < minimum)
        {
            var c = box.Center;

            map = Geometry<Point>.CreatePolygon(new List<Point>
            {
                new(c.X - minimum, c.Y - minimum), new(c.X + minimum, c.Y - minimum),
                new(c.X + minimum, c.Y + minimum), new(c.X - minimum, c.Y + minimum),
            }, SridHelper.WebMercator);
        }

        _workspace.Map.Zoom(map);
    }

    private void BuildSelectedFeatures()
    {
        SelectedFeatures.Clear();

        if (Selected is null)
            return;

        var ruleSet = _workspace.CheckedRuleSet ?? _workspace.RuleSet;

        foreach (var feature in Selected.Issue.Finding.Features)
        {
            var entity = feature.EntityName;
            var id = feature.FeatureId;

            SelectedFeatures.Add(new TopologyIssueFeature(
                TopologyMessages.FeatureText(TopologyMessages.LayerName(ruleSet, entity), $"[{id}]"),
                new RelayCommand(async _ => await ShowInTableAsync(entity, id))));
        }
    }

    private async Task ShowInTableAsync(string entityName, long id)
    {
        var found = await _workspace.MapFeaturesAsync(entityName, new[] { id });

        foreach (var group in found.GroupBy(f => f.Layer))
            await _workspace.Map.ShowInAttributeTableAsync(group.Key, group.Select(f => f.Feature).ToList());
    }

    private async Task ExportAsync()
    {
        var path = await _workspace.Map.DialogService.ShowSaveFileDialogAsync("*.xlsx|*.xlsx", null, "topology");

        if (string.IsNullOrWhiteSpace(path))
            return;

        var rows = View.Cast<TopologyIssueRow>().Where(r => !r.IsGone).Select(TopologyIssueExport.Row).ToList();

        ExcelHelper.WriteDictionary(rows, path!, "Topology", null, TopologyIssueExport.Headers());
    }

    private static string ScopeText(CheckScopeKind kind) => kind switch
    {
        CheckScopeKind.Layer => TopologyMessages.Text("topology_check_scopeLayer") ?? "one layer",
        CheckScopeKind.Extent => TopologyMessages.Text("topology_check_scopeExtent") ?? "map extent",
        CheckScopeKind.Features => TopologyMessages.Text("topology_check_scopeFeatures") ?? "selected features",
        _ => TopologyMessages.Text("topology_check_scopeWholeData") ?? "whole data",
    };

    private static string LocalizedNumber(object value) => Core.Localization.LocalizationManager.GetLocalizedNumberString(value);

    private static string LocalizedTime(string time) => Core.Localization.LocalizationManager.GetLocalizedNumberString(time);
}

/// <summary>The issue list as Excel rows (D47): no attribute values, UTC is not needed (no times).</summary>
public static class TopologyIssueExport
{
    public static List<string> Headers() => new()
    {
        TopologyMessages.Text("topology_export_rule") ?? "Rule",
        TopologyMessages.Text("topology_export_kind") ?? "Kind",
        TopologyMessages.Text("topology_export_severity") ?? "Severity",
        TopologyMessages.Text("topology_export_sentence") ?? "Issue",
        TopologyMessages.Text("topology_export_value") ?? "Value",
        TopologyMessages.Text("topology_export_features") ?? "Features",
        TopologyMessages.Text("topology_export_location") ?? "Location (longitude, latitude)",
    };

    public static Dictionary<string, object> Row(TopologyIssueRow row)
    {
        var location = row.Issue.Finding.Location;

        var centre = location is null || location.IsNullOrEmpty() ? (Point?)null : location.GetBoundingBox().Center;

        return new Dictionary<string, object>
        {
            ["rule"] = row.RuleTitle,
            ["kind"] = row.Issue.Kind,
            ["severity"] = row.SeverityText,
            ["sentence"] = row.Sentence,
            ["value"] = row.ValueText,
            ["features"] = row.FeaturesText,
            ["location"] = centre is null ? string.Empty : string.Format(CultureInfo.InvariantCulture, "{0:0.000000}, {1:0.000000}", centre.X, centre.Y),
        };
    }
}
