using System.Collections.Generic;

namespace IRI.Maptor.Presentation.Wpf.Models.MapExtentBookmarks;

public interface IMapExtentBookmarkStore
{
    string? FilePath { get; set; }

    IReadOnlyList<MapExtentBookmark> Load();

    void Save(IReadOnlyList<MapExtentBookmark> bookmarks);
}
