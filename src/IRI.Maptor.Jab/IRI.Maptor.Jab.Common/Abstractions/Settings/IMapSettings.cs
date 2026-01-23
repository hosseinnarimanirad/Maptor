using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Jab.Common.Abstractions;

public interface IMapSettings
{
    BoundingBox? InitialExtent { get; set; }
    bool IsDoubleClickZoomEnabled { get; set; }
    bool IsGoogleZoomLevelsEnabled { get; set; }
    bool IsMouseWheelZoomEnabled { get; set; }
    int MaxGoogleZoomLevel { get; set; }
    int MinGoogleZoomLevel { get; set; }

    bool CheckIsInScaleRange { get; set; }
    bool CheckIsVisible { get; set; }
}