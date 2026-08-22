using IRI.Maptor.Presentation.Maui.Controls;

namespace IRI.Maptor.Presentation.Maui.Projects;

/// <summary>
/// A saved map project: a named basemap selection plus the set of vector layers the user
/// has loaded. Serialized to a JSON file (one per project) by <see cref="Services.ProjectService"/>.
/// </summary>
public sealed class Project
{
    /// <summary>Stable identifier; also the on-disk file name.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public MauiBaseMap BaseMap { get; set; } = MauiBaseMap.GoogleHybrid;

    public List<ProjectLayer> Layers { get; set; } = new();
}

/// <summary>A single persisted layer within a <see cref="Project"/>.</summary>
public sealed class ProjectLayer
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Stroke color as an ARGB hex string, e.g. "#FFFF0000".</summary>
    public string ColorHex { get; set; } = "#FFFF0000";

    public bool IsVisible { get; set; } = true;

    /// <summary>The GeoJSON source the layer is rebuilt from when the project is opened.</summary>
    public string GeoJson { get; set; } = string.Empty;
}
