using System;
using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Sta.Common.Model;

namespace IRI.Maptor.Sta.Persistence.RasterDataSources;

//Note: Extent is NaN for this class
public class ImagePyramidDataSource : OfflineGoogleMapDataSource
{
    public ImagePyramidDataSource(string directory, Func<int, int, int, string>? makeFileName = null) : base(new List<ImageSource>())
    {
        var availableZoomLevels = System.IO.Directory.EnumerateDirectories(directory, "*.*", System.IO.SearchOption.TopDirectoryOnly).ToList();

        if (makeFileName == null)
        {
            makeFileName = (r, c, z) => $"{directory}\\{z}\\{z}, {r}, {c}.jpg";
        }

        var sources = new List<ImageSource>();

        foreach (var zoomLevelDirectory in availableZoomLevels)
        {
            int zoom;

            var folderName = System.IO.Path.GetFileName(zoomLevelDirectory);

            int.TryParse(folderName, out zoom);

            ImageSources.Add(new ImageSource(zoom, makeFileName));
        }
    }


}
