using System;
using System.Collections.Generic;
using System.Globalization;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.DataSources;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Dialogs;

public enum DelimitedTextError
{
    None,
    TooFewColumns,
    ColumnCountMismatch,
    NotNumber,
    TooFewPoints,
}

/// <summary>
/// Result of <see cref="DelimitedTextValidator.Validate"/>. <see cref="Line"/> is 1-based, counted over
/// every line of the text (blank ones included) so it matches what the user sees in the box.
/// </summary>
public readonly record struct DelimitedTextValidation(DelimitedTextError Error, int Line = 0, int Columns = 0, int ExpectedColumns = 0, int RequiredPoints = 0)
{
    public bool IsValid => Error == DelimitedTextError.None;
}

/// <summary>
/// Checks pasted or previewed CSV/TSV text the same way <see cref="TextDataSource"/> reads it, so the
/// dialog can keep Open disabled instead of letting the import fail.
/// </summary>
public static class DelimitedTextValidator
{
    /// <param name="skipPreviewMarker">True for a file preview: its "... (N more lines)" line is ignored.</param>
    public static DelimitedTextValidation Validate(
        string text,
        char delimiter,
        bool useFirstLineAsHeader,
        bool isLongitudeFirst,
        GeometryType geometryType,
        bool skipPreviewMarker = false)
    {
        var lines = (text ?? string.Empty).Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        int xIndex = isLongitudeFirst ? 0 : 1;
        int yIndex = isLongitudeFirst ? 1 : 0;

        bool headerPending = useFirstLineAsHeader;
        int expectedColumns = -1;
        int points = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (skipPreviewMarker && line.StartsWith("... (", StringComparison.Ordinal))
                continue;

            var cells = line.Split(delimiter);
            int lineNumber = i + 1;

            if (cells.Length < 2)
                return new(DelimitedTextError.TooFewColumns, lineNumber, cells.Length);

            if (headerPending)
            {
                headerPending = false;

                // Same rule as the import: only point imports look the X/Y columns up by name.
                if (geometryType == GeometryType.Point)
                    (xIndex, yIndex) = TextDataSource.FindXyColumnIndices(cells, isLongitudeFirst);

                continue;
            }

            if (expectedColumns < 0)
                expectedColumns = cells.Length;
            else if (cells.Length != expectedColumns)
                return new(DelimitedTextError.ColumnCountMismatch, lineNumber, cells.Length, expectedColumns);

            if (Math.Max(xIndex, yIndex) >= cells.Length || !IsNumber(cells[xIndex]) || !IsNumber(cells[yIndex]))
                return new(DelimitedTextError.NotNumber, lineNumber, cells.Length);

            points++;
        }

        int required = geometryType switch
        {
            GeometryType.Polygon => 3,
            GeometryType.LineString => 2,
            _ => 1,
        };

        if (points < required)
            return new(DelimitedTextError.TooFewPoints, RequiredPoints: required);

        return new(DelimitedTextError.None);
    }

    /// <summary>
    /// Pasted XY text where every data line is exactly two numbers separated by spaces (as Excel can paste
    /// it) is rewritten with <paramref name="delimiter"/> between the two values. Any other text, e.g. lines
    /// with attributes such as "tower 1", is returned unchanged: spaces inside values can't be told apart
    /// from spaces between fields. Blank lines are kept so line numbers still match the text box.
    /// </summary>
    /// <returns>The rewritten text, or <paramref name="text"/> itself when the rule does not apply.</returns>
    public static string ConvertSpaceSeparatedXy(string text, char delimiter, bool useFirstLineAsHeader)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Contains('\t') || text.Contains(','))
            return text;

        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        bool headerPending = useFirstLineAsHeader;
        int dataLines = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            var tokens = lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length != 2)
                return text;

            if (headerPending)
                headerPending = false;
            else if (IsNumber(tokens[0]) && IsNumber(tokens[1]))
                dataLines++;
            else
                return text;

            lines[i] = tokens[0] + delimiter + tokens[1];
        }

        return dataLines > 0 ? string.Join(Environment.NewLine, lines) : text;
    }

    /// <summary>
    /// The first <paramref name="maxPoints"/> points of the text, read with the same column rules as
    /// <see cref="Validate"/> (X/Y by header name for point imports). Lines that are not two numbers are skipped.
    /// </summary>
    public static IReadOnlyList<Point> ExtractSamplePoints(
        string text,
        char delimiter,
        bool useFirstLineAsHeader,
        bool isLongitudeFirst,
        GeometryType geometryType,
        bool skipPreviewMarker = false,
        int maxPoints = 50)
    {
        var result = new List<Point>();

        var lines = (text ?? string.Empty).Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        int xIndex = isLongitudeFirst ? 0 : 1;
        int yIndex = isLongitudeFirst ? 1 : 0;

        bool headerPending = useFirstLineAsHeader;

        foreach (var line in lines)
        {
            if (result.Count >= maxPoints)
                break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (skipPreviewMarker && line.StartsWith("... (", StringComparison.Ordinal))
                continue;

            var cells = line.Split(delimiter);

            if (headerPending)
            {
                headerPending = false;

                if (geometryType == GeometryType.Point)
                    (xIndex, yIndex) = TextDataSource.FindXyColumnIndices(cells, isLongitudeFirst);

                continue;
            }

            if (Math.Max(xIndex, yIndex) >= cells.Length || !TryParse(cells[xIndex], out var x) || !TryParse(cells[yIndex], out var y))
                continue;

            result.Add(new Point(x, y));
        }

        return result;
    }

    private static bool TryParse(string cell, out double value) =>
        double.TryParse(cell, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);

    // Matches double.Parse(s, CultureInfo.InvariantCulture) used by the import.
    private static bool IsNumber(string cell) =>
        double.TryParse(cell, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _);
}
