using System.Globalization;
using System.Text.Json;

namespace IRI.Maptor.Core.Satellite.Elements;

/// <summary>
/// Reads Orbit Mean-Elements Messages (CCSDS OMM) in the JSON form CelesTrak serves as "GP data"
/// (<c>gp.php?…&amp;FORMAT=JSON</c>): an array of objects with keys such as OBJECT_NAME, EPOCH,
/// MEAN_MOTION, ECCENTRICITY and BSTAR. A single object (not in an array) is accepted too.
/// </summary>
/// <remarks>
/// OMM carries the same SGP4 mean elements as a TLE, in plain numbers and without the five-digit
/// catalog-number limit, which is why CelesTrak recommends it over TLE for new software.
/// </remarks>
public static class OmmJsonReader
{
    public static List<OrbitalElements> Read(string json, Action<string>? onError = null)
    {
        if (json is null)
            throw new ArgumentNullException(nameof(json));

        using var document = JsonDocument.Parse(json);

        var result = new List<OrbitalElements>();

        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Object)
        {
            Add(root, 0);
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            int index = 0;

            foreach (var item in root.EnumerateArray())
                Add(item, index++);
        }
        else
        {
            throw new FormatException("OMM JSON must be an object or an array of objects.");
        }

        return result;

        void Add(JsonElement item, int index)
        {
            try
            {
                result.Add(ReadOne(item));
            }
            catch (Exception ex) when (ex is FormatException or KeyNotFoundException or InvalidOperationException or ArgumentException)
            {
                var message = $"Item {index}: {ex.Message}";

                if (onError is null)
                    throw new FormatException(message, ex);

                onError(message);
            }
        }
    }

    private static OrbitalElements ReadOne(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object)
            throw new FormatException("an OMM item must be a JSON object");

        var epochText = String(item, "EPOCH") ?? throw new KeyNotFoundException("EPOCH is missing");

        var epoch = DateTime.Parse(epochText, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

        var catalog = Number(item, "NORAD_CAT_ID");

        var classification = String(item, "CLASSIFICATION_TYPE");

        return new OrbitalElements(
            catalogNumber: ((long)catalog).ToString(CultureInfo.InvariantCulture),
            epoch: epoch,
            inclination: Number(item, "INCLINATION"),
            rightAscension: Number(item, "RA_OF_ASC_NODE"),
            eccentricity: Number(item, "ECCENTRICITY"),
            argumentOfPerigee: Number(item, "ARG_OF_PERICENTER"),
            meanAnomaly: Number(item, "MEAN_ANOMALY"),
            meanMotion: Number(item, "MEAN_MOTION"),
            bStar: OptionalNumber(item, "BSTAR"),
            meanMotionDot: OptionalNumber(item, "MEAN_MOTION_DOT"),
            meanMotionDdot: OptionalNumber(item, "MEAN_MOTION_DDOT"),
            name: String(item, "OBJECT_NAME"),
            internationalDesignator: String(item, "OBJECT_ID"),
            classification: string.IsNullOrEmpty(classification) ? 'U' : classification![0],
            elementSetNumber: (int)OptionalNumber(item, "ELEMENT_SET_NO"),
            revolutionNumber: (int)OptionalNumber(item, "REV_AT_EPOCH"));
    }

    private static string? String(JsonElement item, string key)
        => item.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static double Number(JsonElement item, string key)
    {
        if (!item.TryGetProperty(key, out var value))
            throw new KeyNotFoundException($"{key} is missing");

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetDouble(),
            // some producers quote numbers
            JsonValueKind.String => double.Parse(value.GetString()!, NumberStyles.Float, CultureInfo.InvariantCulture),
            _ => throw new FormatException($"{key} is not a number"),
        };
    }

    private static double OptionalNumber(JsonElement item, string key)
        => item.TryGetProperty(key, out var value) && value.ValueKind is JsonValueKind.Number or JsonValueKind.String ? Number(item, key) : 0;
}
