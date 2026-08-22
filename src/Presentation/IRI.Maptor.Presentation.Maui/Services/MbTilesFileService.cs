using IRI.Maptor.Presentation.Maui.Layers;

using Microsoft.Maui.Storage;

namespace IRI.Maptor.Presentation.Maui.Services;

/// <summary>Default <see cref="IMbTilesFileService"/> built on MAUI's <see cref="FilePicker"/>.</summary>
public sealed class MbTilesFileService : IMbTilesFileService
{
    // .mbtiles has no standard MIME/UTI, so on Android/Apple we accept any file and let the
    // MBTiles reader validate it; WinUI can filter on the extension directly.
    private static readonly FilePickerFileType _mbTilesFileType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        [DevicePlatform.WinUI] = new[] { ".mbtiles" },
        [DevicePlatform.Android] = new[] { "*/*" },
        [DevicePlatform.iOS] = new[] { "public.data" },
        [DevicePlatform.MacCatalyst] = new[] { "public.data" },
    });

    public async Task<IReadOnlyList<MapLayer>?> PickAndLoadAsync()
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select an MBTiles file",
            FileTypes = _mbTilesFileType,
        });

        if (result is null)
        {
            // User cancelled.
            return null;
        }

        var name = Path.GetFileNameWithoutExtension(result.FileName);

        // FilePicker copies the file to an app-cache path that Microsoft.Data.Sqlite can open.
        return MbTilesLayerFactory.CreateLayers(result.FullPath, name);
    }
}
