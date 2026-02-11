using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Sta.Common.Helpers;

public static class IOHelper
{
    public const char CsvDelimiterChar = ',';

    public const char TsvDelimiterChar = '\t';

    /// <summary>
    /// Depth-first recursive delete, with handling for descendant 
    /// directories open in Windows Explorer.
    /// </summary>
    public static void DeleteDirectory(string path)
    {
        foreach (string directory in Directory.GetDirectories(path))
        {
            DeleteDirectory(directory);
        }

        try
        {
            Directory.Delete(path, true);
        }
        catch (IOException)
        {
            Directory.Delete(path, true);
        }
        catch (UnauthorizedAccessException)
        {
            Directory.Delete(path, true);
        }
    }

    public static bool TryCreateDirectory(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string GetExtensionWithoutDot(string path)
    {
        return Path.GetExtension(path).Replace(".", "");
    }

    public static List<string[]> ReadAllDelimitedFile(string fileName, params char[] delimited)
    {
        if (!File.Exists(fileName))
        {
            return new List<string[]>();
        }

        var lines = File.ReadAllLines(fileName);

        var result = new List<string[]>();

        foreach (var line in lines)
        {
            result.Add(line.Split(delimited));
        }

        return result;
    }

    public static async Task<List<string[]>> ReadAllDelimitedFileAsync(string fileName, params char[] delimited)
    {
        if (!File.Exists(fileName))
        {
            return new List<string[]>();
        }

        var lines = await File.ReadAllLinesAsync(fileName);

        var result = new List<string[]>();

        foreach (var line in lines)
        {
            result.Add(line.Split(delimited));
        }

        return result;
    }

    public static List<Point> ReadAllPoints(string fileName, params char[] delimited)
    {
        if (!File.Exists(fileName))
        {
            return new List<Point>();
        }

        var lines = File.ReadAllLines(fileName);

        var result = new List<Point>();

        foreach (var line in lines)
        {
            var splitted = line.Split(delimited);

            var x = double.Parse(splitted[0]);

            var y = double.Parse(splitted[1]);

            result.Add(new Point(x, y));
        }

        return result;
    }

    public static List<string[]> ReadAsCsv(string fileName)
    {
        return ReadAllDelimitedFile(fileName, CsvDelimiterChar);
    }

    public static List<string[]> ReadAsTsv(string fileName)
    {
        return ReadAllDelimitedFile(fileName, TsvDelimiterChar);
    }

}
