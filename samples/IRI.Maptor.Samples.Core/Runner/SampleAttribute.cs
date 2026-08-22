namespace IRI.Maptor.Samples.Core.Runner;

/// <summary>
/// Marks a parameterless static method as a runnable sample.
/// The runner discovers these by reflection; nothing else is needed to register a sample.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SampleAttribute(string id, string title) : Attribute
{
    /// <summary>Stable id used on the command line, e.g. <c>geodesy/precision</c>.</summary>
    public string Id { get; } = id;

    /// <summary>One-line title shown in the list.</summary>
    public string Title { get; } = title;
}
