using System.Text.Json;
using System.Text.Json.Serialization;

using IRI.Maptor.Presentation.Maui.Projects;

using Microsoft.Maui.Storage;

namespace IRI.Maptor.Presentation.Maui.Services;

/// <summary>
/// Default <see cref="IProjectService"/> that stores each project as a JSON file under
/// <c>{AppDataDirectory}/Projects</c>.
/// </summary>
public sealed class ProjectService : IProjectService
{
    private static readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _directory;

    public ProjectService()
        : this(Path.Combine(FileSystem.AppDataDirectory, "Projects"))
    {
    }

    public ProjectService(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(_directory);
    }

    public async Task<List<Project>> LoadAllAsync()
    {
        var projects = new List<Project>();

        foreach (var file in Directory.EnumerateFiles(_directory, "*.json"))
        {
            try
            {
                await using var stream = File.OpenRead(file);
                var project = await JsonSerializer.DeserializeAsync<Project>(stream, _json);

                if (project is not null)
                {
                    projects.Add(project);
                }
            }
            catch
            {
                // Skip unreadable / corrupt project files.
            }
        }

        return projects.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task SaveAsync(Project project)
    {
        var path = Path.Combine(_directory, project.Id + ".json");

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, project, _json);
    }

    public Task DeleteAsync(Project project)
    {
        var path = Path.Combine(_directory, project.Id + ".json");

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }
}
