using IRI.Maptor.Presentation.Maui.Layers;

namespace IRI.Maptor.Presentation.Maui.Services;

/// <summary>
/// Lets the user pick an <c>.mbtiles</c> file from the device and turns it into map layer(s):
/// a single raster layer, or one layer per sub-layer for vector (MVT) files.
/// </summary>
public interface IMbTilesFileService
{
    /// <summary>
    /// Shows the file picker and loads the chosen MBTiles as one or more layers. Returns
    /// <c>null</c> if the user cancels. Throws if the file is not a valid MBTiles database.
    /// </summary>
    Task<IReadOnlyList<MapLayer>?> PickAndLoadAsync();
}
