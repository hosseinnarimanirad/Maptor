using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Sta.Common.Helpers;

public static class IOHelper
{
    public const char CsvDelimiterChar = ',';

    public const char TsvDelimiterChar = '\t';

    //Pipe-separated values
    public const char PsvDelimiterChar = '|';

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

    public static async Task<List<string[]>> ReadAllDelimitedFileAsync(string fileName, bool ignoreEmptyLines, params char[] delimited)
    {
        var result = new List<string[]>();

        if (!File.Exists(fileName))
            return result;

        var lines = await File.ReadAllLinesAsync(fileName);
         
        foreach (var line in lines)
        {
            if (ignoreEmptyLines && string.IsNullOrWhiteSpace(line))
                continue;

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
            var split = line.Split(delimited);

            var x = double.Parse(split[0]);

            var y = double.Parse(split[1]);

            result.Add(new Point(x, y));
        }

        return result;
    }

    /// <summary>
    /// Parses delimited text (e.g. pasted from clipboard) into rows of columns.
    /// </summary>
    public static List<string[]> ReadDelimitedFromText(string text, params char[] delimited)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        return lines.Select(line => line.Split(delimited)).ToList();
    }

    public static Task<List<string[]>> ReadAsCsv(string fileName, bool ignoreEmptyLines)
    {
        return ReadAllDelimitedFileAsync(fileName, ignoreEmptyLines, CsvDelimiterChar);
    }

    public static Task<List<string[]>> ReadAsTsv(string fileName, bool ignoreEmptyLines)
    {
        return ReadAllDelimitedFileAsync(fileName, ignoreEmptyLines, TsvDelimiterChar);
    }

}
