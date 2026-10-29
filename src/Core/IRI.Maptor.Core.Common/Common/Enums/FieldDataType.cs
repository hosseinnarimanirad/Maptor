using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Core.Common.Enums;

/// <summary>
/// The logical type of a field's values, independent of any runtime or database: what a client needs to
/// choose an editor, a format, a filter or a statistic. Sent with every <see cref="Primitives.Field"/>;
/// <see cref="Unknown"/> means the producer did not say (an older server, or a source that only knows a
/// native type name), and clients fall back to <see cref="Primitives.Field.TypeFullName"/>.
/// </summary>
/// <remarks>Members may be added later; readers turn names they do not know into <see cref="Unknown"/>.</remarks>
public enum FieldDataType
{
    Unknown = 0,

    Text,

    Boolean,

    /// <summary>Whole numbers up to 32 bits (byte, short, int and their unsigned forms but uint).</summary>
    Integer,

    /// <summary>64-bit whole numbers (long, uint, ulong).</summary>
    Long,

    /// <summary>Binary floating point (float, double).</summary>
    Double,

    Decimal,

    /// <summary>Dates with or without a time of day (DateTime, DateTimeOffset).</summary>
    DateTime,

    Guid,

    Binary,

    Geometry,
}

/// <summary>
/// Reads a <see cref="FieldDataType"/> from its name or number and writes its name. Unlike
/// <see cref="JsonStringEnumConverter"/> it never fails: a name or number it does not know (a member added
/// by a newer server) reads as <see cref="FieldDataType.Unknown"/>, so one new type cannot break a whole
/// feature set for an older client. Apply it to the property: a property converter wins over the
/// <see cref="JsonStringEnumConverter"/> that clients register in their options.
/// </summary>
public class FieldDataTypeJsonConverter : JsonConverter<FieldDataType>
{
    public override FieldDataType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return Enum.TryParse<FieldDataType>(reader.GetString(), ignoreCase: true, out var named) && Enum.IsDefined(typeof(FieldDataType), named)
                    ? named
                    : FieldDataType.Unknown;

            case JsonTokenType.Number:
                return reader.TryGetInt32(out var number) && Enum.IsDefined(typeof(FieldDataType), number)
                    ? (FieldDataType)number
                    : FieldDataType.Unknown;

            default:
                reader.Skip();
                return FieldDataType.Unknown;
        }
    }

    public override void Write(Utf8JsonWriter writer, FieldDataType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
