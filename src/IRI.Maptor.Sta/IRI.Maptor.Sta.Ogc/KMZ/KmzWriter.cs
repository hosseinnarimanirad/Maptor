using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using IRI.Maptor.Sta.KmlFormat;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Abstractions;

namespace IRI.Maptor.Sta.KmlFormat;

/// <summary>
/// KMZ Writer for exporting geometries to KMZ format (compressed KML archives)
/// KMZ files are ZIP archives containing KML files and optionally embedded resources
/// Supports KML 2.2 specification
/// </summary>
public static class KmzWriter
{
    private const string DefaultKmlFileName = "doc.kml";

    #region Public Methods - Write to File

    /// <summary>
    /// Writes a single geometry to a KMZ file
    /// </summary>
    /// <param name="geometry">Geometry to write</param>
    /// <param name="kmzFilePath">Path to the output KMZ file</param>
    /// <param name="name">Feature name</param>
    /// <param name="description">Feature description</param>
    public static void WriteToFile(
        Geometry<Point> geometry,
        string kmzFilePath,
        string? name = null,
        string? description = null)
    {
        var kmlString = KmlWriter.ToKml(geometry, name, description);

        WriteKmlToKmz(kmzFilePath, kmlString, DefaultKmlFileName);
    }

    /// <summary>
    /// Writes a single geometry with Z values to a KMZ file
    /// </summary>
    /// <param name="geometry">Geometry with Z values to write</param>
    /// <param name="kmzFilePath">Path to the output KMZ file</param>
    /// <param name="name">Feature name</param>
    /// <param name="description">Feature description</param>
    public static void WriteToFile(
        Geometry<PointZ> geometry,
        string kmzFilePath,
        string? name = null,
        string? description = null)
    {
        var kmlString = KmlWriter.ToKml(geometry, name, description);
        WriteKmlToKmz(kmzFilePath, kmlString, DefaultKmlFileName);
    }

    /// <summary>
    /// Writes a single geometry (2D or 3D) to a KMZ file
    /// </summary>
    /// <param name="geometry">Geometry to write (supports both 2D and 3D)</param>
    /// <param name="kmzFilePath">Path to the output KMZ file</param>
    /// <param name="name">Feature name</param>
    /// <param name="description">Feature description</param>
    public static void WriteToFile(
        IGeometry geometry,
        string kmzFilePath,
        string? name = null,
        string? description = null)
    {
        var kmlString = KmlWriter.ToKml(geometry, name, description);
        WriteKmlToKmz(kmzFilePath, kmlString, DefaultKmlFileName);
    }

    /// <summary>
    /// Writes multiple geometries to a KMZ file
    /// </summary>
    /// <param name="geometries">List of geometries to write</param>
    /// <param name="kmzFilePath">Path to the output KMZ file</param>
    /// <param name="documentName">Document name</param>
    /// <param name="projectToGeodeticFunc">Optional function to project coordinates to WGS84</param>
    //public static void WriteToFile(
    //    List<Geometry<Point>> geometries,
    //    string kmzFilePath,
    //    string? documentName = null,
    //    Func<Point, Point>? projectToGeodeticFunc = null)
    //{
    //    var kmlString = KmlWriter.ToKml(geometries, documentName, projectToGeodeticFunc);
    //    WriteKmlToKmz(kmzFilePath, kmlString, DefaultKmlFileName);
    //}

    /// <summary>
    /// Writes multiple geometries with Z values to a KMZ file
    /// </summary>
    /// <param name="geometries">List of geometries with Z values to write</param>
    /// <param name="kmzFilePath">Path to the output KMZ file</param>
    /// <param name="documentName">Document name</param>
    /// <param name="projectToGeodeticFunc">Optional function to project coordinates to WGS84</param>
    //public static void WriteToFile(
    //    List<Geometry<PointZ>> geometries,
    //    string kmzFilePath,
    //    string? documentName = null,
    //    Func<Point, Point>? projectToGeodeticFunc = null)
    //{
    //    var kmlString = KmlWriter.ToKml(geometries, documentName, projectToGeodeticFunc);
    //    WriteKmlToKmz(kmzFilePath, kmlString, DefaultKmlFileName);
    //}

    /// <summary>
    /// Writes multiple geometries (2D or 3D) to a KMZ file
    /// </summary>
    /// <param name="geometries">List of geometries to write (supports both 2D and 3D)</param>
    /// <param name="kmzFilePath">Path to the output KMZ file</param>
    /// <param name="documentName">Document name</param>
    /// <param name="projectToGeodeticFunc">Optional function to project coordinates to WGS84</param>
    //public static void WriteToFile(
    //    List<IGeometry> geometries,
    //    string kmzFilePath,
    //    string? documentName = null,
    //    Func<Point, Point>? projectToGeodeticFunc = null)
    //{
    //    var kmlString = KmlWriter.ToKml(geometries, documentName, projectToGeodeticFunc);
    //    WriteKmlToKmz(kmzFilePath, kmlString, DefaultKmlFileName);
    //}

    /// <summary>
    /// Writes features with attributes to a KMZ file
    /// </summary>
    /// <param name="features">List of features to write</param>
    /// <param name="kmzFilePath">Path to the output KMZ file</param>
    /// <param name="documentName">Document name</param>
    /// <param name="projectToGeodeticFunc">Optional function to project coordinates to WGS84</param>
    //public static void WriteToFile(
    //    List<KmlFeature> features,
    //    string kmzFilePath,
    //    string? documentName = null,
    //    Func<Point, Point>? projectToGeodeticFunc = null)
    //{
    //    var kmlString = KmlWriter.ToKml(features, documentName, projectToGeodeticFunc);
    //    WriteKmlToKmz(kmzFilePath, kmlString, DefaultKmlFileName);
    //}

    /// <summary>
    /// Writes geometries to a KMZ file asynchronously
    /// </summary>
    /// <param name="geometries">List of geometries to write</param>
    /// <param name="kmzFilePath">Path to the output KMZ file</param>
    /// <param name="documentName">Document name</param>
    public static async Task WriteToFileAsync(
        List<Geometry<Point>> geometries,
        string kmzFilePath,
        string? documentName = null)
    {
        var kmlString = KmlWriter.ToKml(geometries, documentName);
        await WriteKmlToKmzAsync(kmzFilePath, kmlString, DefaultKmlFileName);
    }

    /// <summary>
    /// Writes geometries with Z values to a KMZ file asynchronously
    /// </summary>
    /// <param name="geometries">List of geometries with Z values to write</param>
    /// <param name="kmzFilePath">Path to the output KMZ file</param>
    /// <param name="documentName">Document name</param>
    public static async Task WriteToFileAsync(
        List<Geometry<PointZ>> geometries,
        string kmzFilePath,
        string? documentName = null)
    {
        var kmlString = KmlWriter.ToKml(geometries, documentName);
        await WriteKmlToKmzAsync(kmzFilePath, kmlString, DefaultKmlFileName);
    }

    /// <summary>
    /// Writes geometries (2D or 3D) to a KMZ file asynchronously
    /// </summary>
    /// <param name="geometries">List of geometries to write (supports both 2D and 3D)</param>
    /// <param name="kmzFilePath">Path to the output KMZ file</param>
    /// <param name="documentName">Document name</param>
    public static async Task WriteToFileAsync(
        List<IGeometry> geometries,
        string kmzFilePath,
        string? documentName = null)
    {
        var kmlString = KmlWriter.ToKml(geometries, documentName);
        await WriteKmlToKmzAsync(kmzFilePath, kmlString, DefaultKmlFileName);
    }

    /// <summary>
    /// Writes features to a KMZ file asynchronously
    /// </summary>
    /// <param name="features">List of features to write</param>
    /// <param name="kmzFilePath">Path to the output KMZ file</param>
    /// <param name="documentName">Document name</param>
    public static async Task WriteToFileAsync(
        List<KmlFeature> features,
        string kmzFilePath,
        string? documentName = null)
    {
        var kmlString = KmlWriter.ToKml(features, documentName);
        await WriteKmlToKmzAsync(kmzFilePath, kmlString, DefaultKmlFileName);
    }

    #endregion

    #region Public Methods - Resource Management

    /// <summary>
    /// Adds a resource file to an existing KMZ archive
    /// </summary>
    /// <param name="kmzFilePath">Path to the KMZ file</param>
    /// <param name="resourcePath">Path within the archive (e.g., "images/icon.png")</param>
    /// <param name="resourceData">Resource data as byte array</param>
    public static void AddResource(string kmzFilePath, string resourcePath, byte[] resourceData)
    {
        if (string.IsNullOrWhiteSpace(kmzFilePath))
            throw new ArgumentException("KMZ file path cannot be null or empty", nameof(kmzFilePath));

        if (string.IsNullOrWhiteSpace(resourcePath))
            throw new ArgumentException("Resource path cannot be null or empty", nameof(resourcePath));

        if (resourceData == null || resourceData.Length == 0)
            throw new ArgumentException("Resource data cannot be null or empty", nameof(resourceData));

        if (!File.Exists(kmzFilePath))
            throw new FileNotFoundException($"KMZ file not found: {kmzFilePath}", kmzFilePath);

        try
        {
            using (var archive = ZipFile.Open(kmzFilePath, ZipArchiveMode.Update))
            {
                // Remove existing entry if present
                var existingEntry = archive.Entries.FirstOrDefault(
                    e => e.FullName.Equals(resourcePath, StringComparison.OrdinalIgnoreCase));
                existingEntry?.Delete();

                // Add new entry
                var entry = archive.CreateEntry(resourcePath);
                using (var entryStream = entry.Open())
                {
                    entryStream.Write(resourceData, 0, resourceData.Length);
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to add resource to KMZ file: {kmzFilePath}", ex);
        }
    }

    /// <summary>
    /// Adds a resource file to an existing KMZ archive from a stream
    /// </summary>
    /// <param name="kmzFilePath">Path to the KMZ file</param>
    /// <param name="resourcePath">Path within the archive (e.g., "images/icon.png")</param>
    /// <param name="resourceStream">Resource data stream</param>
    public static void AddResource(string kmzFilePath, string resourcePath, Stream resourceStream)
    {
        if (string.IsNullOrWhiteSpace(kmzFilePath))
            throw new ArgumentException("KMZ file path cannot be null or empty", nameof(kmzFilePath));

        if (string.IsNullOrWhiteSpace(resourcePath))
            throw new ArgumentException("Resource path cannot be null or empty", nameof(resourcePath));

        if (resourceStream == null)
            throw new ArgumentNullException(nameof(resourceStream));

        if (!resourceStream.CanRead)
            throw new ArgumentException("Resource stream must be readable", nameof(resourceStream));

        if (!File.Exists(kmzFilePath))
            throw new FileNotFoundException($"KMZ file not found: {kmzFilePath}", kmzFilePath);

        try
        {
            using (var archive = ZipFile.Open(kmzFilePath, ZipArchiveMode.Update))
            {
                // Remove existing entry if present
                var existingEntry = archive.Entries.FirstOrDefault(
                    e => e.FullName.Equals(resourcePath, StringComparison.OrdinalIgnoreCase));
                existingEntry?.Delete();

                // Add new entry
                var entry = archive.CreateEntry(resourcePath);
                using (var entryStream = entry.Open())
                {
                    resourceStream.CopyTo(entryStream);
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to add resource to KMZ file: {kmzFilePath}", ex);
        }
    }

    /// <summary>
    /// Adds a resource file to an existing KMZ archive from a file path
    /// </summary>
    /// <param name="kmzFilePath">Path to the KMZ file</param>
    /// <param name="resourcePath">Path within the archive (e.g., "images/icon.png")</param>
    /// <param name="sourceFilePath">Path to the source file to add</param>
    public static void AddResourceFromFile(string kmzFilePath, string resourcePath, string sourceFilePath)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
            throw new ArgumentException("Source file path cannot be null or empty", nameof(sourceFilePath));

        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException($"Source file not found: {sourceFilePath}", sourceFilePath);

        using (var fileStream = File.OpenRead(sourceFilePath))
        {
            AddResource(kmzFilePath, resourcePath, fileStream);
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Writes KML content to a KMZ file
    /// </summary>
    private static void WriteKmlToKmz(string kmzFilePath, string kmlContent, string kmlFileName)
    {
        if (string.IsNullOrWhiteSpace(kmzFilePath))
            throw new ArgumentException("KMZ file path cannot be null or empty", nameof(kmzFilePath));

        if (string.IsNullOrWhiteSpace(kmlContent))
            throw new ArgumentException("KML content cannot be null or empty", nameof(kmlContent));

        // Ensure directory exists
        var directory = Path.GetDirectoryName(kmzFilePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Create or overwrite the KMZ file
        //using (var archive = ZipFile.Open(kmzFilePath, File.Exists(kmzFilePath) ? ZipArchiveMode.Update : ZipArchiveMode.Create))
        using (var fileStream = new FileStream(kmzFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
        using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Update))
        {
            // Remove existing KML entry if present
            var existingKmlEntry = archive.Entries?.FirstOrDefault(
                e => e.FullName.EndsWith(".kml", StringComparison.OrdinalIgnoreCase));
            existingKmlEntry?.Delete();

            // Add KML entry
            var kmlEntry = archive.CreateEntry(kmlFileName);
            using (var entryStream = kmlEntry.Open())
            using (var writer = new StreamWriter(entryStream))
            {
                writer.Write(kmlContent);
            }
        }
    }

    /// <summary>
    /// Writes KML content to a KMZ file asynchronously
    /// </summary>
    private static async Task WriteKmlToKmzAsync(string kmzFilePath, string kmlContent, string kmlFileName)
    {
        if (string.IsNullOrWhiteSpace(kmzFilePath))
            throw new ArgumentException("KMZ file path cannot be null or empty", nameof(kmzFilePath));

        if (string.IsNullOrWhiteSpace(kmlContent))
            throw new ArgumentException("KML content cannot be null or empty", nameof(kmlContent));

        // Ensure directory exists
        var directory = Path.GetDirectoryName(kmzFilePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Create or overwrite the KMZ file
        //using (var archive = ZipFile.Open(kmzFilePath, File.Exists(kmzFilePath) ? ZipArchiveMode.Update : ZipArchiveMode.Create))
        using (var fileStream = new FileStream(kmzFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
        using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Update))
        {
            // Remove existing KML entry if present
            var existingKmlEntry = archive.Entries.FirstOrDefault(
                e => e.FullName.EndsWith(".kml", StringComparison.OrdinalIgnoreCase));
            existingKmlEntry?.Delete();

            // Add KML entry
            var kmlEntry = archive.CreateEntry(kmlFileName);
            using (var entryStream = kmlEntry.Open())
            using (var writer = new StreamWriter(entryStream))
            {
                await writer.WriteAsync(kmlContent);
            }
        }
    }

    #endregion
}