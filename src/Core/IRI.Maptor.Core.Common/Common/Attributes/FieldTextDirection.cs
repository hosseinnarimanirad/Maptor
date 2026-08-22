using System.Text.Json.Serialization;

namespace IRI.Maptor.Core.Common.Attributes;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FieldTextDirection
{
    /// <summary>Auto-detect from content (default).</summary>
    Auto,

    /// <summary>Force left-to-right layout.</summary>
    LeftToRight,

    /// <summary>Force right-to-left layout.</summary>
    RightToLeft
}
