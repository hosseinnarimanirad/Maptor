using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.Spatial.Analysis.PointsOnLines;

/// <summary>Where one point sits on one line.</summary>
public sealed class LineStation<T> where T : IPoint, new()
{
    public LineStation(int pointIndex, int lineIndex, int partIndex, double measure, double offset, long? sequence, T location)
    {
        PointIndex = pointIndex;
        LineIndex = lineIndex;
        PartIndex = partIndex;
        Measure = measure;
        Offset = offset;
        Sequence = sequence;
        Location = location;
    }

    public int PointIndex { get; }

    public int LineIndex { get; }

    /// <summary>The part of a multi-part line the point is on; 0 for a single line.</summary>
    public int PartIndex { get; }

    /// <summary>
    /// Distance from the start of the part, along it, in the direction it was drawn (metres when
    /// geodetic, coordinate units otherwise).
    /// </summary>
    public double Measure { get; }

    /// <summary>Distance from the point to the line (same unit as <see cref="Measure"/>).</summary>
    public double Offset { get; }

    public long? Sequence { get; }

    public T Location { get; }

    public override string ToString() => $"point {PointIndex} on line {LineIndex}/{PartIndex} at {Measure:0.###} (±{Offset:0.###})";
}

/// <summary>One line of a <see cref="PointLineTopology{T}"/> with its points in order.</summary>
public sealed class PointLineTopologyLine<T> where T : IPoint, new()
{
    private readonly Dictionary<int, int> _positions;

    public PointLineTopologyLine(
        int lineIndex,
        Geometry<T>? geometry,
        IReadOnlyList<double> partLengths,
        IReadOnlyList<LineStation<T>> stations,
        bool isReversed,
        bool isDirectionDecided)
    {
        LineIndex = lineIndex;
        Geometry = geometry;
        PartLengths = partLengths;
        Stations = stations;
        IsReversed = isReversed;
        IsDirectionDecided = isDirectionDecided;

        _positions = new Dictionary<int, int>(stations.Count);

        for (int i = 0; i < stations.Count; i++)
            _positions[stations[i].PointIndex] = i;
    }

    public int LineIndex { get; }

    public Geometry<T>? Geometry { get; }

    /// <summary>Length of each part; 0 for a part that is not usable.</summary>
    public IReadOnlyList<double> PartLengths { get; }

    public double Length => PartLengths.Sum();

    /// <summary>
    /// The points on the line in order: by part, then along each part. When <see cref="IsReversed"/>,
    /// the parts and the points run against the drawn direction so that sequence numbers rise.
    /// </summary>
    public IReadOnlyList<LineStation<T>> Stations { get; }

    /// <summary>True when <see cref="Stations"/> run against the direction the line was drawn in.</summary>
    public bool IsReversed { get; }

    /// <summary>False when sequence numbers were used but rise in neither direction.</summary>
    public bool IsDirectionDecided { get; }

    public bool IsValid => PartLengths.Any(l => l > 0);

    /// <summary>Position of the point in <see cref="Stations"/>, or -1 when it is not on this line.</summary>
    public int IndexOf(int pointIndex) => _positions.TryGetValue(pointIndex, out var position) ? position : -1;

    public bool Contains(int pointIndex) => _positions.ContainsKey(pointIndex);

    public override string ToString() => $"line {LineIndex}: {Stations.Count} points" + (IsReversed ? " (reversed)" : string.Empty);
}

/// <summary>
/// Which points lie on which lines, and in what order: the result of
/// <see cref="PointLineTopologyBuilder{T}"/>. Lines and points keep the indices they had in the input.
/// </summary>
public sealed class PointLineTopology<T> where T : IPoint, new()
{
    private static readonly IReadOnlyList<int> NoLines = Array.Empty<int>();

    private readonly IReadOnlyList<IReadOnlyList<int>> _linesOfPoints;

    public PointLineTopology(
        IReadOnlyList<PointLineTopologyLine<T>> lines,
        IReadOnlyList<IReadOnlyList<int>> linesOfPoints,
        IReadOnlyList<PointLineIssue<T>> issues,
        double tolerance,
        bool isGeodetic)
    {
        Lines = lines;
        _linesOfPoints = linesOfPoints;
        Issues = issues;
        Tolerance = tolerance;
        IsGeodetic = isGeodetic;
    }

    /// <summary>One entry per input line, in input order (unusable lines have no stations).</summary>
    public IReadOnlyList<PointLineTopologyLine<T>> Lines { get; }

    public IReadOnlyList<PointLineIssue<T>> Issues { get; }

    public double Tolerance { get; }

    public bool IsGeodetic { get; }

    public int PointCount => _linesOfPoints.Count;

    /// <summary>Indices of the lines the point is on: none for an orphan, two or more for a shared point.</summary>
    public IReadOnlyList<int> GetLineIndices(int pointIndex)
        => pointIndex >= 0 && pointIndex < _linesOfPoints.Count ? _linesOfPoints[pointIndex] : NoLines;

    public IEnumerable<PointLineTopologyLine<T>> GetLinesOfPoint(int pointIndex)
        => GetLineIndices(pointIndex).Select(i => Lines[i]);

    /// <summary>The issues about the line itself and about every point on it.</summary>
    public IEnumerable<PointLineIssue<T>> GetIssuesOfLine(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= Lines.Count)
            return Enumerable.Empty<PointLineIssue<T>>();

        var line = Lines[lineIndex];

        return Issues.Where(i => i.LineIndex == lineIndex
                                 || (i.LineIndex is null && i.PointIndex is int p && line.Contains(p)));
    }

    public IEnumerable<PointLineIssue<T>> GetIssuesOfPoint(int pointIndex)
        => Issues.Where(i => i.PointIndex == pointIndex || i.OtherPointIndex == pointIndex);
}
