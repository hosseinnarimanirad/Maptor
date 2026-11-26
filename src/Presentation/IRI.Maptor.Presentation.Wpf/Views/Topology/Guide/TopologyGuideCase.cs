namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// The cases a topology guide can illustrate with <see cref="TopologyCaseFigure"/>: one per issue kind of the
/// standard rule types, and one per fix (drawn as before → after).
/// </summary>
public enum TopologyGuideCase
{
    /// <summary>What the marks mean: a line, a second line, a point, the three severities.</summary>
    Legend,

    // ---- geometry
    GeometryInvalid,

    // ---- points of one layer
    PointsSamePlace,
    PointsClose,
    PointsChain,

    // ---- points on lines
    PointNearLine,
    PointFarFromLines,
    LineEndNotCovered,
    LineTooFewPoints,
    PointOnSeveralLines,

    // ---- numbers along a line
    SequenceDuplicate,
    SequenceOutOfOrder,
    SequenceGap,
    SequenceMissing,
    SequenceDirectionUndecided,

    // ---- line ends
    EndUnsnapped,
    EndOvershoot,
    EndUndershoot,
    EndFree,
    EndChain,

    // ---- parts of one line
    PartsGap,
    PartsBranch,
    PartsOutOfOrder,
    PartsReversed,

    // ---- lines of one layer
    LinesDuplicate,
    LinesSharedStretch,
    TouchAndReturn,

    // ---- fixes (before → after)
    FixMoveVertex,
    FixInsertVertex,
    FixSnapEnds,
    FixTrimOvershoot,
    FixExtendAlong,
    FixJoinNearest,
    FixDeleteDuplicatePoint,
    FixDeleteDuplicateLine,
}

/// <summary>The chip on a guide card: how the case is reported, or that the card shows a fix or correct data.</summary>
public enum TopologyGuideBadge
{
    None,
    Error,
    Warning,
    Notice,
    Fix,
    Correct,
}
