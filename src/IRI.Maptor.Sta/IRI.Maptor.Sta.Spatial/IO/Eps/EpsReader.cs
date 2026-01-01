using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.IO.Eps;

/// <summary>
/// EPS reader for parsing EPS files and converting to Geometry types
/// </summary>
public static class EpsReader
{
    private const string EpsHeaderPattern = @"%!PS-Adobe-(\d+\.\d+)\s+EPSF-(\d+\.\d+)";
    private const string BoundingBoxPattern = @"%%BoundingBox:\s*([+-]?\d+(?:\.\d+)?)\s+([+-]?\d+(?:\.\d+)?)\s+([+-]?\d+(?:\.\d+)?)\s+([+-]?\d+(?:\.\d+)?)";
    private const string CreatorPattern = @"%%Creator:\s*(.+)";
    private const string TitlePattern = @"%%Title:\s*(.+)";

    /// <summary>
    /// Reads EPS from file and converts to Geometry
    /// </summary>
    public static Geometry<Point> ReadFromFile(string filePath, int srid = 0)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("EPS file not found", filePath);

        var content = File.ReadAllText(filePath);
        return Read(content, srid);
    }

    /// <summary>
    /// Reads EPS from string and converts to Geometry
    /// </summary>
    public static Geometry<Point> Read(string epsContent, int srid = 0)
    {
        if (string.IsNullOrWhiteSpace(epsContent))
            return Geometry<Point>.Empty;

        try
        {
            // Parse header to extract bounding box and metadata
            var header = ParseEpsHeader(epsContent);

            // Find the start of PostScript commands (after %%EndComments or %%EndProlog)
            var bodyStart = FindBodyStart(epsContent);
            var bodyContent = bodyStart >= 0 ? epsContent.Substring(bodyStart) : epsContent;

            // Parse PostScript drawing commands
            var geometries = ParsePostScriptCommands(bodyContent, srid);

            if (geometries.Count == 0)
                return Geometry<Point>.Empty;

            if (geometries.Count == 1)
                return geometries[0];

            // Multiple geometries - create GeometryCollection
            return Geometry<Point>.Create(geometries, GeometryType.GeometryCollection, srid);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error parsing EPS: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Reads EPS from file and converts to Feature
    /// </summary>
    public static Feature<Point> ReadFeatureFromFile(string filePath, int srid = 0, bool preserveAttributes = true)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("EPS file not found", filePath);

        var content = File.ReadAllText(filePath);
        return ReadFeature(content, srid, preserveAttributes);
    }

    /// <summary>
    /// Reads EPS from string and converts to Feature
    /// </summary>
    public static Feature<Point> ReadFeature(string epsContent, int srid = 0, bool preserveAttributes = true)
    {
        var geometry = Read(epsContent, srid);
        var attributes = new Dictionary<string, object>();

        if (preserveAttributes)
        {
            try
            {
                var header = ParseEpsHeader(epsContent);
                if (!string.IsNullOrEmpty(header.Title))
                    attributes["Title"] = header.Title;
                if (!string.IsNullOrEmpty(header.Creator))
                    attributes["Creator"] = header.Creator;
            }
            catch
            {
                // If attribute extraction fails, continue with empty attributes
            }
        }

        return new Feature<Point>(geometry, attributes);
    }

    /// <summary>
    /// EPS header information
    /// </summary>
    private class EpsHeader
    {
        public double? BoundingBoxXMin { get; set; }
        public double? BoundingBoxYMin { get; set; }
        public double? BoundingBoxXMax { get; set; }
        public double? BoundingBoxYMax { get; set; }
        public string? Creator { get; set; }
        public string? Title { get; set; }
    }

    /// <summary>
    /// Parses EPS header to extract bounding box and metadata
    /// </summary>
    private static EpsHeader ParseEpsHeader(string content)
    {
        var header = new EpsHeader();

        // Parse BoundingBox
        var bboxMatch = Regex.Match(content, BoundingBoxPattern);
        if (bboxMatch.Success)
        {
            if (double.TryParse(bboxMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double xMin) &&
                double.TryParse(bboxMatch.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double yMin) &&
                double.TryParse(bboxMatch.Groups[3].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double xMax) &&
                double.TryParse(bboxMatch.Groups[4].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double yMax))
            {
                header.BoundingBoxXMin = xMin;
                header.BoundingBoxYMin = yMin;
                header.BoundingBoxXMax = xMax;
                header.BoundingBoxYMax = yMax;
            }
        }

        // Parse Creator
        var creatorMatch = Regex.Match(content, CreatorPattern);
        if (creatorMatch.Success)
        {
            header.Creator = creatorMatch.Groups[1].Value.Trim();
        }

        // Parse Title
        var titleMatch = Regex.Match(content, TitlePattern);
        if (titleMatch.Success)
        {
            header.Title = titleMatch.Groups[1].Value.Trim();
        }

        return header;
    }

    /// <summary>
    /// Finds the start of PostScript body (after %%EndComments or %%EndProlog)
    /// </summary>
    private static int FindBodyStart(string content)
    {
        var endCommentsIndex = content.IndexOf("%%EndComments", StringComparison.Ordinal);
        var endPrologIndex = content.IndexOf("%%EndProlog", StringComparison.Ordinal);

        if (endCommentsIndex >= 0)
        {
            var start = content.IndexOf('\n', endCommentsIndex);
            return start >= 0 ? start + 1 : endCommentsIndex + 13;
        }

        if (endPrologIndex >= 0)
        {
            var start = content.IndexOf('\n', endPrologIndex);
            return start >= 0 ? start + 1 : endPrologIndex + 11;
        }

        // If no EndComments/EndProlog, look for first PostScript command
        var firstCommand = content.IndexOf("moveto", StringComparison.Ordinal);
        if (firstCommand < 0)
            firstCommand = content.IndexOf("lineto", StringComparison.Ordinal);
        if (firstCommand < 0)
            firstCommand = content.IndexOf("curveto", StringComparison.Ordinal);

        return firstCommand >= 0 ? firstCommand : -1;
    }

    /// <summary>
    /// Parses PostScript drawing commands and converts to Geometry
    /// </summary>
    private static List<Geometry<Point>> ParsePostScriptCommands(string content, int srid)
    {
        var geometries = new List<Geometry<Point>>();
        var currentPath = new List<Point>();
        Point? currentPoint = null;
        Point? startPoint = null;

        // Split content into lines for easier parsing
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("%"))
                continue;

            // Parse coordinates and commands
            var tokens = TokenizePostScriptLine(trimmedLine);

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                // Check if token is a command
                if (IsPostScriptCommand(token))
                {
                    switch (token.ToLowerInvariant())
                    {
                        case "moveto":
                        case "m":
                            if (i >= 2 && double.TryParse(tokens[i - 2], NumberStyles.Float, CultureInfo.InvariantCulture, out double mx) &&
                                double.TryParse(tokens[i - 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double my))
                            {
                                // If we have accumulated points, create geometry
                                if (currentPath.Count >= 2)
                                {
                                    geometries.Add(Geometry<Point>.Create(currentPath, GeometryType.LineString, srid));
                                }
                                currentPath.Clear();
                                currentPoint = new Point(mx, my);
                                startPoint = currentPoint;
                                currentPath.Add(currentPoint);
                            }
                            break;

                        case "lineto":
                        case "l":
                            if (i >= 2 && double.TryParse(tokens[i - 2], NumberStyles.Float, CultureInfo.InvariantCulture, out double lx) &&
                                double.TryParse(tokens[i - 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double ly))
                            {
                                currentPoint = new Point(lx, ly);
                                currentPath.Add(currentPoint);
                            }
                            break;

                        case "curveto":
                        case "c":
                            // Cubic Bezier: x1 y1 x2 y2 x3 y3 curveto
                            if (i >= 6 && ParseCoordinates(tokens, i - 6, 6, out double[] curveCoords))
                            {
                                // Approximate Bezier curve as line segments
                                var curvePoints = ApproximateBezierCubic(
                                    currentPoint ?? new Point(0, 0),
                                    new Point(curveCoords[0], curveCoords[1]),
                                    new Point(curveCoords[2], curveCoords[3]),
                                    new Point(curveCoords[4], curveCoords[5]),
                                    10); // 10 segments
                                currentPath.AddRange(curvePoints.Skip(1)); // Skip first point (already in path)
                                currentPoint = new Point(curveCoords[4], curveCoords[5]);
                            }
                            break;

                        case "closepath":
                        case "z":
                            if (currentPath.Count >= 3 && startPoint != null)
                            {
                                // Close the path
                                if (currentPath[currentPath.Count - 1].X != startPoint.X ||
                                    currentPath[currentPath.Count - 1].Y != startPoint.Y)
                                {
                                    currentPath.Add(new Point(startPoint.X, startPoint.Y));
                                }
                                // Create polygon
                                var ring = Geometry<Point>.Create(currentPath, GeometryType.LineString, srid);
                                geometries.Add(Geometry<Point>.Create([ring], GeometryType.Polygon, srid));
                            }
                            else if (currentPath.Count >= 2)
                            {
                                // Create closed line string
                                geometries.Add(Geometry<Point>.Create(currentPath, GeometryType.LineString, srid));
                            }
                            currentPath.Clear();
                            currentPoint = null;
                            startPoint = null;
                            break;

                        case "stroke":
                            if (currentPath.Count >= 2)
                            {
                                geometries.Add(Geometry<Point>.Create(currentPath, GeometryType.LineString, srid));
                            }
                            currentPath.Clear();
                            currentPoint = null;
                            startPoint = null;
                            break;

                        case "fill":
                            if (currentPath.Count >= 3)
                            {
                                var fillRing = Geometry<Point>.Create(currentPath, GeometryType.LineString, srid);
                                geometries.Add(Geometry<Point>.Create([fillRing], GeometryType.Polygon, srid));
                            }
                            currentPath.Clear();
                            currentPoint = null;
                            startPoint = null;
                            break;
                    }
                }
            }
        }

        // Add any remaining path
        if (currentPath.Count >= 2)
        {
            geometries.Add(Geometry<Point>.Create(currentPath, GeometryType.LineString, srid));
        }

        return geometries;
    }

    /// <summary>
    /// Tokenizes a PostScript line into tokens (numbers and commands)
    /// </summary>
    private static List<string> TokenizePostScriptLine(string line)
    {
        var tokens = new List<string>();
        var currentToken = new StringBuilder();

        foreach (char c in line)
        {
            if (char.IsWhiteSpace(c))
            {
                if (currentToken.Length > 0)
                {
                    tokens.Add(currentToken.ToString());
                    currentToken.Clear();
                }
            }
            else
            {
                currentToken.Append(c);
            }
        }

        if (currentToken.Length > 0)
        {
            tokens.Add(currentToken.ToString());
        }

        return tokens;
    }

    /// <summary>
    /// Checks if a token is a PostScript command
    /// </summary>
    private static bool IsPostScriptCommand(string token)
    {
        var lowerToken = token.ToLowerInvariant();
        return lowerToken == "moveto" || lowerToken == "m" ||
               lowerToken == "lineto" || lowerToken == "l" ||
               lowerToken == "curveto" || lowerToken == "c" ||
               lowerToken == "closepath" || lowerToken == "z" ||
               lowerToken == "stroke" ||
               lowerToken == "fill" ||
               lowerToken == "newpath" ||
               lowerToken == "gsave" ||
               lowerToken == "grestore";
    }

    /// <summary>
    /// Parses coordinates from token array
    /// </summary>
    private static bool ParseCoordinates(List<string> tokens, int startIndex, int count, out double[] coordinates)
    {
        coordinates = new double[count];
        for (int i = 0; i < count; i++)
        {
            if (startIndex + i >= tokens.Count ||
                !double.TryParse(tokens[startIndex + i], NumberStyles.Float, CultureInfo.InvariantCulture, out coordinates[i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Approximates a cubic Bezier curve as line segments
    /// </summary>
    private static List<Point> ApproximateBezierCubic(Point p0, Point p1, Point p2, Point p3, int segments)
    {
        var points = new List<Point>();
        for (int i = 0; i <= segments; i++)
        {
            double t = i / (double)segments;
            double x = Math.Pow(1 - t, 3) * p0.X + 3 * Math.Pow(1 - t, 2) * t * p1.X + 3 * (1 - t) * Math.Pow(t, 2) * p2.X + Math.Pow(t, 3) * p3.X;
            double y = Math.Pow(1 - t, 3) * p0.Y + 3 * Math.Pow(1 - t, 2) * t * p1.Y + 3 * (1 - t) * Math.Pow(t, 2) * p2.Y + Math.Pow(t, 3) * p3.Y;
            points.Add(new Point(x, y));
        }
        return points;
    }
}
