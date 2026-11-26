using System.Globalization;

namespace IRI.Maptor.Core.Topology.Rules;

/// <summary>
/// The named values a rule is configured with: distances in metres, field names, layer lists. Immutable;
/// <see cref="With"/> returns a changed copy.
/// </summary>
public sealed class RuleParameters
{
    private readonly Dictionary<string, object> _values;

    public RuleParameters(IReadOnlyDictionary<string, object>? values = null)
    {
        _values = values is null
            ? new Dictionary<string, object>(StringComparer.Ordinal)
            : values.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
    }

    public static RuleParameters Empty { get; } = new RuleParameters();

    public IEnumerable<string> Names => _values.Keys;

    public bool Has(string name) => _values.ContainsKey(name);

    /// <summary>The value as a number, or <paramref name="defaultValue"/> when it is missing.</summary>
    public double GetNumber(string name, double defaultValue)
        => _values.TryGetValue(name, out var value) && value is not null
            ? Convert.ToDouble(value, CultureInfo.InvariantCulture)
            : defaultValue;

    /// <summary>The value as text, or null when it is missing.</summary>
    public string? GetText(string name)
        => _values.TryGetValue(name, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    /// <summary>A list of texts (e.g. layer names); a single text counts as a list of one.</summary>
    public IReadOnlyList<string> GetTexts(string name)
    {
        if (!_values.TryGetValue(name, out var value) || value is null)
            return Array.Empty<string>();

        if (value is string text)
            return new[] { text };

        if (value is IEnumerable<string> texts)
            return texts.ToArray();

        return new[] { Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty };
    }

    public RuleParameters With(string name, object value)
    {
        var copy = new Dictionary<string, object>(_values, StringComparer.Ordinal) { [name] = value };

        return new RuleParameters(copy);
    }
}
