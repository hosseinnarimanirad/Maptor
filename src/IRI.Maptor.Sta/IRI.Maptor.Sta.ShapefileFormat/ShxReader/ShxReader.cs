// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.ShapefileFormat.Reader;

internal class ShxReader
{
    MainFileHeader mainHeader;

    List<int> Offsets, ContentLengths;

    public MainFileHeader MainHeader
    {
        get { return this.mainHeader; }
    }

    public int NumberOfRecords
    {
        get { return this.Offsets.Count; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="recordNumber">record number is zero based</param>
    /// <param name="offset"></param>
    /// <param name="contentLength"></param>
    public void GetRecord(int recordNumber, out int offset, out int contentLength)
    {
        if (recordNumber > this.NumberOfRecords - 1 || recordNumber < 0)
        {
            throw new NotImplementedException();
        }

        offset = Offsets[recordNumber];

        contentLength = ContentLengths[recordNumber];
    }

    public ShxReader(string shxFileName)
    {
        if (!System.IO.File.Exists(shxFileName))
        {
            throw new NotImplementedException();
        }

        this.Offsets = new List<int>();

        this.ContentLengths = new List<int>();

        System.IO.FileStream stream = new System.IO.FileStream(shxFileName, System.IO.FileMode.Open);

        System.IO.BinaryReader reader = new System.IO.BinaryReader(stream);

        this.mainHeader = new MainFileHeader(reader.ReadBytes(ShapeConstants.MainHeaderLengthInBytes));

        double numberOfRecords = (mainHeader.FileLength - 50) / 4.0;

        for (int i = 0; i < numberOfRecords; i++)
        {
            byte[] temp = reader.ReadBytes(ShapeConstants.IntegerSize); Array.Reverse(temp);

            this.Offsets.Add(System.BitConverter.ToInt32(temp, 0));

            temp = reader.ReadBytes(ShapeConstants.IntegerSize); Array.Reverse(temp);

            this.ContentLengths.Add(System.BitConverter.ToInt32(temp, 0));
        }

        reader.Close();

        stream.Close();
    }
}
