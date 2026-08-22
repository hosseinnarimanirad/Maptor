using System;
using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Model;
using IRI.Maptor.Core.Persistence.Abstractions;
using IRI.Maptor.Core.Persistence.Model;

namespace IRI.Maptor.Core.Persistence.RasterDataSources;

//Note: Extent is NaN for this class
public class ImagePyramidDataSource : OfflineGoogleMapDataSource
{
    private string _directory;

    public override string SourceAddress => $"Image pyramid: {_directory}";

    public override SourceLocation? Location => new DirectoryLocation { Path = _directory };

    public ImagePyramidDataSource(string directory, Func<int, int, int, string>? makeFileName = null) : base(new List<ImageSource>())
    {
        _directory = directory;

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
