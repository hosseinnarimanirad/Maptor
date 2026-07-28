// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Sta.Common.Primitives;


namespace IRI.Maptor.Sta.ShapefileFormat;


/// <summary>
/// Total Length = 100byte
/// </summary>
public struct MainFileHeader
{
    private int m_FileLength, m_Version, m_ShapeType;

    private double m_XMin, m_YMin, m_ZMin, m_MMin, m_XMax, m_YMax, m_ZMax, m_MMax;

    /// <summary>
    /// Total length of the file in 16-bit words (including the fifty
    ///16-bit words that make up the header). (1 Word = 2 Byte)
    /// </summary>
    public int FileLength
    {
        get { return this.m_FileLength; }
    }

    public int Version
    {
        get { return this.m_Version; }
    }

    public EsriShapeType ShapeType
    {
        get { return (EsriShapeType)this.m_ShapeType; }
    }

    public double XMin
    {
        get { return this.m_XMin; }
        internal set { this.m_XMin = value; }
    }

    public double YMin
    {
        get { return this.m_YMin; }
        internal set { this.m_YMin = value; }
    }

    public double ZMin
    {
        get { return this.m_ZMin; }
        internal set { this.m_ZMin = value; }
    }

    public double MMin
    {
        get { return this.m_MMin; }
        internal set { this.m_MMin = value; }
    }

    public double XMax
    {
        get { return this.m_XMax; }
        internal set { this.m_XMax = value; }
    }

    public double YMax
    {
        get { return this.m_YMax; }
        internal set { this.m_YMax = value; }
    }

    public double ZMax
    {
        get { return this.m_ZMax; }
        internal set { this.m_ZMax = value; }
    }

    public double MMax
    {
        get { return this.m_MMax; }
        internal set { this.m_MMax = value; }
    }

   
    public BoundingBox MinimumBoundingBox
    {
        get { return new BoundingBox(xMin: XMin, yMin: YMin, xMax: XMax, yMax: YMax); }
    }

    public MainFileHeader(byte[] values)
    {
        if (values.Length != ShapeConstants.MainHeaderLengthInBytes)
        {
            throw new NotImplementedException();
        }

        byte[] temp = new byte[ShapeConstants.IntegerSize];

        Array.ConstrainedCopy(values, ShapeConstants.FileCodeOffset, temp, 0, ShapeConstants.IntegerSize); Array.Reverse(temp);

        int fileCode = System.BitConverter.ToInt32(temp, 0);

        if (fileCode != ShapeConstants.FileCode)
        {
            throw new NotImplementedException();
        }

        Array.ConstrainedCopy(values, ShapeConstants.FileLengthOffset, temp, 0, ShapeConstants.IntegerSize); Array.Reverse(temp);

        this.m_FileLength = System.BitConverter.ToInt32(temp, 0);

        this.m_Version = IRI.Maptor.Sta.Common.Helpers.StreamHelper.LittleEndianOrderedBytesToInt32(values, ShapeConstants.VersionOffset);

        this.m_ShapeType = IRI.Maptor.Sta.Common.Helpers.StreamHelper.LittleEndianOrderedBytesToInt32(values, ShapeConstants.ShapeTypeOffset);

        this.m_XMin = IRI.Maptor.Sta.Common.Helpers.StreamHelper.LittleEndianOrderedBytesToDouble(values, ShapeConstants.XMinOffset);

        this.m_YMin = IRI.Maptor.Sta.Common.Helpers.StreamHelper.LittleEndianOrderedBytesToDouble(values, ShapeConstants.YMinOffset);

        this.m_XMax = IRI.Maptor.Sta.Common.Helpers.StreamHelper.LittleEndianOrderedBytesToDouble(values, ShapeConstants.XMaxOffset);

        this.m_YMax = IRI.Maptor.Sta.Common.Helpers.StreamHelper.LittleEndianOrderedBytesToDouble(values, ShapeConstants.YMaxOffset);

        this.m_ZMin = IRI.Maptor.Sta.Common.Helpers.StreamHelper.LittleEndianOrderedBytesToDouble(values, ShapeConstants.ZMinOffset);

        this.m_ZMax = IRI.Maptor.Sta.Common.Helpers.StreamHelper.LittleEndianOrderedBytesToDouble(values, ShapeConstants.ZMaxOffset);

        this.m_MMin = IRI.Maptor.Sta.Common.Helpers.StreamHelper.LittleEndianOrderedBytesToDouble(values, ShapeConstants.MMinOffset);

        this.m_MMax = IRI.Maptor.Sta.Common.Helpers.StreamHelper.LittleEndianOrderedBytesToDouble(values, ShapeConstants.MMaxOffset);
    }

    internal MainFileHeader(int fileLength, EsriShapeType type, BoundingBox minimumBoundingBox)
    {
        this.m_Version = 9994;

        this.m_FileLength = fileLength;

        this.m_ShapeType = (int)type;

        this.m_XMin = minimumBoundingBox.XMin;

        this.m_YMin = minimumBoundingBox.YMin;

        this.m_XMax = minimumBoundingBox.XMax;

        this.m_YMax = minimumBoundingBox.YMax;

        this.m_ZMax = 0;

        this.m_ZMin = 0;

        this.m_MMax = 0;

        this.m_MMin = 0;
    }

    public int TotalFileLengthInBytes
    {
        get { return this.FileLength * 2; }
    }

    //private static int GetLittleEndianIntegerValue(byte[] source, int offset)
    //{
    //    byte[] temp = new byte[ShapeConstants.IntegerSize];

    //    Array.ConstrainedCopy(source, offset, temp, 0, ShapeConstants.IntegerSize);

    //    return System.BitConverter.ToInt32(temp, 0);
    //}

    //private static double GetLittleEndianDoubleValue(byte[] source, int offset)
    //{
    //    byte[] temp = new byte[ShapeConstants.DoubleSize];

    //    Array.ConstrainedCopy(source, offset, temp, 0, ShapeConstants.DoubleSize);

    //    return System.BitConverter.ToDouble(temp, 0);
    //}

    public static bool CanBeForTheSameShapefile(MainFileHeader firstHeader, MainFileHeader secondHeader)
    {
        return
                firstHeader.MMax.Equals(secondHeader.MMax) &&
                firstHeader.MMin.Equals(secondHeader.MMin) &&
                firstHeader.ShapeType.Equals(secondHeader.ShapeType) &&
                firstHeader.Version.Equals(secondHeader.Version) &&
                firstHeader.XMax.Equals(secondHeader.XMax) &&
                firstHeader.XMin.Equals(secondHeader.XMin) &&
                firstHeader.YMax.Equals(secondHeader.YMax) &&
                firstHeader.YMin.Equals(secondHeader.YMin) &&
                firstHeader.ZMax.Equals(secondHeader.ZMax) &&
                firstHeader.ZMin.Equals(secondHeader.ZMin);

        //bool result = (double.NaN.Equals(double.NaN));
    }

    //public static MainFileHeader CalculateHeader(IShapeCollection collection)
    //{
    //    double Xmin = MapStatistics.GetMinX(collection);
    //    double Xmax = MapStatistics.GetMaxX(collection);
    //    double Ymin = MapStatistics.GetMinY(collection);
    //    double Ymax = MapStatistics.GetMaxY(collection);
    //    double Mmin = MapStatistics.GetMinM(collection);
    //    double Mmax = MapStatistics.GetMaxM(collection);
    //    double minZ = MapStatistics.GetMinZ(collection);
    //    double maxZ = MapStatistics.GetMaxZ(collection);
    //}
}
