using System.Linq;
using System.Collections.Generic;

namespace IRI.Maptor.Core.Spatial.Importing;

/// <summary>
/// The outcome of converting many rows (the Check before an import, and the import itself): how many fit, and for
/// the others how many per reason and field, with the first rows of each as examples. Values stay in the report the
/// user reads; nothing here is logged.
/// </summary>
public class FeatureImportReport
{
    private readonly int _examplesPerProblem;

    private readonly Dictionary<(ImportIssueKind Kind, string? Field), ImportProblem> _problems = new Dictionary<(ImportIssueKind, string?), ImportProblem>();

    private readonly Dictionary<string, int> _translatedCodes = new Dictionary<string, int>();

    public FeatureImportReport(int examplesPerProblem = 5)
    {
        _examplesPerProblem = examplesPerProblem;
    }

    public int RowCount { get; private set; }

    public int FitCount { get; private set; }

    public int SkippedCount => RowCount - FitCount;

    /// <summary>Most frequent first. A row with two problems counts once in <see cref="SkippedCount"/> and once in each problem.</summary>
    public IReadOnlyList<ImportProblem> Problems => _problems.Values.OrderByDescending(p => p.RowCount).ThenBy(p => p.Field).ToList();

    /// <summary>Per field, how many values were domain codes written as the code's value.</summary>
    public IReadOnlyDictionary<string, int> TranslatedCodes => _translatedCodes;

    private readonly Dictionary<string, int> _truncatedValues = new Dictionary<string, int>();

    /// <summary>Per field, how many written values were cut to the field's length.</summary>
    public IReadOnlyDictionary<string, int> TruncatedValues => _truncatedValues;

    private readonly Dictionary<string, int> _emptiedValues = new Dictionary<string, int>();

    /// <summary>Per field, how many values outside the domain were written empty or as the default.</summary>
    public IReadOnlyDictionary<string, int> EmptiedValues => _emptiedValues;

    private readonly Dictionary<string, ImportProblem> _functionFailures = new Dictionary<string, ImportProblem>();

    /// <summary>
    /// Per field, the values of written rows that the mapping's function could not read and that were written empty or as
    /// the default, with examples. A required field without a default is in <see cref="Problems"/> instead.
    /// </summary>
    public IReadOnlyList<ImportProblem> FunctionFailures => _functionFailures.Values.OrderByDescending(p => p.RowCount).ThenBy(p => p.Field).ToList();

    /// <param name="rowNumber">The row's position in the source (1-based), for the examples.</param>
    public void Add(int rowNumber, FeatureImportResult result)
    {
        RowCount++;

        if (result.Fits)
        {
            FitCount++;

            foreach (var failure in result.FunctionFailures)
            {
                if (!_functionFailures.TryGetValue(failure.Field ?? string.Empty, out var problem))
                    _functionFailures[failure.Field ?? string.Empty] = problem = new ImportProblem(failure.Kind, failure.Field);

                problem.RowCount++;

                if (problem.Examples.Count < _examplesPerProblem)
                    problem.Examples.Add(new ImportExample(rowNumber, failure.Value));
            }

            foreach (var field in result.TranslatedCodes)
                _translatedCodes[field] = _translatedCodes.TryGetValue(field, out var count) ? count + 1 : 1;

            foreach (var field in result.TruncatedFields)
                _truncatedValues[field] = _truncatedValues.TryGetValue(field, out var count) ? count + 1 : 1;

            foreach (var field in result.EmptiedFields)
                _emptiedValues[field] = _emptiedValues.TryGetValue(field, out var count) ? count + 1 : 1;

            return;
        }

        foreach (var issue in result.Issues)
        {
            var key = (issue.Kind, issue.Field);

            if (!_problems.TryGetValue(key, out var problem))
                _problems[key] = problem = new ImportProblem(issue.Kind, issue.Field);

            problem.RowCount++;

            if (problem.Examples.Count < _examplesPerProblem)
                problem.Examples.Add(new ImportExample(rowNumber, issue.Value));
        }
    }
}

/// <summary>One reason rows did not fit, on one field (null for the shape).</summary>
public class ImportProblem
{
    public ImportProblem(ImportIssueKind kind, string? field)
    {
        Kind = kind;
        Field = field;
    }

    public ImportIssueKind Kind { get; }

    public string? Field { get; }

    public int RowCount { get; internal set; }

    public List<ImportExample> Examples { get; } = new List<ImportExample>();
}

public class ImportExample
{
    public ImportExample(int rowNumber, object? value)
    {
        RowNumber = rowNumber;
        Value = value;
    }

    public int RowNumber { get; }

    public object? Value { get; }
}
