using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Jab.Core.Models.Project;

/// <summary>
/// Reads and writes map project files (*.mtproj). Uses its own pinned
/// <see cref="JsonSerializerOptions"/> so the on-disk format stays stable
/// independently of application-wide serializer defaults.
/// </summary>
public static class MapProjectFile
{
    public const string Extension = "mtproj";

    public const int SupportedFormatVersion = 1;

    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // local file format; keep embedded SLD XML / GeoJSON human-readable
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    public static MapProject? Load(string path)
    {
        var json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<MapProject>(json, _options);
    }

    public static void Save(string path, MapProject project)
    {
        var json = JsonSerializer.Serialize(project, _options);

        File.WriteAllText(path, json);
    }
}
