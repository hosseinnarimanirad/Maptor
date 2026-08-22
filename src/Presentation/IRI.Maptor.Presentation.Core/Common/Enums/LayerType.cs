namespace IRI.Maptor.Presentation.Core;

public enum LayerType
{
    None = 1,
    GroupLayer = 2,

    VectorLayer = 100,

    Drawing = 200,
    Selection = 201,
    Highlight = 202,

    Raster = 300,
    BaseMap = 301,
    ImagePyramid = 302,

    Label = 400,

    Complex = 500,

    RightClickOption = 510,

    EditableItem = 530,

    AnimatingItem = 540,

    GridAndGraticule = 550,

    ActiveExtent = 560,
}
