//using System.Text.Json;
//using System.Text.Json.Serialization;

//using IRI.Maptor.Sta.Common.Primitives;
//using IRI.Maptor.Sta.Spatial.GeoJsonFormat;

//namespace IRI.Maptor.Sta.Spatial.Primitives.GeoJsonFormat;

//public class GeoJsonGeometryConverter : JsonConverter<IGeoJsonGeometry>
//{
//    private const string TypeKey = "type";
//    private const string CoordinateKey = "coordinates";

//    public override bool CanConvert(Type typeToConvert)
//    {
//        return typeof(IGeoJsonGeometry).IsAssignableFrom(typeToConvert);
//    }

//    public override IGeoJsonGeometry Read(
//        ref Utf8JsonReader reader,
//        Type typeToConvert,
//        JsonSerializerOptions options)
//    {
//        if (reader.TokenType == JsonTokenType.Null)
//        {
//            return null;
//        }

//        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
//        JsonElement root = doc.RootElement;

//        if (!root.TryGetProperty(TypeKey, out var typeElement))
//        {
//            throw new JsonException($"Missing '{TypeKey}' property in GeoJSON geometry");
//        }

//        string type = typeElement.GetString();
//        if (!Enum.TryParse<GeometryType>(type, out var geometryType))
//        {
//            return null;
//        }

//        if (!root.TryGetProperty(CoordinateKey, out var coordinatesElement))
//        {
//            throw new JsonException($"Missing '{CoordinateKey}' property in GeoJSON geometry");
//        }

//        return geometryType switch
//        {
//            GeometryType.Point => new GeoJsonPoint
//            {
//                Type = type,
//                Coordinates = coordinatesElement.Deserialize<double[]>(options)
//            },
//            GeometryType.MultiPoint => new GeoJsonMultiPoint
//            {
//                Type = type,
//                Coordinates = coordinatesElement.Deserialize<double[][]>(options)
//            },
//            GeometryType.LineString => new GeoJsonLineString
//            {
//                Type = type,
//                Coordinates = coordinatesElement.Deserialize<double[][]>(options)
//            },
//            GeometryType.MultiLineString => new GeoJsonMultiLineString
//            {
//                Type = type,
//                Coordinates = coordinatesElement.Deserialize<double[][][]>(options)
//            },
//            GeometryType.Polygon => new GeoJsonPolygon
//            {
//                Type = type,
//                Coordinates = coordinatesElement.Deserialize<double[][][]>(options)
//            },
//            GeometryType.MultiPolygon => new GeoJsonMultiPolygon
//            {
//                Type = type,
//                Coordinates = coordinatesElement.Deserialize<double[][][][]>(options)
//            },
//            _ => throw new NotImplementedException($"Geometry type '{type}' is not implemented")
//        };
//    }

//    public override void Write(
//        Utf8JsonWriter writer,
//        IGeoJsonGeometry value,
//        JsonSerializerOptions options)
//    {
//        if (value == null)
//        {
//            writer.WriteNullValue();
//            return;
//        }

//        writer.WriteStartObject();
//        writer.WriteString("type", value.Type);

//        writer.WritePropertyName("coordinates");
//        switch (value)
//        {
//            case GeoJsonPoint point:
//                JsonSerializer.Serialize(writer, point.Coordinates, options);
//                break;
//            case GeoJsonMultiPoint multiPoint:
//                JsonSerializer.Serialize(writer, multiPoint.Coordinates, options);
//                break;
//            case GeoJsonLineString lineString:
//                JsonSerializer.Serialize(writer, lineString.Coordinates, options);
//                break;
//            case GeoJsonMultiLineString multiLineString:
//                JsonSerializer.Serialize(writer, multiLineString.Coordinates, options);
//                break;
//            case GeoJsonPolygon polygon:
//                JsonSerializer.Serialize(writer, polygon.Coordinates, options);
//                break;
//            case GeoJsonMultiPolygon multiPolygon:
//                JsonSerializer.Serialize(writer, multiPolygon.Coordinates, options);
//                break;
//            default:
//                throw new NotImplementedException($"Geometry type '{value.Type}' is not implemented");
//        }

//        writer.WriteEndObject();
//    }
//}