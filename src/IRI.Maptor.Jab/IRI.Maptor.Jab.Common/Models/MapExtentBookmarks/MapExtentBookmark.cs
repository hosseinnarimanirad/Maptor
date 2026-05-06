using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Jab.Common.Models.MapExtentBookmarks;

public class MapExtentBookmark
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public double XMin { get; set; }

    public double YMin { get; set; }

    public double XMax { get; set; }

    public double YMax { get; set; }

    [JsonIgnore]
    public BoundingBox WebMercatorExtent => new BoundingBox(XMin, YMin, XMax, YMax);

    /// <summary>PNG bytes for the 75×75 thumbnail, stored as base64 in JSON.</summary>
    public byte[]? ThumbnailBytes { get; set; }

    /// <summary>Decoded thumbnail ready for WPF binding. Not serialized.</summary>
    [JsonIgnore]
    public BitmapSource? Thumbnail { get; private set; }

    public bool IsValid()
    {
        return WebMercatorExtent.IsValid() &&
                WebMercatorExtent.Width != 0 &&
                WebMercatorExtent.Height != 0 &&
                ThumbnailBytes != null;

    }

    /// <summary>Decodes <see cref="ThumbnailBytes"/> into <see cref="Thumbnail"/>. Must be called on the UI thread.</summary>
    public void LoadThumbnail()
    {
        if (ThumbnailBytes is null || ThumbnailBytes.Length == 0)
            return;

        var bi = new BitmapImage();
        bi.BeginInit();
        bi.StreamSource = new MemoryStream(ThumbnailBytes);
        bi.CacheOption = BitmapCacheOption.OnLoad;
        bi.EndInit();
        bi.Freeze();
        Thumbnail = bi;
    }

    //public BoundingBox ToBoundingBox() => new(XMin, YMin, XMax, YMax);

    public static MapExtentBookmark FromTitleAndExtent(string title, BoundingBox extent)
    {
        return new MapExtentBookmark
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            XMin = extent.XMin,
            YMin = extent.YMin,
            XMax = extent.XMax,
            YMax = extent.YMax,
        };
    }
}
