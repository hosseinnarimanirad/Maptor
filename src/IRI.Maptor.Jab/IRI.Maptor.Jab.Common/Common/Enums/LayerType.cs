using System;

namespace IRI.Maptor.Jab.Common;

[Flags]
public enum LayerType
{
    Point = 1,
    Polyline = 2,
    Polygon = 4,
    Raster = 8,
    BaseMap = 16,
    Drawing = 32,
    Label = 64,
    Selection = 128,
    VectorLayer = 256,
    Feature = Point | Polygon | Polyline,
    Complex = 512,
    RightClickOption = 1024,
    GridAndGraticule = 2048,
    AnimatingItem = 4096,
    ImagePyramid = 8192,
    MoveableItem = 16_384,
    EditableItem = 32_768,
    GroupLayer = 65_536,
    None = 131_072,
    FeatureLayer = 262_144,
    ActiveExtent = 524_288
}
