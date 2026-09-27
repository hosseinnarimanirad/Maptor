namespace IRI.Maptor.Core.MachineLearning;

/// <summary>
/// One synthetic training line: a three-point polyline in screen (pixel) coordinates and its labelled
/// counterpart, which is the same line when the middle point is retained and the two-point line when
/// it is removed. Produced by <see cref="SyntheticDataFactory"/> and by the labelling tool in
/// research/IRI.Maptor.Res.LRSimplification.
/// </summary>
public class SyntheticDataItem
{
    /// <summary>WKT of the three-point line in screen coordinates (pixels).</summary>
    public string OriginalLineString { get; set; }

    /// <summary>WKT of the labelled result in screen coordinates; equal to the original when retained.</summary>
    public string SimplifiedLineString { get; set; }

    public string Title { get; set; }

    public string Note { get; set; }

    public override string ToString()
    {
        return $"Title: {Title}, Note: {Note}";
    }
}
