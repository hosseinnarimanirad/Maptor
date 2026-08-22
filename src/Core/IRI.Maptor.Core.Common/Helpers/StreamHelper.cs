using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace IRI.Maptor.Core.Common.Helpers;

public static class StreamHelper
{
    private const int Int32Length = 4;

    private const int DoubleLength = 8;


    public static void Serialize(object value, string path)
    {
        BinaryFormatter formatter = new BinaryFormatter();

        using (Stream fStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            formatter.Serialize(fStream, value);
        }
    }

    public static object Deserialize(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            throw new NotImplementedException();
        }

        BinaryFormatter formatter = new BinaryFormatter();

        using (Stream fStream = File.OpenRead(path))
        {
            return formatter.Deserialize(fStream);
        }
    }

    public static byte[] Serialize<T>(T structure)
    {
        using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
        {
            BinaryFormatter formatter = new BinaryFormatter();

            formatter.Serialize(stream, structure);

            stream.Flush();

            return stream.ToArray();
        }
    }

    public static T Deserialize<T>(byte[] buffer)
    {
        BinaryFormatter formatter = new BinaryFormatter();

        using (System.IO.MemoryStream stream = new System.IO.MemoryStream(buffer))
        {
            return (T)formatter.Deserialize(stream);
        }
    }

    public static byte[] StructureToByteArray<T>(T structure)
    {
        int len = Marshal.SizeOf(structure);

        byte[] result = new byte[len];

        IntPtr ptr = Marshal.AllocHGlobal(len);

        //if fDeleteOld is set to `true` then it may throw exception!
        Marshal.StructureToPtr(structure, ptr, false);

        Marshal.Copy(ptr, result, 0, len);

        Marshal.DestroyStructure(ptr, typeof(T));

        Marshal.FreeHGlobal(ptr);

        return result;
    }

    public static T ByteArrayToStructure<T>(byte[] buffer)
    {
        //int len = Marshal.SizeOf(typeof(T));
        int length = buffer.Length;

        IntPtr i = Marshal.AllocHGlobal(length);

        Marshal.Copy(buffer, 0, i, length);

        T result = (T)Marshal.PtrToStructure(i, typeof(T));

        Marshal.FreeHGlobal(i);

        return result;
    }

    public static T ParseToStructure<T>(byte[] buffer) where T : struct
    {
        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

        T result = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));

        handle.Free();

        return result;
    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="byteRepresentation"></param>
    /// <param name="startIndex">A 32bit integer that represents the index in the sourceArray at which the copying begins.</param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static int BigEndianOrderedBytesToInt32(byte[] bigEndianOrderedBytesRepresentation)
    {
        Array.Reverse(bigEndianOrderedBytesRepresentation);

        return System.BitConverter.ToInt32(bigEndianOrderedBytesRepresentation, 0);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="byteRepresentation"></param>
    /// <param name="startIndex">A 32bit integer that represents the index in the sourceArray at which the copying begins.</param>
    /// <returns></returns>
    public static double BigEndianOrderedBytesToDouble(byte[] bigEndianOrderedBytesRepresentation)
    {
        Array.Reverse(bigEndianOrderedBytesRepresentation);

        return System.BitConverter.ToDouble(bigEndianOrderedBytesRepresentation, 0);
    }

    public static byte[] Int32ToBigEndianOrderedBytes(int int32Representation)
    {
        byte[] result = System.BitConverter.GetBytes(int32Representation);

        Array.Reverse(result);

        return result;
    }

    public static byte[] DoubleToBigEndianOrderedBytes(double doubleRepresentation)
    {
        byte[] result = System.BitConverter.GetBytes(doubleRepresentation);

        Array.Reverse(result);

        return result;
    }

    public static int LittleEndianOrderedBytesToInt32(byte[] source, int startIndex)
    {
        byte[] temp = new byte[Int32Length];

        Array.ConstrainedCopy(source, startIndex, temp, 0, Int32Length);

        return System.BitConverter.ToInt32(temp, 0);
    }

    public static double LittleEndianOrderedBytesToDouble(byte[] source, int startIndex)
    {
        byte[] temp = new byte[DoubleLength];

        Array.ConstrainedCopy(source, startIndex, temp, 0, DoubleLength);

        return System.BitConverter.ToDouble(temp, 0);
    }

    public static byte[] ToByteArray(Stream stream)
    {
        byte[] bytes;

        using (var memoryStream = new MemoryStream())
        {
            stream.CopyTo(memoryStream);

            bytes = memoryStream.ToArray();
        }

        return bytes;
    }

    public static string ToString(Stream stream, Encoding encoding)
    {
        //return encoding.GetString(ToByteArray(stream));

        string result;

        using (StreamReader reader = new StreamReader(stream))
        {
            result = reader.ReadToEnd();
        }

        return result;
    }
}
