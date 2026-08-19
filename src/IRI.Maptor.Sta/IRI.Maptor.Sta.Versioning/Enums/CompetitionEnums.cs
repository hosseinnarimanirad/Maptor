namespace IRI.Maptor.Sta.Versioning;

/// <remarks>
/// Numeric values are load-bearing: the database enforces "at most one Open and one
/// Resolved competition per feature" with filtered unique indexes on
/// <c>[State] = 0</c> and <c>[State] = 1</c>. Do not renumber.
/// </remarks>
public enum CompetitionState : byte
{
    Open = 0,
    Resolved = 1,
    Committed = 2,
    ClosedNoWinner = 3,
    Dissolved = 4,
}

public enum CompetitionKind : byte
{
    /// <summary>Formed automatically when submissions collide on the same feature id.</summary>
    IdCollision = 1,

    /// <summary>Formed by a reviewer manually grouping proposals (e.g. competing creates).</summary>
    ManualGroup = 2,
}
