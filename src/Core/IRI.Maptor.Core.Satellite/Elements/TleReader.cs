namespace IRI.Maptor.Core.Satellite.Elements;

/// <summary>
/// Reads a file or text of TLEs: 2-line sets, 3-line sets with a name line (with or without the
/// "0 " prefix), or a mix, as CelesTrak and Space-Track publish them. Blank lines and lines starting
/// with '#' are skipped.
/// </summary>
public static class TleReader
{
    /// <param name="text">The whole file's text.</param>
    /// <param name="onError">
    /// Called for each set that cannot be read, which is then skipped. Without it the first bad set throws.
    /// </param>
    public static List<OrbitalElements> Read(string text, Action<TleReadError>? onError = null)
    {
        if (text is null)
            throw new ArgumentNullException(nameof(text));

        using var reader = new StringReader(text);

        return Read(reader, onError);
    }

    public static List<OrbitalElements> ReadFile(string path, Action<TleReadError>? onError = null)
    {
        using var reader = new StreamReader(path);

        return Read(reader, onError);
    }

    public static List<OrbitalElements> Read(TextReader reader, Action<TleReadError>? onError = null)
    {
        if (reader is null)
            throw new ArgumentNullException(nameof(reader));

        var result = new List<OrbitalElements>();

        string? name = null;

        string? pendingLine1 = null;

        int pendingLineNumber = 0;

        int lineNumber = 0;

        void Fail(int at, string message, string? line1, string? line2)
        {
            var error = new TleReadError(at, message, line1, line2);

            if (onError is null)
                throw new FormatException($"Line {at}: {message}");

            onError(error);
        }

        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;

            var trimmed = line.TrimEnd();

            if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal))
                continue;

            if (trimmed.StartsWith("1 ", StringComparison.Ordinal))
            {
                if (pendingLine1 is not null)
                    Fail(pendingLineNumber, "Line 1 is not followed by a line 2.", pendingLine1, null);

                pendingLine1 = trimmed;
                pendingLineNumber = lineNumber;
            }
            else if (trimmed.StartsWith("2 ", StringComparison.Ordinal) && pendingLine1 is not null)
            {
                if (TwoLineElement.TryParse(pendingLine1, trimmed, name, out var elements, out var message))
                    result.Add(elements!);
                else
                    Fail(pendingLineNumber, message!, pendingLine1, trimmed);

                pendingLine1 = null;
                name = null;
            }
            else
            {
                if (pendingLine1 is not null)
                {
                    Fail(pendingLineNumber, "Line 1 is not followed by a line 2.", pendingLine1, null);
                    pendingLine1 = null;
                }

                // anything else is the name line of the next set
                name = trimmed;
            }
        }

        if (pendingLine1 is not null)
            Fail(pendingLineNumber, "The text ends after a line 1.", pendingLine1, null);

        return result;
    }
}

/// <summary>A set <see cref="TleReader"/> could not read.</summary>
public sealed class TleReadError
{
    public TleReadError(int lineNumber, string message, string? line1, string? line2)
    {
        LineNumber = lineNumber;
        Message = message;
        Line1 = line1;
        Line2 = line2;
    }

    /// <summary>1-based line number of the set's line 1.</summary>
    public int LineNumber { get; }

    public string Message { get; }

    public string? Line1 { get; }

    public string? Line2 { get; }

    public override string ToString() => $"Line {LineNumber}: {Message}";
}
