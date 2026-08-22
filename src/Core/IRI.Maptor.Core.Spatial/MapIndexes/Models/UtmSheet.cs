using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Core.Spatial.MapIndexes;

public class UtmSheet : IGeometryAware<Point>
{
    internal const int _2kUtmSheetRowCount = 20;
    internal const int _2kUtmSheetColumnCount = 20;
    internal const int _2kUtmBlockRowCount = 70;
    internal const int _2kUtmBlockColumnCount = 21;

    internal static readonly char[] _2kUtmBlockColumns;
    internal static readonly char[] _2kUtmSheetColumns;

    public Geometry<Point> TheGeometry
    {
        get
        {
            var geodeticExtent = UtmExtent.TransformBy4Point(p => MapProjects.UTMToGeodetic(p, UtmZone));

            return geodeticExtent.Transform(MapProjects.GeodeticWgs84ToWebMercator, SridHelper.WebMercator);
        }

        set => throw new NotImplementedException();
    }

    public int Id { get; set; }

    public Guid Key { get; set; }

    public BoundingBox UtmExtent { get; set; }

    //public Geometry GeodeticExtent
    //{
    //    get
    //    {
    //        return UtmExtent.TransformBy4Point(p => MapProjects.UTMToGeodetic(p, UtmZone));
    //        //return UtmExtent.Transform(p => MapProjects.UTMToGeodetic(p, UtmZone)).AsGeometry(SridHelper.GeodeticWGS84);
    //    }
    //}

    public int Row { get; set; }

    public int Column { get; set; }

    public string SheetName { get; set; }

    public UtmIndexType Type { get; set; }
     
    public int UtmZone { get; set; }

    static UtmSheet()
    {
        _2kUtmBlockColumns = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U'];

        _2kUtmSheetColumns = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T'];
    }

    protected UtmSheet()
    {

    }

    //From BoundingBox
    public static UtmSheet? Create(BoundingBox utmBoundingBox, UtmIndexType type, int utmZone)
    {
        var centerLatLong = MapProjects.UTMToGeodetic(utmBoundingBox.Center, utmZone);

        switch (type)
        {
            case UtmIndexType.Ncc2kBlock:
                return Create2kUtmBlock(centerLatLong.X, centerLatLong.Y, utmZone);

            case UtmIndexType.Ncc2kSheet:
                return Create2kUtmSheet(centerLatLong.X, centerLatLong.Y, utmZone);

            case UtmIndexType.Ncc1k:
                return Create1kUtmSheet(centerLatLong.X, centerLatLong.Y, utmZone);

            case UtmIndexType.Ncc500:
                return Create500UtmSheet(centerLatLong.X, centerLatLong.Y, utmZone);

            default:
                throw new NotImplementedException();
        }
    }

    //private static UtmSheet Create2kBlock(BoundingBox utmBoundingBox, int utmZone)
    //{
    //    UtmSheet result = new UtmSheet()
    //    {
    //        Column = (int)Math.Round((utmBoundingBox.XMin - UtmIndexes._2kUtmXmin) / utmBoundingBox.Width),
    //        Row = (int)Math.Round((UtmIndexes._2kUtmYmax - utmBoundingBox.YMax) / utmBoundingBox.Height),
    //        UtmZone = utmZone,
    //        UtmExtent = utmBoundingBox,
    //        Type = UtmIndexType.Ncc2kBlock
    //    };

    //    result.SheetName = $"{utmZone}{_2kUtmBlockColumns[result.Column]}{result.Row.ToString("00")}";

    //    return result;
    //}

    //private static UtmSheet Create2kSheet(BoundingBox utmBoundingBox, int utmZone)
    //{

    //}

    //From lat/long

    public static UtmSheet? Create2kUtmBlock(double longitude, double latitude, int utmZone)
    {
        var utmPoint = MapProjects.GeodeticToUTM(new Point(longitude, latitude), Ellipsoids.WGS84, utmZone);

        if (!UtmIndexes._2kUtmBoundingBox.Intersects(utmPoint))
        {
            return null;
        }

        var rowColumn = CalculateRowColumn(UtmIndexes._2kUtmBoundingBox, utmPoint, UtmIndexes._2kUtmBlockWidth, UtmIndexes._2kUtmBlockHeight);

        return Create2kBlock(rowColumn.Row, rowColumn.Column, utmZone);
    }

    public static UtmSheet? Create2kUtmSheet(double longitude, double latitude, int utmZone)
    {
        var utmPoint = MapProjects.GeodeticToUTM(new Point(longitude, latitude), Ellipsoids.WGS84, utmZone);

        if (!UtmIndexes._2kUtmBoundingBox.Intersects(utmPoint))
        {
            return null;
        }

        var block = Create2kUtmBlock(longitude, latitude, utmZone);

        var rowColumn = CalculateRowColumn(block.UtmExtent, utmPoint, UtmIndexes._2kUtmSheetWidth, UtmIndexes._2kUtmSheetHeight);

        return Create2kSheet(block, rowColumn.Row, rowColumn.Column);
    }

    public static UtmSheet? Create1kUtmSheet(double longitude, double latitude, int utmZone)
    {
        var utmPoint = MapProjects.GeodeticToUTM(new Point(longitude, latitude), Ellipsoids.WGS84, utmZone);

        if (!UtmIndexes._2kUtmBoundingBox.Intersects(utmPoint))
        {
            return null;
        }

        var utm2kSheet = Create2kUtmSheet(longitude, latitude, utmZone);

        var rowColumn = CalculateRowColumn(utm2kSheet.UtmExtent, utmPoint, UtmIndexes._1kUtmSheetWidth, UtmIndexes._1kUtmSheetHeight);

        return Create1kSheet(utm2kSheet, rowColumn.Row, rowColumn.Column);
    }

    public static UtmSheet? Create500UtmSheet(double longitude, double latitude, int utmZone)
    {
        var utmPoint = MapProjects.GeodeticToUTM(new Point(longitude, latitude), Ellipsoids.WGS84, utmZone);

        if (!UtmIndexes._2kUtmBoundingBox.Intersects(utmPoint))
        {
            return null;
        }

        var utm1kSheet = Create1kUtmSheet(longitude, latitude, utmZone);

        var rowColumn = CalculateRowColumn(utm1kSheet.UtmExtent, utmPoint, UtmIndexes._500UtmSheetWidth, UtmIndexes._500UtmSheetHeight);

        return Create500Sheet(utm1kSheet, rowColumn.Row, rowColumn.Column);
    }


    //From row/column
    private static UtmSheet Create2kBlock(int row, int column, int utmZone)
    {
        UtmSheet result = new UtmSheet()
        {
            Row = row,
            Column = column,
            UtmZone = utmZone,
            SheetName = $"{utmZone}{_2kUtmBlockColumns[column]}{row + 1:00}",
            Type = UtmIndexType.Ncc2kBlock,
            UtmExtent = CreateBound(row, column, UtmIndexes._2kUtmXmin, UtmIndexes._2kUtmYmax, UtmIndexes._2kUtmBlockWidth, UtmIndexes._2kUtmBlockHeight) 
        };

        return result;
    }

    private static UtmSheet Create2kSheet(UtmSheet utm2kBlock, int row, int column)
    {
        if (utm2kBlock.Type != UtmIndexType.Ncc2kBlock)
            throw new NotImplementedException();

        UtmSheet result = new UtmSheet()
        {
            Row = row,
            Column = column,
            UtmZone = utm2kBlock.UtmZone,
            SheetName = $"{utm2kBlock.SheetName}{_2kUtmSheetColumns[column]}{row + 1:00}",
            Type = UtmIndexType.Ncc2kSheet,
            UtmExtent = CreateBound(row, column, utm2kBlock.UtmExtent.XMin, utm2kBlock.UtmExtent.YMax, UtmIndexes._2kUtmSheetWidth, UtmIndexes._2kUtmSheetHeight)
        };

        return result;
    }

    private static UtmSheet Create1kSheet(UtmSheet utm2kSheet, int row, int column)
    {
        if (utm2kSheet.Type != UtmIndexType.Ncc2kSheet)
            throw new NotImplementedException();

        UtmSheet result = new UtmSheet()
        {
            Row = row,
            Column = column,
            UtmZone = utm2kSheet.UtmZone,
            SheetName = $"{utm2kSheet.SheetName}{RowColumnToSheetNumber(row, column)}",
            Type = UtmIndexType.Ncc1k,
            UtmExtent = CreateBound(row, column, utm2kSheet.UtmExtent.XMin, utm2kSheet.UtmExtent.YMax, UtmIndexes._1kUtmSheetWidth, UtmIndexes._1kUtmSheetHeight)
        };

        return result;
    }

    private static UtmSheet Create500Sheet(UtmSheet utm1kSheet, int row, int column)
    {
        if (utm1kSheet.Type != UtmIndexType.Ncc1k)
            throw new NotImplementedException();

        UtmSheet result = new UtmSheet()
        {
            Row = row,
            Column = column,
            UtmZone = utm1kSheet.UtmZone,
            SheetName = $"{utm1kSheet.SheetName}{RowColumnToSheetNumber(row, column)}",
            Type = UtmIndexType.Ncc500,
            UtmExtent = CreateBound(row, column, utm1kSheet.UtmExtent.XMin, utm1kSheet.UtmExtent.YMax, UtmIndexes._500UtmSheetWidth, UtmIndexes._500UtmSheetHeight)
        };

        return result;
    }
      
    private static BoundingBox CreateBound(int row, int column, double xMin, double yMax, double width, double height)
    {
        return new BoundingBox(xMin + column * width,
                                 yMax - (row + 1) * height,
                                 xMin + (column + 1) * width,
                                 yMax - row * height);
    }

    private static int RowColumnToSheetNumber(int row, int column)
    {
        if (row == 0 && column == 1)
            return 1;
         
        else if (row == 1 && column == 1)
            return 2;
         
        else if (row == 1 && column == 0)
            return 3;
         
        else if (row == 0 && column == 0)
            return 4;
         
        else
            throw new NotImplementedException();
    }

    private static RowColumn CalculateRowColumn(BoundingBox utmBoundingBox, Point utmPoint, double columnWidth, double rowHeight)
    {
        //if (!utmBoundingBox.Intersects(utmPoint))
        //{
        //    return null;
        //}

        var column = (int)Math.Floor((utmPoint.X - utmBoundingBox.XMin) / columnWidth);

        var row = (int)Math.Floor((utmBoundingBox.YMax - utmPoint.Y) / rowHeight);

        return new RowColumn() { Row = row, Column = column };
    }
}
