using IRI.Maptor.Jab.Maui.Layers;

using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;

namespace IRI.Maptor.Jab.Maui.Services;

/// <summary>Default <see cref="IGeoJsonFileService"/> built on MAUI's <see cref="FilePicker"/>.</summary>
public sealed class GeoJsonFileService : IGeoJsonFileService
{
    // Best-effort file-type filters per platform. GeoJSON has no universal UTI/MIME, so we
    // also accept generic json/text/data.
    private static readonly FilePickerFileType _geoJsonFileType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        [DevicePlatform.WinUI] = new[] { ".geojson", ".json" },
        [DevicePlatform.Android] = new[] { "application/geo+json", "application/json", "text/plain" },
        [DevicePlatform.iOS] = new[] { "public.json", "public.text", "public.data" },
        [DevicePlatform.MacCatalyst] = new[] { "public.json", "public.text", "public.data" },
    });

    public async Task<MapLayer?> PickAndLoadAsync(Color color)
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select a GeoJSON file",
            FileTypes = _geoJsonFileType,
        });

        if (result is null)
        {
            // User cancelled.
            return null;
        }

        using var stream = await result.OpenReadAsync();
        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync();

        var name = Path.GetFileNameWithoutExtension(result.FileName);

        return GeoJsonLayerFactory.FromGeoJson(text, name, color);
    }
}
