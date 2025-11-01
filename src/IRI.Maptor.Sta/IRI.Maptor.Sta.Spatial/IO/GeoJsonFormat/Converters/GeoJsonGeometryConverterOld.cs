//using IRI.Maptor.Sta.Common.Primitives;
//using IRI.Maptor.Sta.Spatial.GeoJsonFormat;

//namespace IRI.Maptor.Sta.Spatial.Primitives.GeoJson.Converters;

//public class GeoJsonGeometryConverter : JsonConverter
//{
//    private const string typeKey = "type";

//    private const string coordinateKey = "coordinates";

//    public override bool CanConvert(Type objectType)
//    {
//        throw new NotImplementedException();
//    }

//    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
//    {
//        // 1400.02.02
//        if (reader.TokenType == JsonToken.Null)
//        {
//            return null;
//        }

//        JObject jObject = JObject.Load(reader);

//        var type = jObject[typeKey]?.ToString();

//        GeometryType geometryType;

//        if (!Enum.TryParse<GeometryType>(type, out geometryType))
//        {
//            return null;
//        }

//        switch (geometryType)
//        {
//            case GeometryType.Point:
//                return new GeoJsonPoint() { Type = type, Coordinates = jObject[coordinateKey].ToObject<double[]>() };

//            case GeometryType.MultiPoint:
//                return new GeoJsonMultiPoint() { Type = type, Coordinates = jObject[coordinateKey].ToObject<double[][]>() };

//            case GeometryType.LineString:
//                return new GeoJsonLineString() { Type = type, Coordinates = jObject[coordinateKey].ToObject<double[][]>() };

//            case GeometryType.MultiLineString:
//                return new GeoJsonMultiLineString() { Type = type, Coordinates = jObject[coordinateKey].ToObject<double[][][]>() };

//            case GeometryType.Polygon:
//                return new GeoJsonPolygon() { Type = type, Coordinates = jObject[coordinateKey].ToObject<double[][][]>() };

//            case GeometryType.MultiPolygon:
//                return new GeoJsonMultiPolygon() { Type = type, Coordinates = jObject[coordinateKey].ToObject<double[][][][]>() };

//            default:
//                throw new NotImplementedException();
//        }

//    }

//    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
//    {
//        var geometry = value as IGeoJsonGeometry;

//        if (geometry == null)
//        {
//            return;
//        }
//        writer.WriteStartObject();

//        writer.WritePropertyName("type");
//        writer.WriteValue(geometry.Type);

//        //writer.WritePropertyName("coordinates");
//        writer.WriteRaw(", \"coordinates\": ");

//        //writer.WriteStartArray();

//        switch (geometry.Type)
//        {
//            case GeoJson.Point:
//                //writer.WriteValue(geometry as GeoJsonPoint);
//                writer.WriteRaw(JsonConvert.SerializeObject((geometry as GeoJsonPoint).Coordinates));
//                break;
//            case GeoJson.MultiPoint:
//                //writer.WriteValue(geometry as GeoJsonMultiPoint);
//                writer.WriteRaw(JsonConvert.SerializeObject((geometry as GeoJsonMultiPoint).Coordinates));
//                break;
//            case GeoJson.LineString:
//                //writer.WriteValue(geometry as GeoJsonLineString);
//                writer.WriteRaw(JsonConvert.SerializeObject((geometry as GeoJsonLineString).Coordinates));
//                break;
//            case GeoJson.MultiLineString:
//                //writer.WriteValue(geometry as GeoJsonMultiLineString);
//                writer.WriteRaw(JsonConvert.SerializeObject((geometry as GeoJsonMultiLineString).Coordinates));
//                break;
//            case GeoJson.Polygon:
//                //writer.WriteValue(geometry as GeoJsonPolygon);
//                writer.WriteRaw(JsonConvert.SerializeObject((geometry as GeoJsonPolygon).Coordinates));
//                break;
//            case GeoJson.MultiPolygon:
//                //writer.WriteValue(geometry as GeoJsonMultiPolygon);
//                writer.WriteRaw(JsonConvert.SerializeObject((geometry as GeoJsonMultiPolygon).Coordinates));
//                break;
//            default:
//                break;
//        }

//        writer.WriteEndObject();
//        //var type = (value as IGeoJsonGeometry).Type;

//    }
//}
