//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using IRI.Maptor.Sta.Common.Primitives;
//using IRI.Maptor.Sta.Spatial.Primitives;

//namespace IRI.Maptor.Jab.Common.Models.Map;

//public interface ISelectedLayer
//{
//    Guid Id { get; }

//    ILayer AssociatedLayer { get; set; }

//    string LayerName { get; }

//    List<Field>? Fields { get; set; }

//    ObservableCollection<Feature<Point>> HighlightedFeatures { get; set; }

//    void UpdateSelectedFeatures(IEnumerable<Feature<Point>> items);

//    void UpdateHighlightedFeatures(IEnumerable<Feature<Point>> items);

//    bool ShowSelectedOnMap { get; set; }

//    IEnumerable<Feature<Point>> GetSelectedFeatures();

//    int CountOfSelectedFeatures();

//    IEnumerable<Feature<Point>> GetHighlightedFeatures();

//    void UpdateSelectedFeaturesOnMap(IEnumerable<Feature<Point>> enumerable);

//    void UpdateHighlightedFeaturesOnMap(IEnumerable<Feature<Point>> enumerable);

//    Action<IEnumerable<Feature<Point>>> FeaturesChangedAction { get; set; }

//    Action<IEnumerable<Feature<Point>>> HighlightFeaturesChangedAction { get; set; }

//    Action<Feature<Point>> RequestFlashSinglePoint { get; set; }

//    Action<IEnumerable<Feature<Point>>, Action> RequestZoomTo { get; set; }

//    Action<Feature<Point>> RequestEdit { get; set; }

//    Action RequestRemove { get; set; }

//    void Update(Feature<Point> oldGeometry, Feature<Point> newGeometry);

//    void UpdateFeature(Feature<Point> item);
//}
