namespace IRI.Maptor.Core.FileGdb.Catalog.Model;

public sealed class FileGdbSpatialReference
{
    internal FileGdbSpatialReference(string? wkt, int? wkid, int? latestWkid, int srid)
    {
        Wkt = wkt;
        Wkid = wkid;
        LatestWkid = latestWkid;
        Srid = srid;
    }

    /// <summary>Esri WKT, kept even when an SRID is known.</summary>
    public string? Wkt { get; }

    /// <summary>The WKID as stored; may be an Esri id such as 102100.</summary>
    public int? Wkid { get; }

    /// <summary>The current id for the same system, usually the EPSG code (e.g. 3857 for 102100).</summary>
    public int? LatestWkid { get; }

    /// <summary>
    /// <see cref="LatestWkid"/>, else <see cref="Wkid"/>, else the EPSG authority found in the WKT,
    /// else 0 (unknown).
    /// </summary>
    public int Srid { get; }

    public override string ToString() => $"SRID {Srid}";
}
