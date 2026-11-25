using IRI.Maptor.Core.Common.Abstractions;

namespace IRI.Maptor.Core.Spatial.Analysis.PointsOnLines;

public enum PointLineIssueSeverity
{
    /// <summary>Worth knowing; the data may well be right (e.g. a point shared by two lines).</summary>
    Info = 0,

    /// <summary>Probably a data problem; the topology is still usable.</summary>
    Warning = 1,

    /// <summary>The data is wrong; whatever depends on it is unreliable.</summary>
    Error = 2,
}

public enum PointLineIssueCode
{
    /// <summary>A point within the tolerance of no line. <see cref="PointLineIssue{T}.PointIndex"/>.</summary>
    OrphanPoint,

    /// <summary>
    /// Two points of one line at the same place along it. <see cref="PointLineIssue{T}.PointIndex"/> and
    /// <see cref="PointLineIssue{T}.OtherPointIndex"/>.
    /// </summary>
    CoincidentPoints,

    /// <summary>A line with no usable part (null, empty, fewer than two vertices or zero length).</summary>
    InvalidLine,

    /// <summary>A point with no location (null or not a number).</summary>
    InvalidPoint,

    /// <summary>Sequence numbers were given, but this point on a line has none.</summary>
    MissingSequence,

    /// <summary>
    /// The same sequence number twice on one line. <see cref="PointLineIssue{T}.PointIndex"/> is the repeat,
    /// <see cref="PointLineIssue{T}.OtherPointIndex"/> the first one, <see cref="PointLineIssue{T}.Value"/> the number.
    /// </summary>
    DuplicateSequence,

    /// <summary>
    /// A point whose number breaks the rising order along the line (it is not on the longest
    /// non-decreasing run). <see cref="PointLineIssue{T}.Value"/> is its number.
    /// </summary>
    SequenceOutOfOrder,

    /// <summary>
    /// Numbers missing between two neighbours along the line. <see cref="PointLineIssue{T}.OtherPointIndex"/>
    /// is the point before the gap, <see cref="PointLineIssue{T}.PointIndex"/> the one after,
    /// <see cref="PointLineIssue{T}.Value"/> how many numbers are missing.
    /// </summary>
    SequenceGap,

    /// <summary>The numbers rise in neither direction along the line, so it keeps its drawn direction.</summary>
    DirectionUndecided,

    /// <summary>
    /// An end vertex of a line part with no point on it. <see cref="PointLineIssue{T}.PartIndex"/>,
    /// <see cref="PointLineIssue{T}.Location"/> = the vertex, <see cref="PointLineIssue{T}.Value"/> = 0 for
    /// the start, the part length for the end.
    /// </summary>
    OpenLineEnd,

    /// <summary>A line with fewer than two points on it. <see cref="PointLineIssue{T}.Value"/> is the count.</summary>
    LineWithoutPoints,

    /// <summary>A point on two or more lines. <see cref="PointLineIssue{T}.Value"/> is the number of lines.</summary>
    SharedPoint,
}

/// <summary>
/// One finding of <see cref="PointLineTopologyBuilder{T}"/>. Lines and points are referred to by their
/// index in the lists passed to the builder.
/// </summary>
public sealed class PointLineIssue<T> where T : IPoint, new()
{
    public PointLineIssue(
        PointLineIssueCode code,
        int? lineIndex = null,
        int? pointIndex = null,
        int? otherPointIndex = null,
        int? partIndex = null,
        double? value = null,
        T? location = default)
    {
        Code = code;
        Severity = SeverityOf(code);
        LineIndex = lineIndex;
        PointIndex = pointIndex;
        OtherPointIndex = otherPointIndex;
        PartIndex = partIndex;
        Value = value;
        Location = location;
    }

    public PointLineIssueCode Code { get; }

    public PointLineIssueSeverity Severity { get; }

    public int? LineIndex { get; }

    public int? PointIndex { get; }

    public int? OtherPointIndex { get; }

    public int? PartIndex { get; }

    public double? Value { get; }

    /// <summary>Where to show the issue: the point, or the vertex of an open line end.</summary>
    public T? Location { get; }

    public static PointLineIssueSeverity SeverityOf(PointLineIssueCode code) => code switch
    {
        PointLineIssueCode.OrphanPoint => PointLineIssueSeverity.Error,
        PointLineIssueCode.CoincidentPoints => PointLineIssueSeverity.Error,
        PointLineIssueCode.InvalidLine => PointLineIssueSeverity.Error,
        PointLineIssueCode.InvalidPoint => PointLineIssueSeverity.Error,
        PointLineIssueCode.SharedPoint => PointLineIssueSeverity.Info,
        _ => PointLineIssueSeverity.Warning,
    };

    public override string ToString()
        => $"{Severity} {Code}: line {LineIndex?.ToString() ?? "-"}, point {PointIndex?.ToString() ?? "-"}"
           + (OtherPointIndex.HasValue ? $" / {OtherPointIndex}" : string.Empty)
           + (Value.HasValue ? $", value {Value:0.###}" : string.Empty);
}
