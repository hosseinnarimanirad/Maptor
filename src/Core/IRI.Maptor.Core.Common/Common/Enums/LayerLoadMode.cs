using System.Text.Json.Serialization;

namespace IRI.Maptor.Core.Common.Enums;

/// <summary>
/// How a server layer gets its features. Chosen per layer by an administrator (LayerSetting.LoadMode):
/// light layers stay in <see cref="Cache"/>, heavy ones (hundreds of thousands of rows) use <see cref="Live"/>.
/// Stored as an int; on the wire as its name. The converter sits on the type so that every reader accepts
/// the name (and still the number), whatever JsonSerializerOptions it was given.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LayerLoadMode
{
    /// <summary>Downloads the whole layer once, with every attribute, and works on it in memory.</summary>
    Cache = 0,

    /// <summary>Fetches the features of the current map extent from the server as the map moves; the attribute table queries the server.</summary>
    Live = 1,
}
