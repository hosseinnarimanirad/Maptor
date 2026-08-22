using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.ShapefileFormat.Dbf;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct DbfHeader
{
    [FieldOffset(0)]
    private byte versionNumber;

    public byte VersionNumber
    {
        get { return versionNumber; }
    }

    [FieldOffset(1)]
    private byte yearOfLastUpdate;

    public byte YearOfLastUpdate
    {
        get { return yearOfLastUpdate; }
    }

    [FieldOffset(2)]
    private byte monthOfLastUpdate;

    public byte MonthOfLastUpdate
    {
        get { return monthOfLastUpdate; }
    }

    [FieldOffset(3)]
    private byte dayOfLastUpdate;

    public byte DayOfLastUpdate
    {
        get { return dayOfLastUpdate; }
    }

    [FieldOffset(4)]
    private Int32 numberOfRecords;

    public Int32 NumberOfRecords
    {
        get { return numberOfRecords; }
    }

    [FieldOffset(8)]
    private Int16 lengthOfHeader;

    /// <summary>
    /// Number of bytes in the header
    /// </summary>
    public Int16 LengthOfHeader
    {
        get { return lengthOfHeader; }
    }

    [FieldOffset(10)]
    private Int16 lengthOfEachRecord;

    /// <summary>
    /// Number of bytes in each record
    /// </summary>
    public Int16 LengthOfEachRecord
    {
        get { return lengthOfEachRecord; }
    }

    [FieldOffset(12)]
    private Int16 reserved1;

    public Int16 Reserved1
    {
        get { return reserved1; }
    }

    [FieldOffset(14)]
    private byte incompleteTransaction;

    public byte IncompleteTransaction
    {
        get { return incompleteTransaction; }
    }

    [FieldOffset(15)]
    private byte encryptionFlag;

    public byte EncryptionFlag
    {
        get { return encryptionFlag; }
    }

    [FieldOffset(16)]
    private Int32 freeRecordThread;

    public Int32 FreeRecordThread
    {
        get { return freeRecordThread; }
    }

    [FieldOffset(20)]
    private Int64 reserved2;

    public Int64 Reserved2
    {
        get { return reserved2; }
    }

    [FieldOffset(28)]
    private byte mdxFlag;

    public byte MdxFlag
    {
        get { return mdxFlag; }
    }

    [FieldOffset(29)]
    private byte languageDriver;

    public byte LanguageDriver
    {
        get { return languageDriver; }
    }

    [FieldOffset(30)]
    private Int16 reserved3;

    public Int16 Reserved3
    {
        get { return reserved3; }
    }

    public DbfHeader(int numberOfRecords, int numberOfFields, short recordLength, Encoding encoding)
    {
        this.versionNumber = 3;

        DateTime now = DateTime.Now;

        this.yearOfLastUpdate = (byte)(now.Year - 1900);

        this.monthOfLastUpdate = (byte)now.Month;

        this.dayOfLastUpdate = (byte)now.Day;

        this.numberOfRecords = numberOfRecords;

        this.lengthOfHeader = (short)(numberOfFields * 32 + 33); ;

        this.lengthOfEachRecord = recordLength;

        this.reserved1 = 0;

        this.incompleteTransaction = 0;

        this.encryptionFlag = 0;

        this.freeRecordThread = 0;

        this.reserved2 = 0;

        this.mdxFlag = 0;

        this.languageDriver = byte.Parse((EsriCodePages.EncodingToLanguageIdentifier(encoding)),
                                System.Globalization.NumberStyles.HexNumber);

        this.reserved3 = 0;
    }

}
