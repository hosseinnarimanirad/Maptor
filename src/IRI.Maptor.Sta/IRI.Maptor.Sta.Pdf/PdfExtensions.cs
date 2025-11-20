using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Pdf;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Extensions;

/// <summary>
/// Extension methods for converting Geometry and Feature to PDF format
/// </summary>
public static class PdfExtensions
{
    /// <summary>
    /// Converts Geometry to PDF bytes
    /// </summary>
    /// <param name="geometry">The geometry to convert</param>
    /// <param name="options">PDF options for styling and formatting</param>
    /// <returns>PDF file content as byte array</returns>
    public static byte[] ToPdf(this Geometry<Point> geometry, PdfOptions? options = null)
    {
        if (geometry == null)
            throw new ArgumentNullException(nameof(geometry));

        return PdfWriter.Write(geometry, options);
    }

    /// <summary>
    /// Saves Geometry to PDF file
    /// </summary>
    /// <param name="geometry">The geometry to save</param>
    /// <param name="filePath">The path to save the PDF file</param>
    /// <param name="options">PDF options for styling and formatting</param>
    /// <returns>The path to the saved file</returns>
    public static string SaveAsPdf(this Geometry<Point> geometry, string filePath, PdfOptions? options = null)
    {
        if (geometry == null)
            throw new ArgumentNullException(nameof(geometry));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        return PdfWriter.WriteToFile(geometry, filePath, options);
    }

    /// <summary>
    /// Converts Feature to PDF bytes
    /// </summary>
    /// <param name="feature">The feature to convert</param>
    /// <param name="options">PDF options for styling and formatting</param>
    /// <returns>PDF file content as byte array</returns>
    public static byte[] ToPdf(this Feature<Point> feature, PdfOptions? options = null)
    {
        if (feature == null)
            throw new ArgumentNullException(nameof(feature));

        return PdfWriter.Write(feature, options);
    }

    /// <summary>
    /// Saves Feature to PDF file
    /// </summary>
    /// <param name="feature">The feature to save</param>
    /// <param name="filePath">The path to save the PDF file</param>
    /// <param name="options">PDF options for styling and formatting</param>
    /// <returns>The path to the saved file</returns>
    public static string SaveAsPdf(this Feature<Point> feature, string filePath, PdfOptions? options = null)
    {
        if (feature == null)
            throw new ArgumentNullException(nameof(feature));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        return PdfWriter.WriteToFile(feature, filePath, options);
    }
}