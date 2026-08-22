using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;


using IRI.Maptor.Core.Common.Model;
using IRI.Maptor.Core.Spatial.Model;
using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.DataSources;
using IRI.Maptor.Core.Persistence.Abstractions;
using IRI.Maptor.Core.Persistence.Model;
using IRI.Maptor.Core.Common.Enums;
using System.IO;

namespace IRI.Maptor.Core.Persistence.RasterDataSources;

public class ZippedImagePyramidDataSource : RasterDataSource
{
    public const string _extentFileName = "extent.json";

    //private BoundingBox _webMercatorExtent;
    //public BoundingBox WebMercatorExtent
    //{
    //    get => _webMercatorExtent;
    //    private set => _webMercatorExtent = value;
    //}

    //public int Srid => SridHelper.WebMercator;

    public override SourceLocation? Location => new FileLocation { Path = _worldFilePyramidFileName };

    public override DataSourceKind DataSourceKind => DataSourceKind.ZippedImagePyramid;

    string _worldFilePyramidFileName;

    System.IO.Compression.ZipArchive _archive;

    Func<int, int, int, string> _fileNameRule;

    Func<string, TileInfo> _inverseFunction;

    Func<int, int, int, string> _defaultFileNameRule = (zoom, row, column) => System.IO.Path.Combine(zoom.ToString(), $"{zoom}, {row}, {column}.jpg");

    Func<string, TileInfo> _defaultInverseFunction = fileName =>
    {
        try
        {
            var fileParts = fileName.Split('\\')[1].Split(new string[] { ",", ".jpg" }, StringSplitOptions.RemoveEmptyEntries);

            var zoom = int.Parse(fileParts[0]?.Trim());

            var row = int.Parse(fileParts[1]?.Trim());

            var column = int.Parse(fileParts[2]?.Trim());

            return new TileInfo(row, column, zoom);
        }
        catch (Exception ex)
        {
            return null;
        }
    };

    public ZippedImagePyramidDataSource(string worldFilePyramidFileName, Func<int, int, int, string> fileNameRule = null, Func<string, TileInfo> inverseFunction = null)
    {
        if (!System.IO.File.Exists(worldFilePyramidFileName))
            throw new NotImplementedException();

        _worldFilePyramidFileName = worldFilePyramidFileName;

        _archive = System.IO.Compression.ZipFile.OpenRead(worldFilePyramidFileName);

        if (fileNameRule == null)
            //_fileNameRule = (z, r, c) => System.IO.Path.Combine(z.ToString(), $"{z}, {r}, {c}.jpg");
            _fileNameRule = _defaultFileNameRule;
        else
            _fileNameRule = fileNameRule;


        if (inverseFunction == null)
        {
            _inverseFunction = _defaultInverseFunction;
        }
        else
        {
            _inverseFunction = inverseFunction;
        }

        var extentString = ZipFileHelper.ReadAsString(_archive, _extentFileName);

        if (extentString is null || extentString.Contains("NaN"))
        {
            _archive.Dispose();

            _archive = System.IO.Compression.ZipFile.Open(worldFilePyramidFileName, System.IO.Compression.ZipArchiveMode.Update);

            var lastPyramid = _archive.Entries.Max(e =>
                {
                    var folder = e.FullName.Replace($"\\{e.Name}", string.Empty);
                    int zoom = -1;
                    int.TryParse(folder, out zoom);
                    return zoom;
                }
            );

            WebMercatorExtent = BoundingBox.GetMergedBoundingBox(
                            _archive.Entries.Where(e => e.FullName.StartsWith(lastPyramid.ToString()))
                                            .Select(e => _inverseFunction(e.FullName).WebMercatorExtent), true);

            ZipFileHelper.WriteString(_archive, JsonHelper.Serialize(WebMercatorExtent), _extentFileName);

            _archive.Dispose();

            _archive = System.IO.Compression.ZipFile.OpenRead(worldFilePyramidFileName);
        }
        else
        {
            WebMercatorExtent = JsonHelper.Deserialize<BoundingBox>(extentString);
        }


    }

    public List<GeoReferencedImage> GetTiles(BoundingBox geographicBoundingBox, double mapScale)
    {
        //94.12.17
        //int zoomLevel = GetZoomLevel(mapScale);
        int zoomLevel = WebMercatorUtility.GetZoomLevel(mapScale);

        var result = new List<GeoReferencedImage>();

        //What if there were no imagesource for this zoom level
        if (!_archive.Entries.Any(i => i.FullName.StartsWith(zoomLevel.ToString(), StringComparison.OrdinalIgnoreCase)))
        {
            return result;
        }

        var lowerLeft = WebMercatorUtility.LatLonToImageNumber(geographicBoundingBox.YMin, geographicBoundingBox.XMin, zoomLevel);

        var upperRight = WebMercatorUtility.LatLonToImageNumber(geographicBoundingBox.YMax, geographicBoundingBox.XMax, zoomLevel);

        if (upperRight.IsNaN() || lowerLeft.IsNaN())
        {
            return result;
        }

        for (int i = (int)lowerLeft.X; i <= upperRight.X; i++)
        {
            for (int j = (int)upperRight.Y; j <= lowerLeft.Y; j++)
            {

                if (_archive.Entries.Any(e => e.FullName.Equals(_fileNameRule(zoomLevel, j, i)/*.Replace("\\", "/")*/, StringComparison.OrdinalIgnoreCase)))
                {
                    var stream = _archive.Entries.Single(e => e.FullName.Equals(_fileNameRule(zoomLevel, j, i)/*.Replace("\\", "/")*/, StringComparison.OrdinalIgnoreCase)).Open();

                    byte[] bytes = StreamHelper.ToByteArray(stream);

                    result.Add(new GeoReferencedImage(bytes, WebMercatorUtility.GetWgs84ImageBoundingBox(j, i, zoomLevel)));
                }
            }
        }

        //System.Diagnostics.Trace.WriteLine(string.Format("{0} Images founded; zoom level = {1}", result.Count, zoomLevel));

        return result;
    }

}
