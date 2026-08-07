using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Ogc.SLD;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Sta.Persistence.Model;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Jab.Core.Layers;
using IRI.Maptor.Jab.Core.Models.Project;
using IRI.Maptor.Jab.Common.Layers;
using IRI.Maptor.Jab.Common.ViewModels;

namespace IRI.Maptor.Jab.Common.Services.Project;

/// <summary>
/// Builds a <see cref="MapProject"/> from the current map state (save) and applies a loaded
/// project back onto the map (open): replays file imports through the no-dialog Add* methods,
/// restores per-layer state/symbology, drawing items, base map and extent.
/// Dialogs and app hooks are the responsibility of <see cref="MapViewModelBase"/>.
/// </summary>
public class MapProjectService
{
    private readonly MapViewModelBase _map;

    public MapProjectService(MapViewModelBase map)
    {
        _map = map ?? throw new ArgumentNullException(nameof(map));
    }

    #region Save

    public MapProject BuildProject(string projectFilePath)
    {
        var projectDir = Path.GetDirectoryName(projectFilePath) ?? string.Empty;

        var project = new MapProject
        {
            Name = Path.GetFileNameWithoutExtension(projectFilePath),
            SavedOnUtc = DateTime.UtcNow,
            Extent = ToProjectExtent(_map.CurrentExtent),
            BaseMapFullName = _map.SelectedMapProvider?.FullName,
            BaseMapOpacity = _map.BaseMapSettings.BaseMapOpacity,
        };

        CollectImportedLayers(project, projectDir);

        CollectDrawingItems(project);

        return project;
    }

    private void CollectImportedLayers(MapProject project, string projectDir)
    {
        // one entry per imported file; GeoPackage produces two top-level groups
        // (vector + tiles) from one file, so entries are deduped by (kind, path)
        var fileEntries = new Dictionary<string, ImportedLayerEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var layer in _map.Layers.ToList())
        {
            if (layer is DrawingItemLayer || !layer.CanUserDelete)
                continue;

            var candidates = _map.GetAllLayers([layer]);

            var groups = FlattenIncludingGroups(layer).Where(l => l.IsGroupLayer).ToList();

            var dataSource = candidates.Select(l => l.DataSource).FirstOrDefault(ds => ds is not null && HasImportSource(ds));

            if (dataSource is null)
                continue;

            ImportedLayerEntry entry;

            if (dataSource.Location is FileLocation fileLocation)
            {
                var key = $"{dataSource.DataSourceKind}|{fileLocation.Path}";

                if (!fileEntries.TryGetValue(key, out entry!))
                {
                    entry = new ImportedLayerEntry
                    {
                        Kind = dataSource.DataSourceKind,
                        Location = fileLocation,
                        RelativePath = TryGetRelativePath(projectDir, fileLocation.Path),
                        Options = BuildOptions(dataSource),
                    };

                    fileEntries[key] = entry;

                    project.ImportedLayers.Add(entry);
                }
            }
            else
            {
                // pasted raw data (CSV/TSV/GeoJSON/TopoJSON): the content itself is embedded
                entry = new ImportedLayerEntry
                {
                    Kind = dataSource.DataSourceKind,
                    Options = BuildOptions(dataSource),
                };

                project.ImportedLayers.Add(entry);
            }

            foreach (var candidate in candidates)
            {
                entry.LayerStates.Add(new LayerStateEntry
                {
                    LayerName = candidate.LayerName,
                    TocOrder = candidate.TocOrder,
                    IsVisible = candidate.IsVisible,
                    Opacity = candidate.Opacity,
                    SldXml = GetSldXml(candidate),
                });
            }

            // group layers carry the order of the import as a whole (and, for a DXF/KML
            // parent, its own visibility); GetAllLayers above deliberately drops them
            foreach (var group in groups)
            {
                entry.GroupStates.Add(new LayerStateEntry
                {
                    LayerName = group.LayerName,
                    TocOrder = group.TocOrder,
                    IsVisible = group.IsVisible,
                    Opacity = group.Opacity,
                });
            }
        }
    }

    /// <summary>
    /// Flattens a layer subtree including the group layers themselves, unlike
    /// <see cref="MapViewModelBase.GetAllLayers"/> which returns leaves only.
    /// </summary>
    private static IEnumerable<ILayer> FlattenIncludingGroups(ILayer layer)
    {
        yield return layer;

        if (layer.SubLayers is null)
            yield break;

        foreach (var child in layer.SubLayers)
        {
            foreach (var descendant in FlattenIncludingGroups(child))
            {
                yield return descendant;
            }
        }
    }

    private static bool HasImportSource(IDataSource dataSource)
    {
        if (dataSource.Location is FileLocation)
            return true;

        return dataSource switch
        {
            TextDataSource text => !string.IsNullOrWhiteSpace(text.RawText),
            GeoJsonDataSource geoJson => !string.IsNullOrWhiteSpace(geoJson.RawJson),
            TopoJsonDataSource topoJson => !string.IsNullOrWhiteSpace(topoJson.RawJson),
            _ => false,
        };
    }

    private static ImportOptions? BuildOptions(IDataSource dataSource) => dataSource switch
    {
        TextDataSource text => new ImportOptions
        {
            SourceSrid = text.OriginalSrid,
            IsLongitudeFirst = text.IsLongitudeFirst,
            UseFirstLineAsHeader = text.UseFirstLineAsHeader,
            GeometryType = text.TargetGeometryType,
            RawText = text.RawText,
        },
        GeoJsonDataSource geoJson => new ImportOptions
        {
            SourceSrid = geoJson.OriginalSrid,
            IsLongitudeFirst = geoJson.IsLongitudeFirst,
            RawText = geoJson.RawJson,
        },
        TopoJsonDataSource topoJson => new ImportOptions
        {
            SourceSrid = topoJson.OriginalSrid,
            RawText = topoJson.RawJson,
        },
        _ when dataSource.DataSourceKind == DataSourceKind.Dxf => new ImportOptions { SourceSrid = dataSource.OriginalSrid },
        _ when dataSource.DataSourceKind is DataSourceKind.Worldfile or DataSourceKind.GeoTiff => new ImportOptions { SourceSrid = dataSource.Srid },
        _ => null,
    };

    private static string? GetSldXml(ILayer layer)
    {
        if (layer is not SymbolizableLayer symbolizable)
            return null;

        try
        {
            var sld = symbolizable.SourceSld ?? symbolizable.GetSld();

            return sld is null ? null : XmlHelper.Parse(sld);
        }
        catch
        {
            return null;
        }
    }

    private void CollectDrawingItems(MapProject project)
    {
        foreach (var item in _map.DrawingItems.ToList())
        {
            // todo: text items (SpecialPointLayer-based) are not persisted;
            // same limitation as SaveDrawingItemFile
            if (item.IsTextLayer)
                continue;

            try
            {
                project.DrawingItems.Add(new DrawingItemEntry
                {
                    Name = item.LayerName,
                    SerializedFeature = item.Serialize(),
                    IsVisible = item.IsVisible,
                    Opacity = item.Opacity,
                });
            }
            catch
            {
                // an item that cannot serialize should not fail the whole save
            }
        }
    }

    private static string? TryGetRelativePath(string projectDir, string path)
    {
        if (string.IsNullOrWhiteSpace(projectDir) || string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            var relative = Path.GetRelativePath(projectDir, path);

            // different root: GetRelativePath returns the absolute path unchanged
            return Path.IsPathRooted(relative) ? null : relative;
        }
        catch
        {
            return null;
        }
    }

    private static MapProjectExtent? ToProjectExtent(BoundingBox extent)
    {
        if (extent.IsNaN())
            return null;

        return new MapProjectExtent
        {
            XMin = extent.XMin,
            YMin = extent.YMin,
            XMax = extent.XMax,
            YMax = extent.YMax,
        };
    }

    #endregion

    #region Load

    public async Task<MapProjectLoadResult> ApplyAsync(MapProject project, string projectFilePath)
    {
        var result = new MapProjectLoadResult();

        var projectDir = Path.GetDirectoryName(projectFilePath) ?? string.Empty;

        ClearProjectContent();

        var replayed = new List<(ImportedLayerEntry Entry, List<ILayer> Created)>();

        foreach (var entry in project.ImportedLayers)
        {
            try
            {
                // group layers must be part of the diff: they carry the TOC order of the
                // import and are what a DXF/KML sub-layer hangs off
                var before = CurrentLayerTree().Select(l => l.LayerId).ToHashSet();

                if (!await ReplayImportAsync(entry, projectDir, result))
                    continue;

                var created = CurrentLayerTree().Where(l => !before.Contains(l.LayerId)).ToList();

                ApplyLayerStates(entry, created);

                replayed.Add((entry, created));
            }
            catch (Exception ex)
            {
                result.SkippedLayers.Add($"{Describe(entry)} ({ex.Message})");
            }
        }

        // ordering runs once all imports exist, so root layers of different imports
        // can be placed relative to each other
        await ApplyLayerOrders(replayed);

        RestoreDrawingItems(project, result);

        RestoreBaseMap(project);

        if (project.Extent is { } extent)
            _map.ZoomToExtent(new BoundingBox(extent.XMin, extent.YMin, extent.XMax, extent.YMax), isExactExtent: true, isNewExtent: true);
        else
            _map.Refresh(false);

        result.Succeeded = true;

        return result;
    }

    private void ClearProjectContent()
    {
        _map.RemoveAllDrawingItems();

        _map.Clear(l => l.CanUserDelete && l is not DrawingItemLayer && l.DataSource is not null && HasImportSource(l.DataSource),
                   remove: true,
                   forceRemove: true);
    }

    private async Task<bool> ReplayImportAsync(ImportedLayerEntry entry, string projectDir, MapProjectLoadResult result)
    {
        string? path = null;

        var hasRawText = !string.IsNullOrWhiteSpace(entry.Options?.RawText);

        if (entry.Location is FileLocation fileLocation)
        {
            path = ResolvePath(fileLocation.Path, entry.RelativePath, projectDir);

            if (path is null)
            {
                result.SkippedLayers.Add(Describe(entry));

                return false;
            }
        }
        else if (!hasRawText)
        {
            result.SkippedLayers.Add(Describe(entry));

            return false;
        }

        var options = entry.Options;

        // typed as object so the (string, object) overloads are unambiguous
        object owner = null!;

        switch (entry.Kind)
        {
            case DataSourceKind.Shapefile:
                await _map.AddShapefile(path!, owner);
                break;

            case DataSourceKind.Kml:
                await _map.AddKmlfile(path!, owner);
                break;

            case DataSourceKind.Kmz:
                await _map.AddKmzfile(path!, owner);
                break;

            case DataSourceKind.Gpx:
                await _map.AddGpxfile(path!, owner);
                break;

            case DataSourceKind.Dxf:
                await _map.AddDxffile(path!, owner, options?.SourceSrid ?? SridHelper.GeodeticWGS84);
                break;

            case DataSourceKind.EsriJson:
                await _map.AddEsriJson(path!, owner);
                break;

            case DataSourceKind.Worldfile:
            case DataSourceKind.GeoTiff:
                await _map.AddWorldfile(path!, options?.SourceSrid ?? SridHelper.WebMercator, owner);
                break;

            case DataSourceKind.ZippedImagePyramid:
                await _map.AddZippedImagePyramid(path!);
                break;

            case DataSourceKind.MBTiles:
                await _map.AddMBTiles(path!);
                break;

            case DataSourceKind.GeoPackage:
                await _map.AddGeoPackage(path!);
                break;

            case DataSourceKind.Csv:
            case DataSourceKind.Tsv:
                await _map.AddDelimitedTextFile(
                    path,
                    options?.RawText,
                    entry.Kind == DataSourceKind.Csv,
                    options?.SourceSrid ?? SridHelper.GeodeticWGS84,
                    options?.IsLongitudeFirst ?? true,
                    options?.UseFirstLineAsHeader ?? false,
                    options?.GeometryType ?? GeometryType.Point);
                break;

            case DataSourceKind.GeoJson:
                await _map.AddGeoJson(path, options?.RawText, options?.IsLongitudeFirst ?? true, options?.SourceSrid ?? SridHelper.GeodeticWGS84);
                break;

            case DataSourceKind.TopoJson:
                await _map.AddTopoJson(path, options?.RawText, options?.SourceSrid ?? SridHelper.GeodeticWGS84);
                break;

            default:
                result.SkippedLayers.Add(Describe(entry));

                return false;
        }

        return true;
    }

    private static string? ResolvePath(string savedPath, string? relativePath, string projectDir)
    {
        // relative to the project file first, so a project moved together
        // with its data folder keeps working
        if (!string.IsNullOrWhiteSpace(relativePath) && !string.IsNullOrWhiteSpace(projectDir))
        {
            var candidate = Path.GetFullPath(Path.Combine(projectDir, relativePath));

            if (File.Exists(candidate))
                return candidate;
        }

        if (!string.IsNullOrWhiteSpace(savedPath) && File.Exists(savedPath))
            return savedPath;

        return null;
    }

    private IEnumerable<ILayer> CurrentLayerTree() => _map.Layers.SelectMany(FlattenIncludingGroups);

    private static ILayer? MatchByName(List<ILayer> createdLayers, string layerName)
        => createdLayers.FirstOrDefault(l => string.Equals(l.LayerName, layerName, StringComparison.Ordinal));

    private void ApplyLayerStates(ImportedLayerEntry entry, List<ILayer> createdLayers)
    {
        // groups first: setting IsVisible on a group cascades to every child
        // (BaseLayer.SetIsVisible), so doing it afterwards would overwrite the
        // per-sub-layer visibility restored below
        foreach (var state in entry.GroupStates)
        {
            var group = MatchByName(createdLayers, state.LayerName);

            if (group is null)
                continue;

            group.IsVisible = state.IsVisible;
            group.Opacity = state.Opacity;
        }

        foreach (var state in entry.LayerStates)
        {
            var layer = MatchByName(createdLayers, state.LayerName);

            if (layer is null)
                continue;

            layer.IsVisible = state.IsVisible;
            layer.Opacity = state.Opacity;

            if (string.IsNullOrWhiteSpace(state.SldXml) || layer is not SymbolizableLayer symbolizable)
                continue;

            var sld = SldHelper.Parse(state.SldXml);

            if (sld is not null)
                symbolizable.ReplaceSymbolizers(sld.ParseToSymbolizers(), sld);
        }
    }

    /// <summary>
    /// Restores the saved <see cref="ILayer.TocOrder"/> of every replayed layer, then has the
    /// map re-derive ZIndex from it and refresh the legend. Without this each import simply
    /// lands on top (LayerManager.ArrangeTocOrder assigns max + 1), losing both the order of
    /// the imports and the order of sub-layers inside a group.
    /// </summary>
    private async Task ApplyLayerOrders(List<(ImportedLayerEntry Entry, List<ILayer> Created)> replayed)
    {
        // project files written before ordering was persisted have TocOrder 0 throughout;
        // applying that would collapse every layer onto the same position
        var hasOrder = replayed.Any(r => r.Entry.GroupStates.Any(s => s.TocOrder != 0) ||
                                         r.Entry.LayerStates.Any(s => s.TocOrder != 0));

        if (!hasOrder)
            return;

        foreach (var (entry, created) in replayed)
        {
            foreach (var state in entry.GroupStates.Concat(entry.LayerStates))
            {
                var layer = MatchByName(created, state.LayerName);

                if (layer is not null)
                    layer.TocOrder = state.TocOrder;
            }
        }

        // TocOrder is authoritative; ZIndex (render order) is derived from it
        _map.RequestRearrangeLayerOrders?.Invoke();

        if (_map.LegendViewModel.RequestRefreshView is not null)
            await _map.LegendViewModel.RequestRefreshView.Invoke();

        // keeps the chevron up/down buttons correctly enabled after the reshuffle
        _map.UpdateLayerCanMoveUpDown(_map.Layers, _map.LegendViewModel.TocGroup);
    }

    private void RestoreDrawingItems(MapProject project, MapProjectLoadResult result)
    {
        // AddDrawingItem inserts at index 0, so replay in reverse to keep the saved order
        for (int i = project.DrawingItems.Count - 1; i >= 0; i--)
        {
            var entry = project.DrawingItems[i];

            try
            {
                var layer = DrawingItemLayer.Deserialize(entry.Name, entry.SerializedFeature);

                if (layer is null)
                    continue;

                layer.IsVisible = entry.IsVisible;
                layer.Opacity = entry.Opacity;

                _map.AddDrawingItem(layer);
            }
            catch (Exception)
            {
                result.SkippedLayers.Add(entry.Name);
            }
        }
    }

    private void RestoreBaseMap(MapProject project)
    {
        if (!string.IsNullOrWhiteSpace(project.BaseMapFullName))
        {
            var provider = _map.MapProviders?.FirstOrDefault(p => p.Is(project.BaseMapFullName));

            if (provider is not null)
                _map.SetTileBaseMap(provider);
        }

        _map.BaseMapSettings.BaseMapOpacity = project.BaseMapOpacity;
    }

    private static string Describe(ImportedLayerEntry entry)
    {
        var name = entry.Location is FileLocation fileLocation
            ? Path.GetFileName(fileLocation.Path)
            : entry.LayerStates.FirstOrDefault()?.LayerName;

        return string.IsNullOrWhiteSpace(name) ? entry.Kind.ToString() : $"{entry.Kind}: {name}";
    }

    #endregion
}
