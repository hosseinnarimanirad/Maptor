using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.ShapefileFormat.Dbf;

public static class DbfFieldDescriptors
{
    private const byte doubleLength = 19;

    private const byte doubleDecimalCount = 11;

    private const byte integerLength = 9;

    private const byte dateLength = 8;

    private const byte booleanLength = 1;

    private const byte maxStringLength = 255;


    // ******************************
    // 1400.02.03        
    // default mappings in ArcMap
    // ******************************
    //
    // short integer:   'N' (5,0)
    // long integer:    'N' (10,0)  ; ~ less than 2B
    // double:          'F' (19,11)
    // float:           'F' (13,11)
    // text:            'C' (<255,0)
    // Date:            'D' (8,0) 


    /// <summary>
    /// 1400.02.03 Double field is not supported in Shapefile
    /// </summary>
    /// <param name="fieldName"></param>
    /// <returns></returns>
    public static DbfFieldDescriptor GetFloatField(string fieldName)
    {
        return new DbfFieldDescriptor(fieldName, (char)DbfFieldType.FloatingPoint, doubleLength, doubleDecimalCount);
    }

    public static DbfFieldDescriptor GetFloatFieldForLong(string fieldName)
    {
        return new DbfFieldDescriptor(fieldName, (char)DbfFieldType.FloatingPoint, doubleLength, 0);
    }

    public static DbfFieldDescriptor GetIntegerField(string fieldName)
    {
        return new DbfFieldDescriptor(fieldName, (char)DbfFieldType.Number, integerLength, 0);
    }

    public static DbfFieldDescriptor GetStringField(string fieldName)
    {
        return GetStringField(fieldName, maxStringLength);
    }

    public static DbfFieldDescriptor GetStringField(string fieldName, byte length)
    {
        // 1400.02.03
        // not need to check max value: (byte)Math.Max(maxStringLength, length) 
        // because byte cannot exceed 255
        //
        return new DbfFieldDescriptor(fieldName, (char)DbfFieldType.Character, length, 0);
    }

    /// <summary>
    /// 1400.02.03 Boolean field is not supported in Shapefile
    /// </summary>
    /// <param name="fieldName"></param>
    /// <returns></returns>
    public static DbfFieldDescriptor GetBooleanField(string fieldName)
    {
        return new DbfFieldDescriptor(fieldName, (char)DbfFieldType.Logical, booleanLength, 0);
    }

    public static DbfFieldDescriptor GetDateField(string fieldName)
    {
        return new DbfFieldDescriptor(fieldName, (char)DbfFieldType.DateTime, dateLength, 0);
    }
}
