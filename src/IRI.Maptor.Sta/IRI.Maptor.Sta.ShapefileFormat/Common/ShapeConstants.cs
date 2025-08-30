using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.ShapefileFormat;

internal static class ShapeConstants
{
    internal const string shpExtension = "shp";
    
    internal const string shxExtension = "shx";

    internal const string dbfExtension = "dbf";

    /// <summary>
    /// Size is in Bytes
    /// </summary>
    internal const int DoubleSize = 8;

    /// <summary>
    /// Size is in Bytes
    /// </summary>
    internal const int IntegerSize = 4;

    /// <summary>
    /// Length is in Words (1 Word = 2 Bytes)
    /// </summary>
    internal const int MainHeaderLengthInWords = 50;

    internal const int MainHeaderLengthInBytes = 100;

    /// <summary>
    /// Length is in Words (1 Word = 2 Bytes)
    /// </summary>
    internal const int RecordHeaderLengthInWords = 4;

    
    //internal const double NoDataValue = -1.7976931348623157E+308;

    internal const int FileCode = 9994;

    internal const int Version = 1000;

    
    internal const int PointContentLengthInWords = 10;

    internal const int PointMContentLengthInWords = 14;

    internal const int PointZContentLengthInWords = 18;

    
    #region BytePositions

    internal const int FileCodeOffset = 0;

    internal const int FileLengthOffset = 24;

    internal const int VersionOffset = 28;

    internal const int ShapeTypeOffset = 32;

    internal const int XMinOffset = 36;

    internal const int YMinOffset = 44;

    internal const int XMaxOffset = 52;

    internal const int YMaxOffset = 60;

    internal const int ZMinOffset = 68;

    internal const int ZMaxOffset = 76;

    internal const int MMinOffset = 84;

    internal const int MMaxOffset = 92;

    #endregion

}
