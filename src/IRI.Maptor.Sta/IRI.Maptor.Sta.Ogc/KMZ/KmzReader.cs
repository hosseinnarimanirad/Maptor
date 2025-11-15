using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using IRI.Maptor.Ket.KmlFormat;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Ket.KmlFormat;

/// <summary>
/// KMZ Reader for parsing KMZ files (compressed KML archives) and extracting geometries
/// KMZ files are ZIP archives containing KML files and optionally embedded resources
/// Supports KML 2.2 specification
/// </summary>
public static class KmzReader
{
    private const string DefaultKmlFileName = "doc.kml";

    #region Public Methods

    /// <summary>
    /// Reads and parses a KMZ file from the specified path
    /// </summary>
    /// <param name="kmzFilePath">Path to the KMZ file</param>
    /// <param name="targetSrid">Target SRID for the geometries (default: 4326 - WGS84)</param>
    /// <returns>List of geometries extracted from the KMZ file</returns>
    public static List<Geometry<Point>> ReadFromFile(string kmzFilePath, int targetSrid = 4326)
    {
        if (!File.Exists(kmzFilePath))
            throw new FileNotFoundException($"KMZ file not found: {kmzFilePath}", kmzFilePath);

        try
        {
            using (var archive = ZipFile.OpenRead(kmzFilePath))
            {
                var kmlContent = ExtractKmlContent(archive);
                if (string.IsNullOrWhiteSpace(kmlContent))
                    throw new InvalidOperationException($"No KML file found in KMZ archive: {kmzFilePath}");

                return KmlReader.Parse(kmlContent, targetSrid);
            }
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidOperationException($"Invalid KMZ file format: {kmzFilePath}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse KMZ file: {kmzFilePath}", ex);
        }
    }

    /// <summary>
    /// Reads and parses a KMZ file asynchronously
    /// </summary>
    /// <param name="kmzFilePath">Path to the KMZ file</param>
    /// <param name="targetSrid">Target SRID for the geometries (default: 4326 - WGS84)</param>
    /// <returns>List of geometries extracted from the KMZ file</returns>
    public static async Task<List<Geometry<Point>>> ReadFromFileAsync(string kmzFilePath, int targetSrid = 4326)
    {
        if (!File.Exists(kmzFilePath))
            throw new FileNotFoundException($"KMZ file not found: {kmzFilePath}", kmzFilePath);

        return await Task.Run(() => ReadFromFile(kmzFilePath, targetSrid));
    }

    /// <summary>
    /// Parses a KMZ stream and extracts geometries
    /// </summary>
    /// <param name="kmzStream">KMZ content as stream</param>
    /// <param name="targetSrid">Target SRID for the geometries (default: 4326 - WGS84)</param>
    /// <returns>List of geometries extracted from the KMZ stream</returns>
    public static List<Geometry<Point>> Parse(Stream kmzStream, int targetSrid = 4326)
    {
        if (kmzStream == null)
            throw new ArgumentNullException(nameof(kmzStream));

        if (!kmzStream.CanRead)
            throw new ArgumentException("Stream must be readable", nameof(kmzStream));

        try
        {
            using (var archive = new ZipArchive(kmzStream, ZipArchiveMode.Read, leaveOpen: false))
            {
                var kmlContent = ExtractKmlContent(archive);
                if (string.IsNullOrWhiteSpace(kmlContent))
                    throw new InvalidOperationException("No KML file found in KMZ archive");

                return KmlReader.Parse(kmlContent, targetSrid);
            }
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidOperationException("Invalid KMZ file format", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to parse KMZ stream", ex);
        }
    }

    /// <summary>
    /// Reads KMZ with feature attributes (ExtendedData)
    /// </summary>
    /// <param name="kmzFilePath">Path to the KMZ file</param>
    /// <param name="targetSrid">Target SRID for the geometries (default: 4326 - WGS84)</param>
    /// <returns>List of features with geometries and attributes</returns>
    public static List<KmlFeature> ReadFeaturesFromFile(string kmzFilePath, int targetSrid = 4326)
    {
        if (!File.Exists(kmzFilePath))
            throw new FileNotFoundException($"KMZ file not found: {kmzFilePath}", kmzFilePath);

        try
        {
            using (var archive = ZipFile.OpenRead(kmzFilePath))
            {
                var kmlContent = ExtractKmlContent(archive);
                if (string.IsNullOrWhiteSpace(kmlContent))
                    throw new InvalidOperationException($"No KML file found in KMZ archive: {kmzFilePath}");

                return KmlReader.ParseFeatures(kmlContent, targetSrid);
            }
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidOperationException($"Invalid KMZ file format: {kmzFilePath}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse KMZ file: {kmzFilePath}", ex);
        }
    }

    /// <summary>
    /// Reads KMZ features asynchronously
    /// </summary>
    /// <param name="kmzFilePath">Path to the KMZ file</param>
    /// <param name="targetSrid">Target SRID for the geometries (default: 4326 - WGS84)</param>
    /// <returns>List of features with geometries and attributes</returns>
    public static async Task<List<KmlFeature>> ReadFeaturesFromFileAsync(string kmzFilePath, int targetSrid = 4326)
    {
        if (!File.Exists(kmzFilePath))
            throw new FileNotFoundException($"KMZ file not found: {kmzFilePath}", kmzFilePath);

        return await Task.Run(() => ReadFeaturesFromFile(kmzFilePath, targetSrid));
    }

    /// <summary>
    /// Gets a list of all resource file names in the KMZ archive (excluding the KML file)
    /// </summary>
    /// <param name="kmzFilePath">Path to the KMZ file</param>
    /// <returns>List of resource file names</returns>
    public static List<string> GetResourceFiles(string kmzFilePath)
    {
        if (!File.Exists(kmzFilePath))
            throw new FileNotFoundException($"KMZ file not found: {kmzFilePath}", kmzFilePath);

        try
        {
            using (var archive = ZipFile.OpenRead(kmzFilePath))
            {
                var kmlFileName = FindKmlFileName(archive);
                return archive.Entries
                    .Where(e => !e.FullName.Equals(kmlFileName, StringComparison.OrdinalIgnoreCase))
                    .Select(e => e.FullName)
                    .ToList();
            }
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidOperationException($"Invalid KMZ file format: {kmzFilePath}", ex);
        }
    }

    /// <summary>
    /// Extracts a resource file from the KMZ archive as a byte array
    /// </summary>
    /// <param name="kmzFilePath">Path to the KMZ file</param>
    /// <param name="resourcePath">Path to the resource file within the archive</param>
    /// <returns>Resource file content as byte array, or null if not found</returns>
    public static byte[]? ExtractResource(string kmzFilePath, string resourcePath)
    {
        if (!File.Exists(kmzFilePath))
            throw new FileNotFoundException($"KMZ file not found: {kmzFilePath}", kmzFilePath);

        if (string.IsNullOrWhiteSpace(resourcePath))
            throw new ArgumentException("Resource path cannot be null or empty", nameof(resourcePath));

        try
        {
            using (var archive = ZipFile.OpenRead(kmzFilePath))
            {
                var entry = archive.Entries.FirstOrDefault(
                    e => e.FullName.Equals(resourcePath, StringComparison.OrdinalIgnoreCase));

                if (entry == null)
                    return null;

                using (var stream = entry.Open())
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidOperationException($"Invalid KMZ file format: {kmzFilePath}", ex);
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Extracts KML content from a ZIP archive
    /// </summary>
    private static string ExtractKmlContent(ZipArchive archive)
    {
        var kmlFileName = FindKmlFileName(archive);
        if (string.IsNullOrWhiteSpace(kmlFileName))
            return null;

        var entry = archive.Entries.FirstOrDefault(
            e => e.FullName.Equals(kmlFileName, StringComparison.OrdinalIgnoreCase));

        if (entry == null)
            return null;

        using (var stream = entry.Open())
        using (var reader = new StreamReader(stream))
        {
            return reader.ReadToEnd();
        }
    }

    /// <summary>
    /// Finds the KML file name in the archive
    /// Prefers "doc.kml" if present, otherwise returns the first .kml file found
    /// </summary>
    private static string FindKmlFileName(ZipArchive archive)
    {
        // First, try to find "doc.kml" (case-insensitive)
        var docKml = archive.Entries.FirstOrDefault(
            e => e.FullName.Equals(DefaultKmlFileName, StringComparison.OrdinalIgnoreCase));

        if (docKml != null)
            return docKml.FullName;

        // Otherwise, find the first .kml file
        var kmlFile = archive.Entries.FirstOrDefault(
            e => e.FullName.EndsWith(".kml", StringComparison.OrdinalIgnoreCase));

        return kmlFile?.FullName;
    }

    #endregion
}



