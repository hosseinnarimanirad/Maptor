namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>Which kinds of snap <see cref="SnapEngine"/> may return; all on by default.</summary>
public sealed class SnapOptions
{
    /// <summary>Line ends as their own kind, ahead of other vertices. When off, a line end is an ordinary vertex.</summary>
    public bool SnapToEnds { get; set; } = true;

    public bool SnapToVertices { get; set; } = true;

    public bool SnapToIntersections { get; set; } = true;

    public bool SnapToMidpoints { get; set; } = true;

    public bool SnapToEdges { get; set; } = true;

    /// <summary>Every kind on.</summary>
    public static SnapOptions Default => new SnapOptions();
}
