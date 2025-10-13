using System;
using System.Threading.Tasks;

using IRI.Maptor.Sta.Common.Model;
using IRI.Maptor.Sta.Spatial.Model;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Jab.Common.TileServices;

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
        //return $"{BaseDirectory}\\{Enum.GetName(typeof(MapProviderType), Provider)}\\{Enum.GetName(typeof(TileType), Type)}";
        return $"{BaseDirectory}\\{Provider}\\{SubTitle}";
    }

    private string GetFilePath(TileInfo tile)
    {
        //return $"{Url}\\{tile.ZoomLevel}\\{tile.RowNumber}_{tile.ColumnNumber}.png";
        return $"{Url}\\{_getFileName(tile)}";
    }

    public Task<GeoReferencedImage> GetTileAsync(TileInfo tile)
    {
        string filePath = GetFilePath(tile);

        System.IO.DirectoryInfo dinfo = new System.IO.DirectoryInfo(filePath);

        if (System.IO.File.Exists(filePath))
        {
            return Task.Run(() =>
            {
                try
                {
                    var bytes = System.IO.File.ReadAllBytes(filePath);

                    return new GeoReferencedImage(bytes, tile.GeodeticExtent);
                }
                catch (Exception)
                {
                    return GeoReferencedImage.NaN;
                }
            });
        }
        else
            //return Task.Run(() => { return new GeoReferencedImage(null, BoundingBox.NaN, false); });
            return Task.Run(() => { return GeoReferencedImage.NaN; });
    }

    public GeoReferencedImage GetTile(TileInfo tile)
    {
        string filePath = GetFilePath(tile);

        if (System.IO.File.Exists(filePath))
        {
            var bytes = System.IO.File.ReadAllBytes(filePath);

            return new GeoReferencedImage(bytes, tile.GeodeticExtent);
        }
        else
            return new GeoReferencedImage(null, BoundingBox.NaN, false);
    }

    //POTENTIALLY ERROR PRONE: the process cannot access the file or path because another process is using...
    internal Task SaveAsync(GeoReferencedImage tileImage, TileInfo tile)
    {
        //return Task.Run(() =>
        //{
        //    var filePath = GetFilePath(tile);

        //    if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(filePath)))
        //    {
        //        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
        //    }

        //    System.IO.File.WriteAllBytes(GetFilePath(tile), tileImage.Image);
        //});

        return Task.Run(() =>
        {
            Save(tileImage, tile);
        });
    }

    internal void Save(GeoReferencedImage tileImage, TileInfo tile)
    {

        var filePath = GetFilePath(tile);

        if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(filePath)))
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
        }

        try
        {
            System.IO.File.WriteAllBytes(GetFilePath(tile), tileImage.Image);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }
}
