namespace IRI.Maptor.Core.FileGdb.IO;

internal static class GdbStreamExtensions
{
    public static byte[] ReadExactly(this Stream stream, long position, int count, string path)
    {
        var buffer = new byte[count];
        stream.ReadExactly(position, buffer, count, path);
        return buffer;
    }

    /// <summary>
    /// Reads exactly <paramref name="count"/> bytes at <paramref name="position"/> or throws
    /// <see cref="InvalidDataException"/> naming the file.
    /// </summary>
    public static void ReadExactly(this Stream stream, long position, byte[] buffer, int count, string path)
    {
        stream.Position = position;

        int read = 0;
        while (read < count)
        {
            int n = stream.Read(buffer, read, count - read);

            if (n == 0)
                throw new InvalidDataException($"{path}: unexpected end of file at position {position + read}.");

            read += n;
        }
    }
}
