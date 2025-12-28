using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Sta.Spatial.MapIndexes;

public static class GeodeticIndexes
{
    #region Constants

    internal const double _250kWidth = 1.5;

    internal const double _250kHeight = 1.0;

    //size: width and height (degree)
    internal const double _100kSize = 0.5;

    //size: width and height (degree)
    internal const double _50kSize = 0.25;

    //size: width and height (degree)
    internal const double _25kSize = 0.125;

    internal const double _10kWidth = 5.0 / 60.0;

    internal const double _10kHeight = 3.0 / 60.0;

    internal const double _5kWidth = 2.5 / 60.0;

    internal const double _5kHeight = 1.5 / 60.0;



    #endregion


    #region Get SheetNames (LAT/LONG > SHEET NAME)

    private static string Get100kSheetName(double minLongitude, double minLatitude)
    {
        return FormattableString.Invariant($"{minLongitude * 2 - 40}{minLatitude * 2 - 10}");
    }

    private static string Get50kSheetName(double minLongitude, double minLatitude)
    {
        var sheet100k = GetIndexSheet(minLongitude, minLatitude, GeodeticIndexType.Ncc100k);

        var center100k = sheet100k.GeodeticExtent.Center;

        if (minLongitude == sheet100k.GeodeticExtent.XMin)
        {
            if (minLatitude == sheet100k.GeodeticExtent.YMin)
            {
                return FormattableString.Invariant($"{sheet100k.SheetName} III");
            }
            else if (minLatitude == center100k.Y)
            {
                return FormattableString.Invariant($"{sheet100k.SheetName} IV");
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else if (minLongitude == center100k.X)
        {
            if (minLatitude == sheet100k.GeodeticExtent.YMin)
            {
                return FormattableString.Invariant($"{sheet100k.SheetName} II");
            }
            else if (minLatitude == center100k.Y)
            {
                return FormattableString.Invariant($"{sheet100k.SheetName} I");
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private static string Get25kSheetName(double minLongitude, double minLatitude)
    {
        var sheet50k = GetIndexSheet(minLongitude, minLatitude, GeodeticIndexType.Ncc50k);

        var center50k = sheet50k.GeodeticExtent.Center;

        if (minLongitude == sheet50k.GeodeticExtent.XMin)
        {
            if (minLatitude == sheet50k.GeodeticExtent.YMin)
            {
                return FormattableString.Invariant($"{sheet50k.SheetName} SW");
            }
            else if (minLatitude == center50k.Y)
            {
                return FormattableString.Invariant($"{sheet50k.SheetName} NW");
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else if (minLongitude == center50k.X)
        {
            if (minLatitude == sheet50k.GeodeticExtent.YMin)
            {
                return FormattableString.Invariant($"{sheet50k.SheetName} SE");
            }
            else if (minLatitude == center50k.Y)
            {
                return FormattableString.Invariant($"{sheet50k.SheetName} NE");
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private static string Get10kSheetName(double minLongitude, double minLatitude)
    {
        var sheet50k = GetIndexSheet(minLongitude, minLatitude, GeodeticIndexType.Ncc50k);

        //  K   L   M
        //1
        //2
        //3
        //4
        //5
        //************

        var xKcenter = sheet50k.GeodeticExtent.XMin + _10kWidth / 2.0;
        var xLcenter = sheet50k.GeodeticExtent.XMin + _10kWidth + _10kWidth / 2.0;
        var xMcenter = sheet50k.GeodeticExtent.XMax - _10kWidth / 2.0;

        //var yMin5 = sheet50k.Extent.YMin;
        //var yMin4 = sheet50k.Extent.YMin + 1 * _10kHeight;
        //var yMin3 = sheet50k.Extent.YMin + 2 * _10kHeight;
        //var yMin2 = sheet50k.Extent.YMin + 3 * _10kHeight;
        //var yMin1 = sheet50k.Extent.YMin + 4 * _10kHeight;

        //row is between 1 and 5
        var row = 5 - Math.Round((minLatitude - sheet50k.GeodeticExtent.YMin) / _10kHeight);

        //var column = xKmin.AreEqual(minLongitude) ? "K" : xLmin.AreEqual(minLongitude) ? "L" : xMmin.AreEqual(minLongitude) ? "M" : throw new Exception();
        var column = minLongitude < xKcenter ? "K" : minLongitude < xLcenter ? "L" : minLongitude < xMcenter ? "M" : throw new ArgumentException();

        return FormattableString.Invariant($"{sheet50k.SheetName} {column}{row}");
    }

    private static string Get5kSheetName(double minLongitude, double minLatitude)
    {
        var sheet25k = GetIndexSheet(minLongitude, minLatitude, GeodeticIndexType.Ncc25k);

        //  A   B   C
        //1
        //2
        //3
        //4
        //5
        //************

        //var xAmin = sheet25k.GeodeticExtent.XMin;
        //var xBmin = sheet25k.GeodeticExtent.XMin + _5kWidth;
        //var xCmin = sheet25k.GeodeticExtent.XMax - _5kWidth;

        var xAcenter = sheet25k.GeodeticExtent.XMin + _5kWidth / 2.0;
        var xBcenter = sheet25k.GeodeticExtent.XMin + _5kWidth + _5kWidth / 2.0;
        var xCcenter = sheet25k.GeodeticExtent.XMax - _5kWidth / 2.0;

        //var yMin5 = sheet50k.Extent.YMin;
        //var yMin4 = sheet50k.Extent.YMin + 1 * _10kHeight;
        //var yMin3 = sheet50k.Extent.YMin + 2 * _10kHeight;
        //var yMin2 = sheet50k.Extent.YMin + 3 * _10kHeight;
        //var yMin1 = sheet50k.Extent.YMin + 4 * _10kHeight;

        //row is between 1 and 5
        var row = 5 - Math.Round((minLatitude - sheet25k.GeodeticExtent.YMin) / _5kHeight);

        //var column = xAmin.AreEqual(minLongitude) ? "A" : xBmin.AreEqual(minLongitude) ? "B" : xCmin.AreEqual(minLongitude) ? "C" : throw new Exception();
        var column = minLongitude < xAcenter ? "A" : minLongitude < xBcenter ? "B" : minLongitude < xCcenter ? "C" : throw new ArgumentException();

        return FormattableString.Invariant($"{sheet25k.SheetName} {column}{row}");
    }

    #endregion


    #region Get IndexSheet by name or lat/long (LAT/LONG OR NAME > IndexSheet)

    public static GeodeticSheet Get100kIndexSheet(string ncc100kSheetName)
    {
        if (!string.IsNullOrWhiteSpace(ncc100kSheetName) && ncc100kSheetName.Length == 4)
        {
            double firstPart, secondPart;

            if (double.TryParse(ncc100kSheetName.Substring(0, 2), out firstPart))
            {
                if (double.TryParse(ncc100kSheetName.Substring(2, 2), out secondPart))
                {
                    var xMin = (firstPart + 40) / 2.0;

                    var yMin = (secondPart + 10) / 2.0;

                    return new GeodeticSheet(new BoundingBox(xMin, yMin, xMin + _100kSize, yMin + _100kSize), GeodeticIndexType.Ncc100k)
                    {
                        SheetName = ncc100kSheetName,
                    };

                }
            }
        }

        return null;
    }

    public static GeodeticSheet GetIndexSheet(double longitude, double latitude, GeodeticIndexType type)
    {
        switch (type)
        {
            case GeodeticIndexType.Ncc100k:
                return GetIndexSheet(longitude, latitude, type, _100kSize, _100kSize, Get100kSheetName);

            case GeodeticIndexType.Ncc50k:
                return GetIndexSheet(longitude, latitude, type, _50kSize, _50kSize, Get50kSheetName);

            case GeodeticIndexType.Ncc25k:
                return GetIndexSheet(longitude, latitude, type, _25kSize, _25kSize, Get25kSheetName);

            case GeodeticIndexType.Ncc10k:
                return GetIndexSheet(longitude, latitude, type, _10kWidth, _10kHeight, Get10kSheetName);

            case GeodeticIndexType.Ncc5k:
                return GetIndexSheet(longitude, latitude, type, _5kWidth, _5kHeight, Get5kSheetName);

            case GeodeticIndexType.Ncc250k:
            default:
                throw new NotImplementedException();
        }
    }

    private static GeodeticSheet GetIndexSheet(double longitude, double latitude, GeodeticIndexType type, double width, double height, Func<double, double, string> namingFunc)
    {
        var minLongitude = Math.Floor(longitude / width) * width;

        var minLatitude = Math.Floor(latitude / height) * height;

        return new GeodeticSheet(new BoundingBox(minLongitude, minLatitude, minLongitude + width, minLatitude + height), type)
        {
            SheetName = namingFunc(minLongitude, minLatitude)
        };
    }

    //public static IndexSheet Get100kIndexSheet(double longitude, double latitude)
    //{
    //    var minLongitude = Math.Floor(longitude / _100kSize) * _100kSize;

    //    var minLatitude = Math.Floor(latitude / _100kSize) * _100kSize;

    //    return new IndexSheet(new BoundingBox(minLongitude, minLatitude, minLongitude + _100kSize, minLatitude + _100kSize), NccIndexType.Ncc100k)
    //    {
    //        SheetName = Get100kSheetName(minLongitude, minLatitude)
    //    };
    //}

    //public static IndexSheet Get50kIndexSheet(double longitude, double latitude)
    //{
    //    var minLongitude = Math.Floor(longitude / _50kSize) * _50kSize;

    //    var minLatitude = Math.Floor(latitude / _50kSize) * _50kSize;

    //    return new IndexSheet(new BoundingBox(minLongitude, minLatitude, minLongitude + _50kSize, minLatitude + _50kSize), NccIndexType.Ncc50k)
    //    {
    //        SheetName = Get50kSheetName(minLongitude, minLatitude)
    //    };
    //}

    //public static IndexSheet Get25kIndexSheet(double longitude, double latitude)
    //{
    //    var minLongitude = Math.Floor(longitude / _25kSize) * _25kSize;

    //    var minLatitude = Math.Floor(latitude / _25kSize) * _25kSize;

    //    return new IndexSheet(new BoundingBox(minLongitude, minLatitude, minLongitude + _25kSize, minLatitude + _25kSize), NccIndexType.Ncc25k)
    //    {
    //        SheetName = Get25kSheetName(minLongitude, minLatitude)
    //    };
    //}

    //public static IndexSheet Get10kIndexSheet(double longitude, double latitude)
    //{
    //    var minLongitude = Math.Floor(longitude / _10kWidth) * _10kWidth;

    //    var minLatitude = Math.Floor(latitude / _10kHeight) * _10kHeight;

    //    return new IndexSheet(new BoundingBox(minLongitude, minLatitude, minLongitude + _10kWidth, minLatitude + _10kHeight), NccIndexType.Ncc10k)
    //    {
    //        SheetName = Get10kSheetName(minLongitude, minLatitude)
    //    };
    //}

    //public static IndexSheet Get5kIndexSheet(double longitude, double latitude)
    //{
    //    var minLongitude = Math.Floor(longitude / _5kWidth) * _5kWidth;

    //    var minLatitude = Math.Floor(latitude / _5kHeight) * _5kHeight;

    //    return new IndexSheet(new BoundingBox(minLongitude, minLatitude, minLongitude + _5kWidth, minLatitude + _5kHeight), NccIndexType.Ncc5k)
    //    {
    //        SheetName = Get5kSheetName(minLongitude, minLatitude)
    //    };
    //}

    //2k, 1k, 500

    //public static IndexSheet Get2kIndexBlock(double longitude, double latitude, int utmZone)
    //{
    //    var utmSheet = UtmSheet.Create2kUtmBlock(longitude, latitude, utmZone);

    //    return new IndexSheet(utmSheet.GeodeticExtent, NccIndexType.NccUtmBased2kBlock) { SheetName = utmSheet.Title };
    //}

    //public static UtmSheet Get2kUtmBlock(double longitude, double latitude, int utmZone)
    //{
    //    return UtmSheet.Create2kUtmBlock(longitude, latitude, utmzone);
    //    //var utmPoint = MapProjects.GeodeticToUTM(new Point(longitude, latitude), Ellipsoids.WGS84, utmZone);

    //    //if (!_2kUtmBoudingBox.Intersects(utmPoint))
    //    //{
    //    //    return null;
    //    //}

    //    //var column = (int)Math.Floor((utmPoint.X - _2kUtmXmin) / _2kUtmBlockWidth);

    //    //var row = (int)Math.Floor((_2kUtmYmax - utmPoint.Y) / _2kUtmBlockHeight);

    //    //return UtmSheet.Create2kBlock(row, column, utmZone);

    //    //return new IndexSheet(geoBound, NccIndexType.NccUtmBased2kBlock) { SheetName = $"{utmZone.ToString("00")}{column}{row.ToString("00")}" };
    //}

    //public static UtmSheet Get2kUtmSheet(double longitude, double latitude, int utmZone)
    //{
    //    var utmPoint = MapProjects.GeodeticToUTM(new Point(longitude, latitude), Ellipsoids.WGS84, utmZone);

    //    if (!_2kUtmBoudingBox.Intersects(utmPoint))
    //    {
    //        return null;
    //    }

    //    var blockColumnRow = CalculateRowColumn(_2kUtmBoudingBox, utmPoint, _2kUtmBlockWidth, _2kUtmBlockHeight);

    //    var blockBound = CalculateUtm2kBlockBoundingBox((int)blockColumnRow.Y, (int)blockColumnRow.X);

    //    var sheetColumnRow = CalculateRowColumn(blockBound, utmPoint, _2kUtmSheetWidth, _2kUtmSheetHeight);

    //    var geoBound = CalculateUtm2kBlockBoundingBox((int)sheetColumnRow.Y, (int)sheetColumnRow.X)
    //                    .Transform(p => MapProjects.UTMToGeodetic(p, utmZone));

    //    return new UtmSheet(geoBound, NccIndexType.NccUtmBased2kSheet)
    //    {
    //        SheetName = $"{utmZone.ToString("00")}{_2kUtmBlockColumns[(int)blockColumnRow.X]}{blockColumnRow.Y.ToString("00")}{_2kUtmSheetColumns[(int)sheetColumnRow.X]}{sheetColumnRow.Y.ToString("00")}"
    //    };
    //}


    //private static BoundingBox CalculateUtm2kBlockBoundingBox(int row, int column)
    //{
    //    return new BoundingBox(_2kUtmXmin + column * _2kUtmBlockWidth,
    //                                _2kUtmYmax - (row + 1) * _2kUtmBlockHeight,
    //                                _2kUtmXmin + (column + 1) * _2kUtmBlockWidth,
    //                                _2kUtmYmax - row * _2kUtmBlockHeight);
    //}

    //private static Point CalculateRowColumn(BoundingBox utmBoundingBox, Point utmPoint, double columnWidth, double rowHeight)
    //{
    //    if (!utmBoundingBox.Intersects(utmPoint))
    //    {
    //        return null;
    //    }

    //    var column = Math.Floor((utmPoint.X - utmBoundingBox.XMin) / columnWidth);

    //    var row = Math.Floor((utmBoundingBox.YMax - utmPoint.Y) / rowHeight);

    //    return new Point(column, row);
    //}

    ////for 1:1000 and 1:500 sheets
    //private static int? CalculateMapNumber(BoundingBox utmSheetBoundingBox, Point utmPoint)
    //{
    //    if (!utmSheetBoundingBox.Intersects(utmPoint))
    //        return null;

    //    var center = utmSheetBoundingBox.Center;

    //    if (utmPoint.X >= center.X)
    //    {
    //        if (utmPoint.Y >= center.Y)
    //        {
    //            return 1;
    //        }
    //        else
    //        {
    //            return 2;
    //        }
    //    }
    //    else
    //    {
    //        if (utmPoint.Y >= center.Y)
    //        {
    //            return 4;
    //        }
    //        else
    //        {
    //            return 3;
    //        }
    //    }
    //}

    #endregion


    #region Get IndexSheets in a Region (BoundingBox > List<IndexSheet>)

    public static List<GeodeticSheet> FindIndexSheets(BoundingBox geographicIntersectRegion, GeodeticIndexType type)
    {
        switch (type)
        {
            //no function to calculate 250k sheet names
            case GeodeticIndexType.Ncc250k:
                return FindIndexes(geographicIntersectRegion, _250kWidth, _250kHeight, (minLongitude, minLatitude) => string.Empty, GeodeticIndexType.Ncc250k);

            case GeodeticIndexType.Ncc100k:
                return FindIndexes(geographicIntersectRegion, _100kSize, _100kSize, (minLongitude, minLatitude) => Get100kSheetName(minLongitude, minLatitude), GeodeticIndexType.Ncc100k);

            case GeodeticIndexType.Ncc50k:
                return FindIndexes(geographicIntersectRegion, _50kSize, _50kSize, (minLongitude, minLatitude) => Get50kSheetName(minLongitude, minLatitude), GeodeticIndexType.Ncc50k);

            case GeodeticIndexType.Ncc25k:
                return FindIndexes(geographicIntersectRegion, _25kSize, _25kSize, (minLongitude, minLatitude) => Get25kSheetName(minLongitude, minLatitude), GeodeticIndexType.Ncc25k);

            case GeodeticIndexType.Ncc10k:
                return FindIndexes(geographicIntersectRegion, _10kWidth, _10kHeight, (minLongitude, minLatitude) => Get10kSheetName(minLongitude, minLatitude), GeodeticIndexType.Ncc10k);

            case GeodeticIndexType.Ncc5k:
                return FindIndexes(geographicIntersectRegion, _5kWidth, _5kHeight, (minLongitude, minLatitude) => Get5kSheetName(minLongitude, minLatitude), GeodeticIndexType.Ncc5k);

            default:
                throw new NotImplementedException();
        }
    }

    ////no function to calculate 250k sheet names
    //public static List<GeodeticSheet> Find250kIndexSheets(BoundingBox geographicIntersectRegion)
    //{
    //    return FindIndexes(geographicIntersectRegion, _250kWidth, _250kHeight, (minLongitude, minLatitude) => string.Empty, GeodeticIndexType.Ncc250k);
    //}

    //public static List<GeodeticSheet> Find100kIndexSheets(BoundingBox geographicIntersectRegion)
    //{
    //    return FindIndexes(geographicIntersectRegion, _100kSize, _100kSize, (minLongitude, minLatitude) => Get100kSheetName(minLongitude, minLatitude), GeodeticIndexType.Ncc100k);
    //}

    //public static List<GeodeticSheet> Find50kIndexSheets(BoundingBox geographicIntersectRegion)
    //{
    //    return FindIndexes(geographicIntersectRegion, _50kSize, _50kSize, (minLongitude, minLatitude) => Get50kSheetName(minLongitude, minLatitude), GeodeticIndexType.Ncc50k);
    //}

    //public static List<GeodeticSheet> Find25kIndexSheets(BoundingBox geographicIntersectRegion)
    //{
    //    return FindIndexes(geographicIntersectRegion, _25kSize, _25kSize, (minLongitude, minLatitude) => Get25kSheetName(minLongitude, minLatitude), GeodeticIndexType.Ncc25k);
    //}

    //public static List<GeodeticSheet> Find10kIndexSheets(BoundingBox geographicIntersectRegion)
    //{
    //    return FindIndexes(geographicIntersectRegion, _10kWidth, _10kHeight, (minLongitude, minLatitude) => Get10kSheetName(minLongitude, minLatitude), GeodeticIndexType.Ncc10k);
    //}

    //public static List<GeodeticSheet> Find5kIndexSheets(BoundingBox geographicIntersectRegion)
    //{
    //    return FindIndexes(geographicIntersectRegion, _5kWidth, _5kHeight, (minLongitude, minLatitude) => Get5kSheetName(minLongitude, minLatitude), GeodeticIndexType.Ncc5k);
    //}

    /// <summary>
    /// 
    /// </summary>
    /// <param name="geographicIntersectRegion"></param>
    /// <param name="indexWidth"></param>
    /// <param name="indexHeight"></param>
    /// <param name="namingFunc">Based on Lower Left Coordinate</param>
    /// <returns></returns>
    private static List<GeodeticSheet> FindIndexes(BoundingBox geographicIntersectRegion, double indexWidth, double indexHeight, Func<double, double, string> namingFunc, GeodeticIndexType type)
    {
        if (geographicIntersectRegion.IsNaN())
        {
            geographicIntersectRegion = new BoundingBox(0, -75, 360, 75);
        }

        int startLongitude = (int)Math.Floor(geographicIntersectRegion.XMin / indexWidth);

        int endLongitude = (int)Math.Ceiling(geographicIntersectRegion.XMax / indexWidth);

        int startLatitdue = (int)Math.Floor(geographicIntersectRegion.YMin / indexHeight);

        int endLatitdue = (int)Math.Ceiling(geographicIntersectRegion.YMax / indexHeight);

        List<GeodeticSheet> result = new List<GeodeticSheet>();

        for (int i = startLongitude; i < endLongitude; i++)
        {
            for (int j = startLatitdue; j < endLatitdue; j++)
            {
                var sheetName = namingFunc(i * indexWidth, j * indexHeight);

                result.Add(new GeodeticSheet(new BoundingBox(i * indexWidth, j * indexHeight, (i + 1) * indexWidth, (j + 1) * indexHeight), type)
                {
                    SheetName = sheetName
                });
            }
        }

        return result;
    }

    #endregion


    #region Get Index BoundingBoxes in a Region (BoundingBox > List<BoundingBox>)

    public static List<BoundingBox> Find250kIndexMbbs(BoundingBox geographicIntersectRegion)
    {
        return FindIndexMbbs(geographicIntersectRegion, _250kWidth, _250kHeight);
        //var startLongitude = (int)Math.Floor(geographicIntersectRegion.XMin / _nioc250kWidth);

        //var endLongitude = (int)Math.Ceiling(geographicIntersectRegion.XMax / _nioc250kWidth);

        //var startLatitdue = (int)Math.Floor(geographicIntersectRegion.YMin / _nioc250kHeight);

        //var endLatitdue = (int)Math.Floor(geographicIntersectRegion.YMax / _nioc250kHeight);

        //List<BoundingBox> result = new List<BoundingBox>();

        //for (int i = startLongitude; i < endLongitude; i++)
        //{
        //    for (int j = startLatitdue; j < endLatitdue; j++)
        //    {
        //        result.Add(new BoundingBox(i, j, i + _nioc250kWidth, j + _nioc250kHeight));
        //    }
        //}

        //return result;
    }

    public static List<BoundingBox> Find100kIndexMbbs(BoundingBox geographicIntersectRegion)
    {
        return FindIndexMbbs(geographicIntersectRegion, _100kSize, _100kSize);
    }

    public static List<BoundingBox> Find50kIndexMbbs(BoundingBox geographicIntersectRegion)
    {
        return FindIndexMbbs(geographicIntersectRegion, _50kSize, _50kSize);
    }

    public static List<BoundingBox> Find25kIndexMbbs(BoundingBox geographicIntersectRegion)
    {
        return FindIndexMbbs(geographicIntersectRegion, _25kSize, _25kSize);
    }

    public static List<BoundingBox> Find10kIndexMbbs(BoundingBox geographicIntersectRegion)
    {
        return FindIndexMbbs(geographicIntersectRegion, _10kWidth, _10kHeight);
    }

    public static List<BoundingBox> Find5kIndexMbbs(BoundingBox geographicIntersectRegion)
    {
        return FindIndexMbbs(geographicIntersectRegion, _5kWidth, _5kHeight);
    }

    private static List<BoundingBox> FindIndexMbbs(BoundingBox geographicIntersectRegion, double indexWidth, double indexHeight)
    {
        int startLongitude = (int)Math.Floor(geographicIntersectRegion.XMin / indexWidth);

        int endLongitude = (int)Math.Ceiling(geographicIntersectRegion.XMax / indexWidth);

        int startLatitdue = (int)Math.Floor(geographicIntersectRegion.YMin / indexHeight);

        int endLatitdue = (int)Math.Ceiling(geographicIntersectRegion.YMax / indexHeight);

        List<BoundingBox> result = new List<BoundingBox>();

        for (int i = startLongitude; i < endLongitude; i++)
        {
            for (int j = startLatitdue; j < endLatitdue; j++)
            {
                result.Add(new BoundingBox(i * indexWidth, j * indexHeight, (i + 1) * indexWidth, (j + 1) * indexHeight));
            }
        }

        return result;
    }

    #endregion


    #region Get Index Lines (BoundingBox > List<Geometry>)

    //for 250k, 100k, 50k, 25k, 10k, 5k scales
    public static List<Geometry<Point>> GetIndexLines(BoundingBox geographicIntersectRegion, GeodeticIndexType type)
    {
        switch (type)
        {
            case GeodeticIndexType.Ncc250k:
                return GetIndexLines(geographicIntersectRegion, _250kWidth, _250kHeight);

            case GeodeticIndexType.Ncc100k:
                return GetIndexLines(geographicIntersectRegion, _100kSize, _100kSize);

            case GeodeticIndexType.Ncc50k:
                return GetIndexLines(geographicIntersectRegion, _50kSize, _50kSize);

            case GeodeticIndexType.Ncc25k:
                return GetIndexLines(geographicIntersectRegion, _25kSize, _25kSize);

            case GeodeticIndexType.Ncc10k:
                return GetIndexLines(geographicIntersectRegion, _10kWidth, _10kHeight);

            case GeodeticIndexType.Ncc5k:
                return GetIndexLines(geographicIntersectRegion, _5kWidth, _5kHeight);

            default:
                throw new NotImplementedException();
        }
    }

    private static List<Geometry<Point>> GetIndexLines(BoundingBox geographicIntersectRegion, double indexWidth, double indexHeight)
    {
        int startLongitude = (int)Math.Floor(geographicIntersectRegion.XMin / indexWidth);

        int endLongitude = (int)Math.Ceiling(geographicIntersectRegion.XMax / indexWidth);

        int startLatitdue = (int)Math.Floor(geographicIntersectRegion.YMin / indexHeight);

        int endLatitdue = (int)Math.Ceiling(geographicIntersectRegion.YMax / indexHeight);

        List<Geometry<Point>> result = new List<Geometry<Point>>();

        for (int i = startLongitude; i < endLongitude; i++)
        {
            var p1 = new Point(i * indexWidth, geographicIntersectRegion.YMin);

            var p2 = new Point(i * indexWidth, geographicIntersectRegion.YMax);

            result.Add(Geometry<Point>.Create(new List<Point>() { p1, p2 }, GeometryType.LineString, SridHelper.GeodeticWGS84));
        }

        for (int j = startLatitdue; j < endLatitdue; j++)
        {
            var p1 = new Point(geographicIntersectRegion.XMin, j * indexHeight);

            var p2 = new Point(geographicIntersectRegion.XMax, j * indexHeight);

            result.Add(Geometry<Point>.Create(new List<Point>() { p1, p2 }, GeometryType.LineString, SridHelper.GeodeticWGS84));
        }

        return result;
    }

    #endregion


    #region Generate Indexes

    public static List<Index50k> Generate50kIndexes(BoundingBox geographicIntersectRegion)
    {
        return GenerateIndexes(geographicIntersectRegion, _50kSize, _50kSize, (lng, lat) => new Index50k()
        {
            MinLatitude = lat,
            MinLongitude = lng,
            SheetNameEn = Get50kSheetName(lng, lat)
        });
    }

    public static List<Index25k> Generate25kIndexes(BoundingBox geographicIntersectRegion)
    {
        return GenerateIndexes(geographicIntersectRegion, _25kSize, _25kSize, (lng, lat) => new Index25k()
        {
            MinLatitude = lat,
            MinLongitude = lng,
            SheetNameEn = Get50kSheetName(lng, lat)
        });
    }

    public static List<Index10k> Generate10kIndexes(BoundingBox geographicIntersectRegion)
    {
        return GenerateIndexes(geographicIntersectRegion, _10kWidth, _10kHeight, (lng, lat) => new Index10k()
        {
            MinLatitude = lat,
            MinLongitude = lng,
            SheetNameEn = Get10kSheetName(lng, lat)
        });
    }

    public static List<Index5k> Generate5kIndexes(BoundingBox geographicIntersectRegion)
    {
        return GenerateIndexes(geographicIntersectRegion, _5kWidth, _5kHeight, (lng, lat) => new Index5k()
        {
            MinLatitude = lat,
            MinLongitude = lng,
            SheetNameEn = Get5kSheetName(lng, lat)
        });
    }


    private static List<T> GenerateIndexes<T>(BoundingBox geographicIntersectRegion, double indexWidth, double indexHeight, Func<double, double, T> generateFunc)
    {
        int startLongitude = (int)Math.Floor(geographicIntersectRegion.XMin / indexWidth);

        int endLongitude = (int)Math.Ceiling(geographicIntersectRegion.XMax / indexWidth);

        int startLatitdue = (int)Math.Floor(geographicIntersectRegion.YMin / indexHeight);

        int endLatitdue = (int)Math.Ceiling(geographicIntersectRegion.YMax / indexHeight);

        List<T> result = new List<T>();

        for (int i = startLongitude; i < endLongitude; i++)
        {
            for (int j = startLatitdue; j < endLatitdue; j++)
            {
                result.Add(generateFunc(i * indexWidth, j * indexHeight));
            }
        }

        return result;
    }

    #endregion
}
