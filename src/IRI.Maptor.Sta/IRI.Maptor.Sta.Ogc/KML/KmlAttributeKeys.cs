using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives; 

namespace IRI.Maptor.Ket.KmlFormat;

public static class KmlAttributeKeys
{
    public const string NameAttributeKey = "Name";
    public const string DescriptionAttributeKey = "Description";

    public const string StyleKey = "KmlStyleKey";
    public const string StyleId = "KmlStyleId";
    public const string StyleUrl = "KmlStyleUrl";
    public const string StyleIsMap = "KmlStyleIsMap";
    public const string StyleMetadata = "__KmlStyleMetadata";
    public const string RegionMetadata = "__KmlRegionMetadata";
    public const string IconHref = "KmlIconHref";
    public const string IconScale = "KmlIconScale";

    public static string? GetStyleKey(this Feature<Point> feature)
    {
        if (feature?.Attributes == null)
            return null;

        if (feature.Attributes.TryGetValue(StyleKey, out var keyObj) && keyObj != null)
            return keyObj.ToString();

        if (feature.Attributes.TryGetValue(StyleId, out var idObj) && idObj != null)
            return idObj.ToString();

        if (feature.Attributes.TryGetValue(StyleUrl, out var urlObj) && urlObj != null)
            return urlObj.ToString();

        return null;
    }
}
