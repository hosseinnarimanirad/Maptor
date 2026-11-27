namespace IRI.Maptor.Core.Spatial.Analysis;

/// <summary>Which kinds of snap <see cref="SnapEngine"/> may return.</summary>
public sealed class SnapOptions
{
    public bool SnapToVertices { get; set; } = true;

    public bool SnapToEdges { get; set; } = true;

    /// <summary>Both kinds on.</summary>
    public static SnapOptions Default => new SnapOptions();
}
