using System;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Common.Helpers;

public static class ZipArchiveHelper
{
    public static string ReadAsString(string zippedArchiveFile, string fileName)
    {
        var archive = System.IO.Compression.ZipFile.OpenRead(zippedArchiveFile);

        var file = archive.Entries.Where(i => i.FullName.Equals(fileName, StringComparison.OrdinalIgnoreCase));

        if (!(file?.Count() == 1))
        {
            return null;
        }

        var stream = file.First().Open();

        return  StreamHelper.ToString(stream, Encoding.UTF8);

    }


   
}
