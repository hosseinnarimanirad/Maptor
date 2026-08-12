using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Jab.Core;
using IRI.Maptor.Jab.Core.Models;

namespace IRI.Maptor.Jab.Wpf.ViewModels.Map;

public class FeatureChangesViewModel : Notifier
{
    private readonly Feature<Point> _feature;

    private static readonly Func<Point, Point> ToWgs84 = MapProjects.WebMercatorToGeodeticWgs84;

    public FeatureChangesViewModel(Feature<Point> feature, IReadOnlyList<Field>? fields = null)
    {
        _feature = feature ?? throw new ArgumentNullException(nameof(feature));

        string GetDisplayName(string name)
        {
            var field = fields?.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
            return !string.IsNullOrEmpty(field?.Alias) ? field.Alias! : name;
        }

        ChangedAttributes = new ObservableCollection<AttributeChange>(
            DictionaryHelper.GetChangedAttributes(_feature.OldVersion?.Attributes, _feature.Attributes)
                .Select(t => new AttributeChange(t.Name, GetDisplayName(t.Name), t.OldValue, t.NewValue)));
         

        CompareGeometriesCommand = new RelayCommand(_ =>
            RequestShowGeometryComparison?.Invoke(_feature.OldVersion?.TheGeometry, _feature.TheGeometry),
            _ => HasGeometryChanges);
    }

    public Feature<Point> Feature => _feature;

    public int Id => _feature.Id;

    public FeatureStatus Status => _feature.Status;

    public DateTime? LastAddOrUpdate => _feature.LastAddOrUpdate;

    public string LastAddOrUpdateFormatted =>
        _feature.LastAddOrUpdate.HasValue
            ? _feature.LastAddOrUpdate.Value.ToLocalTime().ToString("g")
            : string.Empty;

    public ObservableCollection<AttributeChange> ChangedAttributes { get; }

    public bool HasGeometryChanges =>
        _feature.OldVersion?.TheGeometry != null &&
        _feature.TheGeometry != null &&
        _feature.TheGeometry.AsWkt() != _feature.OldVersion.TheGeometry.AsWkt();

    public string? GeometryOldWkt => GetWgs84Wkt(_feature.OldVersion?.TheGeometry);

    public string? GeometryNewWkt => GetWgs84Wkt(_feature.TheGeometry);

    public string? GeometryOldAreaLabel => GetAreaLabel(_feature.OldVersion?.TheGeometry);

    public string? GeometryNewAreaLabel => GetAreaLabel(_feature.TheGeometry);

    public string? GeometryOldLengthLabel => GetLengthLabel(_feature.OldVersion?.TheGeometry);

    public string? GeometryNewLengthLabel => GetLengthLabel(_feature.TheGeometry);

    public string? GeometryAreaChangeLabel => GetAreaChangeLabel();

    public string? GeometryLengthChangeLabel => GetLengthChangeLabel();

    private static string? GetWgs84Wkt(Geometry<Point>? geometry)
    {
        if (geometry.IsNullOrEmpty())
            return null;

        var wgs84 = geometry.Srid == SridHelper.GeodeticWGS84
            ? geometry
            : geometry.Transform(ToWgs84, SridHelper.GeodeticWGS84);

        return wgs84.AsWkt(6);
    }

    private static string? GetAreaLabel(Geometry<Point>? geometry)
    {
        if (geometry.IsNullOrEmpty())
            return null;

        if (geometry.Type != GeometryType.Polygon && geometry.Type != GeometryType.MultiPolygon)
            return null;

        try
        {
            var area = SpatialUtility.GetEllipsoidalArea(geometry, ToWgs84);
            return UnitHelper.GetAreaLabel(area);
        }
        catch { return null; }
    }

    private static string? GetLengthLabel(Geometry<Point>? geometry)
    {
        if (geometry.IsNullOrEmpty())
            return null;

        if (geometry.Type != GeometryType.LineString && geometry.Type != GeometryType.MultiLineString)
            return null;

        try
        {
            var wgs84 = geometry.Srid == SridHelper.GeodeticWGS84
                ? geometry
                : geometry.Transform(ToWgs84, SridHelper.GeodeticWGS84);

            var length = wgs84.GetEllipsoidalLength();

            return UnitHelper.GetLengthLabel(length);
        }
        catch { return null; }
    }

    private string? GetAreaChangeLabel()
    {
        var oldArea = GetAreaValue(_feature.OldVersion?.TheGeometry);

        var newArea = GetAreaValue(_feature.TheGeometry);

        if (oldArea == null && newArea == null) return null;

        var oldVal = oldArea ?? 0;

        var newVal = newArea ?? 0;

        var diff = newVal - oldVal;

        if (Math.Abs(diff) < 1e-9) return "0";

        var sign = diff > 0 ? "+" : "";

        return $"{sign}{UnitHelper.GetAreaLabel(Math.Abs(diff))}";
    }

    private string? GetLengthChangeLabel()
    {
        var oldLen = GetLengthValue(_feature.OldVersion?.TheGeometry);

        var newLen = GetLengthValue(_feature.TheGeometry);

        if (oldLen == null && newLen == null) return null;

        var oldVal = oldLen ?? 0;

        var newVal = newLen ?? 0;

        var diff = newVal - oldVal;

        if (Math.Abs(diff) < 1e-9) return "0";

        var sign = diff > 0 ? "+" : "";

        return $"{sign}{UnitHelper.GetLengthLabel(Math.Abs(diff))}";
    }

    private static double? GetAreaValue(Geometry<Point>? geometry)
    {
        if (geometry.IsNullOrEmpty() ||
            (geometry.Type != GeometryType.Polygon && geometry.Type != GeometryType.MultiPolygon))
            return null;

        try { return SpatialUtility.GetEllipsoidalArea(geometry, ToWgs84); }
        catch { return null; }
    }

    private static double? GetLengthValue(Geometry<Point>? geometry)
    {
        if (geometry.IsNullOrEmpty() ||
            (geometry.Type != GeometryType.LineString && geometry.Type != GeometryType.MultiLineString))
            return null;

        try
        {
            var wgs84 = geometry!.Srid == SridHelper.GeodeticWGS84
                ? geometry
                : geometry.Transform(ToWgs84, SridHelper.GeodeticWGS84);
            return wgs84.GetEllipsoidalLength();
        }
        catch { return null; }
    }

    private RelayCommand _zoomToCurrentGeometryCommand;
    public RelayCommand ZoomToCurrentGeometryCommand
    {
        get
        {
            if (_zoomToCurrentGeometryCommand == null)
                _zoomToCurrentGeometryCommand = new RelayCommand(_ => RequestZoomToFeature?.Invoke(_feature.TheGeometry));

            return _zoomToCurrentGeometryCommand;
        }
    }


    private RelayCommand _zoomToOldGeometryCommand;
    public RelayCommand ZoomToOldGeometryCommand
    {
        get
        {
            if (_zoomToOldGeometryCommand == null)
            {
                _zoomToOldGeometryCommand = new RelayCommand(_ =>
                {
                    if (_feature.OldVersion?.TheGeometry != null)
                        RequestZoomToFeature?.Invoke(_feature.OldVersion.TheGeometry);

                }, _ => _feature.OldVersion?.TheGeometry != null);
            }

            return _zoomToOldGeometryCommand;
        }
    }


    private RelayCommand _zoomToBothGeometriesCommand;
    public RelayCommand ZoomToBothGeometriesCommand
    {
        get
        {
            if (_zoomToBothGeometriesCommand == null)
            {
                _zoomToBothGeometriesCommand = new RelayCommand(_ =>
                {
                    var boxes = new List<BoundingBox>();

                    if (_feature.TheGeometry != null)
                        boxes.Add(_feature.TheGeometry.GetBoundingBox());

                    if (_feature.OldVersion?.TheGeometry != null)
                        boxes.Add(_feature.OldVersion.TheGeometry.GetBoundingBox());

                    if (boxes.Count > 0)
                    {
                        var merged = BoundingBox.GetMergedBoundingBox(boxes);
                        RequestZoomToExtent?.Invoke(merged, false, true, null);
                    }
                }, _ => HasGeometryChanges);
            }

            return _zoomToBothGeometriesCommand;
        }
    }

     
    public RelayCommand CompareGeometriesCommand { get; }

    public Action<Geometry<Point>>? RequestZoomToFeature { get; set; }
    public Action<Geometry<Point>?, Geometry<Point>?>? RequestShowGeometryComparison { get; set; }
    public Action<BoundingBox, bool, bool, Action?>? RequestZoomToExtent { get; set; }
}
