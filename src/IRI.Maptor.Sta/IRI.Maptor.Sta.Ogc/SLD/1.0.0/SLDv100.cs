using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace IRI.Maptor.Sta.Ogc.SLD;
 

[XmlRoot("StyledLayerDescriptor", Namespace = SldNamespaces.SLD, IsNullable = false)]
public class StyledLayerDescriptor
{
    [XmlElement("Name")]
    public string Name { get; set; }

    [XmlElement("Title")]
    public string Title { get; set; }

    [XmlElement("Abstract")]
    public string Abstract { get; set; }

    [XmlElement("NamedLayer")]
    public List<NamedLayer> NamedLayers { get; set; } = new();

    [XmlElement("UserLayer")]
    public List<UserLayer> UserLayers { get; set; } = new();

    [XmlAttribute("version")]
    public string Version { get; set; } = "1.0.0";

    // schemaLocation attribute (xsi)
    [XmlAttribute("schemaLocation", Namespace = SldNamespaces.XSI)]
    public string SchemaLocation { get; set; } = "http://www.opengis.net/sld StyledLayerDescriptor.xsd";

    // Namespace declarations for proper serialization
    [XmlNamespaceDeclarations]
    public XmlSerializerNamespaces Xmlns { get; set; }

    public StyledLayerDescriptor()
    {
        Xmlns = new XmlSerializerNamespaces();
        Xmlns.Add("", SldNamespaces.SLD);
        Xmlns.Add("ogc", SldNamespaces.OGC);
        Xmlns.Add("xlink", SldNamespaces.XLINK);
        Xmlns.Add("xsi", SldNamespaces.XSI);
    }

    // Optional lightweight validation
    public IEnumerable<string> Validate()
    {
        if ((NamedLayers?.Count ?? 0) == 0 && (UserLayers?.Count ?? 0) == 0)
            yield return "At least one NamedLayer or UserLayer is recommended.";
    }
}

public class NamedLayer
{
    [XmlElement("Name")]
    public string Name { get; set; }

    [XmlElement("LayerFeatureConstraints")]
    public LayerFeatureConstraints LayerFeatureConstraints { get; set; }

    [XmlElement("NamedStyle")]
    public List<NamedStyle> NamedStyles { get; set; } = new();

    [XmlElement("UserStyle")]
    public List<UserStyle> UserStyles { get; set; } = new();
}

public class NamedStyle
{
    [XmlElement("Name")]
    public string Name { get; set; }
}

public class UserLayer
{
    [XmlElement("Name")]
    public string Name { get; set; }

    [XmlElement("RemoteOWS")]
    public RemoteOWS RemoteOWS { get; set; }

    [XmlElement("LayerFeatureConstraints")]
    public LayerFeatureConstraints LayerFeatureConstraints { get; set; }

    [XmlElement("UserStyle")]
    public List<UserStyle> UserStyles { get; set; } = new();
}

public class RemoteOWS
{
    [XmlElement("Service")]
    public ServiceType Service { get; set; }

    [XmlElement("OnlineResource")]
    public OnlineResource OnlineResource { get; set; }
}

public enum ServiceType
{
    WFS,
    WCS
}

public class OnlineResource
{
    [XmlAttribute("type", Namespace = SldNamespaces.XLINK)]
    public string Type { get; set; } = "simple";

    [XmlAttribute("href", Namespace = SldNamespaces.XLINK)]
    public string Href { get; set; }
}

public class LayerFeatureConstraints
{
    [XmlElement("FeatureTypeConstraint")]
    public List<FeatureTypeConstraint> FeatureTypeConstraints { get; set; } = new();
}

public class FeatureTypeConstraint
{
    [XmlElement("FeatureTypeName")]
    public string FeatureTypeName { get; set; }

    [XmlElement("Filter", Namespace = SldNamespaces.OGC)]
    public OgcFilter Filter { get; set; }

    [XmlElement("Extent")]
    public List<Extent> Extents { get; set; } = new();
}

public class Extent
{
    [XmlElement("Name")]
    public string Name { get; set; }

    [XmlElement("Value")]
    public string Value { get; set; }
}

// UserStyle and related classes
public class UserStyle
{
    [XmlElement("Name")]
    public string Name { get; set; }

    [XmlElement("Title")]
    public string Title { get; set; }

    [XmlElement("Abstract")]
    public string Abstract { get; set; }

    [XmlElement("IsDefault")]
    public bool? IsDefault { get; set; }

    [XmlElement("FeatureTypeStyle")]
    public List<FeatureTypeStyle> FeatureTypeStyles { get; set; } = new();

    public bool ShouldSerializeIsDefault() => IsDefault.HasValue;
}

public class FeatureTypeStyle
{
    [XmlElement("Name")]
    public string Name { get; set; }

    [XmlElement("Title")]
    public string Title { get; set; }

    [XmlElement("Abstract")]
    public string Abstract { get; set; }

    [XmlElement("FeatureTypeName")]
    public string FeatureTypeName { get; set; }

    [XmlElement("SemanticTypeIdentifier")]
    public List<string> SemanticTypeIdentifiers { get; set; } = new();

    [XmlElement("Rule")]
    public List<Rule> Rules { get; set; } = new();
}



public class LegendGraphic
{
    [XmlElement("Graphic")]
    public Graphic Graphic { get; set; }
}

public class ElseFilter { }

// Symbolizer hierarchy
[XmlInclude(typeof(LineSymbolizer))]
[XmlInclude(typeof(PolygonSymbolizer))]
[XmlInclude(typeof(PointSymbolizer))]
[XmlInclude(typeof(TextSymbolizer))]
[XmlInclude(typeof(RasterSymbolizer))]
public abstract class Symbolizer
{
    [XmlElement("Geometry")]
    public Geometry Geometry { get; set; }
}

public class LineSymbolizer : Symbolizer
{
    [XmlElement("Stroke")]
    public Stroke Stroke { get; set; }
}

public class PolygonSymbolizer : Symbolizer
{
    [XmlElement("Fill")]
    public Fill Fill { get; set; }

    [XmlElement("Stroke")]
    public Stroke Stroke { get; set; }
}

public class PointSymbolizer : Symbolizer
{
    [XmlElement("Graphic")]
    public Graphic Graphic { get; set; }
}

public class TextSymbolizer : Symbolizer
{
    [XmlElement("Label")]
    public Label Label { get; set; }

    [XmlElement("Font")]
    public Font Font { get; set; }

    [XmlElement("LabelPlacement")]
    public LabelPlacement LabelPlacement { get; set; }

    [XmlElement("Halo")]
    public Halo Halo { get; set; }

    [XmlElement("Fill")]
    public Fill Fill { get; set; }

    // Many engines accept VendorOption in TextSymbolizer
    [XmlElement("VendorOption")]
    public List<VendorOption> VendorOptions { get; set; } = new();
}

public class VendorOption
{
    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlText]
    public string Value { get; set; }
}

public class RasterSymbolizer : Symbolizer
{
    [XmlElement("Opacity")]
    public double? Opacity { get; set; }

    [XmlElement("ChannelSelection")]
    public ChannelSelection ChannelSelection { get; set; }

    [XmlElement("OverlapBehavior")]
    public OverlapBehavior OverlapBehavior { get; set; }

    [XmlElement("ColorMap")]
    public ColorMap ColorMap { get; set; }

    [XmlElement("ContrastEnhancement")]
    public ContrastEnhancement ContrastEnhancement { get; set; }

    [XmlElement("ShadedRelief")]
    public ShadedRelief ShadedRelief { get; set; }

    [XmlElement("ImageOutline")]
    public ImageOutline ImageOutline { get; set; }

    public bool ShouldSerializeOpacity() => Opacity.HasValue;
}

public class Label
{
    [XmlElement("PropertyName", Namespace = SldNamespaces.OGC)]
    public string PropertyName { get; set; }
}

// Supporting classes for symbolizers
public class Geometry
{
    [XmlElement("PropertyName", Namespace = SldNamespaces.OGC)]
    public string PropertyName { get; set; }
}







public enum WellKnownMark
{
    square, circle, triangle, star, cross, x
}

[XmlInclude(typeof(ExternalGraphic))]
[XmlInclude(typeof(Mark))]
public class Graphic
{
    [XmlElement("ExternalGraphic")]
    public List<ExternalGraphic> ExternalGraphics { get; set; } = new();

    [XmlElement("Mark")]
    public List<Mark> Marks { get; set; } = new();

    [XmlElement("Opacity")]
    public double? Opacity { get; set; }

    /// <summary>
    /// Specifies the size of the symbol, in pixels. When used with an 
    /// image file, this specifies the height of the image, with the width 
    /// being scaled accordingly. if omitted the native symbol size is used.
    /// </summary>
    [XmlElement("Size")]
    public int? Size { get; set; }

    [XmlElement("Rotation")]
    public double? Rotation { get; set; }

    public bool ShouldSerializeOpacity() => Opacity.HasValue;
    public bool ShouldSerializeRotation() => Rotation.HasValue;
    public bool ShouldSerializeSize() => Size.HasValue;
}

public class GraphicFill
{
    [XmlElement("Graphic")]
    public Graphic Graphic { get; set; }
}

public class GraphicStroke
{
    [XmlElement("Graphic")]
    public Graphic Graphic { get; set; }
}

public class ExternalGraphic
{
    [XmlElement("OnlineResource")]
    public OnlineResource OnlineResource { get; set; }

    [XmlElement("Format")]
    public string Format { get; set; }
}



public class ParameterValueType
{
    [XmlElement("expression", Namespace = SldNamespaces.OGC)]
    public List<string> Expressions { get; set; } = new();

    [XmlText]
    public string Value { get; set; }
}

// Text symbolizer supporting classes


public class LabelPlacement
{
    [XmlElement("PointPlacement")]
    public PointPlacement PointPlacement { get; set; }

    [XmlElement("LinePlacement")]
    public LinePlacement LinePlacement { get; set; }
}

public class PointPlacement
{
    [XmlElement("AnchorPoint")]
    public AnchorPoint AnchorPoint { get; set; }

    [XmlElement("Displacement")]
    public Displacement Displacement { get; set; }

    [XmlElement("Rotation")]
    public ParameterValueType Rotation { get; set; }
}

public class AnchorPoint
{
    [XmlElement("AnchorPointX")]
    public ParameterValueType AnchorPointX { get; set; }

    [XmlElement("AnchorPointY")]
    public ParameterValueType AnchorPointY { get; set; }
}

public class Displacement
{
    [XmlElement("DisplacementX")]
    public ParameterValueType DisplacementX { get; set; }

    [XmlElement("DisplacementY")]
    public ParameterValueType DisplacementY { get; set; }
}

public class LinePlacement
{
    [XmlElement("PerpendicularOffset")]
    public ParameterValueType PerpendicularOffset { get; set; }
}

public class Halo
{
    [XmlElement("Radius")]
    public ParameterValueType Radius { get; set; }

    [XmlElement("Fill")]
    public Fill Fill { get; set; }
}

// Raster symbolizer supporting classes
public class ChannelSelection
{
    [XmlElement("RedChannel")]
    public SelectedChannelType RedChannel { get; set; }

    [XmlElement("GreenChannel")]
    public SelectedChannelType GreenChannel { get; set; }

    [XmlElement("BlueChannel")]
    public SelectedChannelType BlueChannel { get; set; }

    [XmlElement("GrayChannel")]
    public SelectedChannelType GrayChannel { get; set; }
}

public class SelectedChannelType
{
    [XmlElement("SourceChannelName")]
    public string SourceChannelName { get; set; }

    [XmlElement("ContrastEnhancement")]
    public ContrastEnhancement ContrastEnhancement { get; set; }
}

public class OverlapBehavior
{
    [XmlElement("LATEST_ON_TOP")]
    public LatestOnTop LatestOnTop { get; set; }

    [XmlElement("EARLIEST_ON_TOP")]
    public EarliestOnTop EarliestOnTop { get; set; }

    [XmlElement("AVERAGE")]
    public Average Average { get; set; }

    [XmlElement("RANDOM")]
    public Random Random { get; set; }
}

public class LatestOnTop { }
public class EarliestOnTop { }
public class Average { }
public class Random { }

public class ColorMap
{
    [XmlElement("ColorMapEntry")]
    public List<ColorMapEntry> ColorMapEntries { get; set; } = new();
}

public class ColorMapEntry
{
    [XmlAttribute("color")]
    public string Color { get; set; }

    // double? cannot be used because complexTypes (such as Nullable<T>)
    // are not supported as XmlAttribute
    [XmlAttribute("opacity")]
    public string Opacity { get; set; }

    // double? cannot be used because complexTypes (such as Nullable<T>)
    // are not supported as XmlAttribute
    [XmlAttribute("quantity")]
    public string Quantity { get; set; }

    [XmlAttribute("label")]
    public string Label { get; set; }

    //public bool ShouldSerializeOpacity() => Opacity.HasValue;
    //public bool ShouldSerializeQuantity() => Quantity.HasValue;
}

public class ContrastEnhancement
{
    [XmlElement("Normalize")]
    public Normalize Normalize { get; set; }

    [XmlElement("Histogram")]
    public Histogram Histogram { get; set; }

    [XmlElement("GammaValue")]
    public double? GammaValue { get; set; }

    public bool ShouldSerializeGammaValue() => GammaValue.HasValue;
}

public class Normalize { }
public class Histogram { }

public class ShadedRelief
{
    [XmlElement("BrightnessOnly")]
    public bool? BrightnessOnly { get; set; }

    [XmlElement("ReliefFactor")]
    public double? ReliefFactor { get; set; }

    public bool ShouldSerializeBrightnessOnly() => BrightnessOnly.HasValue;
    public bool ShouldSerializeReliefFactor() => ReliefFactor.HasValue;
}

public class ImageOutline
{
    [XmlElement("LineSymbolizer")]
    public LineSymbolizer LineSymbolizer { get; set; }

    [XmlElement("PolygonSymbolizer")]
    public PolygonSymbolizer PolygonSymbolizer { get; set; }
}

//// OGC Filter types (expanded scaffolding)
//public class FilterType
//{
//    [XmlElement("PropertyIsEqualTo", Namespace = SldNamespaces.OGC)]
//    public PropertyFilterBase PropertyIsEqualTo { get; set; }

//    [XmlElement("PropertyIsLessThan", Namespace = SldNamespaces.OGC)]
//    public PropertyFilterBase PropertyIsLessThan { get; set; }

//    [XmlElement("PropertyIsNotEqualTo", Namespace = SldNamespaces.OGC)]
//    public PropertyFilterBase PropertyIsNotEqualTo { get; set; }

//    [XmlElement("PropertyIsLessThanOrEqualTo", Namespace = SldNamespaces.OGC)]
//    public PropertyFilterBase PropertyIsLessThanOrEqualTo { get; set; }

//    [XmlElement("PropertyIsGreaterThan", Namespace = SldNamespaces.OGC)]
//    public PropertyFilterBase PropertyIsGreaterThan { get; set; }

//    [XmlElement("PropertyIsGreaterThanOrEqualTo", Namespace = SldNamespaces.OGC)]
//    public PropertyFilterBase PropertyIsGreaterThanOrEqualTo { get; set; }

//    // Additional common filters
//    [XmlElement("PropertyIsLike", Namespace = SldNamespaces.OGC)]
//    public PropertyIsLikeFilter PropertyIsLike { get; set; }

//    [XmlElement("PropertyIsNull", Namespace = SldNamespaces.OGC)]
//    public PropertyIsNullFilter PropertyIsNull { get; set; }

//    [XmlElement("PropertyIsBetween", Namespace = SldNamespaces.OGC)]
//    public PropertyIsBetweenFilter PropertyIsBetween { get; set; }
//}

//public class PropertyFilterBase
//{
//    [XmlElement("PropertyName", Namespace = SldNamespaces.OGC)]
//    public string PropertyName { get; set; }

//    [XmlElement("Literal", Namespace = SldNamespaces.OGC)]
//    public string Literal { get; set; }
//}

//public class PropertyIsLikeFilter
//{
//    [XmlAttribute("wildCard")]
//    public string WildCard { get; set; } = "*";

//    [XmlAttribute("singleChar")]
//    public string SingleChar { get; set; } = "?";

//    [XmlAttribute("escape")]
//    public string Escape { get; set; } = "\\";

//    [XmlElement("PropertyName", Namespace = SldNamespaces.OGC)]
//    public string PropertyName { get; set; }

//    [XmlElement("Literal", Namespace = SldNamespaces.OGC)]
//    public string Literal { get; set; }
//}

//public class PropertyIsNullFilter
//{
//    [XmlElement("PropertyName", Namespace = SldNamespaces.OGC)]
//    public string PropertyName { get; set; }
//}

//public class PropertyIsBetweenFilter
//{
//    [XmlElement("PropertyName", Namespace = SldNamespaces.OGC)]
//    public string PropertyName { get; set; }

//    [XmlElement("LowerBoundary", Namespace = SldNamespaces.OGC)]
//    public BoundaryValue LowerBoundary { get; set; }

//    [XmlElement("UpperBoundary", Namespace = SldNamespaces.OGC)]
//    public BoundaryValue UpperBoundary { get; set; }
//}

//public class BoundaryValue
//{
//    [XmlElement("Literal", Namespace = SldNamespaces.OGC)]
//    public string Literal { get; set; }
//}
