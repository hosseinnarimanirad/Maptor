using System.Globalization;
using System.Xml;
using System.Xml.Linq;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Common.IO.Gpx;

/// <summary>
/// Reads and writes GPX (GPS Exchange Format) files. Supports GPX 1.0 and 1.1.
/// </summary>
public static class GpxFormat
{
    private const string Gpx11Namespace = "http://www.topografix.com/GPX/1/1";
    private const string Gpx10Namespace = "http://www.topografix.com/GPX/1/0";
    private static readonly XNamespace DefaultNs = XNamespace.Get(Gpx11Namespace);

    #region Parse - consolidated entry point

    /// <summary>
    /// Parses a GPX file and returns waypoints, routes, tracks, and metadata in a single read.
    /// </summary>
    /// <param name="fileName">Path to the GPX file.</param>
    /// <returns>Parsed GPX data.</returns>
    /// <exception cref="ArgumentNullException">When fileName is null or empty.</exception>
    /// <exception cref="FileNotFoundException">When the file does not exist.</exception>
    public static GpxParseResult Parse(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentNullException(nameof(fileName));
        if (!File.Exists(fileName))
            throw new FileNotFoundException($"GPX file '{fileName}' was not found.", fileName);

        using var stream = File.OpenRead(fileName);
        return Parse(stream);
    }

    /// <summary>
    /// Parses a GPX document from a stream.
    /// </summary>
    /// <param name="stream">Stream containing GPX XML.</param>
    /// <returns>Parsed GPX data.</returns>
    public static GpxParseResult Parse(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        var document = XDocument.Load(stream, LoadOptions.None);
        return ParseDocument(document);
    }

    /// <summary>
    /// Asynchronously parses a GPX document from a stream.
    /// </summary>
    /// <param name="stream">Stream containing GPX XML.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parsed GPX data.</returns>
    public static async Task<GpxParseResult> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true });
        var document = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken).ConfigureAwait(false);
        return ParseDocument(document);
    }

    private static GpxParseResult ParseDocument(XDocument document)
    {
        var root = document.Root;
        if (root == null)
            return new GpxParseResult([], [], [], null);

        var ns = DetectNamespace(root);
        var metadata = ParseMetadata(root, ns);

        var waypoints = ParseWaypoints(root, ns);
        var routes = ParseRoutes(root, ns);
        var tracks = ParseTracks(root, ns);

        return new GpxParseResult(waypoints, routes, tracks, metadata);
    }

    private static XNamespace DetectNamespace(XElement root)
    {
        var ns = root.Name.Namespace;
        if (ns != null && !string.IsNullOrEmpty(ns.NamespaceName))
            return ns;

        var xmlns = root.Attribute("xmlns")?.Value;
        if (!string.IsNullOrEmpty(xmlns))
            return XNamespace.Get(xmlns);

        return DefaultNs;
    }

    #endregion

    #region GetWaypoints / GetTracks / GetRoutes - backward compatibility

    /// <summary>
    /// Gets waypoints from a GPX file. For multiple reads (waypoints + tracks + routes), prefer <see cref="Parse(string)"/>.
    /// </summary>
    /// <param name="gpxFileName">Path to the GPX file.</param>
    /// <param name="xNamespace">Optional explicit namespace; if null, auto-detected.</param>
    /// <returns>List of waypoints.</returns>
    public static List<GpxWaypoint> GetWaypoints(string gpxFileName, string? xNamespace = null)
    {
        var result = Parse(gpxFileName);
        return result.Waypoints;
    }

    /// <summary>
    /// Gets tracks from a GPX file. For multiple reads, prefer <see cref="Parse(string)"/>.
    /// </summary>
    /// <param name="gpxFileName">Path to the GPX file.</param>
    /// <param name="xNamespace">Optional explicit namespace; if null, auto-detected.</param>
    /// <returns>List of tracks.</returns>
    public static List<GpxTrack> GetTracks(string gpxFileName, string? xNamespace = null)
    {
        var result = Parse(gpxFileName);
        return result.Tracks;
    }

    /// <summary>
    /// Gets routes from a GPX file. For multiple reads, prefer <see cref="Parse(string)"/>.
    /// </summary>
    /// <param name="gpxFileName">Path to the GPX file.</param>
    /// <param name="xNamespace">Optional explicit namespace; if null, auto-detected.</param>
    /// <returns>List of routes.</returns>
    public static List<GpxRoute> GetRoutes(string gpxFileName, string? xNamespace = null)
    {
        var result = Parse(gpxFileName);
        return result.Routes;
    }

    #endregion

    #region Parse helpers

    private static GpxMetadata? ParseMetadata(XElement root, XNamespace ns)
    {
        var meta = root.Element(ns + "metadata");
        if (meta == null) return null;

        var m = new GpxMetadata
        {
            Name = (string)meta.Element(ns + "name"),
            Description = (string)meta.Element(ns + "desc"),
            Keywords = (string)meta.Element(ns + "keywords"),
            Time = TryParseDateTime((string)meta.Element(ns + "time"))
        };

        var author = meta.Element(ns + "author");
        if (author != null)
        {
            m.Author = new GpxPerson
            {
                Name = (string)author.Element(ns + "name"),
                Email = ParseEmail(author.Element(ns + "email")),
                Link = ParseSingleLink(author.Element(ns + "link"), ns)
            };
        }

        var copyright = meta.Element(ns + "copyright");
        if (copyright != null)
        {
            var yearStr = (string)copyright.Attribute("year");
            int? year = null;
            if (!string.IsNullOrWhiteSpace(yearStr) && int.TryParse(yearStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var y))
                year = y;
            m.Copyright = new GpxCopyright
            {
                Author = (string?)copyright.Attribute("author") ?? "",
                Year = year,
                License = (string)copyright.Element(ns + "license")
            };
        }

        var links = meta.Elements(ns + "link").Select(linkEl => ParseLink(linkEl, ns)).Where(l => l != null).ToList();
        if (links.Count > 0) m.Links = links!;

        var bounds = meta.Element(ns + "bounds");
        if (bounds != null)
        {
            var minlat = TryParseDouble((string?)bounds.Attribute("minlat"));
            var minlon = TryParseDouble((string?)bounds.Attribute("minlon"));
            var maxlat = TryParseDouble((string?)bounds.Attribute("maxlat"));
            var maxlon = TryParseDouble((string?)bounds.Attribute("maxlon"));
            if (minlat.HasValue && minlon.HasValue && maxlat.HasValue && maxlon.HasValue)
            {
                m.Bounds = new GpxBounds
                {
                    MinLat = minlat.Value, MinLon = minlon.Value,
                    MaxLat = maxlat.Value, MaxLon = maxlon.Value
                };
            }
        }

        return m;
    }

    private static GpxEmail? ParseEmail(XElement? emailEl)
    {
        if (emailEl == null) return null;
        var id = (string?)emailEl.Attribute("id");
        var domain = (string?)emailEl.Attribute("domain");
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(domain)) return null;
        return new GpxEmail { Id = id, Domain = domain };
    }

    private static GpxLink? ParseSingleLink(XElement? linkEl, XNamespace ns) => linkEl != null ? ParseLink(linkEl, ns) : null;

    private static GpxLink? ParseLink(XElement linkEl, XNamespace ns)
    {
        var href = (string?)linkEl.Attribute("href");
        if (string.IsNullOrEmpty(href)) return null;
        return new GpxLink
        {
            Href = href,
            Text = (string)linkEl.Element(ns + "text"),
            Type = (string)linkEl.Element(ns + "type")
        };
    }

    private static List<GpxWaypoint> ParseWaypoints(XElement root, XNamespace ns)
    {
        var elements = root.Descendants(ns + "wpt");
        var list = new List<GpxWaypoint>();
        foreach (var el in elements)
        {
            var wpt = ParseWaypoint(el, ns);
            if (wpt != null) list.Add(wpt);
        }
        return list;
    }

    private static GpxWaypoint? ParseWaypoint(XElement el, XNamespace ns)
    {
        var latVal = (string?)el.Attribute("lat");
        var lonVal = (string?)el.Attribute("lon");
        if (string.IsNullOrEmpty(latVal) || string.IsNullOrEmpty(lonVal))
            return null;

        if (!double.TryParse(latVal, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
            !double.TryParse(lonVal, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
            return null;

        if (lat < -90 || lat > 90 || lon < -180 || lon >= 180)
            return null;

        return new GpxWaypoint
        {
            Latitude = lat,
            Longitude = lon,
            Elevation = TryParseDouble((string)el.Element(ns + "ele")),
            Time = TryParseDateTime((string)el.Element(ns + "time")),
            Name = (string)el.Element(ns + "name"),
            Description = (string)el.Element(ns + "desc"),
            Comment = (string)el.Element(ns + "cmt"),
            Symbol = (string)el.Element(ns + "sym"),
            Type = (string)el.Element(ns + "type"),
            Source = (string)el.Element(ns + "src"),
            Fix = (string)el.Element(ns + "fix"),
            Sat = TryParseInt((string)el.Element(ns + "sat")),
            Hdop = TryParseDecimal((string)el.Element(ns + "hdop")),
            Vdop = TryParseDecimal((string)el.Element(ns + "vdop")),
            Pdop = TryParseDecimal((string)el.Element(ns + "pdop")),
            AgeOfDgpsData = TryParseInt((string)el.Element(ns + "ageofdgpsdata")),
            DgpsId = TryParseInt((string)el.Element(ns + "dgpsid")),
            Links = el.Elements(ns + "link").Select(linkEl => ParseLink(linkEl, ns)).Where(l => l != null).Cast<GpxLink>().ToList()
        };
    }

    private static List<GpxRoute> ParseRoutes(XElement root, XNamespace ns)
    {
        var list = new List<GpxRoute>();
        foreach (var rteEl in root.Elements(ns + "rte"))
        {
            var rte = ParseRoute(rteEl, ns);
            if (rte != null) list.Add(rte);
        }
        return list;
    }

    private static GpxRoute? ParseRoute(XElement rteEl, XNamespace ns)
    {
        var routePoints = new List<GpxRoutePoint>();
        foreach (var rteptEl in rteEl.Elements(ns + "rtept"))
        {
            var pt = ParseRoutePoint(rteptEl, ns);
            if (pt != null) routePoints.Add(pt);
        }

        return new GpxRoute
        {
            Name = (string)rteEl.Element(ns + "name"),
            Comment = (string)rteEl.Element(ns + "cmt"),
            Description = (string)rteEl.Element(ns + "desc"),
            Source = (string)rteEl.Element(ns + "src"),
            Number = TryParseInt((string)rteEl.Element(ns + "number")),
            Type = (string)rteEl.Element(ns + "type"),
            Links = rteEl.Elements(ns + "link").Select(linkEl => ParseLink(linkEl, ns)).Where(l => l != null).Cast<GpxLink>().ToList(),
            RoutePoints = routePoints
        };
    }

    private static GpxRoutePoint? ParseRoutePoint(XElement el, XNamespace ns)
    {
        var latVal = (string?)el.Attribute("lat");
        var lonVal = (string?)el.Attribute("lon");
        if (string.IsNullOrEmpty(latVal) || string.IsNullOrEmpty(lonVal))
            return null;

        if (!double.TryParse(latVal, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
            !double.TryParse(lonVal, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
            return null;

        if (lat < -90 || lat > 90 || lon < -180 || lon >= 180)
            return null;

        return new GpxRoutePoint
        {
            Latitude = lat,
            Longitude = lon,
            Elevation = TryParseDouble((string)el.Element(ns + "ele")),
            Time = TryParseDateTime((string)el.Element(ns + "time")),
            MagVar = TryParseDecimal((string)el.Element(ns + "mag")),
            GeoidHeight = TryParseDecimal((string)el.Element(ns + "geoidheight")),
            Name = (string)el.Element(ns + "name"),
            Comment = (string)el.Element(ns + "cmt"),
            Description = (string)el.Element(ns + "desc"),
            Source = (string)el.Element(ns + "src"),
            Symbol = (string)el.Element(ns + "sym"),
            Type = (string)el.Element(ns + "type"),
            Fix = (string)el.Element(ns + "fix"),
            Sat = TryParseInt((string)el.Element(ns + "sat")),
            Hdop = TryParseDecimal((string)el.Element(ns + "hdop")),
            Vdop = TryParseDecimal((string)el.Element(ns + "vdop")),
            Pdop = TryParseDecimal((string)el.Element(ns + "pdop")),
            AgeOfDgpsData = TryParseInt((string)el.Element(ns + "ageofdgpsdata")),
            DgpsId = TryParseInt((string)el.Element(ns + "dgpsid")),
            Links = el.Elements(ns + "link").Select(linkEl => ParseLink(linkEl, ns)).Where(l => l != null).Cast<GpxLink>().ToList()
        };
    }

    private static List<GpxTrack> ParseTracks(XElement root, XNamespace ns)
    {
        var list = new List<GpxTrack>();
        foreach (var trkEl in root.Elements(ns + "trk"))
        {
            var trk = ParseTrack(trkEl, ns);
            if (trk != null) list.Add(trk);
        }
        return list;
    }

    private static GpxTrack? ParseTrack(XElement trkEl, XNamespace ns)
    {
        var segments = new List<GpxTrackSegment>();
        foreach (var segEl in trkEl.Elements(ns + "trkseg"))
        {
            var segment = ParseTrackSegment(segEl, ns);
            if (segment != null && segment.TrackPoints.Count > 0)
                segments.Add(segment);
        }

        return new GpxTrack
        {
            Name = (string)trkEl.Element(ns + "name") ?? "Track",
            Comment = (string)trkEl.Element(ns + "cmt"),
            Description = (string)trkEl.Element(ns + "desc"),
            Source = (string)trkEl.Element(ns + "src"),
            Number = TryParseInt((string)trkEl.Element(ns + "number")),
            Type = (string)trkEl.Element(ns + "type"),
            Links = trkEl.Elements(ns + "link").Select(linkEl => ParseLink(linkEl, ns)).Where(l => l != null).Cast<GpxLink>().ToList(),
            Segments = segments
        };
    }

    private static GpxTrackSegment? ParseTrackSegment(XElement segEl, XNamespace ns)
    {
        var points = new List<GpxTrackPoint>();
        foreach (var ptEl in segEl.Elements(ns + "trkpt"))
        {
            var pt = ParseTrackPoint(ptEl, ns);
            if (pt != null) points.Add(pt);
        }
        return new GpxTrackSegment(points);
    }

    private static GpxTrackPoint? ParseTrackPoint(XElement el, XNamespace ns)
    {
        var latVal = (string?)el.Attribute("lat");
        var lonVal = (string?)el.Attribute("lon");
        if (string.IsNullOrEmpty(latVal) || string.IsNullOrEmpty(lonVal))
            return null;

        if (!double.TryParse(latVal, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
            !double.TryParse(lonVal, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
            return null;

        if (lat < -90 || lat > 90 || lon < -180 || lon >= 180)
            return null;

        return new GpxTrackPoint
        {
            Latitude = lat,
            Longitude = lon,
            Elevation = TryParseDouble((string)el.Element(ns + "ele")),
            Time = TryParseDateTime((string)el.Element(ns + "time")),
            MagVar = TryParseDecimal((string)el.Element(ns + "mag")),
            GeoidHeight = TryParseDecimal((string)el.Element(ns + "geoidheight")),
            Name = (string)el.Element(ns + "name"),
            Comment = (string)el.Element(ns + "cmt"),
            Description = (string)el.Element(ns + "desc"),
            Source = (string)el.Element(ns + "src"),
            Symbol = (string)el.Element(ns + "sym"),
            Type = (string)el.Element(ns + "type"),
            Fix = (string)el.Element(ns + "fix"),
            Sat = TryParseInt((string)el.Element(ns + "sat")),
            Hdop = TryParseDecimal((string)el.Element(ns + "hdop")),
            Vdop = TryParseDecimal((string)el.Element(ns + "vdop")),
            Pdop = TryParseDecimal((string)el.Element(ns + "pdop")),
            AgeOfDgpsData = TryParseInt((string)el.Element(ns + "ageofdgpsdata")),
            DgpsId = TryParseInt((string)el.Element(ns + "dgpsid")),
            Links = el.Elements(ns + "link").Select(linkEl => ParseLink(linkEl, ns)).Where(l => l != null).Cast<GpxLink>().ToList()
        };
    }

    private static double? TryParseDouble(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static int? TryParseInt(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static decimal? TryParseDecimal(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static DateTime? TryParseDateTime(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var v) ? v : null;
    }

    #endregion

    #region Write from Features

    /// <summary>
    /// Converts features to GPX format and writes to a file. Point geometries become waypoints;
    /// LineString geometries become tracks. Coordinates are transformed using the provided function (e.g. Web Mercator to WGS84).
    /// </summary>
    /// <param name="fileName">Output file path.</param>
    /// <param name="features">Features to write (geometry type Point or LineString).</param>
    /// <param name="toWgs84">Function to transform coordinates to WGS84 (e.g. MapProjects.WebMercatorToGeodeticWgs84).</param>
    public static void WriteFromFeatures(string fileName, IEnumerable<Feature<Point>> features, Func<Point, Point> toWgs84)
    {
        if (toWgs84 == null)
            throw new ArgumentNullException(nameof(toWgs84));

        var waypoints = new List<GpxWaypoint>();
        var tracks = new List<GpxTrack>();

        foreach (var f in features ?? [])
        {
            if (f?.TheGeometry == null || f.TheGeometry.IsNullOrEmpty())
                continue;

            var geom = f.TheGeometry;
            var attrs = f.Attributes ?? new Dictionary<string, object>();

            if (geom.Type == GeometryType.Point)
            {
                var pts = geom.GetAllPoints();
                if (pts != null && pts.Count > 0)
                {
                    var p = toWgs84(pts[0]);
                    waypoints.Add(new GpxWaypoint
                    {
                        Latitude = p.Y,
                        Longitude = p.X,
                        Elevation = GetAttrDouble(attrs, "elevation"),
                        Name = GetAttrString(attrs, "name"),
                        Description = GetAttrString(attrs, "description")
                    });
                }
            }
            else if (geom.Type == GeometryType.LineString)
            {
                var pts = geom.GetAllPoints();
                if (pts != null && pts.Count >= 2)
                {
                    var trackPts = pts.Select(p =>
                    {
                        var w = toWgs84(p);
                        return new GpxTrackPoint { Latitude = w.Y, Longitude = w.X };
                }).ToList();
                    tracks.Add(new GpxTrack
                    {
                        Name = GetAttrString(attrs, "name") ?? "Track",
                        Segments = [new GpxTrackSegment(trackPts)]
                    });
                }
            }
        }

        Write(fileName, waypoints, [], tracks, null);
    }

    private static string? GetAttrString(Dictionary<string, object> attrs, string key)
    {
        if (attrs.TryGetValue(key, out var v) && v != null)
        {
            var s = v.ToString();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }
        return null;
    }

    private static double? GetAttrDouble(Dictionary<string, object> attrs, string key)
    {
        if (!attrs.TryGetValue(key, out var v) || v == null) return null;
        if (v is double d) return d;
        if (v is float f) return f;
        if (v is decimal m) return (double)m;
        if (v is int i) return i;
        if (v is long l) return l;
        if (double.TryParse(v.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        return null;
    }

    #endregion

    #region Write

    /// <summary>
    /// Writes GPX waypoints and tracks to a file.
    /// </summary>
    /// <param name="fileName">Output file path.</param>
    /// <param name="waypoints">Waypoints to write.</param>
    /// <param name="routes">Routes to write.</param>
    /// <param name="tracks">Tracks to write.</param>
    /// <param name="metadata">Optional file metadata.</param>
    public static void Write(string fileName, IEnumerable<GpxWaypoint> waypoints, IEnumerable<GpxRoute> routes, IEnumerable<GpxTrack> tracks, GpxMetadata? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentNullException(nameof(fileName));

        var doc = BuildGpxDocument(waypoints, routes, tracks, metadata);
        doc.Save(fileName);
    }

    /// <summary>
    /// Writes GPX waypoints and tracks to a stream.
    /// </summary>
    public static void Write(Stream stream, IEnumerable<GpxWaypoint> waypoints, IEnumerable<GpxRoute> routes, IEnumerable<GpxTrack> tracks, GpxMetadata? metadata = null)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        var doc = BuildGpxDocument(waypoints, routes, tracks, metadata);
        doc.Save(stream);
    }

    private static XDocument BuildGpxDocument(IEnumerable<GpxWaypoint> waypoints, IEnumerable<GpxRoute> routes, IEnumerable<GpxTrack> tracks, GpxMetadata? metadata)
    {
        var ns = XNamespace.Get(Gpx11Namespace);
        var root = new XElement(ns + "gpx",
            new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
            new XAttribute("version", "1.1"),
            new XAttribute("creator", "IRI.Maptor"),
            new XAttribute("xmlns", Gpx11Namespace)
        );

        if (metadata != null)
            root.Add(BuildMetadataElement(metadata, ns));

        foreach (var wpt in waypoints ?? [])
            root.Add(BuildWaypointElement(wpt, ns));

        foreach (var rte in routes ?? [])
            root.Add(BuildRouteElement(rte, ns));

        foreach (var trk in tracks ?? [])
            root.Add(BuildTrackElement(trk, ns));

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
    }

    private static XElement BuildMetadataElement(GpxMetadata m, XNamespace ns)
    {
        var el = new XElement(ns + "metadata");
        if (!string.IsNullOrEmpty(m.Name)) el.Add(new XElement(ns + "name", m.Name));
        if (!string.IsNullOrEmpty(m.Description)) el.Add(new XElement(ns + "desc", m.Description));
        if (m.Time.HasValue) el.Add(new XElement(ns + "time", m.Time.Value.ToString("o", CultureInfo.InvariantCulture)));
        if (!string.IsNullOrEmpty(m.Keywords)) el.Add(new XElement(ns + "keywords", m.Keywords));
        if (m.Author != null)
        {
            var author = new XElement(ns + "author");
            if (!string.IsNullOrEmpty(m.Author.Name)) author.Add(new XElement(ns + "name", m.Author.Name));
            el.Add(author);
        }
        if (m.Copyright != null)
        {
            var c = new XElement(ns + "copyright", new XAttribute("author", m.Copyright.Author));
            if (m.Copyright.Year.HasValue) c.Add(new XAttribute("year", m.Copyright.Year.Value));
            if (!string.IsNullOrEmpty(m.Copyright.License)) c.Add(new XElement(ns + "license", m.Copyright.License));
            el.Add(c);
        }
        if (m.Bounds != null)
            el.Add(new XElement(ns + "bounds",
                new XAttribute("minlat", m.Bounds.MinLat.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("minlon", m.Bounds.MinLon.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("maxlat", m.Bounds.MaxLat.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("maxlon", m.Bounds.MaxLon.ToString(CultureInfo.InvariantCulture))));
        foreach (var link in m.Links ?? [])
            el.Add(BuildLinkElement(link, ns));
        return el;
    }

    private static XElement BuildWaypointElement(GpxWaypoint w, XNamespace ns)
    {
        var el = new XElement(ns + "wpt",
            new XAttribute("lat", w.Latitude.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("lon", w.Longitude.ToString(CultureInfo.InvariantCulture)));
        if (w.Elevation.HasValue) el.Add(new XElement(ns + "ele", w.Elevation.Value.ToString(CultureInfo.InvariantCulture)));
        if (w.Time.HasValue) el.Add(new XElement(ns + "time", w.Time.Value.ToString("o", CultureInfo.InvariantCulture)));
        if (!string.IsNullOrEmpty(w.Name)) el.Add(new XElement(ns + "name", w.Name));
        if (!string.IsNullOrEmpty(w.Description)) el.Add(new XElement(ns + "desc", w.Description));
        if (!string.IsNullOrEmpty(w.Comment)) el.Add(new XElement(ns + "cmt", w.Comment));
        if (!string.IsNullOrEmpty(w.Symbol)) el.Add(new XElement(ns + "sym", w.Symbol));
        if (!string.IsNullOrEmpty(w.Type)) el.Add(new XElement(ns + "type", w.Type));
        if (!string.IsNullOrEmpty(w.Source)) el.Add(new XElement(ns + "src", w.Source));
        foreach (var link in w.Links ?? []) el.Add(BuildLinkElement(link, ns));
        return el;
    }

    private static XElement BuildRouteElement(GpxRoute r, XNamespace ns)
    {
        var el = new XElement(ns + "rte");
        if (!string.IsNullOrEmpty(r.Name)) el.Add(new XElement(ns + "name", r.Name));
        if (!string.IsNullOrEmpty(r.Description)) el.Add(new XElement(ns + "desc", r.Description));
        if (!string.IsNullOrEmpty(r.Comment)) el.Add(new XElement(ns + "cmt", r.Comment));
        if (r.Number.HasValue) el.Add(new XElement(ns + "number", r.Number.Value));
        if (!string.IsNullOrEmpty(r.Type)) el.Add(new XElement(ns + "type", r.Type));
        foreach (var link in r.Links ?? []) el.Add(BuildLinkElement(link, ns));
        foreach (var rpt in r.RoutePoints ?? [])
        {
            var rptEl = BuildRoutePointElement(rpt, ns);
            if (rptEl != null) el.Add(rptEl);
        }
        return el;
    }

    private static XElement? BuildRoutePointElement(GpxRoutePoint rpt, XNamespace ns)
    {
        var el = new XElement(ns + "rtept",
            new XAttribute("lat", rpt.Latitude.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("lon", rpt.Longitude.ToString(CultureInfo.InvariantCulture)));
        if (rpt.Elevation.HasValue) el.Add(new XElement(ns + "ele", rpt.Elevation.Value.ToString(CultureInfo.InvariantCulture)));
        if (rpt.Time.HasValue) el.Add(new XElement(ns + "time", rpt.Time.Value.ToString("o", CultureInfo.InvariantCulture)));
        if (!string.IsNullOrEmpty(rpt.Name)) el.Add(new XElement(ns + "name", rpt.Name));
        if (!string.IsNullOrEmpty(rpt.Description)) el.Add(new XElement(ns + "desc", rpt.Description));
        if (!string.IsNullOrEmpty(rpt.Comment)) el.Add(new XElement(ns + "cmt", rpt.Comment));
        if (!string.IsNullOrEmpty(rpt.Symbol)) el.Add(new XElement(ns + "sym", rpt.Symbol));
        if (!string.IsNullOrEmpty(rpt.Type)) el.Add(new XElement(ns + "type", rpt.Type));
        foreach (var link in rpt.Links ?? []) el.Add(BuildLinkElement(link, ns));
        return el;
    }

    private static XElement BuildTrackElement(GpxTrack t, XNamespace ns)
    {
        var el = new XElement(ns + "trk");
        if (!string.IsNullOrEmpty(t.Name)) el.Add(new XElement(ns + "name", t.Name));
        if (!string.IsNullOrEmpty(t.Description)) el.Add(new XElement(ns + "desc", t.Description));
        if (!string.IsNullOrEmpty(t.Comment)) el.Add(new XElement(ns + "cmt", t.Comment));
        if (t.Number.HasValue) el.Add(new XElement(ns + "number", t.Number.Value));
        if (!string.IsNullOrEmpty(t.Type)) el.Add(new XElement(ns + "type", t.Type));
        foreach (var link in t.Links ?? []) el.Add(BuildLinkElement(link, ns));
        foreach (var seg in t.Segments ?? [])
        {
            var segEl = new XElement(ns + "trkseg");
            foreach (var pt in seg.TrackPoints ?? [])
            {
                var ptEl = BuildTrackPointElement(pt, ns);
                if (ptEl != null) segEl.Add(ptEl);
            }
            if (segEl.HasElements) el.Add(segEl);
        }
        return el;
    }

    private static XElement? BuildTrackPointElement(GpxTrackPoint pt, XNamespace ns)
    {
        var el = new XElement(ns + "trkpt",
            new XAttribute("lat", pt.Latitude.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("lon", pt.Longitude.ToString(CultureInfo.InvariantCulture)));
        if (pt.Elevation.HasValue) el.Add(new XElement(ns + "ele", pt.Elevation.Value.ToString(CultureInfo.InvariantCulture)));
        if (pt.Time.HasValue) el.Add(new XElement(ns + "time", pt.Time.Value.ToString("o", CultureInfo.InvariantCulture)));
        if (!string.IsNullOrEmpty(pt.Name)) el.Add(new XElement(ns + "name", pt.Name));
        if (!string.IsNullOrEmpty(pt.Description)) el.Add(new XElement(ns + "desc", pt.Description));
        if (!string.IsNullOrEmpty(pt.Comment)) el.Add(new XElement(ns + "cmt", pt.Comment));
        if (!string.IsNullOrEmpty(pt.Symbol)) el.Add(new XElement(ns + "sym", pt.Symbol));
        if (!string.IsNullOrEmpty(pt.Type)) el.Add(new XElement(ns + "type", pt.Type));
        foreach (var link in pt.Links ?? []) el.Add(BuildLinkElement(link, ns));
        return el;
    }

    private static XElement BuildLinkElement(GpxLink link, XNamespace ns)
    {
        var el = new XElement(ns + "link", new XAttribute("href", link.Href));
        if (!string.IsNullOrEmpty(link.Text)) el.Add(new XElement(ns + "text", link.Text));
        if (!string.IsNullOrEmpty(link.Type)) el.Add(new XElement(ns + "type", link.Type));
        return el;
    }

    #endregion
}
