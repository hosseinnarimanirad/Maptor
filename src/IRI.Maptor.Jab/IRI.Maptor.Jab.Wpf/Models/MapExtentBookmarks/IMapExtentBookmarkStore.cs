using System.Collections.Generic;

namespace IRI.Maptor.Jab.Wpf.Models.MapExtentBookmarks;

public interface IMapExtentBookmarkStore
{
    string? FilePath { get; set; }

    IReadOnlyList<MapExtentBookmark> Load();

    void Save(IReadOnlyList<MapExtentBookmark> bookmarks);
}
