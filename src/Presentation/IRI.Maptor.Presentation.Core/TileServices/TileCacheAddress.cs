using System;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Model;
using IRI.Maptor.Core.Spatial.Model;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Presentation.Core.TileServices;

public class TileCacheAddress
{
    private string _provider;

    public string Provider
    {
        get { return _provider; }
        set
        {
            _provider = value;
            _url = MakeUrl();
        }
    }

    private string _subTitle;

    public string SubTitle
    {
        get { return _subTitle; }
        set
        {
            _subTitle = value;
            _url = MakeUrl();
        }
    }

    private string _baseDirectory;

    public string BaseDirectory
    {
        get { return _baseDirectory; }
        set
        {
            _baseDirectory = value;
            _url = MakeUrl();
        }
    }

    private string _url;

    public string Url { get { return _url; } }

    private Func<TileInfo, string> _getFileName;

    public TileCacheAddress(string provider, string subTitle, Func<TileInfo, string>? getFileName)
    {
        if (getFileName == null)
        {
            _getFileName = t => $"{t.ZoomLevel}\\{t.RowNumber}\\{t.RowNumber}_{t.ColumnNumber}.png";
        }
        else
        {
            _getFileName = getFileName;
        }

        Provider = provider;
        SubTitle = subTitle;
    }

    private string MakeUrl()
    {
        return $"{BaseDirectory}\\{Provider}\\{SubTitle}";
    }

    private string GetFilePath(TileInfo tile)
    {
        return $"{Url}\\{_getFileName(tile)}";
    }

    public Task<GeoReferencedImage> GetTileAsync(TileInfo tile)
    {
        string filePath = GetFilePath(tile);

        if (File.Exists(filePath))
        {
            return Task.Run(() =>
            {
                try
                {
                    var bytes = File.ReadAllBytes(filePath);
                    return new GeoReferencedImage(bytes, tile.GeodeticExtent);
                }
                catch (Exception)
                {
                    return GeoReferencedImage.NaN;
                }
            });
        }
        else
            return Task.Run(() => { return GeoReferencedImage.NaN; });
    }

    public GeoReferencedImage GetTile(TileInfo tile)
    {
        string filePath = GetFilePath(tile);

        if (File.Exists(filePath))
        {
            var bytes = File.ReadAllBytes(filePath);
            return new GeoReferencedImage(bytes, tile.GeodeticExtent);
        }
        else
            return new GeoReferencedImage(null, BoundingBox.NaN, false);
    }

    public Task SaveAsync(GeoReferencedImage tileImage, TileInfo tile)
    {
        return Task.Run(() =>
        {
            Save(tileImage, tile);
        });
    }

    internal void Save(GeoReferencedImage tileImage, TileInfo tile)
    {
        var filePath = GetFilePath(tile);

        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        }

        try
        {
            File.WriteAllBytes(GetFilePath(tile), tileImage.Image);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }
}
