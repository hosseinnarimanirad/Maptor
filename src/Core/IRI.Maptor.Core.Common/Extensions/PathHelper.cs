using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Extensions;

public static class PathHelper
{
    public static string CleanFileName(string fileName)
    {
        return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
    }

    public static string CleanDirectoryName(string fileName)
    {
        return Path.GetInvalidPathChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
    }
}
