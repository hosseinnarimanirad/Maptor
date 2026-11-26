using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Topology.Rules;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Localization;

namespace IRI.Maptor.Presentation.Wpf.Topology;

/// <summary>How one kind of a rule is reported: a severity, or not at all (D52).</summary>
public enum TopologyKindSetting
{
    Error = 0,
    Warning = 1,
    Info = 2,
    Hidden = 3,
}

/// <summary>One kind of a rule with its setting.</summary>
public sealed class TopologyKindRow : Notifier
{
    private readonly TopologyRuleRow _owner;

    internal TopologyKindRow(TopologyRuleRow owner, string kind, string text, TopologyKindSetting setting)
    {
        _owner = owner;
        Kind = kind;
        Text = text;
        _setting = setting;
    }

    public string Kind { get; }

    public string Text { get; }

    /// <summary>Built on each read, in the current language; options are equal by value.</summary>
    public static IReadOnlyList<TopologyFilterOption<TopologyKindSetting>> Options => new[]
    {
        new TopologyFilterOption<TopologyKindSetting>(TopologyKindSetting.Error, TopologyMessages.SeverityText(IssueSeverity.Error)),
        new TopologyFilterOption<TopologyKindSetting>(TopologyKindSetting.Warning, TopologyMessages.SeverityText(IssueSeverity.Warning)),
        new TopologyFilterOption<TopologyKindSetting>(TopologyKindSetting.Info, TopologyMessages.SeverityText(IssueSeverity.Info)),
        new TopologyFilterOption<TopologyKindSetting>(TopologyKindSetting.Hidden, TopologyMessages.Text("topology_rules_hidden") ?? "Hidden"),
    };

    private TopologyKindSetting _setting;

    public TopologyFilterOption<TopologyKindSetting> Setting
    {
        get => Options.First(o => o.Value == _setting);
        set
        {
            if (value is null || value.Value == _setting)
                return;

            _setting = value.Value;
            RaisePropertyChanged();
            _owner.KindChanged(Kind, _setting);
        }
    }
}

/// <summary>One numeric parameter of a rule (a distance, a count).</summary>
public sealed class TopologyParameterRow : Notifier
{
    private readonly TopologyRuleRow _owner;

    internal TopologyParameterRow(TopologyRuleRow owner, string name, double value)
    {
        _owner = owner;
        Name = name;
        Label = TopologyMessages.Text($"topology_parameter_{name}") ?? name;
        _text = value.ToString(CultureInfo.InvariantCulture);
    }

    public string Name { get; }

    public string Label { get; }

    private string _text;

    /// <summary>The value as typed; Latin or Persian digits, a point or a comma.</summary>
    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? string.Empty;
            RaisePropertyChanged();

            var parsed = Parse(_text);

            IsValid = parsed is not null;

            if (parsed is not null)
                _owner.ParameterChanged(Name, parsed.Value);
        }
    }

    private bool _isValid = true;

    public bool IsValid
    {
        get => _isValid;
        private set
        {
            _isValid = value;
            RaisePropertyChanged();
        }
    }

    internal static double? Parse(string text)
    {
        var latin = new string(text.Trim().Select(c => c is >= '۰' and <= '۹' ? (char)('0' + (c - '۰')) : c is '٫' or ',' ? '.' : c).ToArray());

        return double.TryParse(latin, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value >= 0 ? value : null;
    }
}

/// <summary>One rule as the Rules panel shows it.</summary>
public sealed class TopologyRuleRow : Notifier
{
    private readonly TopologyRulesPanelViewModel _owner;

    internal TopologyRuleRow(TopologyRulesPanelViewModel owner, TopologyRule rule, ITopologyRuleType? ruleType, TopologyRuleSet ruleSet)
    {
        _owner = owner;
        Rule = rule;
        Title = TopologyMessages.RuleTitle(rule);
        LayersText = rule.LayerB is null
            ? TopologyMessages.LayerName(ruleSet, rule.LayerA)
            : $"{TopologyMessages.LayerName(ruleSet, rule.LayerA)} → {TopologyMessages.LayerName(ruleSet, rule.LayerB)}";

        foreach (var name in rule.Parameters.Names.OrderBy(n => n, StringComparer.Ordinal))
        {
            var text = rule.Parameters.GetText(name);

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                Parameters.Add(new TopologyParameterRow(this, name, value));
        }

        foreach (var kind in ruleType?.Kinds ?? Array.Empty<string>())
        {
            var setting = rule.IsHidden(kind)
                ? TopologyKindSetting.Hidden
                : (TopologyKindSetting)(2 - (int)rule.SeverityOf(kind, ruleType!.DefaultSeverityOf(kind)));

            // The value is not known here: "{0}" reads as "…".
            Kinds.Add(new TopologyKindRow(this, kind, TopologyMessages.KindText(rule, rule.RuleTypeId, kind).Replace("{0}", "…"), setting));
        }
    }

    public TopologyRule Rule { get; private set; }

    public string Title { get; }

    public string LayersText { get; }

    public ObservableCollection<TopologyParameterRow> Parameters { get; } = new();

    public ObservableCollection<TopologyKindRow> Kinds { get; } = new();

    public bool IsEnabled
    {
        get => Rule.Enabled;
        set
        {
            if (value == Rule.Enabled)
                return;

            Update(Rule.WithEnabled(value));
            RaisePropertyChanged();
        }
    }

    /// <summary>Differs from the app's rule set (shown with a dot).</summary>
    public bool IsChanged => !ReferenceEquals(Rule, _owner.DefaultOf(Rule.Id));

    internal void ParameterChanged(string name, double value) => Update(Rule.WithParameters(Rule.Parameters.With(name, value)));

    internal void KindChanged(string kind, TopologyKindSetting setting)
        => Update(setting == TopologyKindSetting.Hidden
            ? Rule.WithHidden(kind, true)
            : Rule.WithSeverity(kind, (IssueSeverity)(2 - (int)setting)));

    private void Update(TopologyRule rule)
    {
        Rule = rule;
        _owner.RuleChanged(rule);
        RaisePropertyChanged(nameof(IsChanged));
    }
}

/// <summary>
/// The Rules panel (doc 05 §6): every rule of the app's set, what it reads, its distances and its kinds' settings. The
/// expert's changes last this session only (D50); Reset all returns to the app's values.
/// </summary>
public sealed class TopologyRulesPanelViewModel : Notifier
{
    private readonly TopologyWorkspace _workspace;

    public TopologyRulesPanelViewModel(TopologyWorkspace workspace)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));

        ResetAllCommand = new RelayCommand(_ =>
        {
            _workspace.ResetSettings();
            Load();
        }, _ => _workspace.HasSessionChanges);

        Load();
    }

    public string Header => TopologyMessages.Format("topology_rules_header", _workspace.RuleSet.Key, _workspace.RuleSet.Version);

    public ObservableCollection<TopologyRuleRow> Rules { get; } = new();

    public bool HasSessionChanges => _workspace.HasSessionChanges;

    /// <summary>Settings that cannot be used (e.g. a search distance below the rule distance), one per line.</summary>
    public string Problems => string.Join(Environment.NewLine, _workspace.SettingsProblems);

    public bool HasProblems => _workspace.SettingsProblems.Count > 0;

    public ICommand ResetAllCommand { get; }

    internal TopologyRule? DefaultOf(string ruleId) => _workspace.DefaultRule(ruleId);

    internal void RuleChanged(TopologyRule rule)
    {
        _workspace.UpdateRule(rule);

        RaisePropertyChanged(nameof(HasSessionChanges));
        RaisePropertyChanged(nameof(Problems));
        RaisePropertyChanged(nameof(HasProblems));
        CommandManager.InvalidateRequerySuggested();
    }

    private void Load()
    {
        Rules.Clear();

        foreach (var rule in _workspace.RuleSet.Rules)
            Rules.Add(new TopologyRuleRow(this, rule, _workspace.Catalog.Find(rule.RuleTypeId), _workspace.RuleSet));

        RaisePropertyChanged(nameof(Header));
        RaisePropertyChanged(nameof(HasSessionChanges));
        RaisePropertyChanged(nameof(Problems));
        RaisePropertyChanged(nameof(HasProblems));
    }
}
