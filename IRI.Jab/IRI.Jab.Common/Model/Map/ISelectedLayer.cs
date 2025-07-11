﻿using System;
using System.Collections.Generic;

using IRI.Sta.Common.Primitives;
using IRI.Sta.Spatial.Primitives;

namespace IRI.Jab.Common.Model.Map;

public interface ISelectedLayer
{
    Guid Id { get; }

    ILayer AssociatedLayer { get; set; }

    string LayerName { get; }

    List<Field>? Fields { get; set; }

    void UpdateSelectedFeatures(IEnumerable<IGeometryAware<Point>> items);

    void UpdateHighlightedFeatures(IEnumerable<IGeometryAware<Point>> items);

    bool ShowSelectedOnMap { get; set; }

    IEnumerable<IGeometryAware<Point>> GetSelectedFeatures();

    int CountOfSelectedFeatures();

    IEnumerable<IGeometryAware<Point>> GetHighlightedFeatures();

    void UpdateSelectedFeaturesOnMap(IEnumerable<IGeometryAware<Point>> enumerable);

    void UpdateHighlightedFeaturesOnMap(IEnumerable<IGeometryAware<Point>> enumerable);

    Action<IEnumerable<IGeometryAware<Point>>> FeaturesChangedAction { get; set; }

    Action<IEnumerable<IGeometryAware<Point>>> HighlightFeaturesChangedAction { get; set; }

    Action<IGeometryAware<Point>> RequestFlashSinglePoint { get; set; }

    Action<IEnumerable<IGeometryAware<Point>>, Action> RequestZoomTo { get; set; }

    Action<IGeometryAware<Point>> RequestEdit { get; set; }

    Action RequestRemove { get; set; }

    void Update(IGeometryAware<Point> oldGeometry, Feature<Point> newGeometry);

    void UpdateFeature(object item);
}
