using System.Collections.Generic;

namespace IRI.Maptor.Jab.Common.Models.MapExtentBookmarks;

public interface IMapExtentBookmarkStore
{
    string? FilePath { get; set; }

    IReadOnlyList<MapExtentBookmark> Load();

    void Save(IReadOnlyList<MapExtentBookmark> bookmarks);
}
