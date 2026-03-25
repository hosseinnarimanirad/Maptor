using System.Collections.Generic;

using IRI.Maptor.Jab.Common.Models;

namespace IRI.Maptor.Jab.Common.Abstractions;

public interface IMapExtentBookmarkStore
{
    string? FilePath { get; set; }

    IReadOnlyList<MapExtentBookmark> Load();

    void Save(IReadOnlyList<MapExtentBookmark> bookmarks);
}
