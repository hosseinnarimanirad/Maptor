// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;

using IRI.Maptor.Sta.ShapefileFormat.EsriType;
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;

namespace IRI.Maptor.Sta.ShapefileFormat.Reader;

public abstract class ShpReader<T> : IEnumerable<T> where T : EsriShapeBase
{
    //private string directoryName;

    //private string fileNameWithoutExtension;

    protected int _srid = 0;

    protected MainFileHeader MainHeader;

    protected System.IO.BinaryReader shpReader;

    /// <summary>
    /// Words
    /// </summary>
    //protected int tempPosition;

    public EsriShapeCollection<T> elements;

    protected ShpReader(string fileName, EsriShapeType type, int srid)
    {
        this._srid = srid;
        //this.directoryName = System.IO.Path.GetDirectoryName(fileName);

        //this.fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);

        if (!Shapefile.CheckAllNeededFilesExists(fileName))
        {
            throw new NotImplementedException();
        }
         
        ShxReader shxFile = new ShxReader(Shapefile.GetShxFileName(fileName));

        using (System.IO.FileStream shpStream = new System.IO.FileStream(fileName, System.IO.FileMode.Open))
        {
            using (this.shpReader = new System.IO.BinaryReader(shpStream))
            {
                this.MainHeader = new MainFileHeader(this.shpReader.ReadBytes(ShapeConstants.MainHeaderLengthInBytes));

                if ((EsriShapeType)this.MainHeader.ShapeType != type)
                {
                    throw new NotImplementedException();
                }

                if (!MainFileHeader.CanBeForTheSameShapefile(this.MainHeader, shxFile.MainHeader))
                {
                    throw new NotImplementedException();
                }

                this.elements = new EsriShapeCollection<T>(this.MainHeader);

                int numberOfRecords = shxFile.NumberOfRecords;

                for (int i = 0; i < numberOfRecords; i++)
                {

                    int offset, contentLength02;

                    shxFile.GetRecord(i, out offset, out contentLength02);

                    shpReader.BaseStream.Position = offset * 2;

                    int recordNumber, contentLength;

                    ReadRecordHeader(out recordNumber, out contentLength);

                    if (contentLength != contentLength02)
                    {
                        System.Diagnostics.Trace.WriteLine("ERROR at contentLength != contentLength02: i=" + i.ToString());
                        //throw new NotImplementedException();
                    }

                    if (contentLength == 2)
                        continue;

                    elements.Add(ReadElement());

                    //4:                length of the record header in words
                    //contentLength:    length of the contents in words
                    //this.tempPosition = this.tempPosition - (ShapeConstants.RecordHeaderLengthInWords + contentLength);
                }

            }
        }

    }

    protected abstract T ReadElement();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="recordNumber"></param>
    /// <param name="contentLength">The length of the record contents section measured in 16-bit words</param>
    protected void ReadRecordHeader(out int recordNumber, out int contentLength)
    {
        byte[] temp;

        temp = shpReader.ReadBytes(ShapeConstants.IntegerSize); Array.Reverse(temp);
        recordNumber = System.BitConverter.ToInt32(temp, 0);

        //The content length for a record is the length of the record contents section measured in
        //16-bit words. Each record, therefore, contributes (4 + content length) 16-bit words
        temp = shpReader.ReadBytes(ShapeConstants.IntegerSize); Array.Reverse(temp);
        contentLength = System.BitConverter.ToInt32(temp, 0);
    }

    #region IEnumerable<T> Members

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < this.elements.Count; i++)
        {
            yield return (T)this.elements[i];
        }
    }

    #endregion

    #region IEnumerable Members

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    #endregion
}
