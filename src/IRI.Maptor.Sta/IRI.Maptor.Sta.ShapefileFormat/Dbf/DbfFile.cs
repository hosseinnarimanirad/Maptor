using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using IRI.Maptor.Sta.ShapefileFormat.Model;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Sta.ShapefileFormat.Dbf;

public static class DbfFile
{

    //private static Dictionary<char, Func<byte[], object>> _mapFunctions;

    internal const int _arabicWindowsEncoding = 1252;

    internal static Encoding _arabicEncoding = Encoding.GetEncoding(_arabicWindowsEncoding);

    internal static Encoding _currentEncoding = Encoding.GetEncoding(_arabicWindowsEncoding);

    internal static Encoding _fieldsEncoding = Encoding.GetEncoding(_arabicWindowsEncoding);

    internal static bool _correctFarsiCharacters = true;

    static DbfFile()
    {
        //InitializeMapFunctions();
    }

    //private static void InitializeMapFunctions()
    //{
    //    _mapFunctions = DbfFieldMappings.GetMappingFunctions();
    //}

    //private static byte[] GetBytes(string value, byte[] array, Encoding encoding)
    //{
    //    var truncatedString = value;

    //    var length = array.Length;

    //    if (encoding.GetByteCount(value) > length)
    //    {
    //        //Truncate Scenario
    //        truncatedString = new string(value.TakeWhile((c, i) => encoding.GetByteCount(value.Substring(0, i + 1)) < length).ToArray());

    //        System.Diagnostics.Trace.WriteLine("Truncation occured in writing the dbf file");
    //        System.Diagnostics.Trace.WriteLine($"Original String: {value}");
    //        System.Diagnostics.Trace.WriteLine($"Truncated String: {truncatedString}");
    //        System.Diagnostics.Trace.WriteLine($"Lost String: {value.Replace(truncatedString, string.Empty)}");
    //        System.Diagnostics.Trace.WriteLine(string.Empty);
    //    }

    //    encoding.GetBytes(truncatedString, 0, truncatedString.Length, array, 0);

    //    //Encoder en = encoding.GetEncoder().Convert(, 0, 0, null, 0, 0,, 0);
    //    //Consider using the Encoder.Convert method instead of GetByteCount.
    //    //The conversion method converts as much data as possible, and does 
    //    //throw an exception if the output buffer is too small.For continuous 
    //    //encoding of a stream, this method is often the best choice.

    //    return array;
    //}


    private static short GetRecordLength(List<DbfFieldDescriptor> columns)
    {
        short result = 0;

        foreach (var item in columns)
        {
            result += item.Length;
        }

        result += 1; //Deletion Flag

        return result;
    }

    public static void ChangeEncoding(Encoding newEncoding)
    {
        _currentEncoding = newEncoding;

        //DbfFieldMappings.ChangeEncoding(newEncoding);

        //InitializeMapFunctions();
    }

    public static List<DbfFieldDescriptor> GetDbfSchema(string dbfFileName)
    {
        System.IO.Stream stream = new System.IO.FileStream(dbfFileName, System.IO.FileMode.Open);

        System.IO.BinaryReader reader = new System.IO.BinaryReader(stream);

        byte[] buffer = reader.ReadBytes(Marshal.SizeOf(typeof(DbfHeader)));

        DbfHeader header = IRI.Maptor.Sta.Common.Helpers.StreamHelper.ByteArrayToStructure<DbfHeader>(buffer);

        List<DbfFieldDescriptor> columns = new List<DbfFieldDescriptor>();

        if ((header.LengthOfHeader - 33) % 32 != 0) { throw new NotImplementedException(); }

        int numberOfFields = (header.LengthOfHeader - 33) / 32;

        for (int i = 0; i < numberOfFields; i++)
        {
            buffer = reader.ReadBytes(Marshal.SizeOf(typeof(DbfFieldDescriptor)));

            columns.Add(IRI.Maptor.Sta.Common.Helpers.StreamHelper.ParseToStructure<DbfFieldDescriptor>(buffer));
        }

        reader.Close();

        stream.Close();

        return columns;
    }

    public static List<DbfFieldDescriptor> GetDbfSchema(string dbfFileName, Encoding encoding)
    {
        System.IO.Stream stream = new System.IO.FileStream(dbfFileName, System.IO.FileMode.Open);

        System.IO.BinaryReader reader = new System.IO.BinaryReader(stream);

        byte[] buffer = reader.ReadBytes(Marshal.SizeOf(typeof(DbfHeader)));

        DbfHeader header = IRI.Maptor.Sta.Common.Helpers.StreamHelper.ByteArrayToStructure<DbfHeader>(buffer);

        List<DbfFieldDescriptor> columns = new List<DbfFieldDescriptor>();

        if ((header.LengthOfHeader - 33) % 32 != 0) { throw new NotImplementedException(); }

        int numberOfFields = (header.LengthOfHeader - 33) / 32;

        for (int i = 0; i < numberOfFields; i++)
        {
            buffer = reader.ReadBytes(Marshal.SizeOf(typeof(DbfFieldDescriptor)));

            columns.Add(DbfFieldDescriptor.Parse(buffer, encoding));
        }

        reader.Close();

        stream.Close();

        return columns;
    }


    public static Encoding TryDetectEncoding(string dbfFileName)
    {
        var cpgFile = Shapefile.GetCpgFileName(dbfFileName);

        if (!System.IO.File.Exists(cpgFile))
        {
            return null;
        }

        var encodingText = System.IO.File.ReadAllText(cpgFile);

        if (encodingText?.ToUpper()?.Trim() == "UTF-8" || encodingText?.ToUpper()?.Trim() == "UTF8")
        {
            return Encoding.UTF8;
        }
        else if (encodingText?.Contains("1256") == true)
        {
            //return Dbf.DbfFile._arabicEncoding;
            return DbfFile._arabicEncoding;
        }
        else
        {
            return null;
        }
    }

    //public static List<Dictionary<string, object>> Read(string dbfFileName, bool correctFarsiCharacters = true, Encoding dataEncoding = null, Encoding fieldHeaderEncoding = null)
    public static EsriAttributeDictionary Read(
        string dbfFileName,
        bool correctFarsiCharacters = true,
        Encoding dataEncoding = null,
        Encoding fieldHeaderEncoding = null)
    {
        dataEncoding = dataEncoding ?? (TryDetectEncoding(dbfFileName) ?? Encoding.UTF8);

        ChangeEncoding(dataEncoding);

        //if (tryDetectEncoding)
        //{
        //    Encoding encoding = TryDetectEncoding(dbfFileName) ?? dataEncoding;

        //    ChangeEncoding(encoding);
        //}
        //else
        //{
        //    ChangeEncoding(dataEncoding);
        //}

        DbfFile._fieldsEncoding = fieldHeaderEncoding ?? DbfFile._arabicEncoding;

        DbfFile._correctFarsiCharacters = correctFarsiCharacters;

        System.IO.Stream stream = new System.IO.FileStream(dbfFileName, System.IO.FileMode.Open);

        System.IO.BinaryReader reader = new System.IO.BinaryReader(stream);

        byte[] buffer = reader.ReadBytes(Marshal.SizeOf(typeof(DbfHeader)));

        DbfHeader header = IRI.Maptor.Sta.Common.Helpers.StreamHelper.ByteArrayToStructure<DbfHeader>(buffer);

        List<DbfFieldDescriptor> fields = new List<DbfFieldDescriptor>();

        if ((header.LengthOfHeader - 33) % 32 != 0) { throw new NotImplementedException(); }

        int numberOfFields = (header.LengthOfHeader - 33) / 32;

        for (int i = 0; i < numberOfFields; i++)
        {
            buffer = reader.ReadBytes(Marshal.SizeOf(typeof(DbfFieldDescriptor)));

            fields.Add(DbfFieldDescriptor.Parse(buffer, DbfFile._fieldsEncoding));
        }

        fields = fields.Where(c => c.Length != 0).ToList();

        var _mapFunctions = DbfFieldMappings.GetMappingFunctions(_currentEncoding, _correctFarsiCharacters);

        //System.Data.DataTable result = MakeTableSchema(tableName, columns);

        var attributes = new List<Dictionary<string, object>>(header.NumberOfRecords);

        ((FileStream)reader.BaseStream).Seek(header.LengthOfHeader, SeekOrigin.Begin);

        for (int i = 0; i < header.NumberOfRecords; i++)
        {
            // First we'll read the entire record into a buffer and then read each field from the buffer
            // This helps account for any extra space at the end of each record and probably performs better
            buffer = reader.ReadBytes(header.LengthOfEachRecord);
            BinaryReader recordReader = new BinaryReader(new MemoryStream(buffer));

            // All dbf field records begin with a deleted flag field. Deleted - 0x2A (asterisk) else 0x20 (space)
            if (recordReader.ReadChar() == '*')
            {
                continue;
            }

            Dictionary<string, object> values = new Dictionary<string, object>();

            for (int j = 0; j < fields.Count; j++)
            {
                int fieldLenth = fields[j].Length;

                //values[j] = MapFunction[columns[j].Type](recordReader.ReadBytes(fieldLenth));
                values.Add(fields[j].Name, _mapFunctions[fields[j].Type](recordReader.ReadBytes(fieldLenth)));
            }

            recordReader.Close();

            attributes.Add(values);
        }

        reader.Close();

        stream.Close();

        return new EsriAttributeDictionary(attributes, fields);
    }

    public static object[][] ReadToObject(
        string dbfFileName,
        bool correctFarsiCharacters = true,
        Encoding dataEncoding = null,
        Encoding fieldHeaderEncoding = null)
    {
        dataEncoding = dataEncoding ?? (TryDetectEncoding(dbfFileName) ?? Encoding.UTF8);

        ChangeEncoding(dataEncoding);

        DbfFile._fieldsEncoding = fieldHeaderEncoding ?? _arabicEncoding;

        DbfFile._correctFarsiCharacters = correctFarsiCharacters;


        System.IO.Stream stream = new System.IO.FileStream(dbfFileName, System.IO.FileMode.Open);

        System.IO.BinaryReader reader = new System.IO.BinaryReader(stream);

        byte[] buffer = reader.ReadBytes(Marshal.SizeOf(typeof(DbfHeader)));

        DbfHeader header = IRI.Maptor.Sta.Common.Helpers.StreamHelper.ByteArrayToStructure<DbfHeader>(buffer);

        List<DbfFieldDescriptor> columns = new List<DbfFieldDescriptor>();

        if ((header.LengthOfHeader - 33) % 32 != 0) { throw new NotImplementedException(); }

        int numberOfFields = (header.LengthOfHeader - 33) / 32;

        for (int i = 0; i < numberOfFields; i++)
        {
            buffer = reader.ReadBytes(Marshal.SizeOf(typeof(DbfFieldDescriptor)));

            columns.Add(DbfFieldDescriptor.Parse(buffer, DbfFile._fieldsEncoding));
        }

        columns = columns.Where(c => c.Length != 0).ToList();

        var _mapFunctions = DbfFieldMappings.GetMappingFunctions(_currentEncoding, _correctFarsiCharacters);

        //System.Data.DataTable result = MakeTableSchema(tableName, columns);
        var result = new object[header.NumberOfRecords][];

        ((FileStream)reader.BaseStream).Seek(header.LengthOfHeader, SeekOrigin.Begin);

        for (int i = 0; i < header.NumberOfRecords; i++)
        {
            // First we'll read the entire record into a buffer and then read each field from the buffer
            // This helps account for any extra space at the end of each record and probably performs better
            buffer = reader.ReadBytes(header.LengthOfEachRecord);
            BinaryReader recordReader = new BinaryReader(new MemoryStream(buffer));

            // All dbf field records begin with a deleted flag field. Deleted - 0x2A (asterisk) else 0x20 (space)
            if (recordReader.ReadChar() == '*')
            {
                continue;
            }

            object[] values = new object[columns.Count];

            for (int j = 0; j < columns.Count; j++)
            {
                int fieldLenth = columns[j].Length;

                values[j] = _mapFunctions[columns[j].Type](recordReader.ReadBytes(fieldLenth));
            }

            recordReader.Close();

            result[i] = values;
        }

        reader.Close();

        stream.Close();

        return result;

        //ChangeEncoding(dataEncoding);

        //DbfFile._fieldsEncoding = fieldHeaderEncoding;

        //DbfFile._correctFarsiCharacters = correctFarsiCharacters;

        //return ReadToObject(dbfFileName, tableName);
    }

    //public static object[][] ReadToObject(string dbfFileName, string tableName)
    //{

    //}

    public static void WriteDefault(string dbfFileName, int numberOfRecords, bool overwrite = false)
    {
        List<int> attributes = Enumerable.Range(0, numberOfRecords).ToList();

        List<ObjectToDbfTypeMap<int>> mapping = new List<ObjectToDbfTypeMap<int>>() { new ObjectToDbfTypeMap<int>(DbfFieldDescriptors.GetIntegerField("Id"), i => i) };

        Write(dbfFileName,
            attributes,
            mapping,
            Encoding.ASCII,
            overwrite);
    }

    public static void Write<T>(string dbfFileName,
                                    IEnumerable<T> values,
                                    List<ObjectToDbfTypeMap<T>> mapping,
                                    Encoding encoding,
                                    bool overwrite = false)
    {
        var columns = mapping.Select(m => m.FieldType).ToList();

        int control = 0;
        try
        {
            //if (columns.Count != mapping.Count)
            //{
            //    throw new NotImplementedException();
            //}

            var mode = Shapefile.GetMode(dbfFileName, overwrite);

            System.IO.Stream stream = new System.IO.FileStream(dbfFileName, mode);

            System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream);

            DbfHeader header = new DbfHeader(values.Count(), mapping.Count, GetRecordLength(columns), encoding);

            writer.Write(IRI.Maptor.Sta.Common.Helpers.StreamHelper.StructureToByteArray(header));

            foreach (var item in columns)
            {
                writer.Write(IRI.Maptor.Sta.Common.Helpers.StreamHelper.StructureToByteArray(item));
            }

            //Terminator
            writer.Write(byte.Parse("0D", System.Globalization.NumberStyles.HexNumber));

            for (int i = 0; i < values.Count(); i++)
            {
                control = i;
                // All dbf field records begin with a deleted flag field. Deleted - 0x2A (asterisk) else 0x20 (space)
                writer.Write(byte.Parse("20", System.Globalization.NumberStyles.HexNumber));

                for (int j = 0; j < mapping.Count; j++)
                {
                    // 1400.02.03-comment
                    //byte[] temp = new byte[columns[j].Length];

                    object value = mapping[j].MapFunction(values.ElementAt(i));

                    var temp = DbfFieldMappings.Encode(value, columns[j].Length, encoding);

                    // 1400.02.03-comment
                    //if (value is DateTime dt)
                    //{
                    //    value = dt.ToString("yyyyMMdd");
                    //}

                    //if (value != null)
                    //{
                    //    //encoding.GetBytes(value.ToString(), 0, value.ToString().Length, temp, 0);
                    //    temp = GetBytes(value.ToString(), temp, encoding);
                    //}

                    ////string tt = encoding.GetString(temp);
                    ////var le = tt.Length;
                    writer.Write(temp);
                }
            }

            //End of file
            writer.Write(byte.Parse("1A", System.Globalization.NumberStyles.HexNumber));

            writer.Close();

            stream.Close();

            System.IO.File.WriteAllText(Shapefile.GetCpgFileName(dbfFileName), encoding.BodyName);

        }
        catch (Exception ex)
        {
            string message = ex.Message;

            string m2 = message + " " + control.ToString();

        }
    }

    // 1400.02.03 - remove similar method
    //public static void Write<T>(string dbfFileName,
    //                               IEnumerable<T> values,
    //                               ObjectToDfbFields<T> mapping,
    //                               Encoding encoding,
    //                               bool overwrite = false)
    //{
    //    //Write(dbfFileName, values, mapping.Select(m => m.MapFunction).ToList(), mapping.Select(m => m.FieldType).ToList(), encoding, overwrite);

    //    var columns = mapping.Fields;

    //    int control = 0;

    //    try
    //    {
    //        //if (columns.Count != mapping.Count)
    //        //{
    //        //    throw new NotImplementedException();
    //        //}

    //        //var mode = overwrite ? System.IO.FileMode.Create : System.IO.FileMode.CreateNew;
    //        var mode = Shapefile.GetMode(dbfFileName, overwrite);

    //        System.IO.Stream stream = new System.IO.FileStream(dbfFileName, mode);

    //        System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream);

    //        DbfHeader header = new DbfHeader(values.Count(), columns.Count, GetRecordLength(columns), encoding);

    //        writer.Write(IRI.Maptor.Sta.Common.Helpers.StreamHelper.StructureToByteArray(header));

    //        foreach (var item in columns)
    //        {
    //            writer.Write(IRI.Maptor.Sta.Common.Helpers.StreamHelper.StructureToByteArray(item));
    //        }

    //        //Terminator
    //        writer.Write(byte.Parse("0D", System.Globalization.NumberStyles.HexNumber));

    //        for (int i = 0; i < values.Count(); i++)
    //        {
    //            control = i;
    //            // All dbf field records begin with a deleted flag field. Deleted - 0x2A (asterisk) else 0x20 (space)
    //            writer.Write(byte.Parse("20", System.Globalization.NumberStyles.HexNumber));

    //            var fieldValues = mapping.ExtractAttributesFunc(values.ElementAt(i));

    //            for (int j = 0; j < columns.Count; j++)
    //            {
    //                byte[] temp = new byte[columns[j].Length];

    //                if (fieldValues[j] != null)
    //                {
    //                    //encoding.GetBytes(value.ToString(), 0, value.ToString().Length, temp, 0);
    //                    temp = GetBytes(fieldValues[j]?.ToString(), temp, encoding);
    //                }

    //                //string tt = encoding.GetString(temp);
    //                //var le = tt.Length;
    //                writer.Write(temp);
    //            }
    //        }

    //        //End of file
    //        writer.Write(byte.Parse("1A", System.Globalization.NumberStyles.HexNumber));

    //        writer.Close();

    //        stream.Close();

    //        System.IO.File.WriteAllText(Shapefile.GetCpgFileName(dbfFileName), encoding.BodyName);

    //    }
    //    catch (Exception ex)
    //    {
    //        string message = ex.Message;

    //        string m2 = message + " " + control.ToString();

    //    }
    //}


    public static void Write(string dbfFileName, List<Dictionary<string, object>> attributes, Encoding encoding, bool overwirte = false)
    {
        if (attributes == null || attributes.Count < 1)
        {
            return;
        }

        //make schema
        //var columns = MakeDbfFields(attributes.First());

        //List<ObjectToDbfTypeMap<Dictionary<string, object>>> mapping = new List<ObjectToDbfTypeMap<Dictionary<string, object>>>();

        //var counter = 0;

        //foreach (var item in attributes.First())
        //{
        //    mapping.Add(new ObjectToDbfTypeMap<Dictionary<string, object>>(columns[counter], d => d[item.Key]));
        //}

        // 1400.02.03
        //make schema and mappings
        var mapping = MakeDbfFieldsAndMaps(attributes);

        Write(dbfFileName, attributes, mapping, encoding, overwirte);
    }

    // 1400.02.03-comment
    //public static List<DbfFieldDescriptor> MakeDbfFields(Dictionary<string, object> dictionary)
    //{
    //    List<DbfFieldDescriptor> result = new List<DbfFieldDescriptor>();

    //    foreach (var item in dictionary)
    //    {
    //        result.Add(new DbfFieldDescriptor(item.Key, 'C', 255, 0));
    //    }

    //    return result;
    //}

    // 1400.02.03-comment
    //public static List<ObjectToDbfTypeMap<T>> MakeDbfFieldsAndMaps<T>(IEnumerable<T> values, Func<T, Dictionary<string, object>> extractAttributeFunc)
    //{
    //    var fields = new List<ObjectToDbfTypeMap<T>>();

    //    if (values.IsNullOrEmpty())
    //        return fields;

    //    return MakeDbfFieldsAndMaps

    //    //var firstProperties = extractAttributeFunc(values.First());

    //    //foreach (var item in firstProperties)
    //    //{
    //    //    var propertyName = item.Key;

    //    //    ObjectToDbfTypeMap<T> typeMap = null;

    //    //    var mapFunc = new Func<T, object>(f => extractAttributeFunc(f)[propertyName]);

    //    //    switch (firstProperties[propertyName])
    //    //    {
    //    //        case string property:
    //    //            // گرفتن بیش‌ترین طول
    //    //            var maxLength = (byte)values.Select(f => extractAttributeFunc(f)[propertyName]?.ToString()).Max(val => val == null ? 0 : val.Length);

    //    //            typeMap = new ObjectToDbfTypeMap<T>(DbfFieldDescriptors.GetStringField(propertyName, maxLength), mapFunc);
    //    //            break;

    //    //        case char charProperty:
    //    //        case bool boolProperty:
    //    //            // 1400.02.03: Shapefile does not support boolean field
    //    //            typeMap = new ObjectToDbfTypeMap<T>(DbfFieldDescriptors.GetStringField(propertyName, 1), mapFunc);
    //    //            break;

    //    //        case int intProperty:
    //    //        case short shortProperty:
    //    //        case byte byteProperty:
    //    //        case uint uintProperty:
    //    //        case ushort ushortProperty:
    //    //        case sbyte sbyteProperty:
    //    //            typeMap = new ObjectToDbfTypeMap<T>(DbfFieldDescriptors.GetIntegerField(propertyName), mapFunc);
    //    //            break;

    //    //        case double doubleProperty:
    //    //        case decimal decimalProperty:
    //    //        case float floatProperty:
    //    //            typeMap = new ObjectToDbfTypeMap<T>(DbfFieldDescriptors.GetFloatField(propertyName), mapFunc);
    //    //            break;

    //    //        case long longProperty:
    //    //        case ulong ulongProperty:
    //    //            typeMap = new ObjectToDbfTypeMap<T>(DbfFieldDescriptors.GetFloatFieldForLong(propertyName), mapFunc);
    //    //            break;

    //    //        case DateTime dateTimeProperty:
    //    //            typeMap = new ObjectToDbfTypeMap<T>(DbfFieldDescriptors.GetDateField(propertyName), mapFunc);
    //    //            break;

    //    //        default:
    //    //            typeMap = new ObjectToDbfTypeMap<T>(DbfFieldDescriptors.GetStringField(propertyName), mapFunc);
    //    //            break;
    //    //            //throw new NotImplementedException();
    //    //    }

    //    //    fields.Add(typeMap);
    //    //}

    //    //return fields;
    //}

    public static List<ObjectToDbfTypeMap<Dictionary<string, object>>> MakeDbfFieldsAndMaps(List<Dictionary<string, object>> dictionaries)
    {
        var fields = new List<ObjectToDbfTypeMap<Dictionary<string, object>>>();

        if (dictionaries.IsNullOrEmpty())
        {
            return fields;
        }

        var firstProperties = dictionaries.First();

        foreach (var item in firstProperties)
        {
            var propertyName = item.Key;

            ObjectToDbfTypeMap<Dictionary<string, object>> typeMap = null;

            var mapFunc = new Func<Dictionary<string, object>, object>(f => f[propertyName]);

            switch (firstProperties[propertyName])
            {
                case string property:
                    // گرفتن بیش‌ترین طول
                    var maxLength = (byte)dictionaries.Select(f => f[propertyName]?.ToString()).Max(val => val == null ? 0 : val.Length);

                    typeMap = new ObjectToDbfTypeMap<Dictionary<string, object>>(DbfFieldDescriptors.GetStringField(propertyName, maxLength), mapFunc);
                    break;

                case char charProperty:
                case bool boolProperty:
                    // 1400.02.03: Shapefile does not support boolean field
                    typeMap = new ObjectToDbfTypeMap<Dictionary<string, object>>(DbfFieldDescriptors.GetStringField(propertyName, 1), mapFunc);
                    break;

                case int intProperty:
                case short shortProperty:
                case byte byteProperty:
                case uint uintProperty:
                case ushort ushortProperty:
                case sbyte sbyteProperty:
                    typeMap = new ObjectToDbfTypeMap<Dictionary<string, object>>(DbfFieldDescriptors.GetIntegerField(propertyName), mapFunc);
                    break;

                case double doubleProperty:
                case decimal decimalProperty:
                case float floatProperty:
                    typeMap = new ObjectToDbfTypeMap<Dictionary<string, object>>(DbfFieldDescriptors.GetFloatField(propertyName), mapFunc);
                    break;

                case long longProperty:
                case ulong ulongProperty:
                    typeMap = new ObjectToDbfTypeMap<Dictionary<string, object>>(DbfFieldDescriptors.GetFloatFieldForLong(propertyName), mapFunc);
                    break;

                case DateTime dateTimeProperty:
                    typeMap = new ObjectToDbfTypeMap<Dictionary<string, object>>(DbfFieldDescriptors.GetDateField(propertyName), mapFunc);
                    break;

                default:
                    typeMap = new ObjectToDbfTypeMap<Dictionary<string, object>>(DbfFieldDescriptors.GetStringField(propertyName), mapFunc);
                    break;
                    //throw new NotImplementedException();
            }

            fields.Add(typeMap);
        }

        return fields;
    }

    #region DataTable

    private static List<DbfFieldDescriptor> MakeDbfFields(System.Data.DataColumnCollection columns)
    {
        List<DbfFieldDescriptor> result = new List<DbfFieldDescriptor>();

        foreach (System.Data.DataColumn item in columns)
        {
            result.Add(new DbfFieldDescriptor(item.ColumnName, 'C', 100, 0));
        }

        return result;
    }

    public static System.Data.DataTable MakeTableSchema(string tableName, List<DbfFieldDescriptor> columns)
    {
        System.Data.DataTable result = new System.Data.DataTable(tableName);

        foreach (DbfFieldDescriptor item in columns)
        {
            if (item.Length == 0)
                continue;

            result.Columns.Add(item.Name, GetType(item));             
        }

        return result;
    }

    public static Type GetType(DbfFieldDescriptor descriptor)
    {
        char typeChar = char.ToUpperInvariant(descriptor.Type);

        // Special case for 'N'
        if (typeChar == 'N')
            return descriptor.DecimalCount == 0 ? typeof(int) : typeof(double);

        // Lookup table for other types
        return typeChar switch
        {
            'F' or 'O' or '+' => typeof(double),
            'I' => typeof(int),
            'Y' => typeof(decimal),
            'L' => typeof(bool),
            'D' or 'T' or '@' => typeof(DateTime),
            'M' or 'B' or 'P' => typeof(byte[]),

            //case 'C':
            //case 'G':
            //case 'V':
            //case 'X':
            _ => typeof(string)
        };
    }

    //Read
    public static System.Data.DataTable Read(
        string dbfFileName,
        string tableName,
        Encoding dataEncoding,
        Encoding fieldHeaderEncoding,
        bool correctFarsiCharacters)
    {
        ChangeEncoding(dataEncoding);

        DbfFile._fieldsEncoding = fieldHeaderEncoding;

        DbfFile._correctFarsiCharacters = correctFarsiCharacters;

        return Read(dbfFileName, tableName);
    }

    public static System.Data.DataTable Read(string dbfFileName, string tableName)
    {
        System.IO.Stream stream = new System.IO.FileStream(dbfFileName, System.IO.FileMode.Open);

        System.IO.BinaryReader reader = new System.IO.BinaryReader(stream);

        byte[] buffer = reader.ReadBytes(Marshal.SizeOf(typeof(DbfHeader)));

        DbfHeader header = IRI.Maptor.Sta.Common.Helpers.StreamHelper.ByteArrayToStructure<DbfHeader>(buffer);

        List<DbfFieldDescriptor> columns = new List<DbfFieldDescriptor>();

        if ((header.LengthOfHeader - 40) % 32 != 0) { throw new NotImplementedException(); }

        int numberOfFields = (header.LengthOfHeader - 40) / 32;

        for (int i = 0; i < numberOfFields; i++)
        {
            buffer = reader.ReadBytes(Marshal.SizeOf(typeof(DbfFieldDescriptor)));

            columns.Add(DbfFieldDescriptor.Parse(buffer, DbfFile._fieldsEncoding));
        }

        columns = columns.Where(c => c.Length != 0).ToList();

        var mapFunctions = DbfFieldMappings.GetMappingFunctions(_currentEncoding, _correctFarsiCharacters);

        System.Data.DataTable result = MakeTableSchema(tableName, columns);

        ((FileStream)reader.BaseStream).Seek(header.LengthOfHeader, SeekOrigin.Begin);

        for (int i = 0; i < header.NumberOfRecords; i++)
        {
            // First we'll read the entire record into a buffer and then read each field from the buffer
            // This helps account for any extra space at the end of each record and probably performs better
            buffer = reader.ReadBytes(header.LengthOfEachRecord);

            BinaryReader recordReader = new BinaryReader(new MemoryStream(buffer));

            // All dbf field records begin with a deleted flag field. Deleted - 0x2A (asterisk) else 0x20 (space)
            if (recordReader.ReadChar() == '*')
            {
                continue;
            }

            object[] values = new object[columns.Count];

            for (int j = 0; j < columns.Count; j++)
            {
                int fieldLenth = columns[j].Length;

                values[j] = mapFunctions[columns[j].Type](recordReader.ReadBytes(fieldLenth));
            }

            recordReader.Close();

            result.Rows.Add(values);
        }

        reader.Close();

        stream.Close();

        return result;
    }

    //Write
    public static void Write(string fileName, System.Data.DataTable table, Encoding encoding, bool overwrite = false)
    {
        var mode = Shapefile.GetMode(fileName, overwrite);

        System.IO.Stream stream = new System.IO.FileStream(fileName, mode);

        System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream);

        List<DbfFieldDescriptor> columns = MakeDbfFields(table.Columns);

        DbfHeader header = new DbfHeader(table.Rows.Count, table.Columns.Count, GetRecordLength(columns), encoding);

        writer.Write(IRI.Maptor.Sta.Common.Helpers.StreamHelper.StructureToByteArray(header));

        foreach (var item in columns)
        {
            writer.Write(IRI.Maptor.Sta.Common.Helpers.StreamHelper.StructureToByteArray(item));
        }

        //Terminator
        writer.Write(byte.Parse("0D", System.Globalization.NumberStyles.HexNumber));

        for (int i = 0; i < table.Rows.Count; i++)
        {
            // All dbf field records begin with a deleted flag field. Deleted - 0x2A (asterisk) else 0x20 (space)
            writer.Write(byte.Parse("20", System.Globalization.NumberStyles.HexNumber));

            for (int j = 0; j < table.Columns.Count; j++)
            {
                // 1400.02.03-comment
                //byte[] temp = new byte[columns[j].Length];

                string value = table.Rows[i][j].ToString().Trim();

                ////encoding.GetBytes(value, 0, value.Length, temp, 0);
                ////writer.Write(temp);

                // 1400.02.03-comment
                //writer.Write(GetBytes(value, temp, encoding));

                writer.Write(DbfFieldMappings.Encode(value, columns[j].Length, encoding));
            }
        }

        //End of file
        writer.Write(byte.Parse("1A", System.Globalization.NumberStyles.HexNumber));

        writer.Close();

        stream.Close();
    }

    #endregion
}