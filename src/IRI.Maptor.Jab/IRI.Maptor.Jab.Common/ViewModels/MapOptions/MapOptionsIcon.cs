using MahApps.Metro.IconPacks;

namespace IRI.Maptor.Jab.Common.ViewModels;

/// <summary>
/// Helper for creating path markup strings for MapOptions buttons from various icon sources.
/// </summary>
public static class MapOptionsIcon
{
    public static string? FromModern(PackIconModernKind kind) =>
        new PackIconModern() { Kind = kind }.Data;

    public static string? FromMaterial(PackIconMaterialKind kind) =>
        new PackIconMaterial() { Kind = kind }.Data;

    public static string? FromPhosphorIcons(PackIconPhosphorIconsKind kind) =>
        new PackIconPhosphorIcons() { Kind = kind }.Data;

    public static string? FromPath(string pathMarkup) => pathMarkup;
}
