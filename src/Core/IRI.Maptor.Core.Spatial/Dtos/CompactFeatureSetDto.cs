using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Spatial.Dtos;

/// <summary>
/// The compact wire form of a <see cref="FeatureSetDto"/>: attribute names are sent once in
/// <see cref="Columns"/> and each feature is a positional JSON array
/// <c>[id, "wkb-base64", v1, v2, …]</c>, instead of an object repeating every name on every row.
/// The set's SRID is sent once too, and the per-feature <c>Key</c> is left out: the legacy form
/// sends a random Guid there, and <see cref="FeatureDto.AsFeature"/> makes one when it is empty.
/// A client asks for it with <see cref="MediaType"/> in the Accept header; a server that does not
/// know it answers with the legacy JSON, so both forms must stay readable.
/// <see cref="ToFeatureSetDto"/> gives the same values, with the same .NET types, as the legacy
/// path (<see cref="Common.Common.JsonConverters.DictionaryStringObjectConverter"/>).
/// </summary>
public class CompactFeatureSetDto
{
    public const string MediaType = "application/vnd.maptor.featureset.compact+json";

    public int Srid { get; set; }

    public int Count { get; set; }

    public int? LayerId { get; set; }

    public List<Field>? Fields { get; set; }

    /// <summary>The attribute names, in the order of <see cref="CompactFeatureRow.Values"/>.</summary>
    public List<string> Columns { get; set; } = new();

    public List<CompactFeatureRow> Rows { get; set; } = new();

    public FeatureSetDto ToFeatureSetDto()
    {
        var features = new List<FeatureDto>(Rows.Count);

        var columnCount = Columns.Count;

        foreach (var row in Rows)
        {
            var attributes = new Dictionary<string, object>(columnCount);

            for (int i = 0; i < columnCount && i < row.Values.Length; i++)
                attributes[Columns[i]] = row.Values[i]!;

            var feature = new FeatureDto
            {
                Id = row.Id,
                Shape = row.Shape,
                Attributes = attributes,
            };

            // FeatureDto.Srid only accepts WGS84; 0 is left for the caller's SetSrid, as in the legacy form.
            if (Srid != 0)
                feature.Srid = Srid;

            features.Add(feature);
        }

        return new FeatureSetDto(features, Fields, LayerId) { Srid = Srid };
    }

    /// <summary>
    /// Fills this instance from <paramref name="featureSet"/> in memory (the inverse of
    /// <see cref="ToFeatureSetDto"/>), for responses that carry the compact rows inside a larger
    /// object, such as <see cref="FeaturePageDto"/>. The columns are the first feature's attribute
    /// names, as in <see cref="WriteAsync"/>.
    /// </summary>
    protected void Fill(FeatureSetDto featureSet)
    {
        var features = featureSet.Features ?? new List<FeatureDto>();

        Columns = features.Count > 0 && features[0].Attributes != null
            ? new List<string>(features[0].Attributes.Keys)
            : new List<string>();

        Srid = featureSet.Srid != 0 ? featureSet.Srid : (features.Count > 0 ? features[0].Srid : 0);
        Count = features.Count;
        LayerId = featureSet.LayerId;
        Fields = featureSet.Fields;

        Rows = new List<CompactFeatureRow>(features.Count);

        foreach (var feature in features)
        {
            var values = new object?[Columns.Count];

            for (int i = 0; i < Columns.Count; i++)
            {
                if (feature.Attributes != null && feature.Attributes.TryGetValue(Columns[i], out var value))
                    values[i] = value;
            }

            Rows.Add(new CompactFeatureRow
            {
                Id = feature.Id,
                Shape = feature.Shape ?? Array.Empty<byte>(),
                Values = values,
            });
        }
    }

    /// <summary>
    /// Writes <paramref name="featureSet"/> in the compact form, 64 KB at a time so a large set is
    /// never buffered whole. The JSON goes to a memory buffer first and is copied to
    /// <paramref name="stream"/> asynchronously: JsonSerializer.Serialize(writer, …) flushes the writer
    /// synchronously, which ASP.NET Core's response stream refuses. Values are serialized with <paramref name="options"/> by their runtime
    /// type, exactly as the legacy attribute dictionary is, so enums, dates and byte arrays look the
    /// same on the wire. The columns are the first feature's attribute names; every feature of a set
    /// is expected to have the same ones (a missing attribute is written as null).
    /// </summary>
    public static async Task WriteAsync(Stream stream, FeatureSetDto featureSet, JsonSerializerOptions options, CancellationToken cancellationToken = default)
    {
        const int flushThreshold = 64 * 1024;

        var features = featureSet.Features ?? new List<FeatureDto>();

        var columns = new List<string>(features.Count > 0 && features[0].Attributes != null ? features[0].Attributes.Keys : Array.Empty<string>());

        var srid = featureSet.Srid != 0 ? featureSet.Srid : (features.Count > 0 ? features[0].Srid : 0);

        using var buffer = new MemoryStream(2 * flushThreshold);

        // Without an explicit encoder, as ASP.NET Core's own JSON output does: the default one writes
        // every non-ASCII (Persian) character as \uXXXX, 6 bytes instead of 2. Escaping is only needed
        // for JSON embedded in HTML, which a layer list never is.
        var encoder = options.Encoder ?? JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

        await using var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Encoder = encoder, SkipValidation = true });

        async Task CopyBufferAsync()
        {
            writer.Flush();

            await stream.WriteAsync(buffer.GetBuffer(), 0, (int)buffer.Length, cancellationToken);

            buffer.SetLength(0);
        }

        writer.WriteStartObject();
        writer.WriteNumber("srid", srid);
        writer.WriteNumber("count", features.Count);

        if (featureSet.LayerId.HasValue)
            writer.WriteNumber("layerId", featureSet.LayerId.Value);
        else
            writer.WriteNull("layerId");

        writer.WritePropertyName("fields");
        JsonSerializer.Serialize(writer, featureSet.Fields, options);

        writer.WritePropertyName("columns");
        writer.WriteStartArray();
        foreach (var column in columns)
            writer.WriteStringValue(column);
        writer.WriteEndArray();

        writer.WritePropertyName("rows");
        writer.WriteStartArray();

        foreach (var feature in features)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(feature.Id);

            if (feature.Shape is null)
                writer.WriteNullValue();
            else
                writer.WriteBase64StringValue(feature.Shape);

            foreach (var column in columns)
            {
                if (feature.Attributes == null || !feature.Attributes.TryGetValue(column, out var value) || value is null)
                    writer.WriteNullValue();
                else
                    JsonSerializer.Serialize(writer, value, value.GetType(), options);
            }

            writer.WriteEndArray();

            if (buffer.Length + writer.BytesPending > flushThreshold)
                await CopyBufferAsync();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();

        await CopyBufferAsync();
    }
}

/// <summary>One feature of a <see cref="CompactFeatureSetDto"/>: <c>[id, "wkb-base64", v1, v2, …]</c>.</summary>
[JsonConverter(typeof(CompactFeatureRowConverter))]
public class CompactFeatureRow
{
    public int Id { get; set; }

    public byte[] Shape { get; set; } = Array.Empty<byte>();

    public object?[] Values { get; set; } = Array.Empty<object?>();
}

/// <summary>
/// Reads a row token by token (no JsonDocument per row). Values get the types the legacy
/// DictionaryStringObjectConverter gives: string, int → long → double, bool, null; a nested array
/// or object (never sent by a feature list) becomes List&lt;object&gt; / Dictionary&lt;string, object&gt;.
/// </summary>
public class CompactFeatureRowConverter : JsonConverter<CompactFeatureRow>
{
    public override CompactFeatureRow Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("A compact feature row must be a JSON array.");

        var row = new CompactFeatureRow();

        reader.Read();
        row.Id = reader.GetInt32();

        reader.Read();
        row.Shape = reader.TokenType == JsonTokenType.Null ? Array.Empty<byte>() : reader.GetBytesFromBase64();

        var values = new List<object?>();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            values.Add(ReadValue(ref reader));

        row.Values = values.ToArray();

        return row;
    }

    private static object? ReadValue(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();

            case JsonTokenType.Number:
                if (reader.TryGetInt32(out int intValue)) return intValue;
                if (reader.TryGetInt64(out long longValue)) return longValue;
                if (reader.TryGetDouble(out double doubleValue)) return doubleValue;
                if (reader.TryGetDecimal(out decimal decimalValue)) return decimalValue;
                throw new JsonException("Unsupported number format");

            case JsonTokenType.True:
                return true;

            case JsonTokenType.False:
                return false;

            case JsonTokenType.Null:
                return null;

            case JsonTokenType.StartArray:
                var list = new List<object?>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    list.Add(ReadValue(ref reader));
                return list;

            case JsonTokenType.StartObject:
                var dictionary = new Dictionary<string, object?>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    var name = reader.GetString()!;
                    reader.Read();
                    dictionary[name] = ReadValue(ref reader);
                }
                return dictionary;

            default:
                throw new JsonException();
        }
    }

    public override void Write(Utf8JsonWriter writer, CompactFeatureRow value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Id);
        writer.WriteBase64StringValue(value.Shape);

        foreach (var item in value.Values)
        {
            if (item is null)
                writer.WriteNullValue();
            else
                JsonSerializer.Serialize(writer, item, item.GetType(), options);
        }

        writer.WriteEndArray();
    }
}
