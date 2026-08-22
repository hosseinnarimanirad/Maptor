using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.Common.Helpers;

public static class ResourceHelper
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="resourceName">ProjectName.FolderName.FileName</param>
    /// <returns></returns>
    public static string ReadAllText(Assembly assembly, string resourceName)
    {
        string result;

        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                //stream.Dispose(); throws null exception

                return null;
            }

            using (StreamReader reader = new StreamReader(stream))
            {
                result = reader.ReadToEnd();
            }
        }

        return result;
    }


}
