using IRI.Maptor.Sta.SpatialReferenceSystem;
using System.Globalization;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Helpers;

namespace IRI.Maptor.Sta.Spatial.Model;

public class TileInfo
{
    public int RowNumber { get; private set; }

    public int ColumnNumber { get; private set; }

    public int ZoomLevel { get; private set; }

    public BoundingBox GeodeticExtent { get; private set; }

    public BoundingBox WebMercatorExtent
    {
        get
        {
            return GeodeticExtent.Transform(i => MapProjects.GeodeticWgs84ToWebMercator(i));
        }
    }

    /// <summary>
    /// Extent based on sphere
    /// </summary>
    public BoundingBox GeocentricExtent
    {
        get
        {
            return GeodeticExtent.Transform(i => Transformations.ChangeDatum(i, Ellipsoids.WGS84, Ellipsoids.Sphere));
        }
    }

    public TileInfo(int row, int column, int zoomLevel)
    {
        this.RowNumber = row;

        this.ColumnNumber = column;

        this.ZoomLevel = zoomLevel;

        //this.UsedMapScale = usedMapScale;

        this.GeodeticExtent = WebMercatorUtility.GetWgs84ImageBoundingBox(row, column, zoomLevel);
    }

    public override string ToString()
    {
        return $"Z:{ZoomLevel}, R: {RowNumber}, C: {ColumnNumber}, W:{WebMercatorExtent.Width}, H:{WebMercatorExtent.Height}, X:{WebMercatorExtent.TopLeft.X}, Y:{WebMercatorExtent.TopLeft.Y}";
    }

    public string ToShortString()
    {
        return $"{ZoomLevel.ToString(CultureInfo.InvariantCulture)}, {RowNumber.ToString(CultureInfo.InvariantCulture)}, {ColumnNumber.ToString(CultureInfo.InvariantCulture)}";
    }

    public override bool Equals(object obj)
    {
        var other = obj as TileInfo;

        if (other == null)
        {
            return false;
        }

        return this.ZoomLevel == other.ZoomLevel && this.RowNumber == other.RowNumber && this.ColumnNumber == other.ColumnNumber;
    }

    public override int GetHashCode()
    {
        return $"{ZoomLevel}-{RowNumber}-{ColumnNumber}".GetHashCode();
    }

    public TileInfo Clone() => new TileInfo(this.RowNumber, this.ColumnNumber, this.ZoomLevel);

    public static List<TileInfo> GetAllForLevel(int level)
    {
        var list = new List<TileInfo>();

        var count = (int)Math.Pow(2, level);

        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < count; j++)
            {
                list.Add(new TileInfo(i, j, level));
            }
        }

        return list;

        //return [new TileInfo(0, 0, 2), new TileInfo(0, 1, 2), new TileInfo(1, 0, 2), new TileInfo(1, 1, 2),];
    }
}
