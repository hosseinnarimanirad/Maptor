using IRI.Maptor.Core.Common.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace IRI.Maptor.Core.ShapefileFormat.Dbf;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct DbfFieldDescriptor
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 11)]
    private string name;            //Field Name (ASCII)

    public string Name => name;

    private char type;              //Field Type (ASCII): C, N, L, D, M, F, B, G, P, ...

    public char Type => type;

    private Int32 dataAddress;     //Field Data Address

    public Int32 DataAddress => dataAddress;

    private byte length;            //Field Length: depends on Field Type; Max = 255

    public byte Length => length;

    private byte decimalCount;      //Decimal Count: Numeric <= 15

    public byte DecimalCount => decimalCount;

    private Int16 reserved1;        //Reserved for Multi-user dBASE

    private byte workAreaId;        //Work Area ID

    private Int16 reserved2;        //Reserved for Multi-user dBASE

    private byte flagForSetFields; //Flag for Set Fields

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
    private byte[] reserved3;

    private byte indexFieldFlag;    //Index Field Flag: 00h-> No key for this field, 01h-> Key exists for this field (in MDX)

    public DbfFieldDescriptor(string name, char type, byte length, byte decimalCount)
    {
        this.name = name;

        this.type = type;

        this.dataAddress = 0;

        this.length = length;

        this.decimalCount = decimalCount;

        this.reserved1 = 0;

        this.workAreaId = 0;

        this.reserved2 = 0;

        this.flagForSetFields = 0;

        this.reserved3 = new byte[7];

        this.indexFieldFlag = 0;
    }

    public static DbfFieldDescriptor Parse(byte[] byteArray, Encoding encoding)
    {
        string name = encoding.GetString(byteArray, 0, 11).Trim('\0');

        //char type = BitConverter.ToChar(byteArray, 11);
        char type = char.Parse(Encoding.ASCII.GetString(byteArray, 11, 1));

        byte length = byteArray[16];

        byte decimalCount = byteArray[17];

        return new DbfFieldDescriptor(name, type, length, decimalCount);
    }

    public override string ToString()
    {
        return $"Name: {Name}; Type: {Type}; Length: {Length}";
    }

    public void UpdateName(string newName)
    {
        this.name = newName;
    }


    public Field AsField() => new Field()
    {
        Alias = this.Name,
        Name = this.Name,
        Length = this.Length,
        Precision = this.DecimalCount,
        Scale = this.Length,
        TypeFullName = DbfFile.GetType(this).FullName/*.ToString()*/,       
    };
}
