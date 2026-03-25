using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Common.Models;

namespace IRI.Maptor.Jab.Common.Helpers;

public sealed class JsonMapExtentBookmarkStore : IMapExtentBookmarkStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string? FilePath { get; set; }

    public JsonMapExtentBookmarkStore(string? filePath = null)
    {
        FilePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IRI.Maptor",
            "map-extent-bookmarks.json");
    }

    public IReadOnlyList<MapExtentBookmark> Load()
    {
        var path = FilePath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return Array.Empty<MapExtentBookmark>();

        try
        {
            var json = File.ReadAllText(path);
            var root = JsonSerializer.Deserialize<BookmarkFileDto>(json, JsonOptions);
            return root?.Bookmarks ?? Array.Empty<MapExtentBookmark>();
        }
        catch
        {
            return Array.Empty<MapExtentBookmark>();
        }
    }

    public void Save(IReadOnlyList<MapExtentBookmark> bookmarks)
    {
        var path = FilePath;
        if (string.IsNullOrWhiteSpace(path))
            return;

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var dto = new BookmarkFileDto { Bookmarks = bookmarks.ToArray() };
        var json = JsonSerializer.Serialize(dto, JsonOptions);
        File.WriteAllText(path, json);
    }

    private sealed class BookmarkFileDto
    {
        public MapExtentBookmark[] Bookmarks { get; set; } = Array.Empty<MapExtentBookmark>();
    }
}
