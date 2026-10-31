using System;
using System.Text.Json;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Versioning;

/// <summary>Text of a feature attribute value in the versioning views (compare, timeline).</summary>
internal static class VersioningValueFormatter
{
    public static string? Format(object? value) => value switch
    {
        null => null,
        byte[] bytes => Convert.ToBase64String(bytes),
        DateTime dt => FormatDate(dt),
        // attribute values arrive as JSON; the server sends its UTC times with a Z suffix
        JsonElement { ValueKind: JsonValueKind.String } element when element.TryGetDateTime(out var dt) => FormatDate(dt),
        _ => value.ToString(),
    };

    // UTC times are shown in local time
    private static string FormatDate(DateTime dt) => (dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt).ToString("yyyy/MM/dd HH:mm");
}
