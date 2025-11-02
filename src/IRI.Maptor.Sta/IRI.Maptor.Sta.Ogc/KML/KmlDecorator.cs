using IRI.Maptor.Ket.KmlFormat.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Ket.KmlFormat;

/// <summary>
/// Helper class for decorating KML placemarks with extended data and styles
/// </summary>
public static class KmlDecorator
{
    #region Extended Data Methods

    /// <summary>
    /// Decorates placemarks with extended data attributes
    /// </summary>
    /// <typeparam name="T">Type of attribute objects</typeparam>
    /// <param name="placemarks">List of placemarks to decorate</param>
    /// <param name="attributes">List of attribute objects</param>
    /// <param name="attributeNames">List of attribute names</param>
    /// <param name="extractFuncs">List of functions to extract attribute values</param>
    /// <returns>KML string with decorated placemarks</returns>
    public static string DecorateWithExtendedData<T>(
        List<PlacemarkType> placemarks,
        List<T> attributes,
        List<string> attributeNames,
        List<Func<T, string>> extractFuncs)
    {
        int numberOfFeatures = placemarks.Count;

        if (numberOfFeatures != attributes.Count)
        {
            throw new ArgumentException(
                $"Number of placemarks ({numberOfFeatures}) must match number of attributes ({attributes.Count})",
                nameof(attributes));
        }

        if (attributeNames.Count != extractFuncs.Count)
        {
            throw new ArgumentException(
                "Number of attribute names must match number of extract functions",
                nameof(extractFuncs));
        }

        var result = new KmlType();
        var document = new DocumentType();

        for (int i = 0; i < numberOfFeatures; i++)
        {
            placemarks[i].id = i.ToString();

            var elements = new SimpleDataType[attributeNames.Count];

            for (int j = 0; j < attributeNames.Count; j++)
            {
                elements[j] = new SimpleDataType()
                {
                    name = attributeNames[j],
                    Value = extractFuncs[j](attributes[i])
                };
            }

            var data = new SchemaDataType[1];
            data[0] = new SchemaDataType() { SimpleData = elements };

            var extendedData = new ExtendedDataType();
            extendedData.SchemaData = data;

            placemarks[i].ExtendedData = extendedData;
            placemarks[i].description = i.ToString();
        }

        document.AbstractFeature = placemarks.OfType<AbstractFeatureType>().ToArray();
        result.KmlObjectExtensionGroup = new AbstractObjectType[] { document };

        return IRI.Maptor.Sta.Common.Helpers.XmlHelper.Parse(result);
    }

    /// <summary>
    /// Adds extended data to a single placemark
    /// </summary>
    /// <param name="placemark">Placemark to decorate</param>
    /// <param name="attributes">Dictionary of attribute name-value pairs</param>
    public static void AddExtendedData(PlacemarkType placemark, Dictionary<string, string> attributes)
    {
        if (placemark == null)
            throw new ArgumentNullException(nameof(placemark));

        if (attributes == null || attributes.Count == 0)
            return;

        var simpleDataList = attributes.Select(kvp =>
            new SimpleDataType
            {
                name = kvp.Key,
                Value = kvp.Value
            }).ToArray();

        var schemaData = new SchemaDataType
        {
            SimpleData = simpleDataList
        };

        placemark.ExtendedData = new ExtendedDataType
        {
            SchemaData = new[] { schemaData }
        };
    }

    #endregion

    #region Style Methods (using KmlStyleBuilder)

    /// <summary>
    /// Decorates placemarks with icon styles
    /// </summary>
    /// <param name="placemarks">List of placemarks to style</param>
    /// <param name="iconHref">URL of the icon image</param>
    /// <param name="scale">Scale factor for the icon (default: 1.0)</param>
    /// <param name="color">Icon color (default: null for default color)</param>
    /// <returns>KML string with styled placemarks</returns>
    public static string DecorateWithIconStyle(
        List<PlacemarkType> placemarks,
        string iconHref,
        double scale = 1.0,
        byte[]? color = null)
    {
        var style = new KmlStyleBuilder()
            .WithIconStyle(iconHref, scale, color)
            .Build();

        foreach (var placemark in placemarks)
        {
            placemark.Styles = new AbstractStyleSelectorType[] { style };
        }

        return SerializePlacemarks(placemarks);
    }

    /// <summary>
    /// Decorates placemarks with line styles
    /// </summary>
    /// <param name="placemarks">List of placemarks to style</param>
    /// <param name="color">Line color</param>
    /// <param name="width">Line width (default: 1.0)</param>
    /// <returns>KML string with styled placemarks</returns>
    public static string DecorateWithLineStyle(
        List<PlacemarkType> placemarks,
        byte[] color,
        double width = 1.0)
    {
        var style = new KmlStyleBuilder()
            .WithLineStyle(color, width)
            .Build();

        foreach (var placemark in placemarks)
        {
            placemark.Styles = new AbstractStyleSelectorType[] { style };
        }

        return SerializePlacemarks(placemarks);
    }

    /// <summary>
    /// Decorates placemarks with polygon styles
    /// </summary>
    /// <param name="placemarks">List of placemarks to style</param>
    /// <param name="fillColor">Fill color</param>
    /// <param name="outlineColor">Outline color</param>
    /// <param name="fill">Whether to fill the polygon</param>
    /// <param name="outline">Whether to draw the outline</param>
    /// <returns>KML string with styled placemarks</returns>
    public static string DecorateWithPolygonStyle(
        List<PlacemarkType> placemarks,
        byte[] fillColor,
        byte[] outlineColor,
        bool fill = true,
        bool outline = true)
    {
        var style = new KmlStyleBuilder()
            .WithPolyStyle(fillColor, fill, outline)
            .WithLineStyle(outlineColor, 1.0)
            .Build();

        foreach (var placemark in placemarks)
        {
            placemark.Styles = new AbstractStyleSelectorType[] { style };
        }

        return SerializePlacemarks(placemarks);
    }

    /// <summary>
    /// Decorates placemarks with custom styles using the style builder
    /// </summary>
    /// <param name="placemarks">List of placemarks to style</param>
    /// <param name="styleBuilder">Configured style builder</param>
    /// <returns>KML string with styled placemarks</returns>
    public static string DecorateWithStyle(
        List<PlacemarkType> placemarks,
        KmlStyleBuilder styleBuilder)
    {
        if (styleBuilder == null)
            throw new ArgumentNullException(nameof(styleBuilder));

        var style = styleBuilder.Build();

        foreach (var placemark in placemarks)
        {
            placemark.Styles = new AbstractStyleSelectorType[] { style };
        }

        return SerializePlacemarks(placemarks);
    }

    /// <summary>
    /// Decorates placemarks with shared styles (referenced by style URL)
    /// </summary>
    /// <param name="placemarks">List of placemarks to style</param>
    /// <param name="sharedStyles">Dictionary of style ID to StyleType</param>
    /// <param name="styleIdSelector">Function to select style ID for each placemark</param>
    /// <returns>KML string with styled placemarks</returns>
    public static string DecorateWithSharedStyles(
        List<PlacemarkType> placemarks,
        Dictionary<string, StyleType> sharedStyles,
        Func<PlacemarkType, int, string> styleIdSelector)
    {
        if (sharedStyles == null || sharedStyles.Count == 0)
            throw new ArgumentException("Shared styles dictionary cannot be null or empty", nameof(sharedStyles));

        var result = new KmlType();
        var document = new DocumentType();

        // Set style IDs on shared styles
        foreach (var kvp in sharedStyles)
        {
            kvp.Value.id = kvp.Key;
        }

        // Add shared styles to document
        document.Styles = sharedStyles.Values.ToArray();

        // Apply style URLs to placemarks
        for (int i = 0; i < placemarks.Count; i++)
        {
            var styleId = styleIdSelector(placemarks[i], i);
            placemarks[i].styleUrl = $"#{styleId}";
        }

        document.AbstractFeature = placemarks.OfType<AbstractFeatureType>().ToArray();
        result.KmlObjectExtensionGroup = new AbstractObjectType[] { document };

        return IRI.Maptor.Sta.Common.Helpers.XmlHelper.Parse(result);
    }

    #endregion

    #region Combined Decoration Methods

    /// <summary>
    /// Decorates placemarks with both extended data and styles
    /// </summary>
    /// <typeparam name="T">Type of attribute objects</typeparam>
    /// <param name="placemarks">List of placemarks to decorate</param>
    /// <param name="attributes">List of attribute objects</param>
    /// <param name="attributeNames">List of attribute names</param>
    /// <param name="extractFuncs">List of functions to extract attribute values</param>
    /// <param name="styleBuilder">Style builder for styling</param>
    /// <returns>KML string with decorated and styled placemarks</returns>
    public static string DecorateWithDataAndStyle<T>(
        List<PlacemarkType> placemarks,
        List<T> attributes,
        List<string> attributeNames,
        List<Func<T, string>> extractFuncs,
        KmlStyleBuilder styleBuilder)
    {
        int numberOfFeatures = placemarks.Count;

        if (numberOfFeatures != attributes.Count)
        {
            throw new ArgumentException(
                $"Number of placemarks ({numberOfFeatures}) must match number of attributes ({attributes.Count})",
                nameof(attributes));
        }

        var style = styleBuilder.Build();

        for (int i = 0; i < numberOfFeatures; i++)
        {
            placemarks[i].id = i.ToString();

            // Add extended data
            var elements = new SimpleDataType[attributeNames.Count];
            for (int j = 0; j < attributeNames.Count; j++)
            {
                elements[j] = new SimpleDataType()
                {
                    name = attributeNames[j],
                    Value = extractFuncs[j](attributes[i])
                };
            }

            var data = new SchemaDataType[1];
            data[0] = new SchemaDataType() { SimpleData = elements };

            placemarks[i].ExtendedData = new ExtendedDataType
            {
                SchemaData = data
            };

            // Add style
            placemarks[i].Styles = new AbstractStyleSelectorType[] { style };
        }

        return SerializePlacemarks(placemarks);
    }

    #endregion

    #region Helper Methods

    private static string SerializePlacemarks(List<PlacemarkType> placemarks)
    {
        var result = new KmlType();
        var document = new DocumentType
        {
            AbstractFeature = placemarks.OfType<AbstractFeatureType>().ToArray()
        };

        result.KmlObjectExtensionGroup = new AbstractObjectType[] { document };

        return IRI.Maptor.Sta.Common.Helpers.XmlHelper.Parse(result);
    }

    #endregion
}
