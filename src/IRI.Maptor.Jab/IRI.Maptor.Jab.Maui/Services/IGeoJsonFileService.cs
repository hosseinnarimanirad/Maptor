using IRI.Maptor.Jab.Maui.Layers;

using Microsoft.Maui.Graphics;

namespace IRI.Maptor.Jab.Maui.Services;

/// <summary>
/// Lets the user pick a GeoJSON file from the device and turns it into a <see cref="MapLayer"/>.
/// </summary>
public interface IGeoJsonFileService
{
    /// <summary>
    /// Shows the file picker and loads the chosen GeoJSON as a layer with the given color.
    /// Returns <c>null</c> if the user cancels. Throws if the file is not valid GeoJSON.
    /// </summary>
    Task<MapLayer?> PickAndLoadAsync(Color color);
}
