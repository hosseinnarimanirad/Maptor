using System;
using System.IO;
using System.Windows;

namespace IRI.Maptor.Presentation.Wpf.Helpers;

public static class ResourceHelper
{
    /// <summary>
    /// Get byte array of a file with build action=Resource
    /// </summary>
    /// <param name="fileAddress">e.g. @"IRI.Maptor.Presentation.Wpf;component/Assets/Images/BaseMaps/satellite.jpg" </param>
    /// <returns></returns>
    public static byte[] ReadBinaryStreamFromResource(string fileAddress)
    { 
        Uri uri = new Uri(fileAddress, UriKind.Relative);

        Stream stream = Application.GetResourceStream(uri).Stream;

        using (var memoryStream = new MemoryStream())
        {
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }

    }


    /// <summary>
    /// Get string of a file with build action=Resource
    /// </summary>
    /// <param name="fileAddress">e.g. @"Restaurant;component/Asset/Images/myImage.png"  </param>
    /// <returns></returns>
    public static string ReadAllTextFromResource(string fileAddress)
    {
        return new StreamReader(Application.GetResourceStream(new Uri(fileAddress, UriKind.Relative)).Stream).ReadToEnd();
    }
}
