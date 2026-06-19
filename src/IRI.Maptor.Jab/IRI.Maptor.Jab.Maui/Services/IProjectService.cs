using IRI.Maptor.Jab.Maui.Projects;

namespace IRI.Maptor.Jab.Maui.Services;

/// <summary>Loads and persists <see cref="Project"/> files.</summary>
public interface IProjectService
{
    /// <summary>Reads every saved project, ordered by name.</summary>
    Task<List<Project>> LoadAllAsync();

    /// <summary>Creates or overwrites the project's file.</summary>
    Task SaveAsync(Project project);

    /// <summary>Deletes the project's file (no-op if it doesn't exist).</summary>
    Task DeleteAsync(Project project);
}
