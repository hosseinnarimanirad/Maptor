using System;
using System.Linq;
using System.Threading;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IRI.Maptor.Core.Ogc.SLD;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Wpf.Helpers;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Symbology;

public enum SldRuleValueSource
{
    /// <summary>Every value of the field's domain, in the domain's order.</summary>
    Domain,

    /// <summary>The values that occur in the layer, most frequent first (domain order when there is a domain).</summary>
    Data,
}

public enum SldRuleColorSource
{
    Palette,

    /// <summary>The colours the data model gives the domain's values (when the host can provide them).</summary>
    Domain,
}

/// <summary>A non-empty value of a field and how many features hold it.</summary>
public sealed record SldValueCount(object Value, int Count);

/// <summary>A field the generator can categorize by.</summary>
public sealed record SldRuleFieldOption(Field Field, string DisplayName)
{
    public bool HasDomain => Field.Domain is not null;
}

/// <summary>One row of the generator's preview: the value, its count when known, and its colour.</summary>
public sealed record SldRulePreviewItem(string Title, string? Count, Brush Swatch);

/// <summary>
/// The "Generate rules from field values" dialog: one rule per value of a field, coloured from the built-in
/// palette or the data model's domain colours, optionally with an "other values" rule. The result is a list of
/// SLD rules (<see cref="SldCategoryRules"/>) for the SLD editor to add or to replace its rules with.
/// </summary>
public class SldRuleGeneratorViewModel : Notifier
{
    /// <summary>A preview longer than this is cut, with a note; the rules are all generated.</summary>
    public const int MaxPreviewItems = 60;

    private readonly Symbolizer? _template;

    private readonly Func<string, CancellationToken, Task<IReadOnlyList<SldValueCount>>>? _valuesProvider;

    private readonly Func<FieldDomain, CancellationToken, Task<IReadOnlyDictionary<string, string>?>>? _domainColorsProvider;

    private CancellationTokenSource? _refresh;

    // the last computed categories, ready to generate
    private List<SldCategory> _categories = new();

    public ObservableCollection<SldRuleFieldOption> Fields { get; } = new();

    public ObservableCollection<SldRulePreviewItem> PreviewItems { get; } = new();

    public Action<bool>? RequestClose;

    /// <param name="fields">The layer's fields; geometry and unreadable ones are left out.</param>
    /// <param name="geometry">The layer's geometry kind; null lets the user choose it.</param>
    /// <param name="template">The style to repeat in each value's colour (e.g. the selected rule's symbolizer).</param>
    /// <param name="valuesProvider">The values present in the layer with their counts; null when the data is not at hand.</param>
    /// <param name="domainColorsProvider">The data model's colours of a domain's values (by invariant value text); null when unavailable.</param>
    public SldRuleGeneratorViewModel(
        IEnumerable<Field> fields,
        SldGeometryKind? geometry,
        Symbolizer? template,
        Func<string, CancellationToken, Task<IReadOnlyList<SldValueCount>>>? valuesProvider,
        Func<FieldDomain, CancellationToken, Task<IReadOnlyDictionary<string, string>?>>? domainColorsProvider)
    {
        _template = template;
        _valuesProvider = valuesProvider;
        _domainColorsProvider = domainColorsProvider;

        IsGeometryKnown = geometry.HasValue;
        _geometry = geometry ?? SldGeometryKind.Polygon;

        // domain fields first: they are what categorized symbology is usually for
        foreach (var field in fields.Where(IsCategorizable).OrderBy(f => f.Domain is null))
            Fields.Add(new SldRuleFieldOption(field, string.IsNullOrWhiteSpace(field.Alias) ? field.Name : field.Alias!));

        _valueSource = valuesProvider is not null ? SldRuleValueSource.Data : SldRuleValueSource.Domain;
        _selectedField = Fields.FirstOrDefault();

        GenerateCommand = new RelayCommand(_ => Generate(), _ => !IsBusy && _categories.Count > 0);
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));

        AdjustSources();
        _ = RefreshAsync();
    }

    #region Choices

    private SldRuleFieldOption? _selectedField;
    public SldRuleFieldOption? SelectedField
    {
        get => _selectedField;
        set
        {
            _selectedField = value;
            RaisePropertyChanged();

            AdjustSources();
            _ = RefreshAsync();
        }
    }

    public bool IsGeometryKnown { get; }

    private SldGeometryKind _geometry;
    public SldGeometryKind Geometry
    {
        get => _geometry;
        set
        {
            _geometry = value;
            RaisePropertyChanged();
        }
    }

    private SldRuleValueSource _valueSource;
    public SldRuleValueSource ValueSource
    {
        get => _valueSource;
        set
        {
            _valueSource = value;
            RaisePropertyChanged();

            _ = RefreshAsync();
        }
    }

    private SldRuleColorSource _colorSource;
    public SldRuleColorSource ColorSource
    {
        get => _colorSource;
        set
        {
            _colorSource = value;
            RaisePropertyChanged();

            _ = RefreshAsync();
        }
    }

    private bool _includeOtherValues = true;
    public bool IncludeOtherValues
    {
        get => _includeOtherValues;
        set
        {
            _includeOtherValues = value;
            RaisePropertyChanged();
        }
    }

    private bool _replaceExisting = true;
    /// <summary>Replace the editor's rules; otherwise the new rules are added after them.</summary>
    public bool ReplaceExisting
    {
        get => _replaceExisting;
        set
        {
            _replaceExisting = value;
            RaisePropertyChanged();
        }
    }

    public bool CanUseDomainValues => SelectedField?.HasDomain == true;

    public bool CanUseDataValues => _valuesProvider is not null;

    public bool CanUseDomainColors => _domainColorsProvider is not null && SelectedField?.Field.Domain?.Name is not null;

    // a choice the new field cannot offer falls back to one it can
    private void AdjustSources()
    {
        if (_valueSource == SldRuleValueSource.Domain && !CanUseDomainValues && CanUseDataValues)
            _valueSource = SldRuleValueSource.Data;
        else if (_valueSource == SldRuleValueSource.Data && !CanUseDataValues && CanUseDomainValues)
            _valueSource = SldRuleValueSource.Domain;

        if (_colorSource == SldRuleColorSource.Domain && !CanUseDomainColors)
            _colorSource = SldRuleColorSource.Palette;

        RaisePropertyChanged(nameof(ValueSource));
        RaisePropertyChanged(nameof(ColorSource));
        RaisePropertyChanged(nameof(CanUseDomainValues));
        RaisePropertyChanged(nameof(CanUseDataValues));
        RaisePropertyChanged(nameof(CanUseDomainColors));
    }

    #endregion

    #region Preview

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            _isBusy = value;
            RaisePropertyChanged();
            (GenerateCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    private string? _summary;
    /// <summary>"N rules", or why there are none.</summary>
    public string? Summary
    {
        get => _summary;
        private set
        {
            _summary = value;
            RaisePropertyChanged();
        }
    }

    public async Task RefreshAsync()
    {
        _refresh?.Cancel();
        _refresh = new CancellationTokenSource();
        var token = _refresh.Token;

        var field = SelectedField?.Field;

        PreviewItems.Clear();
        _categories = new();

        if (field is null)
        {
            Summary = L("sldGenerator_noField");
            (GenerateCommand as RelayCommand)?.RaiseCanExecuteChanged();
            return;
        }

        IsBusy = true;

        try
        {
            var categories = await GetCategoriesAsync(field, token);

            if (token.IsCancellationRequested)
                return;

            _categories = categories;

            var paletteIndex = 0;

            foreach (var category in categories.Take(MaxPreviewItems))
            {
                var color = category.Color ?? SldCategoryRules.GetPaletteColor(paletteIndex++);

                PreviewItems.Add(new SldRulePreviewItem(category.Title ?? Convert.ToString(category.Value, CultureInfo.InvariantCulture) ?? string.Empty,
                                                        _counts.TryGetValue(Key(category.Value), out var count) ? count.ToString("#,0", CultureInfo.InvariantCulture) : null,
                                                        ToBrush(color)));
            }

            Summary = categories.Count == 0
                ? L("sldGenerator_noValues")
                : string.Format(L("sldGenerator_ruleCount"), categories.Count.ToString("#,0", CultureInfo.InvariantCulture))
                  + (categories.Count > MaxPreviewItems ? " " + string.Format(L("sldGenerator_previewCut"), MaxPreviewItems) : string.Empty);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Summary = $"{L("sldGenerator_failed")} {ex.Message}";
        }
        finally
        {
            if (!token.IsCancellationRequested)
                IsBusy = false;
        }
    }

    // counts of the values present, by invariant text, for the preview
    private Dictionary<string, int> _counts = new();

    private async Task<List<SldCategory>> GetCategoriesAsync(Field field, CancellationToken token)
    {
        var domain = field.Domain;

        _counts = new();

        IReadOnlyList<SldValueCount> present = [];

        if (_valuesProvider is not null)
        {
            present = await _valuesProvider(field.Name, token);
            _counts = present.GroupBy(v => Key(v.Value)).ToDictionary(g => g.Key, g => g.Sum(v => v.Count));
        }

        IEnumerable<(object Value, string? Title)> values;

        if (ValueSource == SldRuleValueSource.Domain && domain is not null)
        {
            values = domain.Values.Where(v => v.Value is not null).Select(v => (v.Value!, v.Label));
        }
        else if (domain is not null)
        {
            // the values that occur: the domain's order first, then the others (outside the domain) by count
            var used = new HashSet<string>(_counts.Keys);
            var inDomain = domain.Values.Where(v => v.Value is not null && used.Contains(Key(v.Value))).Select(v => (v.Value!, v.Label)).ToList();
            var domainKeys = new HashSet<string>(inDomain.Select(v => Key(v.Item1)));
            var others = present.Where(v => !domainKeys.Contains(Key(v.Value))).OrderByDescending(v => v.Count).Select(v => (v.Value, (string?)null));

            values = inDomain.Concat(others);
        }
        else
        {
            values = present.OrderByDescending(v => v.Count).Select(v => (v.Value, (string?)null));
        }

        IReadOnlyDictionary<string, string>? colors = null;

        if (ColorSource == SldRuleColorSource.Domain && domain is not null && _domainColorsProvider is not null)
            colors = await _domainColorsProvider(domain, token);

        return values
            .GroupBy(v => Key(v.Value)).Select(g => g.First())
            .Select(v => new SldCategory
            {
                Value = v.Value,
                Title = v.Title,
                Color = colors is not null && colors.TryGetValue(Key(v.Value), out var color) ? color : null,
            })
            .ToList();
    }

    private static string Key(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

    private static Brush ToBrush(string color)
    {
        var parsed = SldCategoryRules.ParseColor(color);

        try
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(parsed?.Rgb ?? "#9e9e9e"));

            if (parsed?.Opacity is double opacity)
                brush.Opacity = opacity;

            brush.Freeze();
            return brush;
        }
        catch (FormatException)
        {
            return Brushes.Gray;
        }
    }

    #endregion

    #region Result

    /// <summary>The generated rules, set when the user confirmed.</summary>
    public List<Rule>? Result { get; private set; }

    public ICommand GenerateCommand { get; }

    public ICommand CancelCommand { get; }

    private void Generate()
    {
        var field = SelectedField?.Field;

        if (field is null || _categories.Count == 0)
            return;

        Result = SldCategoryRules.Create(_categories, new SldCategoryOptions
        {
            FieldName = field.Name,
            Geometry = Geometry,
            Template = _template,
            IncludeOtherValues = IncludeOtherValues,
            OtherValuesTitle = L("sldGenerator_otherValues"),
        });

        RequestClose?.Invoke(true);
    }

    #endregion

    private static string L(string key) => LocalizationManager.Instance[key];

    /// <summary>
    /// A field rules can be generated from: any readable attribute but geometry and rowversion. Looser than the
    /// attribute table's rule on purpose: a field typed "object" (a file layer whose first value is empty) still
    /// has values to categorize.
    /// </summary>
    public static bool IsCategorizable(Field field) =>
        !string.IsNullOrWhiteSpace(field.Name) &&
        field.CanRead &&
        !FeatureTableHelper.IsGeometryField(field) &&
        !string.Equals(field.Name, "rowversion", StringComparison.OrdinalIgnoreCase);

    /// <summary>The geometry kind of an SLD for a layer of <paramref name="mode"/>; null for raster or unknown.</summary>
    public static SldGeometryKind? ToGeometryKind(SpatialModelMode? mode) => mode switch
    {
        SpatialModelMode.Point => SldGeometryKind.Point,
        SpatialModelMode.Polyline => SldGeometryKind.Line,
        SpatialModelMode.Polygon => SldGeometryKind.Polygon,
        _ => null,
    };

    /// <summary>
    /// The "values in the layer" provider of a map layer: a vector layer that holds all its features; null for
    /// a Live layer (it holds only part of them) or a layer without features.
    /// </summary>
    public static Func<string, CancellationToken, Task<IReadOnlyList<SldValueCount>>>? ValuesProviderFor(IRI.Maptor.Presentation.Core.Layers.ILayer? layer)
    {
        if (layer is not IRI.Maptor.Presentation.Wpf.Layers.VectorLayer vector ||
            vector.DataSource is IRI.Maptor.Core.Persistence.Abstractions.IServerQueryableVectorDataSource { CanQueryServer: true })
            return null;

        return CountValuesOf(async () => (await vector.GetFeaturesAsync())?.Features);
    }

    /// <summary>
    /// A values provider over features in memory: each non-empty value of the field and its count. Values are
    /// grouped by their invariant text, which is how the generated rules compare them.
    /// </summary>
    public static Func<string, CancellationToken, Task<IReadOnlyList<SldValueCount>>> CountValuesOf(Func<Task<IEnumerable<IRI.Maptor.Core.Spatial.Primitives.Feature<Point>>?>> features) =>
        async (fieldName, token) =>
        {
            var items = await features() ?? [];

            return await Task.Run<IReadOnlyList<SldValueCount>>(() => items
                .Select(f => f.Attributes is not null && f.Attributes.TryGetValue(fieldName, out var value) ? value : null)
                .Where(v => v is not null && v is not DBNull && !(v is string s && string.IsNullOrWhiteSpace(s)))
                .GroupBy(v => Key(v))
                .Select(g => new SldValueCount(g.First()!, g.Count()))
                .ToList(), token);
        };
}
